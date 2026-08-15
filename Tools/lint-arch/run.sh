#!/usr/bin/env bash
# Gatekeeper Bastion — architecture rules enforcement (M41).
#
# Reads every rule value from the ```arch-lint fences in the document that states the
# rule: folder line caps and the over-cap baseline from Docs/Architecture.md, the
# banned-construct rows from Docs/Architecture/3-Unity-Runtime.md section 9, and the
# named singleton lists from Docs/Architecture/README.md. This script carries no rule
# value of its own — a missing fence or a missing key is exit 2, never a default.
#
#   Tools/lint-arch/run.sh
#
# Exit codes: 0 clean, 1 violations found, 2 could not run.
#
# A row is enforced (any hit past its fence count fails) or baselined (hits print as
# NOTE lines; only exceeding the fence's count fails). The scans are textual — a
# construct inside a string or comment counts; see 3-Unity-Runtime.md section 9.

set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/../.." && pwd)"
CAPS_DOC="Docs/Architecture.md"
RUNTIME_DOC="Docs/Architecture/3-Unity-Runtime.md"
SINGLETON_DOC="Docs/Architecture/README.md"
SCRIPTS_DIR="Assets/Scripts"

cd "$ROOT" || exit 2

for f in "$CAPS_DOC" "$RUNTIME_DOC" "$SINGLETON_DOC"; do
  if [[ ! -f "$f" ]]; then
    echo "lint-arch: expected $ROOT/$f" >&2
    exit 2
  fi
done
if [[ ! -d "$SCRIPTS_DIR" ]]; then
  echo "lint-arch: expected $ROOT/$SCRIPTS_DIR" >&2
  exit 2
fi

if ! command -v rg >/dev/null 2>&1; then
  echo "lint-arch: ripgrep (rg) is required but not on PATH." >&2
  exit 2
fi

# Prove --pcre2 works before we hide behind match loops.
if ! rg --pcre2 -q -e 'a' "$CAPS_DOC" 2>/dev/null; then
  echo "lint-arch: rg --pcre2 is required (word-boundary matching)." >&2
  exit 2
fi

fence_block() {
  awk '
    /^```arch-lint[[:space:]]*$/ { grab=1; next }
    /^```[[:space:]]*$/ { if (grab) exit }
    grab { print }
  ' "$1"
}

CAPS_BLOCK="$(fence_block "$CAPS_DOC")"
if [[ -z "$CAPS_BLOCK" ]]; then
  echo "lint-arch: missing fence — no \`\`\`arch-lint fence in $CAPS_DOC" >&2
  exit 2
fi
RUNTIME_BLOCK="$(fence_block "$RUNTIME_DOC")"
if [[ -z "$RUNTIME_BLOCK" ]]; then
  echo "lint-arch: missing fence — no \`\`\`arch-lint fence in $RUNTIME_DOC" >&2
  exit 2
fi
SINGLETON_BLOCK="$(fence_block "$SINGLETON_DOC")"
if [[ -z "$SINGLETON_BLOCK" ]]; then
  echo "lint-arch: missing fence — no \`\`\`arch-lint fence in $SINGLETON_DOC" >&2
  exit 2
fi

get_key() {
  local block="$1" key="$2" src="$3"
  local line
  line="$(printf '%s\n' "$block" | grep -E "^${key}=" | head -1 || true)"
  if [[ -z "$line" ]]; then
    echo "lint-arch: missing fence key '${key}=' in $src" >&2
    return 2
  fi
  printf '%s\n' "${line#*=}"
}

require_num() {
  if [[ ! "$2" =~ ^[0-9]+$ ]]; then
    echo "lint-arch: fence key '$1' is not a number: '$2'" >&2
    exit 2
  fi
}

