# M74 — The demo speaks

<!--
Status: completed 2026-08-17
Update trigger: closed
Last updated: 2026-08-17
Closed on: 2026-08-17. Foundation pipe: engine-free `DemoVoice` runner,
`DemoLines` table, `DemoConversationIds` registry, `DemoVoiceView` on Ship and
Bastion canvases (YAML-wired), party ClientRpc on `NetRunMirror`, input gates in
`ShipController` / `FirstPersonController` / `HudController.Pause`. EditMode:
23 `DemoVoiceTests` (registry, preempt/queue, no reply field, beat-doc id
coverage, scene `DemoVoiceView` pins). PlayMode: `DemoVoicePlayModeTests`
(timeScale + loopback broadcast). Criterion 8 playthrough stays open until M75
wires beat triggers — the runner is playable via dev scratch only until then.
Integration Check re-armed via `gkb_verify_request.txt` when the editor is back.
-->

The mission-dialogue pipe: a line-id runner, the id registry, and the net RPC —
so every demo beat can say what the job is, as text, with no VO, no vendor
pack, and no player replies. Cut 1 of the demo family (`../../Demo/README.md`,
How to cut milestones).

## Story

As a player, I want the game to tell me the current job as on-screen text —
locking me to read it on the Ship and in Prep, subtitling it in a fight — so
that every beat of the demo can teach without a document.

As the developer, this is the multiplier cut: every line in beats 0–7 rides
this runner. Without it, every beat milestone builds its own text plumbing.

## Why it matters

`../../Demo/voice.md` is the implementable spec: runner contract (Lock /
Subtitle), the ~25-line id registry, input-lock policy, the same-frame Docked
queue rule, and the party RPC. Arc, not loop — authored onboarding copy — and
priced as such: one runner, one table, no framework. `Decisions.md` forbids
`timeScale = 0`; the withdrawn `demo.intruders` exception stays withdrawn —
beat 0 gates execution instead.

Durable-tooling test: the runner outlives every beat that motivated it (any
future mission text uses it). Multiplier: M75, M76, M77, M78, M79, M80 all
list their lines against it.

## Acceptance criteria

1. `DemoVoice.Play(id, Lock)` on the Ship shows the Shift box, eats
   move/look/fire/interact, keeps `Time.timeScale == 1`, and ends on Continue
   or Esc — no reply control exists.
2. A Lock line during Bastion Prep does not stop the sim: `RunElapsed`
   advances while the box is up.
3. A Subtitle line during Assault leaves all fight input live and
   auto-advances in 4 s when the line has no duration.
4. Unknown id logs `[Demo] line skipped — unknown id=<id>` and no-ops; a
   missing table logs `[Demo] line table missing` once and the game continues.
5. `Play` during a live line preempts it (`line preempted from=<a> to=<b>`),
   except the same-frame Docked collision: `demo.first_hatch_down` plays
   first and the other Lock queues (`line queued id=<id> behind=…`), starting
   on its Continue.
6. Party scope: host `PlayLocalAndBroadcast` starts the line on every
   connected seat (`line rpc id=<id> clientId=<n>`); guests never re-broadcast;
   a guest joining mid-line gets no catch-up.
7. Every line start and end logs
   `[Demo] line id=<id> mode=<Lock|Subtitle> scope=<Local|Party> speaker=<…>`
   / `line ended id=<id>` — a line that played with no log is a defect.
8. Playthrough: from Play, trigger any Ship Lock line — the box, the
   Continue, the log pair, and unbroken sim time confirm 1, 2, and 7.

## Implementation

`../../Demo/voice.md` Create / Change / Delete tables are the file list:
**Create** `DemoLines.cs`, `DemoVoice.cs`, `DemoConversationIds.cs`, the
`NetSession` `StartDemoLine` RPC; **Change** `HudController.Pause`,
`ShipController`, `FirstPersonController`, optional mission-drawer standing
objective; **Delete** nothing. `GatekeepersProtocol.Sim` must not reference UI
or line assets (`../../Architecture.md`). Copy may be `TODO` — mechanical
gates in the beats must function mute.

Ids land in `DemoConversationIds` now, from voice.md's registry, including the
beats-6/7 rows; the copy lands per beat milestone.

## Verification

voice.md's Verification list is the gate, mapped: EditMode id-uniqueness +
registry coverage (criterion 7's precondition); PlayMode Ship Lock (1);
Bastion Prep fixture (2); Assault fixture (3); loopback party (6); collision
fixture (5); no `[Dev]` in any of those logs. Playthrough path is criterion 8.

## Traps

voice.md Traps apply verbatim — the two that have already been paid for
elsewhere: importing a dialogue framework for ~25 one-way lines, and any
`timeScale = 0` write.

## Depends on / Unblocks

Depends: Shift HUD (in tree). Unblocks: every other demo milestone's lines
(M75–M80).
