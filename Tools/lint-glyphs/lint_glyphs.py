"""Gatekeeper's Protocol - TextMeshPro glyph coverage check.

Finds user-facing text that asks a shipped font atlas to draw a character it does not
have. Such a character renders as a blank or a box, and nothing else in the tree notices:
it compiles, it tests green, and it is visible only on sight. That has shipped four times.

The rule is not written here. Coverage is derived from the font assets themselves
(coverage.py); scan scope and the ASCII substitution table are read from
Docs/_meta/_definitions/glyphs.md. Delete either and this exits 2.

Precision differs by source, and the check says so rather than pretending otherwise:

  scene m_text   - the exact TMP component, so the exact font. No guessing.
  Compendium .md - the fonts the CompendiumView actually binds in a shipping scene.
  C# literals    - the target label is not statically knowable, so only the character
                   itself can be judged.

Two severities, and only one of them is a gate:

  box   proven. Either the exact bound font lacks the glyph (scene, Compendium), or no
        shipping font has it at all (C#). It draws as a blank or a box. Fatal.
  risk  a C# character some shipping font has and some do not. Whether it boxes depends
        on the label the string reaches. Reported, not fatal, with the sites that sit on
        the narrow surface listed first - those are the ones worth opening. Promote it
        with risk_fails= in the fence when the atlases stop disagreeing.

Exit codes: 0 clean, 1 violations, 2 could not run.
"""

import argparse
import os
import re
import sys
import unicodedata

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import coverage as cov  # noqa: E402


FENCE_REL = "Docs/_meta/_definitions/glyphs.md"
FENCE_TAG = "glyph-lint"
REQUIRED_KEYS = ("cs_roots", "md_roots", "md_owner", "not_tmp_calls", "exempt_paths",
                 "prefab_roots", "risk_fails", "substitutes")


def relpath(root, path):
    """Repo-relative, forward slashes. Every path this check reports or compares is in
    this form, so a Windows walk and a fence entry can be matched."""
    return os.path.relpath(path, root).replace(os.sep, "/")


class RuleError(Exception):
    """A rule source is missing. Never fall back to a default - a default turns a
    deleted rule into a passing run."""


# ---------------------------------------------------------------- the rule document

def read_fence(root):
    path = os.path.join(root, FENCE_REL.replace("/", os.sep))
    if not os.path.isfile(path):
        raise RuleError("expected %s" % FENCE_REL)
    body = []
    grab = False
    with open(path, "r", encoding="utf-8", errors="replace") as fh:
        for line in fh:
            s = line.rstrip("\n")
            if not grab:
                if s.strip() == "```" + FENCE_TAG:
                    grab = True
                continue
            if s.strip() == "```":
                break
            body.append(s)
    if not grab:
        raise RuleError("no ```%s fence in %s" % (FENCE_TAG, FENCE_REL))
    rules = {}
    for s in body:
        s = s.strip()
        if not s or s.startswith("#"):
            continue
        if "=" not in s:
            raise RuleError("unparsable line in the %s fence: %r" % (FENCE_TAG, s))
        k, v = s.split("=", 1)
        rules[k.strip()] = v.strip()
    for key in REQUIRED_KEYS:
        if key not in rules:
            raise RuleError("missing key '%s=' in the %s fence" % (key, FENCE_TAG))
    return rules


def csv(value):
    return [x.strip() for x in value.split(",") if x.strip()]


def parse_substitutes(value):
    """'2192>->' pairs: codepoint (hex) to the ASCII the project writes instead."""
    table = {}
    for item in csv(value):
        if ">" not in item:
            raise RuleError("substitutes entry is not 'HEX>ascii': %r" % item)
        hexcp, ascii_form = item.split(">", 1)
        try:
            table[int(hexcp, 16)] = ascii_form
        except ValueError:
            raise RuleError("substitutes entry has a bad codepoint: %r" % item)
    return table


# ---------------------------------------------------------------- escapes

