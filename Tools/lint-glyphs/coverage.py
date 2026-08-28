"""Derive TextMeshPro glyph coverage from the project's own assets.

Nothing here hardcodes a codepoint range. Coverage comes from:
  * ProjectSettings/EditorBuildSettings.asset  - which scenes ship
  * the shipping scenes/prefabs                - which font assets those scenes bind
  * TMP Settings.asset                         - the default font asset a TMP_Text with
                                                 no explicit binding falls back to
  * each font asset's m_CharacterTable         - what is actually baked into the atlas
  * m_AtlasPopulationMode + m_SourceFontFile   - whether missing glyphs can be added at
                                                 runtime, and from which .ttf cmap
  * m_FallbackFontAssetTable                   - recursively, the same for fallbacks

Change a font, regenerate an atlas, or add a fallback and the numbers move with no edit
to this file.
"""

import os
import re
import struct
import sys


class CoverageError(Exception):
    """A rule source is missing or unreadable. Callers must exit non-zero."""


# ---------------------------------------------------------------- guid resolution

def build_guid_index(assets_dir):
    """Map every Unity asset guid -> file path by reading .meta files."""
    index = {}
    for root, dirs, files in os.walk(assets_dir):
        dirs[:] = [d for d in dirs if d not in ("Library", "Temp", "obj", "__pycache__")]
        for name in files:
            if not name.endswith(".meta"):
                continue
            path = os.path.join(root, name)
            try:
                with open(path, "r", encoding="utf-8", errors="replace") as fh:
                    for _ in range(6):
                        line = fh.readline()
                        if not line:
                            break
                        if line.startswith("guid:"):
                            index[line.split(":", 1)[1].strip()] = path[: -len(".meta")]
                            break
            except OSError:
                continue
    return index


# ---------------------------------------------------------------- ttf cmap

def ttf_codepoints(path):
    """Every codepoint the font file itself can map. Used for DYNAMIC font assets, where
    TMP renders glyphs into the atlas at runtime straight from this cmap."""
    with open(path, "rb") as fh:
        data = fh.read()
    if len(data) < 12:
        raise CoverageError("not a font file: %s" % path)
    num_tables = struct.unpack(">H", data[4:6])[0]
    tables = {}
    for i in range(num_tables):
        off = 12 + 16 * i
        if off + 16 > len(data):
            break
        tag = data[off:off + 4].decode("latin-1")
        toff, tlen = struct.unpack(">II", data[off + 8:off + 16])
        tables[tag] = (toff, tlen)
    if "cmap" not in tables:
        raise CoverageError("font has no cmap table: %s" % path)
    coff = tables["cmap"][0]
    n = struct.unpack(">H", data[coff + 2:coff + 4])[0]
    best = None
    prefer = {(3, 10): 5, (0, 4): 5, (0, 6): 5, (3, 1): 4, (0, 3): 4, (0, 1): 3,
              (0, 2): 3, (0, 0): 3, (3, 0): 1, (1, 0): 0}
    for i in range(n):
        p = coff + 4 + 8 * i
        pid, eid, off = struct.unpack(">HHI", data[p:p + 8])
        score = prefer.get((pid, eid), -1)
        if score >= 0 and (best is None or score > best[0]):
            best = (score, coff + off)
    if best is None:
        raise CoverageError("font has no unicode cmap subtable: %s" % path)
    so = best[1]
    fmt = struct.unpack(">H", data[so:so + 2])[0]
    cps = set()
    if fmt == 4:
        seg_x2 = struct.unpack(">H", data[so + 6:so + 8])[0]
        seg = seg_x2 // 2
        end_o = so + 14
        start_o = end_o + seg_x2 + 2
        delta_o = start_o + seg_x2
        range_o = delta_o + seg_x2
        for i in range(seg):
            end = struct.unpack(">H", data[end_o + 2 * i:end_o + 2 * i + 2])[0]
            start = struct.unpack(">H", data[start_o + 2 * i:start_o + 2 * i + 2])[0]
            delta = struct.unpack(">h", data[delta_o + 2 * i:delta_o + 2 * i + 2])[0]
            ro = struct.unpack(">H", data[range_o + 2 * i:range_o + 2 * i + 2])[0]
            if start == 0xFFFF:
                continue
            for c in range(start, end + 1):
                if ro == 0:
                    g = (c + delta) & 0xFFFF
                else:
                    gi = range_o + 2 * i + ro + 2 * (c - start)
                    if gi + 2 > len(data):
                        continue
                    g = struct.unpack(">H", data[gi:gi + 2])[0]
                    if g:
                        g = (g + delta) & 0xFFFF
                if g:
                    cps.add(c)
    elif fmt == 12:
        ngroups = struct.unpack(">I", data[so + 12:so + 16])[0]
        for i in range(ngroups):
            p = so + 16 + 12 * i
            s, e, _g = struct.unpack(">III", data[p:p + 12])
            if e - s > 0x110000:
                continue
            cps.update(range(s, e + 1))
    elif fmt == 6:
        first, count = struct.unpack(">HH", data[so + 6:so + 10])
        for i in range(count):
            g = struct.unpack(">H", data[so + 10 + 2 * i:so + 12 + 2 * i])[0]
            if g:
                cps.add(first + i)
    else:
        raise CoverageError("unsupported cmap format %d in %s" % (fmt, path))
    return cps


