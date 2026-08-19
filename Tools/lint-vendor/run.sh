#!/usr/bin/env bash
# Gatekeeper's Protocol — a bought pack's Shader Graph keeps the target this project renders with.
#
# The FaxLab3D robot pack ships a Shader Graph authored for three pipelines, Built-In
# among them, so it needs no conversion here. The copy that reached the project had
# been stripped to the URP target alone: every robot rendered magenta, the cause read
# as "wrong pipeline build of the pack", and 24 materials were rebuilt onto Built-In
# Standard before anyone compared the on-disk graph with the one in the package.
# A vendor dump is gitignored and re-importable, so nothing in git showed the loss.
#
# This check reads every value from the ```vendor-lint fence in the document that
# states the rule (Docs/Decisions.md) and carries none of its own — a missing fence
# or a missing key is exit 2, never a default.
#
#   Tools/lint-vendor/run.sh
#
# Exit codes: 0 clean, 1 target missing, 2 could not run.
#
# An absent pack is clean, not a failure: the dump is ignored, so a fresh clone has
# no pack to check until it is imported.

set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/../.." && pwd)"
RULE_DOC="Docs/Decisions.md"

cd "$ROOT" || exit 2

if [[ ! -f "$RULE_DOC" ]]; then
  echo "lint-vendor: expected $RULE_DOC" >&2
  exit 2
fi

FENCE="$(awk '/^```vendor-lint$/{f=1;next} f&&/^```$/{exit} f' "$RULE_DOC")"
if [[ -z "$FENCE" ]]; then
  echo "lint-vendor: no \`\`\`vendor-lint fence in $RULE_DOC — the rule is the document," >&2
  echo "             so a deleted fence is a failure, not a pass." >&2
  exit 2
fi

value_of() {
  local key="$1" line
  line="$(printf '%s\n' "$FENCE" | grep -m1 "^${key}=" || true)"
  if [[ -z "$line" ]]; then
    echo "lint-vendor: $RULE_DOC vendor-lint fence has no '$key='" >&2
    exit 2
  fi
  printf '%s' "${line#*=}"
}

PACK="$(value_of pack)" || exit 2
SHADER_REL="$(value_of shader)" || exit 2
REQUIRED_TARGET="$(value_of required_target)" || exit 2

if [[ ! -d "$PACK" ]]; then
  echo "lint-vendor: $PACK not imported — nothing to check."
  exit 0
fi

SHADER="$PACK/$SHADER_REL"
if [[ ! -f "$SHADER" ]]; then
  echo "lint-vendor: $SHADER is missing, but $PACK is imported." >&2
  echo "             The pack's materials reference it; without it they render magenta." >&2
  exit 1
fi

if ! grep -q "$REQUIRED_TARGET" "$SHADER"; then
  echo "lint-vendor: $SHADER_REL has lost its Built-In target." >&2
  echo "             Wanted: $REQUIRED_TARGET" >&2
  echo "             Found:  $(grep -o 'UnityEditor\.Rendering\.[A-Za-z0-9.]*Target' "$SHADER" | sort -u | tr '\n' ' ')" >&2
  echo "             This project is Built-In, so every material on this graph renders magenta." >&2
  echo "             Restore the file from the pack's .unitypackage; see the vendor-dump" >&2
  echo "             decision in $RULE_DOC." >&2
  exit 1
fi

echo "lint-vendor: clean — $(basename "$SHADER_REL") keeps its Built-In target."
exit 0