CS_ESCAPE = re.compile(r"\\u([0-9a-fA-F]{4})|\\U([0-9a-fA-F]{8})|\\x([0-9a-fA-F]{1,4})")
YAML_ESCAPE = re.compile(r"\\u([0-9a-fA-F]{4})|\\U([0-9a-fA-F]{8})|\\x([0-9a-fA-F]{2})")
ESCAPES = {"csharp": CS_ESCAPE, "yaml": YAML_ESCAPE, "none": None}


def codepoints_of(value, dialect):
    """(codepoint, offset) for every character, decoding the escapes of that dialect.

    An escape counts: it is the same glyph request as the raw character, and
    NavMapPanelView.cs shipped its box written as \\u2713. C# allows \\xN with one to
    four digits; Unity YAML's \\xNN is always two, and reading it greedily turns
    "9\\xd76" into U+0D76 - a false positive this check had before it split them."""
    pattern = ESCAPES[dialect]
    out = []
    i = 0
    while i < len(value):
        if pattern is not None and value[i] == "\\":
            m = pattern.match(value, i)
            if m:
                grp = m.group(1) or m.group(2) or m.group(3)
                out.append((int(grp, 16), i))
                i = m.end()
                continue
            if i + 1 < len(value):
                i += 2
                continue
        out.append((ord(value[i]), i))
        i += 1
    return out


# ---------------------------------------------------------------- C# lexing

def lex_csharp(text):
    """Split C# into string/char literals and everything else.

    Returns (literals, skeleton). A literal is (start, end, line, kind, value); the span
    covers the quotes. The skeleton is the same length as the text with comment and
    literal *content* blanked to spaces, so bracket matching over it is safe.

    This is a lexer, not a parser: it knows quoting, not syntax. What it buys over a
    regex is that it never mistakes a quote inside a comment - or a // inside a string -
    for the real thing, which is the whole failure mode of regex scanning here. Comments
    and XML doc become invisible, which is what keeps 2048 legitimate em-dashes out of
    the report.

    Known limits, stated rather than hidden:
      * an interpolation hole ($"{expr}") is treated as part of the literal. A non-ASCII
        character there would have to be inside an identifier - legal C#, absent here.
      * C# 11 raw string literals are lexed as adjacent empty strings, so their content
        is scanned as code. None exist in this tree, and the error is toward reporting.
    """
    n = len(text)
    skel = list(text)
    literals = []
    i = 0
    line = 1

    def blank(a, b):
        for k in range(a, b):
            if skel[k] not in "\r\n":
                skel[k] = " "

    while i < n:
        c = text[i]
        if c == "\n":
            line += 1
            i += 1
            continue

        if c == "/" and i + 1 < n and text[i + 1] == "/":
            j = text.find("\n", i)
            j = n if j < 0 else j
            blank(i, j)
            i = j
            continue
        if c == "/" and i + 1 < n and text[i + 1] == "*":
            j = text.find("*/", i + 2)
            j = n if j < 0 else j + 2
            line += text.count("\n", i, j)
            blank(i, j)
            i = j
            continue

        if c == "'":
            start, start_line = i, line
            i += 1
            while i < n:
                if text[i] == "\\":
                    i += 2
                    continue
                if text[i] == "'":
                    i += 1
                    break
                if text[i] == "\n":
                    line += 1
                i += 1
            literals.append((start, i, start_line, "char", text[start + 1:max(start + 1, i - 1)]))
            blank(start + 1, max(start + 1, i - 1))
            continue

        if (c == "@" and i + 1 < n and text[i + 1] == '"') or \
           (c == "$" and i + 2 < n and text[i + 1] == "@" and text[i + 2] == '"'):
            start, start_line = i, line
            q = text.index('"', i)
            i = q + 1
            while i < n:
                if text[i] == '"':
                    if i + 1 < n and text[i + 1] == '"':
                        i += 2
                        continue
                    i += 1
                    break
                if text[i] == "\n":
                    line += 1
                i += 1
            literals.append((start, i, start_line, "verbatim", text[q + 1:max(q + 1, i - 1)]))
            blank(q + 1, max(q + 1, i - 1))
            continue

        if c == '"' or (c == "$" and i + 1 < n and text[i + 1] == '"'):
            start, start_line = i, line
            q = i if c == '"' else i + 1
            i = q + 1
            depth = 0
            while i < n:
                ch = text[i]
                if ch == "\\":
                    i += 2
                    continue
                if ch == "{" and c == "$":
                    depth += 1
                elif ch == "}" and c == "$" and depth:
                    depth -= 1
                elif ch == '"' and depth == 0:
                    i += 1
                    break
                elif ch == "\n":
                    line += 1
                    i += 1
                    break  # unterminated; do not swallow the rest of the file
                i += 1
            literals.append((start, i, start_line, "string", text[q + 1:max(q + 1, i - 1)]))
            blank(q + 1, max(q + 1, i - 1))
            continue

        i += 1

    return literals, "".join(skel)


