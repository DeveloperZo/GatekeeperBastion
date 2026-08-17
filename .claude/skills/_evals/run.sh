#!/usr/bin/env bash
# Gatekeeper's Protocol — skill evals.
#
#   run.sh trigger   would each prompt pick the right skill? one batched call per skill
#   run.sh quality   does the skill body improve the answer? two arms per skill, then grading
#   run.sh all       both
#
# Ask for this in a sentence ("close M44's eval criteria"); the agent runs it.
# Requires `claude` and `jq`. Writes results/ (gitignored).
#
# ---------------------------------------------------------------------------
# Why this looks nothing like the first version
#
# v1 spawned a full agent session per case and tried to detect a Skill tool_use
# in the event stream. One probe cost ~199k tokens and 162 s and still reported
# nothing, because tools were enabled and the agent went exploring, and because
# skill loading is not reliably visible in the stream under default tools.
#
# Two changes fix it:
#
#   --allowedTools ""  every call. No repo exploration. A call is ~3 s and its
#                      system prompt is a cache read after the first.
#
#   Control, don't detect. For quality, the skill body is PREPENDED to the with
#   arm and omitted from the without arm. There is nothing to detect, no skill
#   hiding, no selftest gate. Whether a skill *loads* is the trigger mode's
#   question, and that is a classification, not an agent run.
# ---------------------------------------------------------------------------

set -uo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SKILLS="$(cd "$HERE/.." && pwd)"
OUT="$HERE/results"

for c in claude jq; do command -v "$c" >/dev/null || { echo "evals: need $c on PATH" >&2; exit 2; }; done
# Claude Code on Windows needs git-bash; inherit if set, else locate it.
if [[ -z "${CLAUDE_CODE_GIT_BASH_PATH:-}" ]]; then
  for candidate in "/e/Program Files/Git/bin/bash.exe" "/c/Program Files/Git/bin/bash.exe"; do
    if [[ -x "$candidate" ]]; then
      case "$candidate" in
        /e/*) export CLAUDE_CODE_GIT_BASH_PATH='E:\Program Files\Git\bin\bash.exe' ;;
        /c/*) export CLAUDE_CODE_GIT_BASH_PATH='C:\Program Files\Git\bin\bash.exe' ;;
      esac
      break
    fi
  done
fi
mkdir -p "$OUT"
die() { echo "evals: $*" >&2; exit 1; }

# One call. No tools. Returns the text, or dies — an empty result must never be
# recorded as a legitimate answer.
say() {
  local raw status
  raw=$(claude -p "$1" \
    --output-format json \
    --allowedTools "" \
    --permission-mode bypassPermissions \
    --no-session-persistence \
    2>"$OUT/.stderr"); status=$?
  (( status == 0 )) && [[ -n "$raw" ]] || die "claude exited $status: $(head -2 "$OUT/.stderr")"
  jq -r '.result // ""' <<<"$raw"
}

# First JSON value of the expected shape in a reply, so a chatty model still parses.
extract() { grep -o "$1" <<<"$2" | head -1; }

descriptions() {
  local f d out=""
  for f in "$SKILLS"/gkb-*/SKILL.md; do
    d=$(awk '/^description: /{sub(/^description: /,""); print; exit}' "$f")
    out+="- $(basename "$(dirname "$f")"): $d"$'\n'
  done
  printf '%s' "$out"
}

