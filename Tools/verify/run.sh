#!/usr/bin/env bash
# Gatekeeper's Protocol — EditMode test harness.
#
# Compiles Sim/, Util/Log.cs and the EditMode tests outside Unity and runs them, reporting
# pass/fail in seconds. This is verification path 1 in
# Docs/Working-Rules.md: the fast signal on any sim or tuning change. It is not a
# substitute for the Unity Test Runner — anything touching MonoBehaviours, scenes or
# the save file is excluded below and only Unity can check it.
#
#   Tools/verify/run.sh                  run everything
#   Tools/verify/run.sh --verbose        also print passing test names and game log lines
#   Tools/verify/run.sh --filter Weapon  run only tests whose Fixture.Method contains "Weapon"
#
# Two toolchains are supported and whichever is installed is used, mono first:
#   * mono      — mcs to compile, mono to run. Debian/Ubuntu: apt-get install -y mono-devel.
#   * .NET SDK  — the Roslyn csc the SDK ships, run through `dotnet exec`. The SDK, runtime
#                 and reference pack are discovered at run time; nothing is hardcoded.
# Both take the same two compile stages and produce the same output; only the compiler and
# the launcher differ. Exit codes: 0 all passed, 1 a test failed, 2 the harness could not run.

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

# Glyph coverage. A character no shipping font atlas can draw compiles, tests green, and
# is a blank or a box on screen — four have shipped, and nothing else in the tree looks.
# The allowed set is derived from the font assets, not listed here. Fail before compile,
# like the other lints, so the codepoint is named rather than buried under mcs noise.
LINT_GLYPHS="$ROOT/Tools/lint-glyphs/run.sh"
if [[ ! -f "$LINT_GLYPHS" ]]; then
  echo "verify: expected $LINT_GLYPHS" >&2
  exit 2
fi
if ! bash "$LINT_GLYPHS"; then
  echo "verify: glyph check failed (Tools/lint-glyphs/run.sh)" >&2
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

# The hand-written counterpart to the `Tools/docgen/run.sh --check` further down: docgen
# proves the GENERATED blocks of a countable doc, this proves the prose and comments that
# restate the same arrays. Both halves of Docs/Design/4-Data.md drifted on 2026-08-26 and
# only the generated half went red. Runs up here with the other fence-reading lints because
# it is text over the tree — it needs python3, not mono, so it still reports on a machine
# where the harness itself cannot start.
LINT_CONST="$ROOT/Tools/lint-constants/run.sh"
if [[ ! -f "$LINT_CONST" ]]; then
  echo "verify: expected $LINT_CONST" >&2
  exit 2
fi
if ! bash "$LINT_CONST"; then
  echo "verify: restated-constant check failed (Tools/lint-constants/run.sh)" >&2
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
  ["Dev/CheckpointConfigTaintTests.cs"]="needs GameController (MonoBehaviour) for BuildDemoCheckpoint and the config-taint rule"
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
  ["Bastion/BastionCoopSceneTests.cs"]="uses Assert.Inconclusive; the shim carries Assert.Ignore but not Inconclusive"
  ["Bastion/BastionDoorApertureTests.cs"]="needs BastionSegment MonoBehaviour, GameObject, and UnityEngine"
  ["Bastion/BastionSegmentDoorAlongTests.cs"]="needs GatekeepersProtocol.Game (BastionSegment.TryDoorPillars)"
  ["Bastion/BastionDoorOpensForTests.cs"]="needs EnemyGatekeeper, CharacterController, and UnityEngine"
  ["Bastion/GatekeeperTargetingTests.cs"]="needs EnemyGatekeeper (MonoBehaviour) and UnityEngine; GatekeeperRoot's close/swing split is covered offline in GatekeeperRootTests"
  ["Bastion/PylonMeshTests.cs"]="needs UnityEngine.Mesh and GatekeepersProtocol.Game (PylonMesh); nothing offline covers the pylon's geometry, so the shared-instance and vertex/triangle counts are Unity Test Runner only"
  ["Bastion/SpawnRoomPrefabTests.cs"]="needs SpawnRoom prefab load and UnityEngine"
  ["Net/RunSeatsTests.cs"]="needs GatekeepersProtocol.Game (RunSeats) and Unity.Netcode"
  ["Run/RespawnQueueTests.cs"]="needs GatekeepersProtocol.Game (RespawnQueue lives in Game/Range)"
  ["Ui/HudVisibilityMatrixTests.cs"]="needs BastionHudView MonoBehaviour, TMPro and UnityEngine.UI; ProtocolHudSourceTests keeps the same rule offline"
  ["Ui/RealmSelectCoopMarkTests.cs"]="needs GatekeepersProtocol.Game (RealmRegistry); added to the tree after this list, same debt as the row above it"
)