# ---------------------------------------------------------------- font asset

GUID_RE = re.compile(r"guid:\s*([0-9a-f]{32})")


class FontAsset(object):
    def __init__(self, path, name, dynamic, source_guid, unicodes, fallback_guids):
        self.path = path
        self.name = name
        self.dynamic = dynamic
        self.source_guid = source_guid
        self.unicodes = unicodes
        self.fallback_guids = fallback_guids


def parse_font_asset(path):
    """Read the baked character table, the population mode, the source ttf and the
    fallback chain out of a TMP_FontAsset .asset (Unity YAML)."""
    with open(path, "r", encoding="utf-8", errors="replace") as fh:
        lines = fh.readlines()

    name = os.path.splitext(os.path.basename(path))[0]
    dynamic = None
    source_guid = None
    unicodes = set()
    fallbacks = []

    in_char_table = False
    in_fallback_table = False
    saw_char_table = False

    for line in lines:
        stripped = line.strip()
        indent = len(line) - len(line.lstrip(" "))

        if stripped.startswith("m_CharacterTable:"):
            in_char_table, in_fallback_table = True, False
            saw_char_table = True
            continue
        if stripped.startswith("m_FallbackFontAssetTable:"):
            in_char_table = False
            in_fallback_table = not stripped.endswith("[]")
            continue
        # Any other key at the font asset's own indent level ends a list.
        if indent <= 2 and stripped and not stripped.startswith("-") and ":" in stripped:
            in_char_table = False
            in_fallback_table = False

        if in_char_table and stripped.startswith("m_Unicode:"):
            try:
                unicodes.add(int(stripped.split(":", 1)[1].strip()))
            except ValueError:
                pass
            continue
        if in_fallback_table and stripped.startswith("- {"):
            m = GUID_RE.search(stripped)
            if m:
                fallbacks.append(m.group(1))
            continue

        if stripped.startswith("m_AtlasPopulationMode:"):
            dynamic = stripped.split(":", 1)[1].strip() != "0"
            continue
        if stripped.startswith("m_SourceFontFile:"):
            m = GUID_RE.search(stripped)
            if m:
                source_guid = m.group(1)
            continue

    if dynamic is None or not saw_char_table:
        raise CoverageError(
            "%s does not look like a TMP font asset (no m_AtlasPopulationMode / "
            "m_CharacterTable)" % path)
    return FontAsset(path, name, dynamic, source_guid, unicodes, fallbacks)


def font_coverage(guid, guid_index, cache=None, seen=None):
    """Codepoints this font asset can draw, following its fallback chain.

    A STATIC asset draws exactly what is baked into its atlas. A DYNAMIC asset can also
    draw anything its source .ttf maps, because TMP renders it on demand at runtime."""
    cache = {} if cache is None else cache
    seen = set() if seen is None else seen
    if guid in cache:
        return cache[guid]
    if guid in seen:
        return set()
    seen.add(guid)

    path = guid_index.get(guid)
    if path is None or not os.path.isfile(path):
        raise CoverageError("font asset guid %s is referenced but not on disk" % guid)
    asset = parse_font_asset(path)

    cps = set(asset.unicodes)
    if asset.dynamic:
        if not asset.source_guid:
            raise CoverageError(
                "%s is DYNAMIC but has no m_SourceFontFile - runtime coverage is "
                "undecidable" % path)
        ttf = guid_index.get(asset.source_guid)
        if ttf is None or not os.path.isfile(ttf):
            raise CoverageError(
                "%s is DYNAMIC and its source font (guid %s) is not on disk"
                % (path, asset.source_guid))
        cps |= ttf_codepoints(ttf)
    for fb in asset.fallback_guids:
        cps |= font_coverage(fb, guid_index, cache, seen)
    cache[guid] = cps
    return cps


# ---------------------------------------------------------------- project wiring

FONT_FIELD_RE = re.compile(r"m_fontAsset:\s*\{fileID:\s*\d+,\s*guid:\s*([0-9a-f]{32})")


