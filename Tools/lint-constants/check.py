#!/usr/bin/env python3
"""Gatekeeper's Protocol — restated-constant drift check.

Docgen owns the DERIVED half of a countable document: everything between GKB:GEN and
GKB:END is regenerated from Sim/, so `Tools/docgen/run.sh --check` can prove it matches.
It says nothing about the hand-written half — the prose beside a generated table, and the
XML comment sitting three lines below the array it describes. Both restate a number whose
source is Sim/, and both drifted from `CloakerPerWave` without anything going red.

This check covers that half, in two narrow and mechanical cases.

ARRAYS. A numeric array literal, restated outside its declaration, that nearly matches a
live Sim/ array. Near, not equal, is the whole signal — a restatement that agrees is silent,
and a list of unrelated numbers is too far away to match.

SCALARS. A number written beside the name of a WATCHED Sim/ constant, or beside one of that
constant's declared plain-English aliases, that disagrees with the value the code compiles.
Scanning every `const` was built and measured on 2026-08-26 and dropped at 6 hits, all false
positives: `Build`, `Count` and `Curve` are English words before they are symbols, and a line
may cite one constant while quoting another's number. The allow-list is the answer to that —
nothing is watched until someone writes it into the fence with the words the docs actually
use, so an accidental symbol-shaped word can never be a subject. What that buys is narrower
than it sounds; see the closing note and Working-Rules for what it still cannot see.

The allow-list alone was not enough. The first run over the tree put 15 lines up and 11 were
noise — worse precision than the 2026-08-26 version, and a check people learn to ignore is
worse than no check. Five further rules answer five measured shapes, one each: a name used as
an operand, a full stop between the number and the name, a `X -> Y` history whose far half is
live, a `path:NNN` citation, and a number some OTHER Sim type named on the line declares. Each
is defined and justified where it is written, below. Each also makes this check quieter, which
is how a check dies, so each carries a --selftest PAIR — the shape it silences and the
near-identical shape it must still catch — and every rule's blind spot is written into
Working-Rules rather than left for the next reader to discover.

See Docs/Working-Rules.md, "Restated constants", for the rule and the ```constant-lint fence
this script reads. It carries no rule value of its own — the watch list included: a missing
fence or a missing key is exit 2, never a default. The watch list lives in the document that
states the rule for the same reason every other value here does, and putting it in this
header as data would split the rule across two homes.

  Tools/lint-constants/run.sh              scan the tree
  Tools/lint-constants/run.sh --selftest   fixtures; exit 0 when each is caught
  Tools/lint-constants/run.sh --verbose    also print the resolved source-of-truth table

Exit codes: 0 clean, 1 drift found, 2 could not run.
"""

import argparse
import os
import re
import subprocess
import sys
import tempfile

# ---------------------------------------------------------------- fence

FENCE_DOC = "Docs/Working-Rules.md"
FENCE_NAME = "constant-lint"

# The one key that may repeat. Every other key is single-valued and a repeat would silently
# win; `watch` is a list by nature, and one entry per line is the only way a name and the
# four or five phrases the docs use for it stay legible on one line. Splitting the watch
# across commas inside a single `watch=` would collide with the aliases themselves, which
# are English and contain spaces.
WATCH_KEY = "watch"


def fail(msg):
    sys.stderr.write("lint-constants: " + msg + "\n")
    sys.exit(2)


def read_fence(root):
    path = os.path.join(root, FENCE_DOC)
    if not os.path.isfile(path):
        fail("expected " + FENCE_DOC)
    body, grabbing = [], False
    with open(path, encoding="utf-8", errors="replace") as fh:
        for line in fh:
            if not grabbing:
                if line.strip() == "```" + FENCE_NAME:
                    grabbing = True
                continue
            if line.strip() == "```":
                break
            body.append(line.rstrip("\n"))
    if not body:
        fail("missing fence — no ```" + FENCE_NAME + " fence in " + FENCE_DOC)
    keys, watch = {}, []
    for line in body:
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        if "=" not in line:
            fail("fence line is not key=value in " + FENCE_DOC + ": " + line)
        k, v = line.split("=", 1)
        k, v = k.strip(), v.strip()
        if k == WATCH_KEY:
            watch.append(v)
        else:
            keys[k] = v
    return keys, watch


def key(keys, name):
    if name not in keys:
        fail("missing fence key '" + name + "=' in " + FENCE_DOC)
    return keys[name]


def key_int(keys, name):
    raw = key(keys, name)
    if not re.fullmatch(r"\d+", raw):
        fail("fence key '" + name + "' is not a number: '" + raw + "'")
    return int(raw)


def key_list(keys, name):
    raw = key(keys, name)
    return [p.strip() for p in raw.split(",") if p.strip()]


# ---------------------------------------------------------------- source of truth

NUM = r"-?\d+(?:\.\d+)?[fFdDmM]?"

