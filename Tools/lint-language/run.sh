#!/usr/bin/env bash
# Gatekeeper's Protocol — language table enforcement (M28).
#
# Fails when a retired term reappears, when builder vocabulary leaks into Docs/Play/,
# or when a station name loses its caps in a player-facing string. The lists live in
# Docs/_meta/_definitions/language.md (```language-lint fence); this script only reads.
#
#   Tools/lint-language/run.sh
#
# Exit codes: 0 clean, 1 violations found, 2 could not run.
#
# boarder-for-enemy is collocational, not bare-word. False negatives are expected —
# a clean run is not proof the tree uses Boarder correctly everywhere.

set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/../.." && pwd)"
LANG_FILE="$ROOT/Docs/_meta/_definitions/language.md"

cd "$ROOT" || exit 2

if [[ ! -f "$LANG_FILE" ]]; then
  echo "lint-language: expected Docs/_meta/_definitions/language.md" >&2
  exit 2
fi

if ! command -v rg >/dev/null 2>&1; then
  echo "lint-language: ripgrep (rg) is required but not on PATH." >&2
  exit 2
fi

# Prove --pcre2 works before we hide behind match loops.
if ! rg --pcre2 -q -e 'a' "$LANG_FILE" 2>/dev/null; then
  echo "lint-language: rg --pcre2 is required (word-boundary matching)." >&2
  exit 2
fi

BLOCK="$(awk '
  /^```language-lint[[:space:]]*$/ { grab=1; next }
  /^```[[:space:]]*$/ { if (grab) exit }
  grab { print }
' "$LANG_FILE")"

if [[ -z "$BLOCK" ]]; then
  echo "lint-language: no \`\`\`language-lint fence in Docs/_meta/_definitions/language.md" >&2
  exit 2
fi

get_list() {
  local key="$1"
  local line
  line="$(printf '%s\n' "$BLOCK" | grep -E "^${key}=" | head -1 || true)"
  if [[ -z "$line" ]]; then
    echo "lint-language: missing key '${key}=' in language-lint fence" >&2
    exit 2
  fi
  printf '%s\n' "${line#*=}"
}

IFS=',' read -r -a RETIRED <<< "$(get_list retired)"
IFS=',' read -r -a BUILDER <<< "$(get_list builder_play_ban)"
IFS=',' read -r -a STATIONS <<< "$(get_list stations_title)"
IFS=',' read -r -a BOARDER <<< "$(get_list boarder_enemy)"

trim() {
  local s="$1"
  s="${s#"${s%%[![:space:]]*}"}"
  s="${s%"${s##*[![:space:]]}"}"
  printf '%s' "$s"
}

escape_regex() {
  printf '%s' "$1" | sed -e 's/[.\\^$*+?()[\]{}|]/\\&/g'
}

VIOLATIONS=0
NOTE="note: boarder-for-enemy is collocational; false negatives expected"

is_allowed() {
  local rel="${1//\\//}"
  # Strip a leading ./ if present.
  rel="${rel#./}"
  case "$rel" in
    Docs/_meta/_definitions/language.md) return 0 ;;
    Tools/lint-language/*) return 0 ;;
    Docs/Milestones/3.\ completed/*) return 0 ;;
    Docs/_meta/Logs/*) return 0 ;;
  esac
  case "$rel" in
    *_to_delete/*|*/_to_delete/*|_to_delete/*) return 0 ;;
    *_stage_snap/*|*/_stage_snap/*) return 0 ;;
  esac
  return 1
}

report() {
  local rel="${1//\\//}"
  rel="${rel#./}"
  local line="$2"
  local term="$3"
  if is_allowed "$rel"; then
    return 0
  fi
  printf '%s:%s: %s\n' "$rel" "$line" "$term"
  VIOLATIONS=$((VIOLATIONS + 1))
}

# Globs only — do not pass --path-separator '/' on Git Bash/Windows; bare '/' is rewritten.
RG_COMMON=(
  --glob '!Library/**'
  --glob '!Temp/**'
  --glob '!Logs/**'
  --glob '!.git/**'
  --glob '!**/_to_delete/**'
  --glob '!**/*_stage_snap*/**'
  --glob '!Docs/_meta/Logs/**'
  --glob '!Tools/lint-language/**'
  --glob '!Docs/Milestones/3. completed/**'
)

scan_pcre() {
  local pattern="$1"
  local scope="$2"
  local term_label="$3"
  local hit file rest lineno
  # rg exit 0 = matches, 1 = no matches, 2 = error. Only 2 is fatal.
  local out rc
  out="$(rg -n --no-heading --pcre2 -e "$pattern" "${RG_COMMON[@]}" "$scope" 2>&1)" && rc=0 || rc=$?
  if [[ "$rc" -ge 2 ]]; then
    echo "lint-language: rg failed (exit $rc) for pattern /$pattern/ in $scope:" >&2
    printf '%s\n' "$out" >&2
    exit 2
  fi
  [[ "$rc" -eq 1 || -z "$out" ]] && return 0
  while IFS= read -r hit; do
    [[ -z "$hit" ]] && continue
    file="${hit%%:*}"
    rest="${hit#*:}"
    lineno="${rest%%:*}"
    report "$file" "$lineno" "$term_label"
  done <<< "$out"
}

for raw in "${RETIRED[@]}"; do
  t="$(trim "$raw")"
  [[ -z "$t" ]] && continue
  esc="$(escape_regex "$t")"
  if [[ "$t" == *" "* ]]; then
    scan_pcre "$esc" "." "$t"
  else
    scan_pcre "\\b${esc}\\b" "." "$t"
  fi
done

for raw in "${BOARDER[@]}"; do
  t="$(trim "$raw")"
  [[ -z "$t" ]] && continue
  esc="$(escape_regex "$t")"
  scan_pcre "$esc" "." "$t"
done

if [[ -d Docs/Play ]]; then
  for raw in "${BUILDER[@]}"; do
    t="$(trim "$raw")"
    [[ -z "$t" ]] && continue
    esc="$(escape_regex "$t")"
    scan_pcre "$esc" "Docs/Play" "$t"
  done
fi

for raw in "${STATIONS[@]}"; do
  t="$(trim "$raw")"
  [[ -z "$t" ]] && continue
  esc="$(escape_regex "$t")"
  if [[ -d Assets/Scripts ]]; then
    scan_pcre "\"[^\"\\n]*\\b${esc}\\b[^\"\\n]*\"" "Assets/Scripts" "$t (use caps)"
  fi
  if [[ -d Docs/Play ]]; then
    scan_pcre "\\b${esc}\\b" "Docs/Play" "$t (use caps)"
  fi
done

echo "$NOTE"
if [[ "$VIOLATIONS" -gt 0 ]]; then
  echo "lint-language: $VIOLATIONS violation(s)" >&2
  exit 1
fi
echo "lint-language: clean"
exit 0
