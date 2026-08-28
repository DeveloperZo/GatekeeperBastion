#!/usr/bin/env bash
# Gatekeeper's Protocol — TextMeshPro glyph coverage check.
#
# Fails when user-facing text asks a shipped font atlas for a character it does not have.
# Such a character draws as a blank or a box; nothing else in the tree notices, because
# it compiles and it tests green. Four have shipped.
#
# The allowed characters are NOT listed anywhere. Coverage is derived every run from the
# font assets themselves — EditorBuildSettings decides which scenes ship, the scenes
# decide which fonts are bound, each font asset's m_CharacterTable says what is baked,
# and a DYNAMIC asset's source .ttf cmap says what it can still add at runtime. Change
# the font and the check changes with it. The scan scope and the ASCII substitution
# table live in Docs/_meta/_definitions/glyphs.md; a missing fence is exit 2, never a
# pass.
#
#   Tools/lint-glyphs/run.sh              check the tree
#   Tools/lint-glyphs/run.sh --coverage   print the derived coverage and narrow surface
#
# Requires python3 (3.8+). No third-party packages.
# Exit codes: 0 clean, 1 violations found, 2 could not run.

set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/../.." && pwd)"

PY=""
for c in python3 python py; do
  if command -v "$c" >/dev/null 2>&1; then
    if "$c" -c 'import sys; sys.exit(0 if sys.version_info >= (3, 8) else 1)' \
        >/dev/null 2>&1; then
      PY="$c"
      break
    fi
  fi
done

if [[ -z "$PY" ]]; then
  echo "lint-glyphs: python 3.8+ is required but not on PATH." >&2
  exit 2
fi

# PYTHONIOENCODING: the report names codepoints as U+XXXX and never prints the offending
# character, so a cp1252 console cannot choke on it — but the font asset paths are read
# from disk and the guard costs nothing.
PYTHONIOENCODING="${PYTHONIOENCODING:-utf-8}" \
  "$PY" "$HERE/lint_glyphs.py" --root "$ROOT" "$@"