# A Sim array declaration: `... int[] Name = { 1, 2, 3 };`, with or without `new int[]`.
ARRAY_DECL = re.compile(
    r"\b(?:public|private|internal|protected|static|readonly|const|\s)*"
    r"(?:int|float|double|long|byte)\s*\[\s*\]\s*([A-Za-z_]\w*)\s*=\s*"
    r"(?:new\s+\w+\s*\[\s*\]\s*)?\{([^}]*)\}\s*;"
)

BRACE_LIST = re.compile(r"\{\s*(" + NUM + r"(?:\s*,\s*" + NUM + r"){1,})\s*\}")

# A generated block is docgen's to prove; blanking it keeps the two checks from
# reporting the same drift twice.
GEN_BLOCK = re.compile(r"<!--\s*GKB:GEN\b.*?<!--\s*GKB:END\b[^>]*-->", re.S)

LINE_COMMENT = re.compile(r"//.*$")


def number(token):
    token = token.strip().rstrip("fFdDmMlLuU")
    try:
        return float(token)
    except ValueError:
        return None


def parse_list(text, sep=","):
    vals = [number(t) for t in text.split(sep)]
    return None if any(v is None for v in vals) else tuple(vals)


def rel(root, path):
    return os.path.relpath(path, root).replace("\\", "/")


def walk(base, suffix, skip_dirs=(".git", "Library", "Temp", "obj", "bin")):
    for dirpath, dirnames, filenames in os.walk(base):
        dirnames[:] = [d for d in dirnames if d not in skip_dirs]
        for name in sorted(filenames):
            if name.endswith(suffix):
                yield os.path.join(dirpath, name)


def collect_arrays(root, sim_dir, min_length):
    """Every Sim/ array literal of at least min_length values, by name."""
    found = {}
    base = os.path.join(root, sim_dir)
    if not os.path.isdir(base):
        fail("expected " + sim_dir)
    for path in walk(base, ".cs"):
        src = open(path, encoding="utf-8", errors="replace").read()
        for m in ARRAY_DECL.finditer(src):
            name = m.group(1)
            vals = parse_list(m.group(2))
            if vals is None or len(vals) < min_length:
                continue
            found[name] = (
                vals,
                rel(root, path),
                src[: m.start()].count("\n") + 1,
            )
    return found


# ---------------------------------------------------------------- scalar source of truth

# `long` literals carry an L this file's array NUM never had to read (Respawns.BaseCost is
# `150L`), and `number()` already strips the suffix.
SNUM = r"-?\d+(?:\.\d+)?[fFdDmMlLuU]*"

TYPE_DECL = re.compile(r"\b(?:class|struct|record)\s+([A-Za-z_]\w*)")

COMMENT_OR_STRING = re.compile(r"//[^\n]*|/\*.*?\*/|\"(?:\\.|[^\"\\\n])*\"", re.S)


def blank_prose(src):
    """Comments and strings, replaced by spaces of the same width so every offset and line
    number still lines up. Without this, `class`, `struct` and `record` are read out of the
    prose that surrounds the code — WaveDirector's own comments contain "the record of a
    lever" and "not a constant this struct could push", and both parsed as a type
    declaration that then owned every constant below them."""
    return COMMENT_OR_STRING.sub(lambda m: re.sub(r"[^\n]", " ", m.group(0)), src)

CONST_DECL = re.compile(
    r"\bconst\s+(?:int|float|double|long|byte|decimal|short|uint|ulong)\s+"
    r"([A-Za-z_]\w*)\s*=\s*(" + SNUM + r")\s*;")

# A zero-argument method whose entire body is `return <literal>;` is a constant wearing
# parentheses, and MetaUpgrades writes its whole tuning table that way so the attribute
# tracks can grow a curve later without every caller changing. Reading it here is deliberate:
# `SalvagePerKill` is cited in balance arguments exactly as often as any `const`, and the
# alternative was to watch it and silently never resolve it.
CONST_METHOD = re.compile(
    r"\b(?:int|float|double|long|byte|decimal|short|uint|ulong)\s+([A-Za-z_]\w*)\s*"
    r"\(\s*\)\s*\{\s*return\s+(" + SNUM + r")\s*;\s*\}")


def enclosing_type(src, pos):
    """The last type declared before pos. File-per-type is this codebase's layout, so this
    is the qualifier the docs use; a nested type would report its parent's name, which is
    why watch entries name a type that a reader can find by opening the file."""
    name = None
    for m in TYPE_DECL.finditer(src, 0, pos):
        name = m.group(1)
    return name


