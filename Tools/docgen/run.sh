#!/usr/bin/env bash
# Gatekeeper Bastion — doc generator.
#
# Regenerates Docs/Play/Field-Manual.md (whole file) and splices countable blocks into
# Docs/Design/4-Data.md from the live Sim/ code.
#
#   Tools/docgen/run.sh                         write both targets
#   Tools/docgen/run.sh field-manual|data|all   write one or both (default all)
#   Tools/docgen/run.sh --check [target]        exit 1 if stale (no write)
#   Tools/docgen/run.sh --selftest              structural-fault fixtures; exit 0 when each yields 2
#
# Requires mono (mcs + mono on PATH), same as Tools/verify. Shares Tools/verify/Shims.cs.
# Exit codes: 0 fine, 1 --check found drift, 2 could not run / structural fault.

set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/../.." && pwd)"
OUT="${GKB_DOCGEN_OUT:-${TMPDIR:-/tmp}/gkb-docgen}"
FM_TARGET="$ROOT/Docs/Play/Field-Manual.md"
DATA_TARGET="$ROOT/Docs/Design/4-Data.md"

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

CHECK=0
SELFTEST=0
TARGET=all

while [[ $# -gt 0 ]]; do
  case "$1" in
    --check) CHECK=1; shift ;;
    --selftest) SELFTEST=1; shift ;;
    field-manual|data|all) TARGET="$1"; shift ;;
    *)
      echo "docgen: unknown arg $1" >&2
      exit 2
      ;;
  esac
done

if [[ "$SELFTEST" -eq 1 ]]; then
  FIX="$OUT/selftest"
  mkdir -p "$FIX"
  FAIL=0

  # 1. unclosed GKB:GEN
  cat > "$FIX/unclosed.md" <<'EOF'
# t
<!-- GKB:GEN mk1-base — WeaponCatalog -->
body
EOF
  if mono "$EXE" --data "$FIX/unclosed.md" >"$FIX/unclosed.out" 2>"$FIX/unclosed.err"; then
    echo "selftest FAIL: unclosed GEN expected exit 2, got 0" >&2; FAIL=1
  else
    rc=$?
    if [[ $rc -ne 2 ]] || ! grep -q 'unclosed' "$FIX/unclosed.err"; then
      echo "selftest FAIL: unclosed GEN (rc=$rc)" >&2; cat "$FIX/unclosed.err" >&2; FAIL=1
    else
      echo "selftest: unclosed GKB:GEN → exit 2"
    fi
  fi

  # 2. duplicate block id
  cat > "$FIX/dup.md" <<'EOF'
# t
<!-- GKB:GEN mk1-base — WeaponCatalog -->
a
<!-- GKB:END mk1-base -->
<!-- GKB:GEN mk1-base — WeaponCatalog -->
b
<!-- GKB:END mk1-base -->
EOF
  # Need all known blocks for emitter-with-no-block to not fire first; inject stubs for others
  # after duplicate detection — duplicate should fire during ExtractBodies before emitter check.
  if mono "$EXE" --data "$FIX/dup.md" >"$FIX/dup.out" 2>"$FIX/dup.err"; then
    echo "selftest FAIL: duplicate id expected exit 2, got 0" >&2; FAIL=1
  else
    rc=$?
    if [[ $rc -ne 2 ]] || ! grep -qi 'duplicate' "$FIX/dup.err"; then
      echo "selftest FAIL: duplicate id (rc=$rc)" >&2; cat "$FIX/dup.err" >&2; FAIL=1
    else
      echo "selftest: duplicate block id → exit 2"
    fi
  fi

  # 3. block id with no emitter
  cat > "$FIX/noemit.md" <<'EOF'
# t
<!-- GKB:GEN not-a-real-block — Nowhere -->
x
<!-- GKB:END not-a-real-block -->
EOF
  if mono "$EXE" --data "$FIX/noemit.md" >"$FIX/noemit.out" 2>"$FIX/noemit.err"; then
    echo "selftest FAIL: no-emitter expected exit 2, got 0" >&2; FAIL=1
  else
    rc=$?
    if [[ $rc -ne 2 ]] || ! grep -qi 'no emitter' "$FIX/noemit.err"; then
      echo "selftest FAIL: no-emitter (rc=$rc)" >&2; cat "$FIX/noemit.err" >&2; FAIL=1
    else
      echo "selftest: block id with no emitter → exit 2"
    fi
  fi

  # 4. emitter with no block in the file (empty markers set)
  cat > "$FIX/noblock.md" <<'EOF'
# t

No markers at all.
EOF
  if mono "$EXE" --data "$FIX/noblock.md" >"$FIX/noblock.out" 2>"$FIX/noblock.err"; then
    echo "selftest FAIL: emitter-no-block expected exit 2, got 0" >&2; FAIL=1
  else
    rc=$?
    if [[ $rc -ne 2 ]] || ! grep -qi 'has no block' "$FIX/noblock.err"; then
      echo "selftest FAIL: emitter-no-block (rc=$rc)" >&2; cat "$FIX/noblock.err" >&2; FAIL=1
    else
      echo "selftest: emitter with no block → exit 2"
    fi
  fi

  if [[ "$FAIL" -ne 0 ]]; then
    echo "docgen --selftest: FAILED" >&2
    exit 1
  fi
  echo "docgen --selftest: ok (four faults → exit 2)"
  exit 0
fi

ARGS=()
[[ "$CHECK" -eq 1 ]] && ARGS+=(--check)

case "$TARGET" in
  field-manual)
    ARGS+=(--field-manual "$FM_TARGET")
    ;;
  data)
    ARGS+=(--data "$DATA_TARGET")
    ;;
  all)
    ARGS+=(--field-manual "$FM_TARGET" --data "$DATA_TARGET")
    ;;
esac

mono "$EXE" "${ARGS[@]}"
rc=$?
exit $rc