IDENT_TAIL = re.compile(r"[A-Za-z0-9_.]")


def enclosing_context(skeleton, pos):
    """(callee, in_attribute) for a literal.

    Walks back over balanced brackets to the innermost unmatched '(', then reads the
    dotted identifier in front of it. Enough to tell Log.Event("...") from
    label.text = "...", which is the distinction that decides whether a string ever
    reaches a font atlas at all.

    Then keeps walking for an unmatched '['. A literal inside one is an attribute
    argument - [Tooltip("...")], [Header("...")] - which the Unity Inspector draws in
    the editor's own font and TMP never sees. Without this the check reports every
    Tooltip in the tree, and the em-dash house style makes that about forty of them."""
    depth = 0
    callee = None
    i = pos - 1
    while i >= 0:
        c = skeleton[i]
        if c in ")]}":
            depth += 1
        elif c in "([{":
            if depth == 0:
                if c == "[":
                    return callee, True
                if c == "{":
                    return callee, False
                if callee is None:
                    j = i - 1
                    while j >= 0 and skeleton[j] in " \t\r\n":
                        j -= 1
                    k = j
                    while k >= 0 and IDENT_TAIL.match(skeleton[k]):
                        k -= 1
                    callee = skeleton[k + 1:j + 1] or None
                # keep walking: an attribute's argument list sits inside '['
            else:
                depth -= 1
        elif c == ";" and depth == 0:
            return callee, False
        i -= 1
    return callee, False


# ---------------------------------------------------------------- scene binding map

SCRIPT_RE = re.compile(r"m_Script: \{fileID: \d+, guid: ([0-9a-f]{32})")
FILEID_RE = re.compile(r"\{fileID: (\d+)\}")
DOC_RE = re.compile(r"!u!(\d+) &(\d+)")


def script_guid_index(root, cs_roots):
    """guid -> .cs path, from the .cs.meta files."""
    out = {}
    for cs_root in cs_roots:
        base = os.path.join(root, cs_root.replace("/", os.sep))
        if not os.path.isdir(base):
            raise RuleError("cs_roots names a directory that does not exist: %s" % cs_root)
        for cur, dirs, files in os.walk(base):
            dirs[:] = [d for d in dirs if d not in ("__pycache__", "obj", "bin")]
            for name in files:
                if not name.endswith(".cs.meta"):
                    continue
                p = os.path.join(cur, name)
                with open(p, "r", encoding="utf-8", errors="replace") as fh:
                    m = re.search(r"guid: ([0-9a-f]{32})", fh.read(400))
                if m:
                    out[m.group(1)] = relpath(root, p[: -len(".meta")])
    return out