# Non-test sources under SETTINGS_DIR / DEMO_DIR that stopped being engine-free. Those two
# folders are compiled wholesale on the claim that everything in them is plain C#; when a
# file breaks that claim it needs a reason here for the same reason the table above does —
# otherwise it is coverage quietly dropped. Keyed on the path relative to Assets/Scripts/.
declare -A EXCLUDED_GAME=(
  ["Game/Demo/DemoTeach.cs"]="a MonoBehaviour over GameController and Interactor; the folder's engine-free claim no longer holds for it, and only Unity can cover it"
)

# Toolchain. mono is tried first so a machine that has always compiled this with mcs keeps
# doing so; the .NET SDK is the alternative for machines without mono. Everything below goes
# through compile_lib / compile_exe / run_tests, so the two stages and the runner invocation
# are identical whichever compiler answers.
TOOLCHAIN=""
if command -v mcs >/dev/null 2>&1 && command -v mono >/dev/null 2>&1; then
  TOOLCHAIN="mono"
elif command -v dotnet >/dev/null 2>&1; then
  TOOLCHAIN="dotnet"
else
  echo "harness: no usable toolchain on PATH — need either mcs+mono or the .NET SDK." >&2
  echo "         mono, Debian/Ubuntu: apt-get install -y mono-devel" >&2
  echo "         .NET SDK: https://dotnet.microsoft.com/download" >&2
  exit 2
fi

# Windows bash (Git Bash / Cygwin / MSYS) hands POSIX paths to a native dotnet that cannot
# read them, and paths inside a csc response file are never converted for us. Everything
# written into a response file or passed to dotnet goes through this.
native_path() {
  if command -v cygpath >/dev/null 2>&1; then cygpath -w "$1"; else printf '%s' "$1"; fi
}

if [[ "$TOOLCHAIN" == "dotnet" ]]; then
  # Resolve the SDK (for csc), the shared runtime (for runtimeconfig.json) and the matching
  # reference pack (for the compile-time framework surface). All three come from `dotnet`
  # itself so this tracks whatever is installed rather than a version pinned in this file.
  DOTNET_HOME="$(cd "$(dirname "$(command -v dotnet)")" && pwd)"

  CSC_DLL=""
  while IFS= read -r sdk; do
    [[ -z "$sdk" ]] && continue
    if [[ -f "$DOTNET_HOME/sdk/$sdk/Roslyn/bincore/csc.dll" ]]; then
      CSC_DLL="$DOTNET_HOME/sdk/$sdk/Roslyn/bincore/csc.dll"
      break
    fi
  done < <(dotnet --list-sdks 2>/dev/null | sed -n 's/^\([^ ]*\) .*/\1/p' | sort -Vr)

  if [[ -z "$CSC_DLL" ]]; then
    echo "harness: dotnet is on PATH but no SDK ships Roslyn/bincore/csc.dll." >&2
    echo "         SDKs found: $(dotnet --list-sdks 2>/dev/null | tr '\n' ' ')" >&2
    echo "         The .NET *runtime* alone cannot compile; install the SDK." >&2
    exit 2
  fi

  # Pair the newest installed Microsoft.NETCore.App runtime with a reference pack of the same
  # major.minor. Walking down means a runtime with no matching ref pack is skipped rather than
  # failing the run.
  CSC_REFDIR=""
  RUNTIME_VERSION=""
  RUNTIME_TFM=""
  while IFS= read -r rt; do
    [[ -z "$rt" ]] && continue
    majmin="${rt%.*}"
    for pack in $(ls -1 "$DOTNET_HOME/packs/Microsoft.NETCore.App.Ref" 2>/dev/null | sort -Vr); do
      [[ "$pack" == "$majmin".* ]] || continue
      if [[ -d "$DOTNET_HOME/packs/Microsoft.NETCore.App.Ref/$pack/ref/net$majmin" ]]; then
        CSC_REFDIR="$DOTNET_HOME/packs/Microsoft.NETCore.App.Ref/$pack/ref/net$majmin"
        RUNTIME_VERSION="$rt"
        RUNTIME_TFM="net$majmin"
        break
      fi
    done
    [[ -n "$CSC_REFDIR" ]] && break
  done < <(dotnet --list-runtimes 2>/dev/null | sed -n 's/^Microsoft\.NETCore\.App \([^ ]*\) .*/\1/p' | sort -Vr)

  if [[ -z "$CSC_REFDIR" ]]; then
    echo "harness: found csc at $CSC_DLL but no Microsoft.NETCore.App runtime paired with a" >&2
    echo "         reference pack under $DOTNET_HOME/packs/Microsoft.NETCore.App.Ref." >&2
    echo "         Runtimes: $(dotnet --list-runtimes 2>/dev/null | sed -n 's/^Microsoft\.NETCore\.App \([^ ]*\) .*/\1/p' | tr '\n' ' ')" >&2
    exit 2
  fi