# ---------------------------------------------------------------------------
# trigger — one batched classification per skill's prompt set.
# The model sees exactly what is always in context (the descriptions) and picks.
# ---------------------------------------------------------------------------
trigger() {
  local file="$HERE/triggers.json" descs results="[]" skill
  descs=$(descriptions)
  # tr -d '\r': Windows jq/command substitution can leave CR on skill names, which
  # makes .[$s] null and every length/iterate fail with a clean-looking empty result.
  for skill in $(jq -r 'keys[] | select(startswith("_") | not)' "$file" | tr -d '\r'); do
    local prompts expected reply verdicts n_exp n_pass
    prompts=$(jq -r --arg s "$skill" '[.[$s].should[], .[$s].near_miss[]] | to_entries | map("\(.key+1). \(.value)") | join("\n")' "$file")
    expected=$(jq -c --arg s "$skill" '[.[$s].should[] as $x | true] + [.[$s].near_miss[] as $x | false]' "$file")
    n_exp=$(jq 'length' <<<"$expected")
    [[ "$n_exp" -gt 0 ]] || die "trigger: no prompts for skill '$skill' (CR in key name?)"

    reply=$(say "You have these skills available. Their descriptions are all you know about them.

$descs

For each numbered request below, decide which single skill you would invoke, or \"none\" if you would answer without one. Judge only from the descriptions.

$prompts

Output ONLY a JSON array of strings, one per request, in order. No prose.")

    verdicts=$(extract '\[[^]]*\]' "$reply")
    [[ -n "$verdicts" ]] || die "trigger: no JSON array back for $skill
reply was: ${reply:0:200}"

    results=$(jq -n --argjson acc "$results" --arg s "$skill" --argjson v "$verdicts" \
                    --argjson e "$expected" --argjson p "$(jq -c --arg s "$skill" '[.[$s].should[], .[$s].near_miss[]]' "$file")" '
      $acc + [range(0; $e|length) as $i | {
        skill: $s,
        kind:  (if $e[$i] then "should" else "near_miss" end),
        prompt: $p[$i],
        chose: ($v[$i] // "none"),
        pass:  (if $e[$i] then ($v[$i] == $s) else ($v[$i] != $s) end)
      }]')
    n_pass=$(jq -r --arg s "$skill" '[.[] | select(.skill==$s and .pass)] | length' <<<"$results")
    echo "  $skill  $n_pass/$n_exp pass"
  done

  jq -n --argjson r "$results" '{
    mode:"trigger", prompts:$r,
    by_skill: ($r | group_by(.skill) | map({skill:.[0].skill,
      should_fired:   (map(select(.kind=="should"    and .pass)) | length),
      should_total:   (map(select(.kind=="should"))   | length),
      near_miss_fail: (map(select(.kind=="near_miss" and (.pass|not))))})),
    failures: ($r | map(select(.pass|not))),
    note:"should failures = description too narrow. near_miss failures = too broad, and note which skill it chose instead — that names the boundary to sharpen. Fix by adding what the skill does NOT cover, never by pasting keywords from the failing prompt."
  }' > "$OUT/trigger.json"
  echo "wrote $OUT/trigger.json"
}

# ---------------------------------------------------------------------------
# quality — per skill: one call with the body, one without, then one grading call.
# Three calls per skill, not thirty sessions.
# ---------------------------------------------------------------------------
quality() {
  local cases="$HERE/cases.json" results="[]" skill
  for skill in $(jq -r 'keys[] | select(startswith("_") | not)' "$cases" | tr -d '\r'); do
    local body tasks with without reply grades
    body=$(sed '1{/^---$/!q};1,/^---$/d' "$SKILLS/$skill/SKILL.md")
    tasks=$(jq -r --arg s "$skill" '.[$s].cases | to_entries | map("TASK \(.key+1)\nContext: \(.value.state)\nRequest: \(.value.prompt)") | join("\n\n")' "$cases")
    [[ -n "$tasks" ]] || die "quality: no cases for skill '$skill'"

    with=$(say "You are working on the Gatekeeper's Protocol Unity project. Follow this guidance.

<guidance>
$body
</guidance>

Answer each task below. Number your answers to match. Be concise.

$tasks")

    without=$(say "You are working on the Gatekeeper's Protocol Unity project.

Answer each task below. Number your answers to match. Be concise.

$tasks")

    local asserts; asserts=$(jq -r --arg s "$skill" '.[$s].cases | to_entries | map("TASK \(.key+1):\n" + (.value.assertions | to_entries | map("  \(.key+1). \(.value)") | join("\n"))) | join("\n")' "$cases")
    reply=$(say "Score two answer sets against the same assertions. Be strict: an assertion passes only if the answer actually does what it says.

ASSERTIONS BY TASK:
$asserts

ANSWER SET A:
$with

ANSWER SET B:
$without

Output ONLY JSON: {\"a\":[[true,false,...],...],\"b\":[[...],...]} — one inner array per task, one boolean per assertion, in order. No prose.")

    grades=$(extract '{"a".*}' "$reply")
    [[ -n "$grades" ]] || die "quality: grader returned no JSON for $skill"

    results=$(jq -n --argjson acc "$results" --arg s "$skill" --argjson g "$grades" \
                    --argjson a "$(jq -c --arg s "$skill" '[.[$s].cases[] | {id, assertions}]' "$cases")" '
      ($g.a | flatten) as $fa | ($g.b | flatten) as $fb |
      $acc + [{
        skill: $s,
        with_skill_pass_rate:    (([$fa[] | select(.)] | length) / ($fa | length)),
        without_skill_pass_rate: (([$fb[] | select(.)] | length) / ($fb | length)),
        per_case: [range(0; $a|length) as $i | {case: $a[$i].id,
                    with: $g.a[$i], without: $g.b[$i], assertions: $a[$i].assertions}],
        non_discriminating: [range(0; $a|length) as $i
          | range(0; $a[$i].assertions|length) as $j
          | select($g.a[$i][$j] and $g.b[$i][$j]) | $a[$i].assertions[$j]]
      }]')
    echo "  $skill  with=$(jq -r '.[-1].with_skill_pass_rate' <<<"$results")  without=$(jq -r '.[-1].without_skill_pass_rate' <<<"$results")"
  done

  jq -n --argjson r "$results" '{
    mode:"quality",
    by_skill: ($r | map({skill, with_skill_pass_rate, without_skill_pass_rate,
      delta: (.with_skill_pass_rate - .without_skill_pass_rate),
      verdict: (if (.with_skill_pass_rate - .without_skill_pass_rate) > 0 then "ship"
                else "DELETE — at or below baseline" end)})),
    detail: $r,
    non_discriminating_assertions: ($r | map(.non_discriminating) | flatten | unique),
    note:"Delete every skill whose verdict says DELETE — do not tune it. Remove every assertion in non_discriminating_assertions from cases.json and re-run; they passed in both arms and measure nothing."
  }' > "$OUT/quality.json"
  jq -r '.by_skill[] | "\(.skill)\t\(.verdict)\tdelta=\(.delta)"' "$OUT/quality.json"
  echo "wrote $OUT/quality.json"
}


# ---------------------------------------------------------------------------
# quality is NOT a gate and must not run as part of closing M44.
# It is ~10 minutes and a large token spend for the suite. It refuses unless a
# human explicitly forces it, because an agent asked to "close M44" will
# otherwise reach for it and burn the budget. Enforcement lives here rather
# than in a document, because the document did not stop it.
# ---------------------------------------------------------------------------
require_quality_optin() {
  [[ "${EVAL_QUALITY:-0}" == "1" ]] && return 0
  cat >&2 <<'MSG'
evals: refusing to run `quality`.

  It is not a gate. Closing M44 needs only:  run.sh trigger   (5 calls, ~1 min)

  quality prepends each skill body to one arm and omits it from the other, then
  grades both. Roughly ten minutes and a large token spend for the suite. Run it
  only when a specific skill is suspected of not earning its keep.

  To force it deliberately:  EVAL_QUALITY=1 run.sh quality
MSG
  exit 3
}

case "${1:-}" in
  trigger) trigger ;;
  quality) require_quality_optin; quality ;;
  all)     trigger; require_quality_optin; quality ;;
  *) echo "usage: run.sh trigger|quality|all" >&2; exit 2 ;;
esac