def scene_binding_map(root, cs_roots):
    """Which fonts each MonoBehaviour script can write to.

    A view class holds serialized TMP_Text fields; the scene records both the reference
    and the referenced component's m_fontAsset. Following that gives, per .cs file, the
    exact set of atlases its strings can land on - derived from the scene, not asserted
    here."""
    guid_to_cs = script_guid_index(root, cs_roots)
    per_script = {}
    for rel in cov.enabled_scenes(root):
        path = os.path.join(root, rel.replace("/", os.sep))
        with open(path, "r", encoding="utf-8", errors="replace") as fh:
            text = fh.read()
        tmp_font = {}
        behaviours = []
        for doc in text.split("\n--- "):
            m = DOC_RE.match(doc)
            if not m:
                continue
            cls, fid = m.group(1), m.group(2)
            f = cov.FONT_FIELD_RE.search(doc)
            if f:
                tmp_font[fid] = f.group(1)
            if cls == "114":
                sm = SCRIPT_RE.search(doc)
                if sm:
                    behaviours.append((sm.group(1), doc))
        for guid, doc in behaviours:
            cs = guid_to_cs.get(guid)
            if cs is None:
                continue
            for ref in FILEID_RE.findall(doc):
                fg = tmp_font.get(ref)
                if fg:
                    per_script.setdefault(cs, set()).add(fg)
    return per_script


TYPE_WORD = re.compile(r"[A-Za-z_]\w*")


def primary_type(cs_path):
    """Unity requires the primary class name to match the file name; this project puts
    partials in Type.Part.cs. The stem before the first dot is the type."""
    return os.path.basename(cs_path).split(".", 1)[0]


def narrow_surface(root, per_script, per_font_by_guid, full, cs_roots):
    """Files whose strings can reach a label whose atlas is poorer than the richest one.

    Seed: the type of every script that binds a narrow-font TMP component in a shipping
    scene. In this tree that is exactly BastionHudView and ShipHudView - the HUD, whose
    labels are Rajdhani-Bold.

    Then ONE hop, and no further: every file that names a seed type, plus every
    partial-class sibling of such a file. One hop reaches the controller that calls the
    view (HudController -> BastionHudView) and its partials (HudController.Weapon.cs),
    which is where all four historical bugs lived. It does not reach a caller two hops
    out; that is a false negative, and it is deliberate - a transitive closure over
    identifier mentions swallows two thirds of the runtime tree and turns every em-dash
    in the codebase into a gate failure, which is how a check gets switched off."""
    seeds = set()
    for cs, guids in per_script.items():
        for g in guids:
            if per_font_by_guid[g] != full:
                seeds.add(primary_type(cs))
                break
    if not seeds:
        return set(), set()

    all_cs = []
    for cs_root in cs_roots:
        base = os.path.join(root, cs_root.replace("/", os.sep))
        for cur, dirs, files in os.walk(base):
            dirs[:] = [d for d in dirs if d not in ("__pycache__", "obj", "bin")]
            for name in files:
                if name.endswith(".cs"):
                    all_cs.append(relpath(root, os.path.join(cur, name)))

    words = {}
    for p in all_cs:
        with open(os.path.join(root, p.replace("/", os.sep)), "r",
                  encoding="utf-8", errors="replace") as fh:
            words[p] = set(TYPE_WORD.findall(fh.read()))

    direct = {p for p in all_cs if primary_type(p) in seeds or (words[p] & seeds)}
    types = seeds | {primary_type(p) for p in direct}
    files = {p for p in all_cs if primary_type(p) in types}
    return files | direct, types


# ---------------------------------------------------------------- scanning

class Hit(object):
    def __init__(self, path, line, cp, severity, context, source):
        self.path = path
        self.line = line
        self.cp = cp
        self.severity = severity
        self.context = context
        self.source = source


def char_name(cp):
    try:
        return unicodedata.name(chr(cp))
    except ValueError:
        return "<unnamed>"


def walk_files(root, rel, suffixes):
    base = os.path.join(root, rel.replace("/", os.sep))
    if not os.path.isdir(base):
        raise RuleError("scan root does not exist: %s" % rel)
    for cur, dirs, files in os.walk(base):
        dirs[:] = [d for d in dirs if d not in ("__pycache__", "obj", "bin")]
        for name in sorted(files):
            if name.endswith(suffixes):
                yield os.path.join(cur, name)


