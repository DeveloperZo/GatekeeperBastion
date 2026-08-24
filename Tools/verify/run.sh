#!/usr/bin/env bash
# Gatekeeper's Protocol — EditMode test harness.
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

# M41 — architecture rules. Fail before compile so a violation is not buried under mcs noise.
LINT_ARCH="$ROOT/Tools/lint-arch/run.sh"
if [[ ! -f "$LINT_ARCH" ]]; then
  echo "verify: expected $LINT_ARCH" >&2
  exit 2
fi
if ! bash "$LINT_ARCH"; then
  echo "verify: architecture check failed (Tools/lint-arch/run.sh)" >&2
  exit 1
fi

# Imported vendor packs keep the repairs made inside them. A dump is gitignored and
# re-importable, so an overwrite there is invisible to git; this turns a silent revert
# into a failing gate. Fail before compile, like the other lints.
LINT_VENDOR="$ROOT/Tools/lint-vendor/run.sh"
if [[ ! -f "$LINT_VENDOR" ]]; then
  echo "verify: expected $LINT_VENDOR" >&2
  exit 2
fi
if ! bash "$LINT_VENDOR"; then
  echo "verify: vendor pack check failed (Tools/lint-vendor/run.sh)" >&2
  exit 1
fi

# M41 — one-shot editor work runs from Assets/_Scratch/ and is removed before this
# gate; cleanup is not the scratch script's job. Unity's generated .meta files are
# tolerated so an empty folder does not cry wolf forever.
SCRATCH="$ROOT/Assets/_Scratch"
if [[ -d "$SCRATCH" ]]; then
  SCRATCH_LEFTOVER="$(find "$SCRATCH" -mindepth 1 ! -name '.gitkeep' ! -name '*.meta' -print | sort)"
  if [[ -n "$SCRATCH_LEFTOVER" ]]; then
    echo "verify: Assets/_Scratch/ is not empty — remove scratch work before the gate:" >&2
    printf '%s\n' "$SCRATCH_LEFTOVER" >&2
    exit 1
  fi
fi

# EditMode tests that cannot compile outside Unity. Each needs a reason; a file added
# here without one is coverage quietly dropped.
declare -A EXCLUDED=(
  ["EditModeSaveFixture.cs"]="assembly-level [SetUpFixture]; needs GatekeepersProtocol.Game (SaveService.SaveRoot), UnityEngine.PlayerPrefs and NUnit SetUpFixture"
  ["Dev/DevSessionTests.cs"]="needs GatekeepersProtocol.Game (SaveService) and a real filesystem save"
  ["Dev/DevCheckpointTests.cs"]="needs GameController (MonoBehaviour) for BuildDemoCheckpoint"
  ["Weapons/ReloadSwapTests.cs"]="needs PlayerGun MonoBehaviour, GameObject, and UnityEngine.TestTools.LogAssert"
  ["ControlsTests.cs"]="needs GatekeepersProtocol.Game (Controls) and UnityEngine.Input/KeyCode"
  ["Flow/RealmShareServiceTests.cs"]="needs GatekeepersProtocol.Game (RealmShareService, GameSession) and async Task tests"
  ["Settings/BuildSceneRegistrationTests.cs"]="needs GatekeepersProtocol.EditorTools (SceneBuilder); added to the tree after this list and owed a row since — without it stage 2 does not compile"
  ["Ship/PremiseCopyTests.cs"]="needs GatekeepersProtocol.Game (PremiseCopy) and scene strings"
  ["Ship/RealmRecordViewTests.cs"]="needs RealmRecordView MonoBehaviour, GameSession and UnityEngine GameObjects"
  ["Ship/StationPurposeTests.cs"]="needs GatekeepersProtocol.Game (station Purpose constants)"
  ["Ship/StationPanelDriftTests.cs"]="needs GatekeepersProtocol.Editor StationSceneGate assembly"
  ["Ui/ShortcutRowApiTests.cs"]="needs ShortcutRow MonoBehaviour, TMPro, and UnityEditor"
  ["Ui/UiBindGatingTests.cs"]="needs GatekeepersProtocol.Game, TMPro, and UnityEngine.UI"
  ["Ui/CompendiumFormTests.cs"]="needs GatekeepersProtocol.Game (CompendiumCatalog) and UnityEngine.Application"
  ["Ui/ArmedActionTests.cs"]="needs GatekeepersProtocol.Game (ArmedAction lives in Game/Flow)"
  ["Bastion/BastionCoopSceneTests.cs"]="uses Assert.Inconclusive, which the shim surface deliberately does not carry"
  ["Bastion/BastionDoorApertureTests.cs"]="needs BastionSegment MonoBehaviour, GameObject, and UnityEngine"
  ["Bastion/BastionSegmentDoorAlongTests.cs"]="needs GatekeepersProtocol.Game (BastionSegment.TryDoorPillars)"
  ["Bastion/BastionDoorOpensForTests.cs"]="needs EnemyGatekeeper, CharacterController, and UnityEngine"
  ["Bastion/SpawnRoomPrefabTests.cs"]="needs SpawnRoom prefab load and UnityEngine"
  ["Net/RunSeatsTests.cs"]="needs GatekeepersProtocol.Game (RunSeats) and Unity.Netcode"
  ["Run/RespawnQueueTests.cs"]="needs GatekeepersProtocol.Game (RespawnQueue lives in Game/Range)"
)

