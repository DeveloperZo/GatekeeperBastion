#!/usr/bin/env bash
# Gatekeeper Bastion — Friends Alpha release-gate machine checks (M64).
#
# Reads PlayerSettings, the Windows build profile, FrameBudget / DevCommands source,
# and ProjectVersion.txt. Fails (exit 1) on any automatable red item.
#
#   Tools/release-gate/run.sh
#   Tools/release-gate/run.sh --expect-fail   exit 0 only when a check fails (seed proof)
#
# Exit codes: 0 pass (or expected fail under --expect-fail), 1 fail, 2 could not run.
# Optional Unity dump: drop gkb_release_gate_request.txt → gkb_release_gate.json
# (ReleaseGateDump.cs). This script does not require that JSON.

set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/../.." && pwd)"
EXPECT_FAIL=0
for arg in "$@"; do
  case "$arg" in
    --expect-fail) EXPECT_FAIL=1 ;;
    -h|--help)
      echo "Usage: Tools/release-gate/run.sh [--expect-fail]"
      exit 0
      ;;
    *)
      echo "release-gate: unknown argument: $arg" >&2
      exit 2
      ;;
  esac
done

cd "$ROOT" || exit 2

PS="$ROOT/ProjectSettings/ProjectSettings.asset"
PROFILE="$ROOT/Assets/Settings/Build Profiles/Windows.asset"
VERSION_TXT="$ROOT/ProjectSettings/ProjectVersion.txt"
DEV_CMDS="$ROOT/Assets/Scripts/Game/Dev/DevCommands.cs"
SCRIPTS="$ROOT/Assets/Scripts"

for f in "$PS" "$PROFILE" "$VERSION_TXT" "$DEV_CMDS"; do
  if [[ ! -f "$f" ]]; then
    echo "release-gate: expected $f" >&2
    exit 2
  fi
done

if ! command -v rg >/dev/null 2>&1; then
  echo "release-gate: ripgrep (rg) is required but not on PATH." >&2
  exit 2
fi

FAILS=()
NOTES=()

fail() { FAILS+=("$1"); }

yaml_scalar() {
  # First matching "  key: value" line; strips trailing CR.
  local file="$1" key="$2"
  rg -n --no-heading -e "^[[:space:]]*${key}:" "$file" | head -1 | sed -E "s/^[^:]+:[[:space:]]*${key}:[[:space:]]*//; s/\r$//"
}

# --- company / product / version ---
company="$(yaml_scalar "$PS" companyName)"
product="$(yaml_scalar "$PS" productName)"
bundle="$(yaml_scalar "$PS" bundleVersion)"

if [[ -z "$company" || "$company" == "DefaultCompany" ]]; then
  fail "companyName=${company:-<empty>}"
fi
if [[ -z "$product" ]]; then
  fail "productName=<empty>"
fi
if [[ -z "$bundle" ]]; then
  fail "bundleVersion=<empty>"
fi

# --- display / focus ---
fullscreen="$(yaml_scalar "$PS" fullscreenMode)"
native="$(yaml_scalar "$PS" defaultIsNativeResolution)"
resizable="$(yaml_scalar "$PS" resizableWindow)"
allow_fs="$(yaml_scalar "$PS" allowFullscreenSwitch)"
bg="$(yaml_scalar "$PS" runInBackground)"

[[ "$fullscreen" == "1" ]] || fail "fullscreenMode=${fullscreen:-<missing>} (want 1=FullscreenWindow)"
[[ "$native" == "1" ]] || fail "defaultIsNativeResolution=${native:-<missing>} (want 1)"
[[ "$resizable" == "1" ]] || fail "resizableWindow=${resizable:-<missing>} (want 1)"
[[ "$allow_fs" == "1" ]] || fail "allowFullscreenSwitch=${allow_fs:-<missing>} (want 1)"
[[ "$bg" == "1" ]] || fail "runInBackground=${bg:-<missing>} (want 1)"

# --- graphics APIs: empty = Auto = pass; else first API must be D3D11 (enum 2) ---
apis_line="$(rg -n --no-heading -e '^[[:space:]]*m_BuildTargetGraphicsAPIs:' "$PS" | head -1 || true)"
if [[ -z "$apis_line" ]]; then
  fail "m_BuildTargetGraphicsAPIs=<missing>"
