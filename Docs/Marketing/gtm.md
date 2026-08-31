# Go to market

<!--
Purpose: The first real market is a friend with a zip, not a Steam page.
Status: working
Update trigger: when a friends five-Wave dry-run closes, when Release-Gate #19–25 change,
or when a friends send is actually made.
Last updated: 2026-08-30 (0.34.2 zip: Ante parked, Hatch-start, kills bank on landing,
realm-bank respawn cap 2, join-code reuse, Boarder name field; Convert-as-payroll withdrawn)
-->

## First market

Two people, not the internet. Friends alpha (`../Roadmap.md`, Zo 2026-08-13):
install a build, join **without the developer touching a keyboard**, play a
full five-Wave run with both participating, end screen that says what happened.

That sentence is the only launch criterion that exists. Steam is not scheduled
in the roadmap (no dates projected). Shared Ship lobby closed with **M72**
(2026-08-17); the remaining gap is dry-run length and send polish, not topology.

**0.34.2** (`ProductVersion.Display`, `bundleVersion`) is a friends zip of that
lobby. It is not the five-Wave evening in the Roadmap sentence.

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
SmartScreen, firewall Private **and** Public, OPEN TO FRIENDS → COPY → TO SHIP,
host EMBARKs first, `Player.log` path, `run-windowed.bat`). Closing OPEN TO FRIENDS
and reopening the same realm reuses the Relay allocation rather than minting a
second code (2026-08-30).

**M72** closed 2026-08-17: host Relay log `13-24-37` / joinCode `KPRMPR`,
`playerCount=2`, Ready gate, party return.
`../Milestones/3. completed/M72-the-party-shares-a-ship.md`.

`M64` closed the machine send and NetPrep. Human items 13–25 were not claimed
played (`M64` header; Roadmap still lists that dry-run). It is not the five-Wave
evening in the Roadmap sentence.

`../Release-Send.md` teaches extract, SmartScreen, AV, firewall, OPEN TO FRIENDS,
host EMBARKs first, log path, `run-windowed.bat`. It does **not** teach Ship
consoles, Waves, or Ready. That is `../Play Mechanics/Getting-Started.md`.

## What this send may promise

- A shooter they can finish solo if Join fails. A Wave starts at the Hatch and ends at the far terminal (`Getting-Started.md`).
- Host/Join by Relay code or typed IPv4 (`M60`). Reopening OPEN TO FRIENDS on the same realm keeps the code.
- A Boarder name field on REALMS, separate from the realm name (2026-08-30).
- Personal wallets (`M65`); a guest who goes down waits on the Ship (`M57`).
- **Priced returns from the realm's bank**, host and solo included: `150 / 300`, then refused `out of respawns`. **10 s** to confirm (`Respawns.MaxRespawns = 2`, `BleedOutSeconds = 10`). Banked Salvage from kills is already theirs (`AwardKill`).
- Two more Protocols to claim in CREW — VEIL and FORGE (`M94`). FORGE's turret and Outrider are in the build (`../Play Mechanics/Protocol-FORGE.md`).
- Hold-to-recall pads beside T2 and T5 (`HatchRecallPad`).
- Salvage that banks as the body falls. A lost Boarding still pays for every kill.

## What this send may not promise

- "It just works with a friend" until Gate #19 is green on **two networks**.
- Steam distribution or Steam join (no real appid; `M60`/`M64`).
- Signed, silent install (unsigned → SmartScreen; AV may quarantine).
- **Ante / intensity / "author the swarm."** Parked. `+/-` does nothing a friend can see.
- Convert as the way Salvage is kept. Convert is an opt-in Withdraw bet, struck from the demo (2026-08-25).
- Weapon Tiers or a Repair Rig before the realm has earned any Salvage (`MetaUpgrades.UpgradesUnlocked` → `LifetimeSalvageEarned > 0`). Buying a weapon itself is not gated. Regen between hits already ships (`Getting-Started.md`). FABRICATION is additionally locked "FULL VERSION" on a `DemoActive` save until the Bastion is stripped (`DemoStationGate`; `Getting-Started.md`).
- AEGIS or LEDGER, a second Bastion, or Steam features (achievements, cards).
- A realm a friend opens while they are offline (`M100` is built and has never run on two machines).
- An easy first session (play 2026-08-25: MK-1 T2, no Primary, lost zone 1's composite at 25 live, 2 respawns, 227 banked; verdict pending. Play 2026-08-16: starting solo too hard, `[unconfirmed]`).
- That Play's shared-Ship prose is the build they have tonight.
- Nameplates or remote-gun VFX (`M66`). Voice is rejected in `M66` and parked
  in `../Brainstorms/co-op.md`.
- The faucet-loop graybox (auto-chain, Hatch-hunt, endure income, the draft).
- The practice range (`M99`) as a store beat.
- The SHIFT-wordmark swap as a feature.
- Always being able to buy back from a death (cap 2; bank must cover; 10 s).
- A two-deck Ship.

## Later markets

Only after friends-alpha: a wider zip, then a Steam page whose printable
sentences are the **true** rows in `pitch.md`. A Steam Playtest is a child AppID
with no public reviews; it is not Next Fest-eligible, and a live Playtest during
Next Fest splits the demo ([Playtest](https://partner.steamgames.com/doc/features/playtest);
[Next Fest FAQ](https://partner.steamgames.com/doc/marketing/upcoming_events/nextfest)).
October 2026 Next Fest needs a **published store page and a public demo**; this
project has no real appid, so none of that is a send promise. Trailer proof should
be: quiet Prep, `T` at the Hatch, the walk to a terminal shutoff, two people on a
Ship — not a body-count flex, not an ante up, not an extract. Strategy for tags:
`strategy.md`.