def is_exempt(rel, exempt):
    for pat in exempt:
        if pat.endswith("/"):
            if rel.startswith(pat) or ("/" + pat) in rel:
                return True
        elif rel == pat:
            return True
    return False


def scan_csharp(root, rules, drawable, any_drawable, surface, hits, stats):
    not_tmp = csv(rules["not_tmp_calls"])
    exempt = csv(rules["exempt_paths"])
    for cs_root in csv(rules["cs_roots"]):
        for path in walk_files(root, cs_root, (".cs",)):
            rel = relpath(root, path)
            if is_exempt(rel, exempt):
                stats["cs_exempt"] += 1
                continue
            stats["cs_files"] += 1
            with open(path, "r", encoding="utf-8", errors="replace") as fh:
                text = fh.read()
            if not any(ord(ch) > 126 for ch in text) and "\\u" not in text:
                continue
            in_surface = rel in surface
            literals, skeleton = lex_csharp(text)
            for start, _end, line, kind, value in literals:
                if kind == "char":
                    continue
                callee, in_attribute = enclosing_context(skeleton, start)
                skip = in_attribute
                if callee and not skip:
                    for lc in not_tmp:
                        if callee == lc or (lc.endswith(".") and callee.startswith(lc)):
                            skip = True
                            break
                if skip:
                    for cp, _o in codepoints_of(value, "csharp"):
                        if cp > 126:
                            stats["not_tmp_nonascii"] += 1
                    continue
                for cp, _off in codepoints_of(value, "csharp" if kind != "verbatim" else "none"):
                    if cp <= 126:
                        continue
                    if cp in drawable:
                        continue
                    sev = "box" if cp not in any_drawable else "risk"
                    ctx = "string literal"
                    if callee:
                        ctx += " passed to %s()" % callee
                    if sev == "box":
                        ctx += " -- no shipping font has this glyph"
                    elif in_surface:
                        ctx += " [narrow surface]"
                    hits.append(Hit(rel, line, cp, sev, ctx, "cs"))


TEXT_KEY = re.compile(r"^(\s*)m_text:\s?(.*)$")
KEY_LINE = re.compile(r"^\s*[A-Za-z_]\w*:")


def scan_scenes(root, per_font_by_guid, guid_index, hits, stats):
    """Scene m_text resolves to an exact font, so this tier is precise and always fatal."""
    for rel in cov.enabled_scenes(root):
        path = os.path.join(root, rel.replace("/", os.sep))
        with open(path, "r", encoding="utf-8", errors="replace") as fh:
            lines = fh.readlines()
        stats["scenes"] += 1
        bounds = []
        start = 0
        for idx, line in enumerate(lines):
            if line.startswith("--- !u!"):
                bounds.append((start, idx))
                start = idx
        bounds.append((start, len(lines)))
        for a, b in bounds:
            font_guid = None
            texts = []
            i = a
            while i < b:
                m = cov.FONT_FIELD_RE.search(lines[i])
                if m:
                    font_guid = m.group(1)
                t = TEXT_KEY.match(lines[i].rstrip("\n"))
                if t:
                    indent, first = t.group(1), t.group(2)
                    texts.append((i + 1, first))
                    j = i + 1
                    while j < b:
                        nxt = lines[j].rstrip("\n")
                        deeper = len(nxt) - len(nxt.lstrip(" ")) > len(indent)
                        if nxt.strip() and deeper and not KEY_LINE.match(nxt):
                            texts.append((j + 1, nxt))
                            j += 1
                            continue
                        break
                    i = j
                    continue
                i += 1
            if font_guid is None or not texts:
                continue
            font_cps = per_font_by_guid.get(font_guid)
            if font_cps is None:
                font_cps = cov.font_coverage(font_guid, guid_index)
                per_font_by_guid[font_guid] = font_cps
            font_name = os.path.basename(guid_index.get(font_guid, font_guid))
            for lineno, chunk in texts:
                for cp, _off in codepoints_of(chunk, "yaml"):
                    if cp <= 126 or cp in font_cps:
                        continue
                    hits.append(Hit(rel, lineno, cp, "box",
                                    "authored m_text on a label bound to %s, which has "
                                    "no such glyph" % font_name, "scene"))