def collect_scalars(root, sim_dir):
    """Every watchable Sim/ scalar, keyed BOTH by `Type.Name` and by bare `Name`.

    The bare key is dropped when two types declare it — `MetaUpgrades.BaseCost` is 200 and
    `Respawns.BaseCost` is 150, and a doc-side `BaseCost` with a number beside it cannot be
    judged against either. Ambiguity resolves to nothing rather than to the wrong lever."""
    qualified, seen = {}, {}
    base = os.path.join(root, sim_dir)
    if not os.path.isdir(base):
        fail("expected " + sim_dir)
    for path in walk(base, ".cs"):
        src = blank_prose(open(path, encoding="utf-8", errors="replace").read())
        for pattern in (CONST_DECL, CONST_METHOD):
            for m in pattern.finditer(src):
                val = number(m.group(2))
                if val is None:
                    continue
                owner = enclosing_type(src, m.start())
                if owner is None:
                    continue
                name = m.group(1)
                qualified[owner + "." + name] = (
                    val, rel(root, path), src[: m.start()].count("\n") + 1)
                seen.setdefault(name, set()).add(owner)
    return qualified, {n for n, owners in seen.items() if len(owners) == 1}


# ---------------------------------------------------------------- the watch list


# What may sit between two words of an alias, and nothing else may: punctuation, the number
# itself, a possessive, and a closed list of connectives. `Hatch HP` has to read "the Hatch's
# 300 HP" and "Thief has 180 HP" without also reading "540 HP of gatekeeper on Wave", and an
# open gap of N characters reads all four. Measured on the tree: an open gap put eight hits
# on lines like "HP muls, Hunter", "HP for a Default" and "| Cloaker | `HP" where the two
# words belong to different clauses or different table cells. `|`, `;` and `,` are excluded
# for that last reason — a cell or clause boundary is a subject change, and "player 100 HP;
# a Default gatekeeper swings 6 damage" is two facts, not one.
ALIAS_GAP = r"(?:'s|\d[\d.,%]*|is|are|was|were|has|have|had|at|of|to|the|an|a|[^\w\n|;,])"


class Watched(object):
    """One `watch=` fence line: a type-qualified Sim constant and the phrases the docs use
    for it. The symbol is matched case-sensitively; an alias is a SEQUENCE OF WORDS that
    must appear in order with only ALIAS_GAP between them, so `Hatch HP` reads
    "the Hatch's 300 HP" and "Hatch HP (Armor 0)" alike without an alias per phrasing."""

    def __init__(self, spec, window):
        parts = [p.strip() for p in spec.split("|")]
        self.qualified = parts[0]
        self.aliases = [p for p in parts[1:] if p]
        if "." not in self.qualified:
            fail("watch entry is not type-qualified in " + FENCE_DOC + ": " + spec)
        self.type_name, self.name = self.qualified.rsplit(".", 1)
        self.value = None
        self.where = None
        self.patterns = []

    def bind(self, qualified, unambiguous, window):
        """Resolve against Sim/ and build the mention patterns. Returns False when the
        constant is gone — the caller turns that into exit 2, never a quiet pass."""
        if self.qualified not in qualified:
            return False
        self.value, path, line = qualified[self.qualified]
        self.where = "%s:%d" % (path, line)
        gap = ALIAS_GAP + r"{0,%d}?" % window
        pats = [re.compile(r"(?<![\w.])" + re.escape(self.qualified) + r"(?![\w])")]
        if self.name in unambiguous:
            # Only when nothing else in Sim/ declares this name. `BaseCost` never qualifies.
            pats.append(re.compile(r"(?<![\w.])" + re.escape(self.name) + r"(?![\w])"))
        for alias in self.aliases:
            # Every word is boundary-anchored on BOTH sides, not just the first and the
            # last. Anchoring only the ends let a word match inside a longer one across the
            # gap: `bleed out` read "Bleeding out", and `HP per Wave` found its "per" in
            # the middle of "gatekeeper". Two measured false positives from one missing
            # lookahead.
            # A hyphen counts as part of the word, so `spawn gap` does not read
            # "fight-spawn gap", which is a different gap in a different beat.
            words = [r"(?<![\w-])" + re.escape(w) + r"(?![\w-])" for w in alias.split()]
            pats.append(re.compile(gap.join(words), re.I))
        self.patterns = pats
        return True


def build_watch(specs, qualified, unambiguous, window):
    watch, missing = [], []
    for spec in specs:
        w = Watched(spec, window)
        if w.bind(qualified, unambiguous, window):
            watch.append(w)
        else:
            missing.append(w.qualified)
    return watch, missing


# ---------------------------------------------------------------- scan surfaces


def doc_lines(root, doc_dirs, exempt):
    for doc_dir in doc_dirs:
        base = os.path.join(root, doc_dir)
        if not os.path.isdir(base):
            fail("expected " + doc_dir)
        for path in walk(base, ".md"):
            r = rel(root, path)
            if any(r.startswith(e) or ("/" + e + "/") in ("/" + r + "/") for e in exempt):
                continue
            src = open(path, encoding="utf-8", errors="replace").read()
            # Blank generated blocks but keep line numbers intact.
            src = GEN_BLOCK.sub(lambda m: re.sub(r"[^\n]", " ", m.group(0)), src)
            for i, line in enumerate(src.split("\n"), 1):
                yield r, i, line