trim() {
  local s="$1"
  s="${s#"${s%%[![:space:]]*}"}"
  s="${s%"${s##*[![:space:]]}"}"
  printf '%s' "$s"
}

SIM_CAP="$(get_key "$CAPS_BLOCK" sim_cap "$CAPS_DOC")" || exit 2
GAME_CAP="$(get_key "$CAPS_BLOCK" game_cap "$CAPS_DOC")" || exit 2
EDITOR_CAP="$(get_key "$CAPS_BLOCK" editor_cap "$CAPS_DOC")" || exit 2
OVER_BASELINE="$(get_key "$CAPS_BLOCK" over_cap_baseline "$CAPS_DOC")" || exit 2
OVER_KNOWN_RAW="$(get_key "$CAPS_BLOCK" over_cap_known "$CAPS_DOC")" || exit 2
require_num sim_cap "$SIM_CAP"
require_num game_cap "$GAME_CAP"
require_num editor_cap "$EDITOR_CAP"
require_num over_cap_baseline "$OVER_BASELINE"
IFS=',' read -r -a OVER_KNOWN <<< "$OVER_KNOWN_RAW"

FAKENULL_PAT="$(get_key "$RUNTIME_BLOCK" fakenull_pattern "$RUNTIME_DOC")" || exit 2
FAKENULL_BASELINE="$(get_key "$RUNTIME_BLOCK" fakenull_file_baseline "$RUNTIME_DOC")" || exit 2
LINQ_PAT="$(get_key "$RUNTIME_BLOCK" linq_pattern "$RUNTIME_DOC")" || exit 2
LINQ_MAX="$(get_key "$RUNTIME_BLOCK" linq_enforced "$RUNTIME_DOC")" || exit 2
WFS_PAT="$(get_key "$RUNTIME_BLOCK" waitforseconds_pattern "$RUNTIME_DOC")" || exit 2
WFS_BASELINE="$(get_key "$RUNTIME_BLOCK" waitforseconds_baseline "$RUNTIME_DOC")" || exit 2
WFS_NONLOOP_RAW="$(get_key "$RUNTIME_BLOCK" waitforseconds_nonloop_allow "$RUNTIME_DOC")" || exit 2
FOT_PAT="$(get_key "$RUNTIME_BLOCK" findobjectoftype_pattern "$RUNTIME_DOC")" || exit 2
FOT_MAX="$(get_key "$RUNTIME_BLOCK" findobjectoftype_enforced "$RUNTIME_DOC")" || exit 2
EMPTYBODY_PAT="$(get_key "$RUNTIME_BLOCK" emptybody_pattern "$RUNTIME_DOC")" || exit 2
EMPTYBODY_MAX="$(get_key "$RUNTIME_BLOCK" emptybody_enforced "$RUNTIME_DOC")" || exit 2
MIDID_PAT="$(get_key "$RUNTIME_BLOCK" midid_pattern "$RUNTIME_DOC")" || exit 2
MIDID_NAME_PAT="$(get_key "$RUNTIME_BLOCK" midid_name_pattern "$RUNTIME_DOC")" || exit 2
MIDID_MAX="$(get_key "$RUNTIME_BLOCK" midid_enforced "$RUNTIME_DOC")" || exit 2
MIDTEXT_PAT="$(get_key "$RUNTIME_BLOCK" midtext_pattern "$RUNTIME_DOC")" || exit 2
MIDTEXT_LINE_BASELINE="$(get_key "$RUNTIME_BLOCK" midtext_line_baseline "$RUNTIME_DOC")" || exit 2
MIDTEXT_FILE_BASELINE="$(get_key "$RUNTIME_BLOCK" midtext_file_baseline "$RUNTIME_DOC")" || exit 2
IOL_PAT="$(get_key "$RUNTIME_BLOCK" initializeonload_pattern "$RUNTIME_DOC")" || exit 2
IOL_ALLOW_RAW="$(get_key "$RUNTIME_BLOCK" initializeonload_allow "$RUNTIME_DOC")" || exit 2
SCENELOOKUP_PAT="$(get_key "$RUNTIME_BLOCK" scenelookup_pattern "$RUNTIME_DOC")" || exit 2
SCENELOOKUP_ALLOW_RAW="$(get_key "$RUNTIME_BLOCK" scenelookup_allow "$RUNTIME_DOC")" || exit 2
AMBINPUT_PAT="$(get_key "$RUNTIME_BLOCK" ambientinput_pattern "$RUNTIME_DOC")" || exit 2
AMBINPUT_ALLOW_RAW="$(get_key "$RUNTIME_BLOCK" ambientinput_allow "$RUNTIME_DOC")" || exit 2
AMBRANDOM_PAT="$(get_key "$RUNTIME_BLOCK" ambientrandom_pattern "$RUNTIME_DOC")" || exit 2
AMBRANDOM_ALLOW_RAW="$(get_key "$RUNTIME_BLOCK" ambientrandom_allow "$RUNTIME_DOC")" || exit 2
SELFDELETE_PAT="$(get_key "$RUNTIME_BLOCK" selfdelete_pattern "$RUNTIME_DOC")" || exit 2
SELFDELETE_MAX="$(get_key "$RUNTIME_BLOCK" selfdelete_enforced "$RUNTIME_DOC")" || exit 2
NONSOURCE_MAX="$(get_key "$RUNTIME_BLOCK" nonsource_enforced "$RUNTIME_DOC")" || exit 2
require_num fakenull_file_baseline "$FAKENULL_BASELINE"
require_num linq_enforced "$LINQ_MAX"
require_num waitforseconds_baseline "$WFS_BASELINE"
require_num findobjectoftype_enforced "$FOT_MAX"
require_num emptybody_enforced "$EMPTYBODY_MAX"
require_num midid_enforced "$MIDID_MAX"
require_num midtext_line_baseline "$MIDTEXT_LINE_BASELINE"
require_num midtext_file_baseline "$MIDTEXT_FILE_BASELINE"
require_num selfdelete_enforced "$SELFDELETE_MAX"
require_num nonsource_enforced "$NONSOURCE_MAX"
IFS=',' read -r -a WFS_NONLOOP <<< "$WFS_NONLOOP_RAW"
IFS=',' read -r -a IOL_ALLOW <<< "$IOL_ALLOW_RAW"
IFS=',' read -r -a SCENELOOKUP_ALLOW <<< "$SCENELOOKUP_ALLOW_RAW"
IFS=',' read -r -a AMBINPUT_ALLOW <<< "$AMBINPUT_ALLOW_RAW"
IFS=',' read -r -a AMBRANDOM_ALLOW <<< "$AMBRANDOM_ALLOW_RAW"

SINGLETON_ALLOW_RAW="$(get_key "$SINGLETON_BLOCK" singleton_allow "$SINGLETON_DOC")" || exit 2
SINGLETON_OWED_RAW="$(get_key "$SINGLETON_BLOCK" singleton_owed "$SINGLETON_DOC")" || exit 2
MENU_ROOT="$(trim "$(get_key "$SINGLETON_BLOCK" menu_root "$SINGLETON_DOC")")" || exit 2
MENU_ROWS_RAW="$(get_key "$SINGLETON_BLOCK" menu_rows "$SINGLETON_DOC")" || exit 2
IFS=',' read -r -a SINGLETON_ALLOW <<< "$SINGLETON_ALLOW_RAW"
IFS=',' read -r -a SINGLETON_OWED <<< "$SINGLETON_OWED_RAW"
IFS=',' read -r -a MENU_ROWS <<< "$MENU_ROWS_RAW"
if [[ -z "$MENU_ROOT" ]]; then
  echo "lint-arch: menu_root is empty in $SINGLETON_DOC" >&2
  exit 2
fi
for i in "${!MENU_ROWS[@]}"; do
  MENU_ROWS[$i]="$(trim "${MENU_ROWS[$i]}")"
done
if (( ${#MENU_ROWS[@]} == 0 )) || [[ -z "${MENU_ROWS[0]}" ]]; then
  echo "lint-arch: menu_rows is empty in $SINGLETON_DOC" >&2
  exit 2
fi

VIOLATIONS=0

# The counted-line unit is defined in Docs/Architecture/7-File-Layout.md; this awk
# implements it. The cap numbers come from the fence, never from here. String and
# char literals are walked before comment markers so a "/*" inside a literal cannot
# open a phantom comment span and swallow the lines after it. Verbatim strings that
# span lines are the one heuristic edge left: each of their lines counts as code.
counted_lines() {
  awk -v q="'" '
    BEGIN { inblock = 0; total = 0 }
    {
      line = $0
      if (inblock) {
        p = index(line, "*/")
        if (p == 0) next
        line = substr(line, p + 2)
        inblock = 0
      }
      out = ""
      i = 1
      len = length(line)
      instr = 0; verb = 0; inchr = 0
      while (i <= len) {
        c = substr(line, i, 1)
        d = substr(line, i, 2)
        if (instr) {
          if (verb) {
            if (d == "\"\"") { i += 2; continue }
            if (c == "\"") { instr = 0; verb = 0 }
          } else {
            if (c == "\\") { i += 2; continue }
            if (c == "\"") instr = 0
          }
          i++
          continue
        }
        if (inchr) {
          if (c == "\\") { i += 2; continue }
          if (c == q) inchr = 0
          i++
          continue
        }
        if (d == "//") break
        if (d == "/*") {
          p = index(substr(line, i + 2), "*/")
          if (p == 0) { inblock = 1; break }
          i = i + 2 + p + 1
          continue
        }
        if (c == "\"") {
          instr = 1
          if (i > 1 && substr(line, i - 1, 1) == "@") verb = 1
          out = out c
          i++
          continue
        }
        if (c == q) { inchr = 1; out = out c; i++; continue }
        out = out c
        i++
      }
      gsub(/^[ \t\r]+/, "", out)
      if (out == "") next
      if (substr(out, 1, 1) == "#") next
      total++
    }
    END { print total }
  ' "$1"
}

# Fence paths use forward slashes; rg on Windows may report backslashes.
norm_path() {
  printf '%s' "${1//\\//}"
}

in_list() {
  local needle; needle="$(norm_path "$1")"; shift
  local raw
  for raw in "$@"; do
    [[ "$(trim "$(norm_path "$raw")")" == "$needle" ]] && return 0
  done
  return 1
}

# Path relative to Assets/Scripts, always forward-slash.
scripts_rel() {
  local file; file="$(norm_path "$1")"
  local root; root="$(norm_path "$SCRIPTS_DIR")"
  printf '%s' "${file#"$root"/}"
}

# rg exit 0 = matches, 1 = no matches, 2 = error. Only 2 is fatal. Matches land in
# HITS (file:line:text, one per line) so callers stay out of subshells. Scope
# directories that do not exist are dropped, matching lint-language's guards.
HITS=""
SCOPES=()
existing_scopes() {
  local d
  SCOPES=()
  for d in "$@"; do
    [[ -d "$d" ]] && SCOPES+=("$d")
  done
}

scan() {
  local pattern="$1"; shift
  local rc
  existing_scopes "$@"
  if (( ${#SCOPES[@]} == 0 )); then
    HITS=""
    return 0
  fi
  HITS="$(rg -n --no-heading --pcre2 -g '*.cs' -e "$pattern" "${SCOPES[@]}" 2>/dev/null)" && rc=0 || rc=$?
  if (( rc >= 2 )); then
    echo "lint-arch: rg failed (exit $rc) for pattern /$pattern/ in: ${SCOPES[*]}" >&2
    exit 2
  fi
  (( rc == 1 )) && HITS=""
  return 0
}

scan_multiline() {
  local pattern="$1"; shift
  local rc
  existing_scopes "$@"
  if (( ${#SCOPES[@]} == 0 )); then
    HITS=""
    return 0
  fi
  HITS="$(rg -n --no-heading --pcre2 -U -g '*.cs' -e "$pattern" "${SCOPES[@]}" 2>/dev/null)" && rc=0 || rc=$?
  if (( rc >= 2 )); then
    echo "lint-arch: rg failed (exit $rc) for multiline pattern /$pattern/ in: ${SCOPES[*]}" >&2
    exit 2
  fi
  (( rc == 1 )) && HITS=""
  return 0
}

# Enforced row: every hit past the fence's allowed count is a violation.
enforced_row() {
  local label="$1" max="$2"
  local n=0 hit file rest lineno
  [[ -n "$HITS" ]] && n="$(printf '%s\n' "$HITS" | wc -l | tr -d '[:space:]')"
  (( n <= max )) && return 0
  while IFS= read -r hit; do
    [[ -z "$hit" ]] && continue
    file="${hit%%:*}"
    rest="${hit#*:}"
    lineno="${rest%%:*}"
    printf '%s:%s: %s\n' "$file" "$lineno" "$label"
    VIOLATIONS=$((VIOLATIONS + 1))
  done <<< "$HITS"
}

# --- Row A: a file over its folder's cap -------------------------------------------
OVER_COUNT=0
check_caps_dir() {
  local sub="$1" cap="$2"
  local f n short
  [[ -d "$SCRIPTS_DIR/$sub" ]] || return 0
  while IFS= read -r f; do
    n="$(counted_lines "$f")"
    if (( n > cap )); then
      OVER_COUNT=$((OVER_COUNT + 1))
      short="$(scripts_rel "$f")"
      if in_list "$short" "${OVER_KNOWN[@]}"; then
        printf 'NOTE %s: %s counted lines against a cap of %s (baselined by name)\n' "$f" "$n" "$cap"
      else
        printf '%s:1: over-cap (%s counted lines against a cap of %s)\n' "$f" "$n" "$cap"
        VIOLATIONS=$((VIOLATIONS + 1))
      fi
    fi
  done < <(find "$SCRIPTS_DIR/$sub" -name '*.cs' -type f | sort)
}
check_caps_dir Sim "$SIM_CAP"
check_caps_dir Game "$GAME_CAP"
check_caps_dir Editor "$EDITOR_CAP"
if (( OVER_COUNT > OVER_BASELINE )); then
  printf 'over-cap file count %s exceeds the fence baseline %s\n' "$OVER_COUNT" "$OVER_BASELINE"
  VIOLATIONS=$((VIOLATIONS + 1))
elif (( OVER_COUNT < OVER_BASELINE )); then
  printf 'NOTE over-cap file count %s is below the fence baseline %s — update the fence\n' "$OVER_COUNT" "$OVER_BASELINE"
fi

# --- Fake-null constructs in Game/ (baselined by file count) -----------------------
scan "$FAKENULL_PAT" "$SCRIPTS_DIR/Game"
FAKENULL_FILES=0
LAST_FILE=""
if [[ -n "$HITS" ]]; then
  while IFS= read -r hit; do
    [[ -z "$hit" ]] && continue
    file="${hit%%:*}"
    rest="${hit#*:}"
    lineno="${rest%%:*}"
    if [[ "$file" != "$LAST_FILE" ]]; then
      LAST_FILE="$file"
      FAKENULL_FILES=$((FAKENULL_FILES + 1))
      printf 'NOTE %s:%s: fake-null construct (baselined; counted per file)\n' "$file" "$lineno"
    fi
  done <<< "$HITS"
fi
if (( FAKENULL_FILES > FAKENULL_BASELINE )); then
  printf 'fake-null file count %s exceeds the fence baseline %s\n' "$FAKENULL_FILES" "$FAKENULL_BASELINE"
  VIOLATIONS=$((VIOLATIONS + 1))
elif (( FAKENULL_FILES < FAKENULL_BASELINE )); then
  printf 'NOTE fake-null file count %s is below the fence baseline %s — update the fence\n' "$FAKENULL_FILES" "$FAKENULL_BASELINE"
fi

# --- LINQ in runtime code (enforced) -----------------------------------------------
scan "$LINQ_PAT" "$SCRIPTS_DIR/Game" "$SCRIPTS_DIR/Sim" "$SCRIPTS_DIR/Util"
enforced_row "linq-in-runtime" "$LINQ_MAX"

# --- WaitForSeconds allocation (baselined, minus the named non-loop site) ----------
scan "$WFS_PAT" "$SCRIPTS_DIR/Game"
WFS_COUNT=0
if [[ -n "$HITS" ]]; then
  while IFS= read -r hit; do
    [[ -z "$hit" ]] && continue
    file="${hit%%:*}"
    rest="${hit#*:}"
    lineno="${rest%%:*}"
    short="$(scripts_rel "$file")"
    if in_list "$short" "${WFS_NONLOOP[@]}"; then
      continue  # named in the fence as a non-loop site
    fi
    WFS_COUNT=$((WFS_COUNT + 1))
    printf 'NOTE %s:%s: waitforseconds-allocation (baselined)\n' "$file" "$lineno"
  done <<< "$HITS"
fi
if (( WFS_COUNT > WFS_BASELINE )); then
  printf 'waitforseconds count %s exceeds the fence baseline %s\n' "$WFS_COUNT" "$WFS_BASELINE"
  VIOLATIONS=$((VIOLATIONS + 1))
elif (( WFS_COUNT < WFS_BASELINE )); then
  printf 'NOTE waitforseconds count %s is below the fence baseline %s — update the fence\n' "$WFS_COUNT" "$WFS_BASELINE"
fi

# --- Deprecated object lookup (enforced) -------------------------------------------
scan "$FOT_PAT" "$SCRIPTS_DIR"
enforced_row "findobjectoftype-deprecated" "$FOT_MAX"

# --- Empty per-frame bodies (enforced) ---------------------------------------------
scan_multiline "$EMPTYBODY_PAT" "$SCRIPTS_DIR/Game"
enforced_row "empty-lifecycle-body" "$EMPTYBODY_MAX"

# --- Milestone IDs in identifiers: file names, types, namespaces, menus, defines ---
# Enforced. Declarations, menu strings and directives via the fence's pattern; file
# names checked separately against the fence's name pattern.
scan "$MIDID_PAT" "$SCRIPTS_DIR"
enforced_row "milestone-id-in-identifier" "$MIDID_MAX"
while IFS= read -r f; do
  [[ -z "$f" ]] && continue
  if printf '%s\n' "${f##*/}" | rg -q --pcre2 -e "$MIDID_NAME_PAT"; then
    printf '%s:1: milestone-id-in-filename\n' "$f"
    VIOLATIONS=$((VIOLATIONS + 1))
  fi
done < <(find "$SCRIPTS_DIR" -name '*.cs' -type f | sort)

# --- Milestone IDs in comments and string literals (baselined) ---------------------
scan "$MIDTEXT_PAT" "$SCRIPTS_DIR"
MIDTEXT_LINES=0
MIDTEXT_FILES=0
LAST_FILE=""
if [[ -n "$HITS" ]]; then
  while IFS= read -r hit; do
    [[ -z "$hit" ]] && continue
    file="${hit%%:*}"
    rest="${hit#*:}"
    lineno="${rest%%:*}"
    MIDTEXT_LINES=$((MIDTEXT_LINES + 1))
    if [[ "$file" != "$LAST_FILE" ]]; then
      LAST_FILE="$file"
      MIDTEXT_FILES=$((MIDTEXT_FILES + 1))
    fi
    printf 'NOTE %s:%s: milestone-id-in-text (baselined)\n' "$file" "$lineno"
  done <<< "$HITS"
fi
if (( MIDTEXT_LINES > MIDTEXT_LINE_BASELINE || MIDTEXT_FILES > MIDTEXT_FILE_BASELINE )); then
  printf 'milestone-id text count %s line(s) / %s file(s) exceeds the fence baseline %s / %s\n' \
    "$MIDTEXT_LINES" "$MIDTEXT_FILES" "$MIDTEXT_LINE_BASELINE" "$MIDTEXT_FILE_BASELINE"
  VIOLATIONS=$((VIOLATIONS + 1))
elif (( MIDTEXT_LINES < MIDTEXT_LINE_BASELINE && MIDTEXT_FILES < MIDTEXT_FILE_BASELINE )); then
  printf 'NOTE milestone-id text count %s line(s) / %s file(s) is below the fence baseline %s / %s — update the fence\n' \
    "$MIDTEXT_LINES" "$MIDTEXT_FILES" "$MIDTEXT_LINE_BASELINE" "$MIDTEXT_FILE_BASELINE"
fi

# --- Self-deleting scripts (enforced) ----------------------------------------------
scan "$SELFDELETE_PAT" "$SCRIPTS_DIR"
enforced_row "self-deleting-script" "$SELFDELETE_MAX"

# --- [InitializeOnLoad] outside the fence allowlist (enforced) ---------------------
scan "$IOL_PAT" "$SCRIPTS_DIR"
if [[ -n "$HITS" ]]; then
  while IFS= read -r hit; do
    [[ -z "$hit" ]] && continue
    file="${hit%%:*}"
    rest="${hit#*:}"
    lineno="${rest%%:*}"
    short="$(scripts_rel "$file")"
    if in_list "$short" "${IOL_ALLOW[@]}"; then
      continue
    fi
    printf '%s:%s: initializeonload-outside-allowlist\n' "$file" "$lineno"
    VIOLATIONS=$((VIOLATIONS + 1))
  done <<< "$HITS"
fi

# --- Scene lookups outside the named exemptions (Game/ only) -----------------------
# These three rows are the standing form of the two-runs milestone gate's manual greps
# (criteria: no scene lookup / one input producer / seeded draws); the greps they replace
# lived only in that milestone file.
scan "$SCENELOOKUP_PAT" "$SCRIPTS_DIR/Game"
if [[ -n "$HITS" ]]; then
  while IFS= read -r hit; do
    [[ -z "$hit" ]] && continue
    file="${hit%%:*}"
    rest="${hit#*:}"
    lineno="${rest%%:*}"
    short="$(scripts_rel "$file")"
    if in_list "$short" "${SCENELOOKUP_ALLOW[@]}"; then
      continue
    fi
    printf '%s:%s: scene-lookup-outside-allowlist
' "$file" "$lineno"
    VIOLATIONS=$((VIOLATIONS + 1))
  done <<< "$HITS"
fi

# --- Ambient Input reads outside the sole producer (Game/ only) ---------------------
scan "$AMBINPUT_PAT" "$SCRIPTS_DIR/Game"
if [[ -n "$HITS" ]]; then
  while IFS= read -r hit; do
    [[ -z "$hit" ]] && continue
    file="${hit%%:*}"
    rest="${hit#*:}"
    lineno="${rest%%:*}"
    short="$(scripts_rel "$file")"
    if in_list "$short" "${AMBINPUT_ALLOW[@]}"; then
      continue
    fi
    printf '%s:%s: ambient-input-outside-producer
' "$file" "$lineno"
    VIOLATIONS=$((VIOLATIONS + 1))
  done <<< "$HITS"
fi

# --- Ambient UnityEngine.Random outside the named cosmetic/crit sites (Game/ only) --
scan "$AMBRANDOM_PAT" "$SCRIPTS_DIR/Game"
if [[ -n "$HITS" ]]; then
  while IFS= read -r hit; do
    [[ -z "$hit" ]] && continue
    file="${hit%%:*}"
    rest="${hit#*:}"
    lineno="${rest%%:*}"
    short="$(scripts_rel "$file")"
    if in_list "$short" "${AMBRANDOM_ALLOW[@]}"; then
      continue
    fi
    printf '%s:%s: ambient-random-outside-allowlist
' "$file" "$lineno"
    VIOLATIONS=$((VIOLATIONS + 1))
  done <<< "$HITS"
fi

# --- Static singletons: type names from the fence, never a count -------------------
scan '\bstatic[ \t]+[A-Za-z_][A-Za-z0-9_]*[ \t]+Instance\b' "$SCRIPTS_DIR"
if [[ -n "$HITS" ]]; then
  while IFS= read -r hit; do
    [[ -z "$hit" ]] && continue
    file="${hit%%:*}"
    rest="${hit#*:}"
    lineno="${rest%%:*}"
    text="${rest#*:}"
    tname="$(printf '%s\n' "$text" | sed -E 's/.*static[ \t]+([A-Za-z_][A-Za-z0-9_]*)[ \t]+Instance.*/\1/')"
    if in_list "$tname" "${SINGLETON_ALLOW[@]}"; then
      continue  # grandfathered by name in the fence
    fi
    if in_list "$tname" "${SINGLETON_OWED[@]}"; then
      printf 'NOTE %s:%s: static Instance on an owed type (baselined by name)\n' "$file" "$lineno"
      continue
    fi
    printf '%s:%s: static-instance-singleton (type not named in the fence)\n' "$file" "$lineno"
    VIOLATIONS=$((VIOLATIONS + 1))
  done <<< "$HITS"
fi

# --- Editor menu: one root, every row named in the fence ---------------------------
# Concatenated MenuItem paths are invisible to the fence, so they fail first.
scan '\[MenuItem\((?!\s*")' "$SCRIPTS_DIR"
if [[ -n "$HITS" ]]; then
  while IFS= read -r hit; do
    [[ -z "$hit" ]] && continue
    file="${hit%%:*}"
    rest="${hit#*:}"
    lineno="${rest%%:*}"
    printf '%s:%s: menu-item-not-a-literal\n' "$file" "$lineno"
    VIOLATIONS=$((VIOLATIONS + 1))
  done <<< "$HITS"
fi

FOUND_ROWS=()
scan '\[MenuItem\(\s*"([^"]+)"' "$SCRIPTS_DIR"
if [[ -n "$HITS" ]]; then
  while IFS= read -r hit; do
    [[ -z "$hit" ]] && continue
    file="${hit%%:*}"
    rest="${hit#*:}"
    lineno="${rest%%:*}"
    text="${rest#*:}"
    path="$(printf '%s\n' "$text" | sed -E 's/.*\[MenuItem\([ \t]*"([^"]+)".*/\1/')"
    prefix="$MENU_ROOT/"
    if [[ "$path" != "$prefix"* ]]; then
      printf '%s:%s: menu-item-outside-root (%s)\n' "$file" "$lineno" "$path"
      VIOLATIONS=$((VIOLATIONS + 1))
      continue
    fi
    row="${path#"$prefix"}"
    if ! in_list "$row" "${MENU_ROWS[@]}"; then
      printf '%s:%s: menu-item-not-in-fence (%s)\n' "$file" "$lineno" "$row"
      VIOLATIONS=$((VIOLATIONS + 1))
      continue
    fi
    if (( ${#FOUND_ROWS[@]} == 0 )) || ! in_list "$row" "${FOUND_ROWS[@]}"; then
      FOUND_ROWS+=("$row")
    fi
  done <<< "$HITS"
fi
for expected in "${MENU_ROWS[@]}"; do
  [[ -z "$expected" ]] && continue
  if (( ${#FOUND_ROWS[@]} == 0 )) || ! in_list "$expected" "${FOUND_ROWS[@]}"; then
    printf 'menu-row-missing-from-code: %s\n' "$expected"
    VIOLATIONS=$((VIOLATIONS + 1))
  fi
done

# --- Non-source files under Assets/Scripts (enforced) ------------------------------
# Source here is .cs, .asmdef and Unity's .meta — see 3-Unity-Runtime.md section 9.
NONSOURCE_COUNT=0
while IFS= read -r f; do
  [[ -z "$f" ]] && continue
  NONSOURCE_COUNT=$((NONSOURCE_COUNT + 1))
  if (( NONSOURCE_COUNT > NONSOURCE_MAX )); then
    printf '%s:1: non-source-file\n' "$f"
    VIOLATIONS=$((VIOLATIONS + 1))
  fi
done < <(find "$SCRIPTS_DIR" -type f ! -name '*.cs' ! -name '*.meta' ! -name '*.asmdef' | sort)

if (( VIOLATIONS > 0 )); then
  echo "lint-arch: $VIOLATIONS violation(s)" >&2
  exit 1
fi
echo "lint-arch: clean"
exit 0