if ! command -v mcs >/dev/null 2>&1 || ! command -v mono >/dev/null 2>&1; then
  echo "harness: mcs and mono are required but not on PATH." >&2
  echo "         Debian/Ubuntu: apt-get install -y mono-devel" >&2
  exit 2
fi

SIM_DIR="$ROOT/Assets/Scripts/Sim"
TEST_DIR="$ROOT/Assets/Tests/EditMode"
# Engine-free settings live in Game/ (they are per-player preferences, not Run state) but are
# plain C#, so their tests keep offline coverage by compiling alongside the tests below.
SETTINGS_DIR="$ROOT/Assets/Scripts/Game/Settings"
# Same deal for the demo line runner: Game/Demo/ is engine-free by construction (the view,
# in Game/Ui/, owns every UnityEngine touch), so the runner's queue/preempt/mode rules keep
# offline coverage here.
DEMO_DIR="$ROOT/Assets/Scripts/Game/Demo"

for p in "$SIM_DIR" "$TEST_DIR" "$SETTINGS_DIR" "$DEMO_DIR"; do
  if [[ ! -e "$p" ]]; then
    echo "harness: expected $p — is the repo laid out as this script assumes?" >&2
    exit 2
  fi
done

mkdir -p "$OUT"

# Stage 1 — Sim/ compiles ALONE into GatekeepersProtocol.Sim.dll against the Shims stubs.
# This is the offline enforcement of the Sim boundary: the stubs define the entire allowed
# engine surface (CoreModule value types + Debug), so a MonoBehaviour, scene, Time or ambient
# Random reference inside Sim/ fails HERE, not in a review. Sim/Util/Log.cs ships inside the
# Sim assembly (it is plain C# over Debug), which mirrors the asmdef layout in Unity.
mapfile -t SIM_SOURCES < <(find "$SIM_DIR" -name '*.cs' | sort)
SIM_SOURCES+=("$HERE/Shims.cs")

SIM_DLL="$OUT/GatekeepersProtocol.Sim.dll"
if ! mcs -nologo -langversion:latest -warn:0 -target:library -out:"$SIM_DLL" "${SIM_SOURCES[@]}" 2> "$OUT/compile-sim.log"; then
  echo "SIM COMPILE FAILED — Sim/ referenced something outside its allowed surface" >&2
  grep -E 'error CS' "$OUT/compile-sim.log" | head -40 >&2
  echo "  full output: $OUT/compile-sim.log" >&2
  exit 2
fi

# Stage 2 — tests (plus the engine-free settings and demo-line sources) compile against the
# Sim dll, the same dependency direction the Unity asmdefs enforce. Test code cannot donate
# helpers to Sim.
mapfile -t SOURCES < <(find "$SETTINGS_DIR" "$DEMO_DIR" -name '*.cs' | sort)

SKIPPED=()
while IFS= read -r f; do
  rel="${f#"$TEST_DIR"/}"
  if [[ -n "${EXCLUDED[$rel]:-}" ]]; then
    SKIPPED+=("$rel — ${EXCLUDED[$rel]}")
  else
    SOURCES+=("$f")
  fi
done < <(find "$TEST_DIR" -name '*.cs' | sort)

SOURCES+=("$HERE/Runner.cs")

EXE="$OUT/editmode.exe"

if ! mcs -nologo -langversion:latest -warn:0 -r:"$SIM_DLL" -out:"$EXE" "${SOURCES[@]}" 2> "$OUT/compile.log"; then
  echo "COMPILE FAILED" >&2
  grep -E 'error CS' "$OUT/compile.log" | head -40 >&2
  echo "  full output: $OUT/compile.log" >&2
  exit 2
fi
cp -f "$SIM_DLL" "$(dirname "$EXE")/" 2>/dev/null || true

for s in "${SKIPPED[@]}"; do
  echo "skipped  $s"
done

mono "$EXE" "$@"
test_rc=$?
if [[ $test_rc -ne 0 ]]; then
  exit $test_rc
fi

# M33 — countable docs stay in lockstep with Sim/. Fail after tests so a stale doc is named
# as the cause, not buried under compile noise.
DOCGEN="$ROOT/Tools/docgen/run.sh"
if [[ ! -f "$DOCGEN" ]]; then
  echo "verify: expected $DOCGEN" >&2
  exit 2
fi
if ! bash "$DOCGEN" --check; then
  echo "verify: docgen check failed (Tools/docgen/run.sh --check)" >&2
  exit 1
fi

# M64 — Friends Alpha release-gate machine checks (PlayerSettings / build profile / frame budget).
RELEASE_GATE="$ROOT/Tools/release-gate/run.sh"
if [[ ! -f "$RELEASE_GATE" ]]; then
  echo "verify: expected $RELEASE_GATE" >&2
  exit 2
fi
if ! bash "$RELEASE_GATE"; then
  echo "verify: release-gate check failed (Tools/release-gate/run.sh)" >&2
  exit 1
fi
