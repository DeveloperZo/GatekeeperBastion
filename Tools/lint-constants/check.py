#!/usr/bin/env python3
"""Gatekeeper's Protocol — restated-constant drift check.

Docgen owns the DERIVED half of a countable document: everything between GKB:GEN and
GKB:END is regenerated from Sim/, so `Tools/docgen/run.sh --check` can prove it matches.
It says nothing about the hand-written half — the prose beside a generated table, and the
XML comment sitting three lines below the array it describes. Both restate a number whose
source is Sim/, and both drifted from `CloakerPerWave` without anything going red.

This check covers that half, for one narrow and mechanical case: a NUMERIC ARRAY LITERAL,
restated outside its declaration, that nearly matches a live Sim/ array. Near, not equal,
is the whole signal — a restatement that agrees is silent, and a list of unrelated numbers
is too far away to match. See Docs/Working-Rules.md, "Restated constants", for the rule and
the ```constant-lint fence this script reads. It carries no rule value of its own: a missing
fence or a missing key is exit 2, never a default.

  Tools/lint-constants/run.sh              scan the tree
  Tools/lint-constants/run.sh --selftest   fixtures; exit 0 when each is caught
  Tools/lint-constants/run.sh --verbose    also print the resolved source-of-truth table

Exit codes: 0 clean, 1 drift found, 2 could not run.
"""

import argparse
import os
import re
import sys

# ---------------------------------------------------------------- fence

FENCE_DOC = "Docs/Working-Rules.md"
FENCE_NAME = "constant-lint"


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
    keys = {}
    for line in body:
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        if "=" not in line:
            fail("fence line is not key=value in " + FENCE_DOC + ": " + line)
        k, v = line.split("=", 1)
        keys[k.strip()] = v.strip()
    return keys


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


def render(vals):
    out = []
    for v in vals:
        out.append(str(int(v)) if v == int(v) else str(v))
    return "{" + ",".join(out) + "}"


# ---------------------------------------------------------------- selftest


def selftest(arrays, max_distance, min_length):
    """A gutted check catches nothing. These fixtures prove this one still fires,
    and — just as important — that it stays silent on a restatement that agrees."""
    if not arrays:
        fail("selftest: no Sim arrays resolved, nothing to build fixtures from")
    name = sorted(arrays)[0]
    truth = arrays[name][0]
    drifted = list(truth)
    drifted[-1] = drifted[-1] + 1  # one value wrong: the Cloaker shape

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
    failed = 0
    for label, surface, expected in cases:
        got = len(scan(surface, arrays, max_distance, min_length))
        if got != expected:
            sys.stderr.write(
                "selftest FAIL: %s — expected %d hit(s), got %d\n" % (label, expected, got))
            failed = 1
        else:
            print("selftest: %s -> %d hit(s)" % (label, got))
    if failed:
        sys.stderr.write("lint-constants --selftest: FAILED\n")
        return 1
    print("lint-constants --selftest: ok (%d fixtures)" % len(cases))
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

    keys = read_fence(root)
    min_length = key_int(keys, "min_length")
    max_distance = key_int(keys, "max_distance")
    must_resolve = key_list(keys, "must_resolve")
    sim_dir = key(keys, "sim_dir")
    doc_dirs = key_list(keys, "doc_dirs")
    code_dirs = key_list(keys, "code_dirs")
    exempt = [] if args.no_exempt else key_list(keys, "exempt")

    if min_length < 3:
        fail("min_length must be at least 3 — two-value arrays match every 'n/m' in prose "
             "(measured: they were the only false positive on the tree)")

    arrays = collect_arrays(root, sim_dir, min_length)

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
        sys.exit(selftest(arrays, max_distance, min_length))

    if args.verbose:
        print("source of truth — %d array(s) in %s with >= %d values:"
              % (len(arrays), sim_dir, min_length))
        for name in sorted(arrays):
            vals, path, line = arrays[name]
            print("   %-24s %-22s %s:%d" % (name, render(vals), path, line))

    surfaces = list(doc_lines(root, doc_dirs, exempt)) + \
        list(code_comment_lines(root, code_dirs, exempt))
    hits = scan(surfaces, arrays, max_distance, min_length)

    for where, lineno, raw, name, truth, dist in hits:
        print("%s:%d: restated-constant-drift %s — %s is %s in %s (%d value%s differ)"
              % (where, lineno, raw, name, render(truth), arrays[name][1],
                 dist, "" if dist == 1 else "s"))

    print("note: only numeric array literals are compared. Prose that paraphrases a "
          "constant in words is out of reach — a clean run is not proof the docs are right.")

    if hits:
        sys.stderr.write("lint-constants: %d restated-constant drift(s)\n" % len(hits))
        return 1
    print("lint-constants: clean")
    return 0


if __name__ == "__main__":
    sys.exit(main())
