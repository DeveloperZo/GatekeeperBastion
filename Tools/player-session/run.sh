# Gatekeeper's Protocol — same-machine two-player session check (M58 dual-box).
#
# Launches two Windows player builds: host (-gkb-host) then client (-gkb-join 127.0.0.1).
# Passes when the host log shows "host started" and the client log shows "role=client".
#
#   Tools/player-session/run.sh
#   Tools/player-session/run.sh --exe /path/to/GatekeepersProtocol.exe
#   Tools/player-session/run.sh --build   # write gkb_player_build_request.txt and wait
#
# Requires a Development player with PlayerLaunch (-gkb-host / -gkb-join). Default exe:
#   Logs/Builds/GatekeepersProtocol.exe  or  $GKB_PLAYER_EXE
#
# Exit: 0 pass, 1 connect failed, 2 could not run.

set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/../.." && pwd)"
EXE="${GKB_PLAYER_EXE:-}"
DO_BUILD=0
QUIT_AFTER=18
JOIN_WAIT=4
POLL_SECS=25

while [[ $# -gt 0 ]]; do
  case "$1" in
    --exe) EXE="$2"; shift 2 ;;
    --build) DO_BUILD=1; shift ;;
    --quit-after) QUIT_AFTER="$2"; shift 2 ;;
    *) echo "player-session: unknown arg $1" >&2; exit 2 ;;
  esac
done

if [[ "$DO_BUILD" -eq 1 ]]; then
  REPORT="$ROOT/gkb_player_build_report.json"
  REQ="$ROOT/gkb_player_build_request.txt"
  rm -f "$REPORT"
  echo "build $(date -u +%Y-%m-%dT%H:%M:%SZ)" > "$REQ"
  echo "player-session: waiting for Unity to build (gkb_player_build_request.txt)…"
  for _ in $(seq 1 600); do
    if [[ -f "$REPORT" ]]; then
      break
    fi
    sleep 1
  done
  if [[ ! -f "$REPORT" ]]; then
    echo "player-session: timed out waiting for gkb_player_build_report.json" >&2
    exit 2
  fi
  if ! grep -q '"status":"ok"' "$REPORT"; then
    echo "player-session: player build failed:" >&2
    cat "$REPORT" >&2
    exit 2
  fi
  EXE="$ROOT/Logs/Builds/GatekeepersProtocol.exe"
fi

if [[ -z "$EXE" ]]; then
  if [[ -f "$ROOT/Logs/Builds/GatekeepersProtocol.exe" ]]; then
    EXE="$ROOT/Logs/Builds/GatekeepersProtocol.exe"
  elif [[ -f "$ROOT/Logs/build/Gatekeeper's Protocol.exe" ]]; then
    EXE="$ROOT/Logs/build/Gatekeeper's Protocol.exe"
  elif [[ -f "$ROOT/Logs/Builds/Gatekeeper Bastion.exe" ]]; then
    EXE="$ROOT/Logs/Builds/Gatekeeper Bastion.exe"
  elif [[ -f "$ROOT/Logs/build/Gatekeeper Bastion.exe" ]]; then
    EXE="$ROOT/Logs/build/Gatekeeper Bastion.exe"
  fi
fi

if [[ -z "$EXE" || ! -f "$EXE" ]]; then
  echo "player-session: no player exe. Pass --exe, set GKB_PLAYER_EXE, or use --build." >&2
  exit 2
fi

# Player logs: companyName Gatekeeper, productName Gatekeeper's Protocol.
# Try current then legacy product, each under Gatekeeper then DefaultCompany.
LL=""
if [[ -n "${LOCALAPPDATA:-}" ]]; then
  LL="$(cd "${LOCALAPPDATA}/../LocalLow" 2>/dev/null && pwd)" || true
fi
if [[ -z "$LL" ]]; then
  LL="$(cmd.exe /c "echo %USERPROFILE%\\AppData\\LocalLow" 2>/dev/null | tr -d '\r')"
fi
PERSIST_CANDIDATES=()
if [[ -n "$LL" ]]; then
  PERSIST_CANDIDATES=(
    "$LL/Gatekeeper/Gatekeeper's Protocol/Logs"
    "$LL/Gatekeeper/Gatekeeper Bastion/Logs"
    "$LL/DefaultCompany/Gatekeeper's Protocol/Logs"
    "$LL/DefaultCompany/Gatekeeper Bastion/Logs"
  )
fi

MARKER="$ROOT/Logs/player-session-marker.txt"
mkdir -p "$ROOT/Logs"
date -u +%Y-%m-%dT%H:%M:%SZ > "$MARKER"
BEFORE="$(date +%s)"