def scan_markdown(root, rules, md_drawable, owner, hits, stats):
    for md_root in csv(rules["md_roots"]):
        for path in walk_files(root, md_root, (".md",)):
            rel = relpath(root, path)
            stats["md_files"] += 1
            with open(path, "r", encoding="utf-8", errors="replace") as fh:
                for lineno, line in enumerate(fh, 1):
                    for cp, _off in codepoints_of(line.rstrip("\n"), "none"):
                        if cp <= 126 or cp in md_drawable:
                            continue
                        hits.append(Hit(rel, lineno, cp, "box",
                                        "Compendium page rendered by %s, whose label "
                                        "has no such glyph" % owner, "md"))


# ---------------------------------------------------------------- reporting

def suggest(cp, subs):
    if cp in subs:
        return "write %s instead" % subs[cp]
    return ("no ASCII substitute agreed - add one to the %s fence, or bake the glyph "
            "into the atlas" % FENCE_TAG)


def emit(rows, subs, stream, indent="  ", cap=0):
    """One heading per (codepoint, severity), then the sites. cap>0 truncates the site
    list - the gate prints a summary, --risks prints all of it."""
    by_key = {}
    for h in rows:
        by_key.setdefault((h.cp, h.severity), []).append(h)
    for cp, sev in sorted(by_key):
        group = sorted(by_key[(cp, sev)], key=lambda h: (h.path, h.line))
        stream.write("%sU+%04X %s -- %s, %d site(s); %s\n"
                     % (indent, cp, char_name(cp), sev, len(group),
                        suggest(cp, subs)))
        shown = group if cap <= 0 else group[:cap]
        for h in shown:
            stream.write("%s  %s:%d  %s\n" % (indent, h.path, h.line, h.context))
        if len(shown) < len(group):
            stream.write("%s  ... and %d more; Tools/lint-glyphs/run.sh --risks\n"
                         % (indent, len(group) - len(shown)))


