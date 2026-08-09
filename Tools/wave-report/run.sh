#!/usr/bin/env bash
# Gatekeeper Bastion — mean live Gatekeepers per Wave, offline from session logs.
#
#   Tools/wave-report/run.sh <log>...
#   Tools/wave-report/run.sh --summary <log>...
#   Tools/wave-report/run.sh --csv <log>...
#   Tools/wave-report/run.sh --html <out.html> <log>...
#
# Interactive page (parses logs in-browser): Docs/dashboard.html
#
# Requires mono (mcs + mono on PATH), same as Tools/verify and Tools/docgen.
# Exit codes: 0 fine, 1 named parse failure on a log, 2 could not run.

set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/../.." && pwd)"
OUT="${GKB_WAVE_REPORT_OUT:-${TMPDIR:-/tmp}/gkb-wave-report}"

if ! command -v mcs >/dev/null 2>&1 || ! command -v mono >/dev/null 2>&1; then
  echo "wave-report: mcs and mono are required but not on PATH." >&2
  echo "             Debian/Ubuntu: apt-get install -y mono-devel" >&2
  exit 2
fi

if [[ ! -f "$HERE/Program.cs" ]]; then
  echo "wave-report: expected $HERE/Program.cs" >&2
  exit 2
fi

mkdir -p "$OUT"
EXE="$OUT/wave-report.exe"

if ! mcs -nologo -langversion:latest -warn:0 -out:"$EXE" "$HERE/Program.cs" 2> "$OUT/compile.log"; then
  echo "COMPILE FAILED" >&2
  grep -E 'error CS' "$OUT/compile.log" | head -40 >&2
  echo "  full output: $OUT/compile.log" >&2
  exit 2
fi

# Resolve log paths relative to the repo root when the caller passes a repo-relative path.
# --html's output path must stay literal (may not exist yet).
ARGS=()
EXPECT_HTML_OUT=0
for a in "$@"; do
  if [[ "$EXPECT_HTML_OUT" -eq 1 ]]; then
    ARGS+=("$a")
    EXPECT_HTML_OUT=0
    continue
  fi
  if [[ "$a" == "--html" ]]; then
    EXPECT_HTML_OUT=1
    ARGS+=("$a")
    continue
  fi
  if [[ "$a" == --* ]]; then
    ARGS+=("$a")
    continue
  fi
  if [[ "$a" != *.html && -e "$ROOT/$a" && ! -e "$a" ]]; then
    ARGS+=("$ROOT/$a")
  else
    ARGS+=("$a")
  fi
done

mono "$EXE" "${ARGS[@]}"