def code_comment_lines(root, code_dirs, exempt):
    """Only comment text. A declaration is the source, not a restatement of one."""
    for code_dir in code_dirs:
        base = os.path.join(root, code_dir)
        if not os.path.isdir(base):
            fail("expected " + code_dir)
        for path in walk(base, ".cs"):
            r = rel(root, path)
            if any(r.startswith(e) for e in exempt):
                continue
            src = open(path, encoding="utf-8", errors="replace").read()
            for i, line in enumerate(src.split("\n"), 1):
                m = LINE_COMMENT.search(line)
                if m:
                    yield r, i, m.group(0)


# ---------------------------------------------------------------- the rule


def scan(surfaces, arrays, max_distance, min_length):
    live = {name: vals for name, (vals, _, _) in arrays.items()}
    exact = set(live.values())
    hits = []
    for where, lineno, text in surfaces:
        for m in BRACE_LIST.finditer(text):
            vals = parse_list(m.group(1))
            if vals is None or len(vals) < min_length:
                continue
            if vals in exact:
                continue  # agrees with a live array: this is what correct looks like
            best = None
            for name in sorted(live):
                a = live[name]
                if len(a) != len(vals):
                    continue
                d = sum(1 for x, y in zip(a, vals) if x != y)
                if 0 < d <= max_distance and (best is None or d < best[1]):
                    best = (name, d)
            if best:
                hits.append((where, lineno, m.group(0), best[0], live[best[0]], best[1]))
    return hits


# A bare numeral in prose. The lookbehind keeps the `40` out of `2.40` and out of `M110`.
# Thousands separators are read (`1,200 Salvage` is one number, not 1 and then 200), and a
# trailing `%` is captured because a fraction constant is written as a percentage as often
# as it is written as a fraction (`FieldWaveDamageScale` is 0.15 and reads "15%"). The
# trailing boundary deliberately allows a full stop: `Cloaker 45 -> 75.` ends a sentence,
# and refusing the 75 there turned a correct dated line into a hit.
DOC_NUMERAL = re.compile(r"(?<![\w.])(\d+(?:,\d{3})*(?:\.\d+)?)(%?)(?![\w])")

# A numbered heading or a numbered list item counts nothing. "### 2 — Bleed-out" and
# "4. Hatch HP unchanged during the Terminal half" both put an ordinal one space from a
# watched alias, and both were measured hits. A bullet whose text simply starts with a
# number ("- 300 HP door") is NOT an ordinal and stays in scope.
ORDINALS = (re.compile(r"^[\s>]*#{1,6}\s*\d+(?=\s)"),
            re.compile(r"^[\s>*+\-]*\d+[.)](?=\s)"))


def ordinal_end(text):
    for pat in ORDINALS:
        m = pat.match(text)
        if m:
            return m.end()
    return 0


# A watched name with an arithmetic operator against it is being USED, not quoted. The
# measured shape is WaveDirector's own formula block: `enemyHealthMultiplier = 1 +
# EnemyHealthPerWave*waveIndex` and `max(0.4, SpawnGapBase + SpawnGapPerWave*min(...))` —
# two lines that name four watched constants and state the value of none of them, while the
# `1` and the `0.4` beside them belong to the formula and to MinSpawnInterval.
#
# Only `*` and `+`. `-` is an em-dash and a bullet as often as it is subtraction, `/` is a
# unit separator ("HP/wave"), and `×` is how a doc table writes a multiplier — `` `BaseEnemyHp`
# × 1.0 `` in a spec table is a claim, and putting `×` here would silence it. What this lets
# through: a formula written with any of those three, and a genuine restatement written as
# "Hatch HP + 100 of armour".
USE_AFTER = re.compile(r"\s*[*+]")
USE_BEFORE = re.compile(r"[*+]\s*$")


def arithmetic_use(text, span):
    s, e = span
    return bool(USE_AFTER.match(text, e) or USE_BEFORE.search(text[:s]))


def agrees(shown, truth):
    return abs(shown - truth) <= 1e-9 * max(1.0, abs(truth))


# A full stop between the number and the name ends the claim the number belonged to.
# ALIAS_GAP already refuses `|`, `;` and `,` between two words of one alias for this reason —
# "a cell or clause boundary is a subject change" — and the same is true of the distance from
# a numeral to the constant it is supposed to be quoting. Measured shape, `4-Data.md`:
# "PRI-C 90 -> 180. `EnemyHealthPerWave` was restored ..." — the 180 is PRI-C's damage, one
# sentence earlier, and the live 0.40 is on the same line just past the window.
#
# A full stop only, not `|` or `,`: a spec-table row is `| Default HP W1 | 30 | `BaseEnemyHp` |`
# and a pipe there separates the label from its own value, so refusing pipes would silence the
# exact shape this check exists for. What this lets through: a stale number in the sentence
# before the one that names the constant.
SENTENCE_END = re.compile(r"\.(?:\s|$)")