echo "player-session: host — $EXE -gkb-host -gkb-quit-after $QUIT_AFTER"
"$EXE" -gkb-host -gkb-quit-after "$QUIT_AFTER" -logfile "$ROOT/Logs/player-session-host-unity.log" &
HOST_PID=$!
sleep "$JOIN_WAIT"
echo "player-session: join — $EXE -gkb-join 127.0.0.1 -gkb-quit-after $QUIT_AFTER"
"$EXE" -gkb-join 127.0.0.1 -gkb-quit-after "$QUIT_AFTER" -logfile "$ROOT/Logs/player-session-client-unity.log" &
CLIENT_PID=$!

# Wait for both to exit (quit-after) or poll timeout.
for _ in $(seq 1 "$POLL_SECS"); do
  if ! kill -0 "$HOST_PID" 2>/dev/null && ! kill -0 "$CLIENT_PID" 2>/dev/null; then
    break
  fi
  sleep 1
done
kill "$HOST_PID" "$CLIENT_PID" 2>/dev/null || true
wait "$HOST_PID" 2>/dev/null || true
wait "$CLIENT_PID" 2>/dev/null || true

# Newest session logs after launch (both current and legacy prefixes).
PERSIST=""
for cand in "${PERSIST_CANDIDATES[@]}"; do
  if [[ -d "$cand" ]]; then
    PERSIST="$cand"
    break
  fi
done
mapfile -t NEW_LOGS < <(
  {
    for cand in "${PERSIST_CANDIDATES[@]}"; do
      [[ -d "$cand" ]] || continue
      find "$cand" \( -name 'GatekeepersProtocol_*.log' -o -name 'GatekeeperBastion_*.log' \) -type f -printf '%T@ %p\n' 2>/dev/null
    done
  } | sort -nr | head -6 | awk '{print $2}'
)
if [[ ${#NEW_LOGS[@]} -eq 0 ]]; then
  # Windows find may lack -printf — fall back to ls by mtime via powershell
  mapfile -t NEW_LOGS < <(powershell.exe -NoProfile -Command "
    \$cands = @(
      (Join-Path \$env:USERPROFILE 'AppData\LocalLow\Gatekeeper\Gatekeeper''s Protocol\Logs'),
      (Join-Path \$env:USERPROFILE 'AppData\LocalLow\Gatekeeper\Gatekeeper Bastion\Logs'),
      (Join-Path \$env:USERPROFILE 'AppData\LocalLow\DefaultCompany\Gatekeeper''s Protocol\Logs'),
      (Join-Path \$env:USERPROFILE 'AppData\LocalLow\DefaultCompany\Gatekeeper Bastion\Logs')
    )
    \$logs = foreach (\$dir in \$cands) {
      if (-not (Test-Path -LiteralPath \$dir)) { continue }
      Get-ChildItem -LiteralPath \$dir -Filter 'GatekeepersProtocol_*.log' -ErrorAction SilentlyContinue
      Get-ChildItem -LiteralPath \$dir -Filter 'GatekeeperBastion_*.log' -ErrorAction SilentlyContinue
    }
    @(\$logs) | Sort-Object LastWriteTime -Descending | Select-Object -First 6 -ExpandProperty FullName
  " | tr -d '\r')
fi
if [[ ${#NEW_LOGS[@]} -eq 0 && ( -z "$PERSIST" || ! -d "$PERSIST" ) ]]; then
  echo "player-session: player log dir not found (tried Gatekeeper/DefaultCompany × Gatekeeper's Protocol/Gatekeeper Bastion)" >&2
  exit 2
fi

HOST_HIT=""
CLIENT_HIT=""
for f in "${NEW_LOGS[@]}"; do
  [[ -f "$f" ]] || continue
  if grep -q '\[Net\] host started' "$f" 2>/dev/null; then
    HOST_HIT="$f"
  fi
  if grep -q '\[Net\] connected .* role=client' "$f" 2>/dev/null \
     || grep -q 'role=client' "$f" 2>/dev/null; then
    CLIENT_HIT="$f"
  fi
done

echo "player-session: host log  = ${HOST_HIT:-NONE}"
echo "player-session: client log = ${CLIENT_HIT:-NONE}"

if [[ -n "$HOST_HIT" && -n "$CLIENT_HIT" ]]; then
  echo "player-session: PASS — host started and client connected (same-machine 127.0.0.1)"
  grep -E '\[Net\] (host started|connected|client starting|join refused)' "$HOST_HIT" "$CLIENT_HIT" || true
  exit 0
fi

echo "player-session: FAIL — need host started + client role=client in persistentDataPath logs" >&2
for f in "${NEW_LOGS[@]}"; do
  echo "---- $f ----" >&2
  grep -E '\[Net\]|\[Launch\]' "$f" 2>/dev/null | head -20 >&2 || true
done
exit 1
