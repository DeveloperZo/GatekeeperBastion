#!/usr/bin/env bash
# Thin wrapper — Windows-native entry is run.ps1.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
if command -v pwsh >/dev/null 2>&1; then
  exec pwsh -NoProfile -File "$ROOT/Tools/dashboard-data/run.ps1"
fi
if command -v powershell >/dev/null 2>&1; then
  exec powershell -NoProfile -File "$ROOT/Tools/dashboard-data/run.ps1"
fi
echo "dashboard-data: need pwsh or powershell" >&2
exit 1