fi

# Compile one assembly. $1 target (library|exe), $2 output path, $3 extra reference ("" for
# none), $4 log path, rest sources. Both compilers write the whole diagnostic stream to the
# log (Roslyn reports on stdout, mcs on stderr) so the `error CS` grep below sees it either way.
compile_assembly() {
  local target="$1" out="$2" extra="$3" log="$4"
  shift 4
  if [[ "$TOOLCHAIN" == "mono" ]]; then
    local args=(-nologo -langversion:latest -warn:0 "-target:$target" "-out:$out")
    [[ -n "$extra" ]] && args+=("-r:$extra")
    mcs "${args[@]}" "$@" > "$log" 2>&1
    return $?
  fi

  # A response file, not a command line: 160-odd reference assemblies plus 150-odd sources
  # overruns the Windows command-line limit.
  local rsp="$OUT/$(basename "$out").rsp"
  {
    echo "-nologo"
    echo "-langversion:latest"
    echo "-warn:0"
    echo "-nostdlib+"
    echo "-target:$target"
    echo "-out:\"$(native_path "$out")\""
    local d
    for d in "$CSC_REFDIR"/*.dll; do echo "-r:\"$(native_path "$d")\""; done
    [[ -n "$extra" ]] && echo "-r:\"$(native_path "$extra")\""
    local s
    for s in "$@"; do echo "\"$(native_path "$s")\""; done
  } > "$rsp"
  dotnet "$(native_path "$CSC_DLL")" "@$(native_path "$rsp")" > "$log" 2>&1
  return $?
}

# Run the compiled test assembly, passing this script's own arguments through unchanged.
run_tests() {
  local exe="$1"
  shift
  if [[ "$TOOLCHAIN" == "mono" ]]; then
    mono "$exe" "$@"
    return $?
  fi
  # A bare csc-produced assembly has no runtimeconfig.json and `dotnet exec` refuses to start
  # it. Emit one beside the assembly, pinned to the runtime actually resolved above.
  cat > "${exe%.dll}.runtimeconfig.json" <<EOF
{
  "runtimeOptions": {
    "tfm": "$RUNTIME_TFM",
    "framework": {
      "name": "Microsoft.NETCore.App",
      "version": "$RUNTIME_VERSION"
    },
    "rollForward": "latestPatch"
  }
}
EOF
  dotnet exec "$(native_path "$exe")" "$@"
  return $?
}

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
if ! compile_assembly library "$SIM_DLL" "" "$OUT/compile-sim.log" "${SIM_SOURCES[@]}"; then
  echo "SIM COMPILE FAILED — Sim/ referenced something outside its allowed surface" >&2
  grep -E 'error CS' "$OUT/compile-sim.log" | head -40 >&2
  echo "  full output: $OUT/compile-sim.log" >&2
  exit 2
fi

# Stage 2 — tests (plus the engine-free settings and demo-line sources) compile against the
# Sim dll, the same dependency direction the Unity asmdefs enforce. Test code cannot donate
# helpers to Sim.
SOURCES=()
SKIPPED=()
while IFS= read -r f; do
  rel="${f#"$ROOT"/Assets/Scripts/}"
  if [[ -n "${EXCLUDED_GAME[$rel]:-}" ]]; then
    SKIPPED+=("$rel — ${EXCLUDED_GAME[$rel]}")
  else
    SOURCES+=("$f")
  fi
done < <(find "$SETTINGS_DIR" "$DEMO_DIR" -name '*.cs' | sort)

while IFS= read -r f; do
  rel="${f#"$TEST_DIR"/}"
  if [[ -n "${EXCLUDED[$rel]:-}" ]]; then
    SKIPPED+=("$rel — ${EXCLUDED[$rel]}")
  else
    SOURCES+=("$f")
  fi
done < <(find "$TEST_DIR" -name '*.cs' | sort)

SOURCES+=("$HERE/Runner.cs")

# .exe under mono, .dll under dotnet: `dotnet exec` wants a managed assembly and finds its
# runtimeconfig by stripping the extension.
if [[ "$TOOLCHAIN" == "mono" ]]; then EXE="$OUT/editmode.exe"; else EXE="$OUT/editmode.dll"; fi

if ! compile_assembly exe "$EXE" "$SIM_DLL" "$OUT/compile.log" "${SOURCES[@]}"; then
  echo "COMPILE FAILED" >&2
  grep -E 'error CS' "$OUT/compile.log" | head -40 >&2
  echo "  full output: $OUT/compile.log" >&2
  exit 2
fi
cp -f "$SIM_DLL" "$(dirname "$EXE")/" 2>/dev/null || true

for s in "${SKIPPED[@]}"; do
  echo "skipped  $s"
done

run_tests "$EXE" "$@"
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