# Dated history, written as the transition it was. `WeaponCatalog.cs`: "when BaseEnemyHp went
# 30 -> 50 the same day" states the live 50 correctly, but it sits 12 characters from the name
# and the stale-looking 30 sits 6, so the window sees only the wrong half of a right sentence.
# Reading through the arrow is the fix, and it is narrow because the OTHER half still has to
# equal the live value — `300 -> 450` against a live 600 is two wrong numbers and stays a hit.
ARROW = r"(?:->|→|=>)"
ARROW_AFTER = re.compile(r"\s*" + ARROW + r"\s*(" + r"\d+(?:,\d{3})*(?:\.\d+)?" + r")")
ARROW_BEFORE = re.compile(r"(\d+(?:,\d{3})*(?:\.\d+)?)\s*" + ARROW + r"\s*$")


def transition_partner(text, span):
    """The other half of an `X -> Y` pair the numeral at `span` sits in, or None."""
    s, e = span
    after = ARROW_AFTER.match(text, e)
    if after:
        return float(after.group(1).replace(",", ""))
    before = ARROW_BEFORE.search(text[:s])
    if before:
        return float(before.group(1).replace(",", ""))
    return None


# `Docs/Decisions.md:542` is a line citation, not a value. The colon has to be tight against
# both the numeral and a path-ish character, so `Wave 3: 50 HP` (colon, space) is untouched.
# What this lets through: a ratio written `1:2`, and a value written `Hatch HP:600`.
CITATION_BEFORE = re.compile(r"[\w/\\.-]:$")

# A type name a reader can find by opening a file — two CamelCase words or more. One word is
# not enough: `Log`, `Curve` and `Respawns` are English before they are types, which is the
# 2026-08-26 finding this whole allow-list exists to answer, and a single-word type name in a
# sentence is not evidence that the sentence is about that type's tuning table.
CAMEL_TYPE = re.compile(r"(?<![\w.])([A-Z][A-Za-z0-9]*[a-z0-9][A-Z][A-Za-z0-9]*)(?![\w])")


def owned_values(qualified):
    """Two-word-or-longer Sim type name -> the set of values that type declares."""
    out = {}
    for q, (val, _, _) in qualified.items():
        owner = q.rsplit(".", 1)[0]
        if CAMEL_TYPE.fullmatch(owner):
            out.setdefault(owner, set()).add(val)
    return out


def sibling_values(text, owned):
    """Every value declared by a Sim type this line names, by that type's name.

    Measured shape, `Demo - Coop/README.md`: "`MasterControl.ApplyFinaleElite`: one Thief at
    **240** HP" — the alias `Thief HP` matches, but 240 is `MasterControl.FinaleEliteHp` and
    the line says so. A number that a named type declares is that type's number.

    Deliberately keyed by the type the line NAMES and not by value alone: 30 is
    `ShipCamera.FovDegrees`, 300 is `EquipmentCatalog.SlotPrice` and 180 is
    `RealmShare.StaleSeconds`, so a bare value-anywhere-in-Sim test would have silenced three
    of the four real drifts M110 was written to catch. Measured, not reasoned."""
    out = {}
    for m in CAMEL_TYPE.finditer(text):
        name = m.group(1)
        if name in owned:
            out[name] = owned[name]
    return out


def scan_scalars(surfaces, watch, window, owned=None):
    """A watched constant's name or alias, with a number written beside it that the code
    does not agree with.

    Two rules keep this quiet. A mention with no number within `window` characters is not a
    restatement at all — most prose names a constant to argue about it, not to quote it. And
    a mention with the TRUE value anywhere in reach is silent even when other numbers sit
    beside it, because `7.3 + 0.40*w` states two watched constants at once and each is
    correct next to the other's number.

    Five more, each measured against one shape on the tree and each documented at its own
    definition above: `arithmetic_use` (the name is an operand, not a subject), `SENTENCE_END`
    (a full stop between number and name), `transition_partner` (`30 -> 50` with the live
    value on the far side), `CITATION_BEFORE` (`file.md:542`), and `sibling_values` (the line
    names the type that declares this number). Every one of them makes the check quieter, so
    every one of them has a `--selftest` fixture proving the shape it must still catch."""
    hits = []
    owned = owned or {}
    for where, lineno, text in surfaces:
        siblings = sibling_values(text, owned)
        for w in watch:
            spans = []
            for pat in w.patterns:
                spans.extend(sp for sp in (m.span() for m in pat.finditer(text))
                             if not arithmetic_use(text, sp))
            if not spans:
                continue
            best, correct = None, False
            skip_before = ordinal_end(text)
            # Every value the line's OTHER named types declare. The watched constant's own
            # type is excluded: a page that names `WaveDirector` must not thereby get a free
            # pass on every WaveDirector number it quotes, which is most of 4-Data.md.
            elsewhere = set()
            for owner, vals in siblings.items():
                if owner != w.type_name:
                    elsewhere |= vals
            for (s, e) in spans:
                for m in DOC_NUMERAL.finditer(text):
                    ns, ne = m.span()
                    if ne <= skip_before:
                        continue
                    dist = 0 if (ns < e and ne > s) else (
                        ns - e if ns >= e else s - ne)
                    if dist > window:
                        continue
                    if dist and SENTENCE_END.search(text[ne:s] if ne <= s else text[e:ns]):
                        continue
                    if CITATION_BEFORE.search(text[:ns]):
                        continue
                    shown = float(m.group(1).replace(",", ""))
                    if m.group(2) == "%":
                        shown /= 100.0
                    if shown in elsewhere:
                        continue
                    if shown == 0.0 and w.value != 0.0:
                        # "Hatch 0 HP", "0 damage", "at 0". Zero beside a watched constant
                        # is the empty state or a design absolute, never a stale copy of a
                        # non-zero tuning value — seven measured hits, all of them the
                        # Hatch being destroyed rather than the Hatch being wrong.
                        continue
                    if agrees(shown, w.value):
                        correct = True
                        break
                    partner = transition_partner(text, (ns, ne))
                    if partner is not None and agrees(partner, w.value):
                        correct = True
                        break
                    if best is None or dist < best[0]:
                        best = (dist, m.group(0), text[s:e].strip())
                if correct:
                    break
            if correct or best is None:
                continue
            hits.append((where, lineno, best[2], best[1], w))
    return hits