else
  apis_val="$(printf '%s\n' "$apis_line" | sed -E 's/^[^:]+:[[:space:]]*m_BuildTargetGraphicsAPIs:[[:space:]]*//; s/\r$//')"
  if [[ "$apis_val" != "[]" && -n "$apis_val" ]]; then
    # Multi-line list: first m_APIs hex (little-endian). D3D11 = GraphicsDeviceType 2 → 02…
    first_api="$(rg -A 40 -e '^[[:space:]]*m_BuildTargetGraphicsAPIs:' "$PS" \
      | rg -m1 -e 'm_APIs:' \
      | sed -E 's/.*m_APIs:[[:space:]]*//; s/\r$//' || true)"
    if [[ -z "$first_api" ]]; then
      fail "m_BuildTargetGraphicsAPIs non-empty but no m_APIs found"
    elif [[ ! "$first_api" =~ ^02 ]]; then
      fail "m_BuildTargetGraphicsAPIs first API=${first_api} (want D3D11=02…)"
    fi
  fi
fi

# --- build profile: development / profiler / deep / debugging off ---
for key in m_Development m_ConnectProfiler m_BuildWithDeepProfilingSupport m_AllowDebugging; do
  val="$(yaml_scalar "$PROFILE" "$key")"
  [[ "$val" == "0" ]] || fail "BuildProfiles/Windows.${key}=${val:-<missing>} (want 0)"
done

# --- frame budget ---
if ! rg -q -e 'targetFrameRate[[:space:]]*=[[:space:]]*60|OutsideFightTarget[[:space:]]*=[[:space:]]*60' "$SCRIPTS"; then
  fail "targetFrameRate/OutsideFightTarget=60 missing under Assets/Scripts"
fi

# --- DevCommands compiled out of release ---
if ! rg -q -e '^#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD' "$DEV_CMDS"; then
  fail "DevCommands.cs missing UNITY_EDITOR || DEVELOPMENT_BUILD wrap"
fi
if ! rg -q -e '^#endif' "$DEV_CMDS"; then
  fail "DevCommands.cs missing #endif for editor/dev wrap"
fi

# --- editor version (CVE note; do not fail solely on version unless clearly ancient) ---
editor_ver="$(rg -e '^m_EditorVersion:' "$VERSION_TXT" | head -1 | sed -E 's/^m_EditorVersion:[[:space:]]*//; s/\r$//' || true)"
if [[ -z "$editor_ver" ]]; then
  NOTES+=("ProjectVersion.txt: m_EditorVersion missing — check CVE-2025-59489 / Application Patcher by hand")
elif [[ "$editor_ver" =~ ^[0-5]\. ]]; then
  fail "editorVersion=${editor_ver} (clearly ancient; patch for CVE-2025-59489)"
else
  NOTES+=("editorVersion=${editor_ver} — confirm patched for CVE-2025-59489 (human item 4)")
fi

# --- optional: run-windowed template at repo root ---
if [[ ! -f "$ROOT/run-windowed.bat" ]]; then
  NOTES+=("run-windowed.bat missing at repo root (must ship beside exe — human item 17)" )
fi

# --- report ---
if ((${#NOTES[@]})); then
  for n in "${NOTES[@]}"; do
    echo "NOTE  $n"
  done
fi

gate_failed=0
if ((${#FAILS[@]})); then
  gate_failed=1
  for f in "${FAILS[@]}"; do
    echo "FAIL  $f"
  done
fi

if [[ "$EXPECT_FAIL" -eq 1 ]]; then
  if [[ "$gate_failed" -eq 1 ]]; then
    echo "PASS  --expect-fail (caught ${#FAILS[@]} failure(s))"
    exit 0
  fi
  echo "FAIL  --expect-fail but all machine checks passed"
  exit 1
fi

if [[ "$gate_failed" -eq 1 ]]; then
  echo "FAIL  release-gate (${#FAILS[@]} item(s))"
  exit 1
fi

echo "PASS  release-gate (company=$company product=$product version=$bundle)"
exit 0
