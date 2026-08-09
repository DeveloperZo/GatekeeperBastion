#!/usr/bin/env bash
# Gatekeeper Bastion — Field Manual generator.
#
# Regenerates Docs/Play/Field-Manual.md by calling the live code: the binding list, the
# weapon catalog, the wave curve, the dial and the upgrade tracks. Run it after changing
# any of those, and the player-facing tables cannot drift from the game.
#
#   Tools/docgen/run.sh              write Docs/Play/Field-Manual.md
#   Tools/docgen/run.sh --check      exit 1 if the committed file is out of date (no write)
#
# Requires mono (mcs + mono on PATH), same as Tools/verify. Shares Tools/verify/Shims.cs.
# Exit codes: 0 fine, 1 --check found drift, 2 could not run.

set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/../.." && pwd)"
OUT="${GKB_DOCGEN_OUT:-${TMPDIR:-/tmp}/gkb-docgen}"
TARGET="$ROOT/Docs/Play/Field-Manual.md"

if ! command -v mcs >/dev/null 2>&1 || ! command -v mono >/dev/null 2>&1; then
  echo "docgen: mcs and mono are required but not on PATH." >&2
  echo "        Debian/Ubuntu: apt-get install -y mono-devel" >&2
  exit 2
fi

SIM_DIR="$ROOT/Assets/Scripts/Sim"
CONTROLS="$ROOT/Assets/Scripts/Game/Input/Controls.cs"
LOG_CS="$ROOT/Assets/Scripts/Util/Log.cs"
SHIMS="$ROOT/Tools/verify/Shims.cs"

for p in "$SIM_DIR" "$CONTROLS" "$LOG_CS" "$SHIMS"; do
  if [[ ! -e "$p" ]]; then
    echo "docgen: expected $p — is the repo laid out as this script assumes?" >&2
    exit 2
  fi
done

mkdir -p "$OUT"
EXE="$OUT/docgen.exe"

# Controls.cs is C# 9 (Unity 6); mcs is a C# 7 compiler and cannot parse target-typed
# `new(...)`. Rewrite that one construct into a temp copy — the repo file is never touched.
# If a `new(` survives the rewrite, Controls.cs has started using target-typed new for
# something other than a ControlBinding and this shim needs widening; say so and stop
# rather than compiling a file that silently differs from the one the game builds.
CONTROLS_TMP="$OUT/Controls.compat.cs"
sed 's/new(ControlId\./new ControlBinding(ControlId./g' "$CONTROLS" > "$CONTROLS_TMP"
if grep -qE '(^|[^[:alnum:]_])new\(' "$CONTROLS_TMP"; then
  echo "docgen: Controls.cs uses target-typed new() for something this shim does not rewrite:" >&2
  grep -nE '(^|[^[:alnum:]_])new\(' "$CONTROLS_TMP" | head -5 >&2
  echo "        Widen the sed in Tools/docgen/run.sh, or spell the type out in Controls.cs." >&2
  exit 2
fi

mapfile -t SOURCES < <(find "$SIM_DIR" -name '*.cs' | sort)
SOURCES+=("$LOG_CS" "$CONTROLS_TMP" "$SHIMS" "$HERE/InputShims.cs" "$HERE/Program.cs")

if ! mcs -nologo -langversion:latest -warn:0 -out:"$EXE" "${SOURCES[@]}" 2> "$OUT/compile.log"; then
  echo "COMPILE FAILED" >&2
  grep -E 'error CS' "$OUT/compile.log" | head -40 >&2
  echo "  full output: $OUT/compile.log" >&2
  echo "  a new KeyCode in Controls.cs may need a row in Tools/docgen/InputShims.cs." >&2
  exit 2
fi

if [[ "${1:-}" == "--check" ]]; then
  TMPOUT="$OUT/Field-Manual.md"
  mono "$EXE" "$TMPOUT" >/dev/null || exit 2
  if [[ ! -f "$TARGET" ]]; then
    echo "docgen --check: $TARGET does not exist. Run Tools/docgen/run.sh." >&2
    exit 1
  fi
  if ! diff -q "$TARGET" "$TMPOUT" >/dev/null; then
    echo "docgen --check: Docs/Play/Field-Manual.md is stale." >&2
    diff -u "$TARGET" "$TMPOUT" | head -40 >&2
    echo "  Run Tools/docgen/run.sh to regenerate." >&2
    exit 1
  fi
  echo "docgen --check: Field-Manual.md matches the build."
  exit 0
fi

mono "$EXE" "$TARGET"
