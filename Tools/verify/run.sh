#!/usr/bin/env bash
# Gatekeeper Bastion — EditMode test harness.
#
# Compiles Sim/, Util/Log.cs and the EditMode tests outside Unity with mcs, runs them
# with mono, and reports pass/fail in seconds. This is verification path 1 in
# Docs/Working-Rules.md: the fast signal on any sim or tuning change. It is not a
# substitute for the Unity Test Runner — anything touching MonoBehaviours, scenes or
# the save file is excluded below and only Unity can check it.
#
#   Tools/verify/run.sh                  run everything
#   Tools/verify/run.sh --verbose        also print passing test names and game log lines
#   Tools/verify/run.sh --filter Weapon  run only tests whose Fixture.Method contains "Weapon"
#
# Requires mono (mcs + mono on PATH). Debian/Ubuntu: apt-get install -y mono-devel.
# Exit codes: 0 all passed, 1 a test failed, 2 the harness could not run.

set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/../.." && pwd)"
OUT="${GKB_HARNESS_OUT:-${TMPDIR:-/tmp}/gkb-harness}"

# M28 — language table. Fail before compile so a retired term is not buried under mcs noise.
LINT_LANG="$ROOT/Tools/lint-language/run.sh"
if [[ ! -f "$LINT_LANG" ]]; then
  echo "verify: expected $LINT_LANG" >&2
  exit 2
fi
if ! bash "$LINT_LANG"; then
  echo "verify: language check failed (Tools/lint-language/run.sh)" >&2
  exit 1
fi

# EditMode tests that cannot compile outside Unity. Each needs a reason; a file added
# here without one is coverage quietly dropped.
declare -A EXCLUDED=(
  ["Dev/DevSessionTests.cs"]="needs GatekeeperBastion.Game (SaveService) and a real filesystem save"
  ["Weapons/ReloadSwapTests.cs"]="needs PlayerGun MonoBehaviour, GameObject, and UnityEngine.TestTools.LogAssert"
  ["ControlsTests.cs"]="needs GatekeeperBastion.Game (Controls) and UnityEngine.Input/KeyCode"
)

if ! command -v mcs >/dev/null 2>&1 || ! command -v mono >/dev/null 2>&1; then
  echo "harness: mcs and mono are required but not on PATH." >&2
  echo "         Debian/Ubuntu: apt-get install -y mono-devel" >&2
  exit 2
fi

SIM_DIR="$ROOT/Assets/Scripts/Sim"
TEST_DIR="$ROOT/Assets/Tests/EditMode"
LOG_CS="$ROOT/Assets/Scripts/Util/Log.cs"

for p in "$SIM_DIR" "$TEST_DIR" "$LOG_CS"; do
  if [[ ! -e "$p" ]]; then
    echo "harness: expected $p — is the repo laid out as this script assumes?" >&2
    exit 2
  fi
done

mapfile -t SOURCES < <(find "$SIM_DIR" -name '*.cs' | sort)
SOURCES+=("$LOG_CS")

SKIPPED=()
while IFS= read -r f; do
  rel="${f#"$TEST_DIR"/}"
  if [[ -n "${EXCLUDED[$rel]:-}" ]]; then
    SKIPPED+=("$rel — ${EXCLUDED[$rel]}")
  else
    SOURCES+=("$f")
  fi
done < <(find "$TEST_DIR" -name '*.cs' | sort)

SOURCES+=("$HERE/Shims.cs" "$HERE/Runner.cs")

mkdir -p "$OUT"
EXE="$OUT/editmode.exe"

if ! mcs -nologo -langversion:latest -warn:0 -out:"$EXE" "${SOURCES[@]}" 2> "$OUT/compile.log"; then
  echo "COMPILE FAILED" >&2
  grep -E 'error CS' "$OUT/compile.log" | head -40 >&2
  echo "  full output: $OUT/compile.log" >&2
  exit 2
fi

for s in "${SKIPPED[@]}"; do
  echo "skipped  $s"
done

mono "$EXE" "$@"