def enabled_scenes(root):
    """The scenes the build ships, from EditorBuildSettings - not from a list here."""
    path = os.path.join(root, "ProjectSettings", "EditorBuildSettings.asset")
    if not os.path.isfile(path):
        raise CoverageError("missing rule source: ProjectSettings/EditorBuildSettings.asset")
    scenes = []
    enabled = False
    with open(path, "r", encoding="utf-8", errors="replace") as fh:
        for line in fh:
            s = line.strip()
            if s.startswith("- enabled:"):
                enabled = s.split(":", 1)[1].strip() == "1"
            elif s.startswith("path:") and enabled:
                scenes.append(s.split(":", 1)[1].strip())
    if not scenes:
        raise CoverageError(
            "EditorBuildSettings.asset lists no enabled scenes - cannot decide which "
            "fonts ship")
    return scenes


def tmp_default_font_guids(root):
    path = os.path.join(root, "Assets", "TextMesh Pro", "Resources", "TMP Settings.asset")
    if not os.path.isfile(path):
        raise CoverageError(
            "missing rule source: Assets/TextMesh Pro/Resources/TMP Settings.asset")
    with open(path, "r", encoding="utf-8", errors="replace") as fh:
        text = fh.read()
    m = re.search(r"m_defaultFontAsset:\s*\{fileID:\s*\d+,\s*guid:\s*([0-9a-f]{32})", text)
    if not m:
        raise CoverageError("TMP Settings.asset has no m_defaultFontAsset")
    guids = [m.group(1)]
    fb = re.search(r"m_fallbackFontAssets:\s*(\[\]|(?:\n\s+- \{[^}]*\})+)", text)
    if fb and fb.group(1).strip() != "[]":
        guids.extend(GUID_RE.findall(fb.group(1)))
    return guids


def shipping_font_guids(root, prefab_dirs=()):
    """Every font asset a shipping scene or prefab binds, plus the TMP default."""
    guids = set(tmp_default_font_guids(root))
    scanned = []
    for rel in enabled_scenes(root):
        p = os.path.join(root, rel.replace("/", os.sep))
        if not os.path.isfile(p):
            raise CoverageError(
                "EditorBuildSettings names a scene that is not on disk: %s" % rel)
        scanned.append(rel)
        with open(p, "r", encoding="utf-8", errors="replace") as fh:
            for line in fh:
                m = FONT_FIELD_RE.search(line)
                if m:
                    guids.add(m.group(1))
    for d in prefab_dirs:
        base = os.path.join(root, d.replace("/", os.sep))
        if not os.path.isdir(base):
            raise CoverageError("prefab scan root does not exist: %s" % d)
        for cur, dirs, files in os.walk(base):
            dirs[:] = [x for x in dirs if x not in ("__pycache__",)]
            for name in files:
                if not name.endswith(".prefab"):
                    continue
                p = os.path.join(cur, name)
                with open(p, "r", encoding="utf-8", errors="replace") as fh:
                    for line in fh:
                        m = FONT_FIELD_RE.search(line)
                        if m:
                            guids.add(m.group(1))
                scanned.append(os.path.relpath(p, root).replace(os.sep, "/"))
    return guids, scanned


def drawable_set(root, prefab_dirs=()):
    """The codepoints EVERY shipping font can draw.

    Intersection, not union: a C# literal is assigned to a TMP_Text whose font this check
    cannot know statically, so a character is safe only if all of them have it."""
    guid_index = build_guid_index(os.path.join(root, "Assets"))
    guids, scanned = shipping_font_guids(root, prefab_dirs)
    if not guids:
        raise CoverageError("no font assets found in the shipping scenes")
    cache = {}
    per_font = {}
    for g in sorted(guids):
        cps = font_coverage(g, guid_index, cache, set())
        path = guid_index.get(g, g)
        per_font[os.path.relpath(path, root).replace(os.sep, "/")] = cps
    combined = None
    for cps in per_font.values():
        combined = set(cps) if combined is None else (combined & cps)
    return combined, per_font, scanned


def as_ranges(cps):
    ranges = []
    for c in sorted(cps):
        if ranges and c == ranges[-1][1] + 1:
            ranges[-1][1] = c
        else:
            ranges.append([c, c])
    return ranges


def format_ranges(cps):
    return ", ".join(
        ("U+%04X" % a) if a == b else ("U+%04X-U+%04X" % (a, b))
        for a, b in as_ranges(cps))


if __name__ == "__main__":
    _root = sys.argv[1] if len(sys.argv) > 1 else "."
    _extra = sys.argv[2:]
    _cov, _per, _scanned = drawable_set(_root, _extra)
    for _name in sorted(_per):
        print("%-72s %6d codepoints" % (_name, len(_per[_name])))
    print("scanned: %d scene/prefab file(s)" % len(_scanned))
    print("INTERSECTION: %d codepoints" % len(_cov))
    print("ranges: " + format_ranges(_cov))