def main(argv):
    ap = argparse.ArgumentParser(description="TextMeshPro glyph coverage check")
    ap.add_argument("--root", default=None)
    ap.add_argument("--risks", action="store_true",
                    help="list every risk site, not just the narrow-surface ones")
    ap.add_argument("--coverage", action="store_true",
                    help="print the derived coverage and the binding map, then exit")
    args = ap.parse_args(argv)

    here = os.path.dirname(os.path.abspath(__file__))
    root = args.root or os.path.abspath(os.path.join(here, "..", ".."))

    try:
        rules = read_fence(root)
        subs = parse_substitutes(rules["substitutes"])
        cs_roots = csv(rules["cs_roots"])
        drawable, per_font, scanned = cov.drawable_set(root, csv(rules["prefab_roots"]))
        guid_index = cov.build_guid_index(os.path.join(root, "Assets"))
        guids, _ = cov.shipping_font_guids(root, csv(rules["prefab_roots"]))
        per_font_by_guid = {g: cov.font_coverage(g, guid_index) for g in guids}

        any_drawable = set()
        for cps in per_font.values():
            any_drawable |= cps

        per_script = scene_binding_map(root, cs_roots)
        surface, surface_types = narrow_surface(
            root, per_script, per_font_by_guid, any_drawable, cs_roots)

        md_owner = rules["md_owner"]
        owner_fonts = per_script.get(md_owner)
        if owner_fonts is None:
            raise RuleError(
                "md_owner=%s binds no TMP label in any shipping scene - the Compendium "
                "font cannot be resolved" % md_owner)
        md_drawable = None
        for g in owner_fonts:
            cps = per_font_by_guid.get(g) or cov.font_coverage(g, guid_index)
            md_drawable = set(cps) if md_drawable is None else (md_drawable & cps)
    except (RuleError, cov.CoverageError) as exc:
        sys.stderr.write("lint-glyphs: %s\n" % exc)
        return 2

    if args.coverage:
        print("shipping fonts (EditorBuildSettings scenes + TMP Settings + prefab scan):")
        for name in sorted(per_font):
            print("  %-72s %5d" % (name, len(per_font[name])))
        print("scanned %d scene/prefab file(s)" % len(scanned))
        print("drawable by EVERY shipping font: %d codepoints" % len(drawable))
        print("  " + cov.format_ranges(drawable))
        print("drawable by AT LEAST ONE: %d codepoints" % len(any_drawable))
        print("Compendium (%s) can draw %d codepoints" % (md_owner, len(md_drawable)))
        print("narrow surface: %d type(s), %d file(s)"
              % (len(surface_types), len(surface)))
        for t in sorted(surface_types):
            print("  type %s" % t)
        for f in sorted(surface):
            print("  file %s" % f)
        return 0

    hits = []
    stats = {"cs_files": 0, "cs_exempt": 0, "md_files": 0, "scenes": 0,
             "not_tmp_nonascii": 0}
    try:
        scan_csharp(root, rules, drawable, any_drawable, surface, hits, stats)
        scan_scenes(root, per_font_by_guid, guid_index, hits, stats)
        scan_markdown(root, rules, md_drawable, md_owner, hits, stats)
    except (RuleError, cov.CoverageError) as exc:
        sys.stderr.write("lint-glyphs: %s\n" % exc)
        return 2

    risk_fails = rules["risk_fails"]
    if risk_fails not in ("no", "surface", "all"):
        sys.stderr.write("lint-glyphs: risk_fails must be no|surface|all, not %r\n"
                         % risk_fails)
        return 2

    fatal, warn = [], []
    for h in hits:
        if h.severity == "box":
            fatal.append(h)
        elif risk_fails == "all" or (risk_fails == "surface" and h.path in surface):
            fatal.append(h)
        else:
            warn.append(h)
    warn.sort(key=lambda h: (h.path not in surface, h.cp, h.path, h.line))

    print("lint-glyphs: %d C# file(s) (%d exempt), %d scene(s), %d Compendium page(s)"
          % (stats["cs_files"], stats["cs_exempt"], stats["scenes"], stats["md_files"]))
    print("lint-glyphs: every shipping font can draw %s" % cov.format_ranges(drawable))
    print("lint-glyphs: %d non-ASCII character(s) in strings TMP never draws (log lines, "
          "IMGUI, inspector attributes), not checked" % stats["not_tmp_nonascii"])
    print("lint-glyphs: narrow surface is %d file(s) from %s"
          % (len(surface), ", ".join(sorted(surface_types)) or "-"))

    if warn:
        on_surface = [h for h in warn if h.path in surface]
        print("lint-glyphs: %d character(s) at risk -- LiberationSans draws them, "
              "Rajdhani does not; %d sit on the narrow surface"
              % (len(warn), len(on_surface)))
        if args.risks:
            emit(warn, subs, sys.stdout)
        else:
            by_cp = {}
            for h in warn:
                by_cp[h.cp] = by_cp.get(h.cp, 0) + 1
            for cp in sorted(by_cp):
                print("    U+%04X %-22s %4d site(s); %s"
                      % (cp, char_name(cp), by_cp[cp], suggest(cp, subs)))
            if on_surface:
                print("  on the narrow surface -- these can reach a Rajdhani label:")
                emit(on_surface, subs, sys.stdout, indent="    ", cap=4)

    if fatal:
        sys.stderr.write("lint-glyphs: %d glyph violation(s) -- each renders as a blank "
                         "or a box:\n" % len(fatal))
        emit(fatal, subs, sys.stderr)
        sys.stderr.write("lint-glyphs: coverage is derived from the font assets; the "
                         "scan rules are in %s\n" % FENCE_REL)
        return 1

    print("lint-glyphs: clean")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
