#!/usr/bin/env bash
# Gatekeeper's Protocol — restated-constant drift check.
#
# `Tools/docgen/run.sh --check` proves the GENERATED half of a countable document matches
# Sim/. It cannot see the hand-written half. On 2026-08-26 `CloakerPerWave` drifted in three
# hand-written places at once — the prose in Docs/Design/4-Data.md, the formula block beside
# it, and WaveDirector's own XML comment three lines under the array — while the generated
# table in the same file stayed correct and --check stayed green.
#
# This covers one narrow case of that: a numeric array literal restated outside its
# declaration that NEARLY matches a live Sim/ array. The rule values live in the
# ```constant-lint fence in Docs/Working-Rules.md; this script and check.py only read them.
#
#   Tools/lint-constants/run.sh              scan the tree
#   Tools/lint-constants/run.sh --selftest   fixtures; proves the check still fires
#   Tools/lint-constants/run.sh --verbose    also print the resolved source-of-truth table
#   Tools/lint-constants/run.sh --no-exempt  show what the fence's exempt list hides
#
# Requires python3 (>= 3.8) on PATH. No mono, no Unity — it is text over the tree.
# Exit codes: 0 clean, 1 drift found, 2 could not run.

set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/../.." && pwd)"
CHECK="$HERE/check.py"

if [[ ! -f "$CHECK" ]]; then
  echo "lint-constants: expected $CHECK" >&2
  exit 2
fi

PY=""
for c in python3 python; do
  if command -v "$c" >/dev/null 2>&1; then
    if "$c" -c 'import sys; sys.exit(0 if sys.version_info >= (3, 8) else 1)' 2>/dev/null; then
      PY="$c"
      break
    fi
  fi
done

if [[ -z "$PY" ]]; then
  echo "lint-constants: python3 (>= 3.8) is required but not on PATH." >&2
  echo "                Debian/Ubuntu: apt-get install -y python3" >&2
  exit 2
fi

PYTHONIOENCODING=utf-8 "$PY" "$CHECK" --root "$ROOT" "$@"
exit $?
