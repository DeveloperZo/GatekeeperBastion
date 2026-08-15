#!/usr/bin/env bash
# Temp wrapper: put Mono on PATH (PowerShell arg-quoting mangles inline exports).
export PATH="/c/Program Files/Mono/bin:$PATH"
cd "$(dirname "$0")"
exec ./Tools/verify/run.sh "$@"
