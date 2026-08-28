# Go to market

<!--
Purpose: The first real market is a friend with a zip, not a Steam page.
Status: working
Update trigger: when a friends five-Wave dry-run closes, when Release-Gate #19–25 change,
or when a friends send is actually made.
Last updated: 2026-08-25 (the send may now promise a priced respawn and two more Protocols;
"host death ends the Run" withdrawn; trailer beats renamed to the player's words)
-->

## First market

Two people, not the internet. Friends alpha (`../Roadmap.md`, Zo 2026-08-13):
install a build, join **without the developer touching a keyboard**, play a
full five-Wave run with both participating, end screen that says what happened.

That sentence is the only launch criterion that exists. Steam is not scheduled
in the roadmap (no dates projected). Shared Ship lobby closed with **M72**
(2026-08-17); the remaining gap is dry-run length and send polish, not topology.

## What they must succeed at

`../Release-Gate.md` human red items 13–25, especially:

| # | If it fails, the friend concludes |
|---|---|
| 13 | "Doesn't launch" (exe from inside the zip) |
| 15 | Spinner forever; evening spent guessing |
| 19 | No shared Ship; guest local embark (`M72` closed — re-verify on send) |
| 20 | Wrong gun in Bastion |
| 24 | One seat stuck; cannot rematch |
| 25 | Partial evening; `[Dev]` taint |

Install copy already exists: `../Release-Send.md` (extract whole folder,
SmartScreen, firewall Private **and** Public, host EMBARKs first, `Player.log`
path, `run-windowed.bat`).

**M72** closed 2026-08-17: host Relay log `13-24-37` / joinCode `KPRMPR`,
`playerCount=2`, Ready gate, party return.
`../Milestones/3. completed/M72-the-party-shares-a-ship.md`.

`M64` closed the machine send and NetPrep. Human items 13–25 were not claimed
played (`M64` header; Roadmap still lists that dry-run). It is not the five-Wave
evening in the Roadmap sentence.

`../Release-Send.md` teaches extract, SmartScreen, AV, firewall, host EMBARKs
first, log path, `run-windowed.bat`. It does **not** teach Ship consoles, Waves,
or Ready. That is `../Play/Getting-Started.md`.

## What this send may promise

- A shooter they can finish solo if Join fails.
- Host/Join by Relay code or typed IPv4 (`M60`).
- Personal wallets (`M65`); a guest who goes down waits on the Ship (`M57`).
- **One priced way back for any dead seat, host and solo included** (`M98` recut,
  2026-08-24): it pays from the Unbanked it is carrying, `150 × (taken + 1)` with the
  counter the Boarding's, and the remainder of the carry survives the death
  (`Sim/Run/Respawns.cs`). This row previously read "host death ends the Boarding
  (`M49`)", which stopped being true on that date.
- Two more Protocols to claim in CREW — VEIL and FORGE (`M94`).

## What this send may not promise

- "It just works with a friend" until Gate #19 is green on **two networks**.
- Steam distribution or Steam join (no real appid; `M60`/`M64`).
- Signed, silent install (unsigned → SmartScreen; AV may quarantine).
- Mid-Run healing, weapon Tiers, or a Repair Rig on a fresh save (first Lost
  unlocks Tiers **and** the FAB equipment slot; the Rig is then a purchase —
  `../Play/Getting-Started.md`).
- AEGIS or LEDGER, a second Bastion, or Steam features (achievements, cards).
- A realm a friend opens while they are offline (`M100` is built and has never run on
  two machines).
- An easy first session (play 2026-08-16: starting solo too hard, `[unconfirmed]`).
- That Play's shared-Ship prose is the build they have tonight.
- Nameplates or remote-gun VFX (`M66`). Voice is rejected in `M66` and parked
  in `../Brainstorms/co-op.md`.

## Later markets

Only after friends-alpha: a wider zip, then a Steam page whose printable
sentences are the **true** rows in `pitch.md`. Trailer proof should be: quiet
Prep, an ante up, a start, a Convert, two people on a Ship — not a body-count
flex. Strategy for tags: `strategy.md`.
