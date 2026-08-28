#!/usr/bin/env bash
# Gatekeeper's Protocol — offline loop cost model.
#
# What one zone COSTS the player: time, bodies, hit points. Reads the live constants
# out of Assets/Scripts/Sim/ (plus the authored geometry in Assets/Scenes/) and prints
# a beat-by-beat account of a zone, calibrated against a real playthrough log.
#
#   Tools/loop-model/run.sh                                     zone 1, uncalibrated
#   Tools/loop-model/run.sh --zone 1 --calibrate Logs/<log>     zone 1, calibrated
#   Tools/loop-model/run.sh --zone 2 --start-from 20 ...        where the walk starts
#   Tools/loop-model/run.sh --selftest                          a missing const is exit 2
#
# THIS IS NOT A COMBAT SIMULATOR. The kill rate is an INPUT measured from a log, not a
# computed dps: the gap between a weapon's ceiling and what a real player achieves is
# the largest term in the whole thing and no arithmetic recovers it. Every number the
# model prints carries the file:line it was read from, so a stale one is visible.
#
# Python 3 rather than mcs/mono (Tools/verify, Tools/docgen) on purpose: this reads C#
# as text and never compiles it, so it does not need the toolchain those two do — and
# mono is not installed here. Tools/lint-arch/run.sh is the same shape: a checker that
# reads the sources rather than building them.
#
# Exit codes: 0 fine, 2 a constant could not be read (never a default).

set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/../.." && pwd)"

PY=""
for candidate in python3 python py; do
  if command -v "$candidate" >/dev/null 2>&1; then
    if "$candidate" -c 'import sys; sys.exit(0 if sys.version_info[0] == 3 else 1)' 2>/dev/null; then
      PY="$candidate"
      break
    fi
  fi
done

if [[ -z "$PY" ]]; then
  echo "loop-model: Python 3 is required but not on PATH." >&2
  exit 2
fi

if [[ ! -f "$HERE/model.py" ]]; then
  echo "loop-model: expected $HERE/model.py" >&2
  exit 2
fi

# Resolve a repo-relative log path so `--calibrate Logs/foo.log` works from anywhere.
ARGS=()
EXPECT_PATH=0
for a in "$@"; do
  if [[ "$EXPECT_PATH" -eq 1 ]]; then
    if [[ ! -e "$a" && -e "$ROOT/$a" ]]; then
      ARGS+=("$ROOT/$a")
    else
      ARGS+=("$a")
    fi
    EXPECT_PATH=0
    continue
  fi
  [[ "$a" == "--calibrate" ]] && EXPECT_PATH=1
  ARGS+=("$a")
done

exec "$PY" "$HERE/model.py" "${ARGS[@]}"