def scalar_str(v):
    return str(int(v)) if v == int(v) else str(v)


def render(vals):
    return "{" + ",".join(scalar_str(v) for v in vals) + "}"


# ---------------------------------------------------------------- selftest


RENAMED_TREE_FENCE = """```constant-lint
min_length=3
max_distance=2
scalar_window={window}
sim_dir=Assets/Scripts/Sim
doc_dirs=Docs
code_dirs=Assets/Scripts
must_resolve=Curve
exempt=Docs/Changelog.md
scalar_exempt=Assets/Tests
watch=Fixture.HatchMaxHp | Hatch HP
```
"""

RENAMED_TREE_SOURCE = """namespace Fixture {
    public static class Fixture {
        public static readonly int[] Curve = { 1, 2, 3, 4 };
        // The watched name is spelled HatchMaxHp in the fence and HatchTopHp here: this is
        // exactly what a rename looks like from the check's side.
        public const float HatchTopHp = 600f;
    }
}
"""


def renamed_watch_exits_two(window):
    """Build a throwaway tree whose watched constant has been renamed out from under the
    fence, then run this same script against it as a subprocess. Asserting on the RETURN
    CODE and not on an internal call is the point: a negative check that can pass by seeing
    nothing is not a gate, and the only honest proof of exit 2 is exit 2."""
    with tempfile.TemporaryDirectory() as tree:
        for sub in ("Docs", "Assets/Scripts/Sim"):
            os.makedirs(os.path.join(tree, *sub.split("/")))
        with open(os.path.join(tree, "Docs", "Working-Rules.md"), "w",
                  encoding="utf-8") as fh:
            fh.write(RENAMED_TREE_FENCE.format(window=window))
        with open(os.path.join(tree, "Assets", "Scripts", "Sim", "Fixture.cs"), "w",
                  encoding="utf-8") as fh:
            fh.write(RENAMED_TREE_SOURCE)
        proc = subprocess.run(
            [sys.executable, os.path.abspath(__file__), "--root", tree],
            stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        return proc.returncode, proc.stderr.decode("utf-8", "replace").strip()


def selftest(arrays, watch, max_distance, min_length, window):
    """A gutted check catches nothing. These fixtures prove this one still fires,
    and — just as important — that it stays silent on a restatement that agrees."""
    if not arrays:
        fail("selftest: no Sim arrays resolved, nothing to build fixtures from")
    if not watch:
        fail("selftest: no watched scalars resolved, nothing to build fixtures from")
    name = sorted(arrays)[0]
    truth = arrays[name][0]
    drifted = list(truth)
    drifted[-1] = drifted[-1] + 1  # one value wrong: the Cloaker shape

    hatch = next((w for w in watch if w.name == "HatchMaxHp"), watch[0])

    cases = [
        ("one value wrong is caught",
         [("fixture.md", 1, "count = " + render(drifted) + "[i]")], 1),
        ("the true values are silent",
         [("fixture.md", 1, "count = " + render(truth) + "[i]")], 0),
        ("an unrelated list is silent",
         [("fixture.md", 1, "{991,992,993,994,995}")], 0),
        ("a comment restatement is caught",
         [("fixture.cs", 1, "/// count = " + render(drifted) + "[waveIndex]")], 1),
    ]
    # The scalar half. The seeded string is the one M110 names, written the way a document
    # writes it — possessive, with the number between the alias's two words.
    #
    # Every quietening rule is a PAIR: the shape it must silence, and the shape it must still
    # catch, written as close to identically as the rule allows. A rule that makes the check
    # quieter is how a check dies, so a fixture that only proves silence is not proof of
    # anything. Both halves are built from `hatch.value` so a retune moves them together.
    stale = scalar_str(hatch.value / 2.0)
    live = scalar_str(hatch.value)
    other = scalar_str(hatch.value * 0.75)
    scalar_cases = [
        ("a seeded scalar in prose is caught",
         [("fixture.md", 1, "the Hatch's %s HP holds against the Wave" % stale)], {}, 1),
        ("the live scalar value is silent",
         [("fixture.md", 1, "the Hatch's %s HP holds against the Wave" % live)], {}, 0),
        ("a named scalar with no number beside it is silent",
         [("fixture.md", 1, "%s is the door's whole budget" % hatch.qualified)], {}, 0),

        ("a name used as an operand is silent",
         [("fixture.md", 1, "total = %s*%s" % (hatch.qualified, stale))], {}, 0),
        ("the same name quoting the same number is caught",
         [("fixture.md", 1, "total = %s of %s" % (hatch.qualified, stale))], {}, 1),

        ("a number one full stop away is silent",
         [("fixture.md", 1, "PRI-C raised to %s. %s was restored in the same pass"
           % (stale, hatch.qualified))], {}, 0),
        ("the same number in the same clause is caught",
         [("fixture.md", 1, "PRI-C raised to %s, %s was restored in the same pass"
           % (stale, hatch.qualified))], {}, 1),

        ("a transition whose other half is live is silent",
         [("fixture.md", 1, "when %s went %s -> %s the same day"
           % (hatch.qualified, stale, live))], {}, 0),
        ("a transition whose halves are both wrong is caught",
         [("fixture.md", 1, "when %s went %s -> %s the same day"
           % (hatch.qualified, stale, other))], {}, 1),

        ("a number the line's named type declares is silent",
         [("fixture.md", 1, "`FixtureCatalog.Apply`: one Hatch HP at %s pre-multiplier"
           % stale)], {"FixtureCatalog": {hatch.value / 2.0}}, 0),
        ("the same number with no type named is caught",
         [("fixture.md", 1, "one Hatch HP at %s pre-multiplier" % stale)],
         {"FixtureCatalog": {hatch.value / 2.0}}, 1),

        ("a path:line citation is not a value",
         [("fixture.md", 1, "`Docs/Decisions.md:%s`'s Hatch HP entry records it" % stale)],
         {}, 0),
        ("the same number written as a value is caught",
         [("fixture.md", 1, "Decisions entry %s, Hatch HP records it" % stale)], {}, 1),
    ]

    failed = 0
    for label, surface, expected in cases:
        got = len(scan(surface, arrays, max_distance, min_length))
        if got != expected:
            sys.stderr.write(
                "selftest FAIL: %s — expected %d hit(s), got %d\n" % (label, expected, got))
            failed = 1
        else:
            print("selftest: %s -> %d hit(s)" % (label, got))
    for label, surface, owned, expected in scalar_cases:
        got = len(scan_scalars(surface, [hatch], window, owned))
        if got != expected:
            sys.stderr.write(
                "selftest FAIL: %s — expected %d hit(s), got %d\n" % (label, expected, got))
            failed = 1
        else:
            print("selftest: %s -> %d hit(s)" % (label, got))

    # And the reason matters as much as the code: a fence typo also exits 2, so the fixture
    # is only proof if the message names the constant it could not find.
    code, why = renamed_watch_exits_two(window)
    if code != 2 or "Fixture.HatchMaxHp" not in why:
        sys.stderr.write(
            "selftest FAIL: a renamed watched constant must exit 2 naming the constant; "
            "got exit %d, %r\n" % (code, why))
        failed = 1
    else:
        print("selftest: a renamed watched constant exits 2 -> %s" % why)

    total = len(cases) + len(scalar_cases) + 1
    if failed:
        sys.stderr.write("lint-constants --selftest: FAILED\n")
        return 1
    print("lint-constants --selftest: ok (%d fixtures)" % total)
    return 0


# ---------------------------------------------------------------- main


def main():
    ap = argparse.ArgumentParser(add_help=False)
    ap.add_argument("--root", default=None)
    ap.add_argument("--selftest", action="store_true")
    ap.add_argument("--verbose", action="store_true")
    ap.add_argument("--no-exempt", action="store_true",
                    help="ignore the fence's exempt list (shows what the exemptions hide)")
    args = ap.parse_args()

    root = args.root or os.path.abspath(
        os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))

    keys, watch_specs = read_fence(root)
    min_length = key_int(keys, "min_length")
    max_distance = key_int(keys, "max_distance")
    scalar_window = key_int(keys, "scalar_window")
    must_resolve = key_list(keys, "must_resolve")
    sim_dir = key(keys, "sim_dir")
    doc_dirs = key_list(keys, "doc_dirs")
    code_dirs = key_list(keys, "code_dirs")
    declared_exempt = key_list(keys, "exempt")
    declared_scalar_exempt = key_list(keys, "scalar_exempt")
    exempt = [] if args.no_exempt else declared_exempt
    scalar_exempt = [] if args.no_exempt else declared_scalar_exempt

    if min_length < 3:
        fail("min_length must be at least 3 — two-value arrays match every 'n/m' in prose "
             "(measured: they were the only false positive on the tree)")

    if scalar_window < 4:
        fail("scalar_window must be at least 4 — below that the check cannot see the number "
             "in `Hatch HP is 300`, which is the shape the seeded proof uses, and a window "
             "small enough to silence the tool is a way to gut it without deleting a line")

    if not watch_specs:
        fail("no 'watch=' line in the ```" + FENCE_NAME + " fence in " + FENCE_DOC +
             ". The scalar watch has no default list: an empty watch is a check that "
             "cannot fire, and it must not be reachable by deleting a line.")

    arrays = collect_arrays(root, sim_dir, min_length)
    qualified, unambiguous = collect_scalars(root, sim_dir)
    watch, unresolved = build_watch(watch_specs, qualified, unambiguous, scalar_window)

    # Anti-gutting, scalar half — the same guard `must_resolve` is for arrays, except the
    # watch list is its own must_resolve: every name written into the fence is a name that
    # has to exist. Rename `HatchMaxHp` and this goes exit 2 rather than quietly watching
    # eleven constants instead of twelve.
    if unresolved:
        fail("watched constant(s) %s did not resolve in %s. Renamed, deleted, or written "
             "in a shape this check cannot read — fix the fence or the pattern, but a "
             "watch that resolves to nothing must never be a passing run."
             % (", ".join(unresolved), sim_dir))

    # Anti-gutting. If a tracked array is renamed, deleted, or rewritten into a shape the
    # declaration pattern no longer reads, this check would silently find nothing to compare
    # and pass. The fence names what must resolve, so that failure is loud.
    missing = [n for n in must_resolve if n not in arrays]
    if missing:
        fail("fence key 'must_resolve' names %s, which did not resolve in %s. "
             "Renamed, deleted, or written in a shape this check cannot read — "
             "update the fence or the pattern, but do not let it pass silently."
             % (", ".join(missing), sim_dir))

    if args.selftest:
        sys.exit(selftest(arrays, watch, max_distance, min_length, scalar_window))

    if args.verbose:
        print("source of truth — %d array(s) in %s with >= %d values:"
              % (len(arrays), sim_dir, min_length))
        for name in sorted(arrays):
            vals, path, line = arrays[name]
            print("   %-24s %-22s %s:%d" % (name, render(vals), path, line))
        print("source of truth — %d watched scalar(s) in %s, within %d characters of a "
              "name or an alias:" % (len(watch), sim_dir, scalar_window))
        for w in sorted(watch, key=lambda x: x.qualified):
            print("   %-32s %-10s %-26s aliases: %s"
                  % (w.qualified, scalar_str(w.value), w.where,
                     ", ".join(w.aliases) or "(name only)"))

    # Criterion 7. Say what was not read, on every run and not only a dirty one, because a
    # clean line above an unstated exemption reads as "the tree is right" when it means
    # "the tree minus its history is right".
    if args.no_exempt:
        print("exempt: none — --no-exempt is scanning the history too. A hit inside "
              "Changelog.md or a completed milestone is usually the record doing its job.")
    else:
        print("exempt (not scanned, so not audited): " + ", ".join(declared_exempt))
        print("exempt from the scalar watch on top of that: "
              + ", ".join(declared_scalar_exempt))

    surfaces = list(doc_lines(root, doc_dirs, exempt)) + \
        list(code_comment_lines(root, code_dirs, exempt))
    hits = scan(surfaces, arrays, max_distance, min_length)
    scalar_hits = scan_scalars(
        [s for s in surfaces
         if not any(s[0].startswith(e) for e in scalar_exempt)],
        watch, scalar_window, owned_values(qualified))

    for where, lineno, raw, name, truth, dist in hits:
        print("%s:%d: restated-constant-drift %s — %s is %s in %s (%d value%s differ)"
              % (where, lineno, raw, name, render(truth), arrays[name][1],
                 dist, "" if dist == 1 else "s"))

    for where, lineno, mention, shown, w in scalar_hits:
        print("%s:%d: restated-scalar-drift '%s' says %s — %s is %s in %s"
              % (where, lineno, mention, shown, w.qualified, scalar_str(w.value), w.where))

    print("note: array literals are compared value by value; a watched scalar is compared "
          "only where its name or a declared alias sits beside a number. Unwatched "
          "constants, prose that paraphrases instead of quoting, and a line that happens to "
          "carry the right value beside the wrong claim are all out of reach — a clean run "
          "is not proof the docs are right.")

    total = len(hits) + len(scalar_hits)
    if total:
        sys.stderr.write("lint-constants: %d restated-constant drift(s) (%d array, "
                         "%d scalar)\n" % (total, len(hits), len(scalar_hits)))
        return 1
    print("lint-constants: clean — %d array(s) and %d watched scalar(s) compared"
          % (len(arrays), len(watch)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
