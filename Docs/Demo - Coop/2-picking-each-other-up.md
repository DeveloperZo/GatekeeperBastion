# 2 — Picking each other up

<!--
Purpose: A seat goes down mid-Boarding. Live path is the priced self-respawn
from the realm bank. Teammate revive is still the stamped Build.
Status: working
Authority: implementable against 0.34.2
Update trigger: when the revive stamp is overridden, when Respawns constants move
Last updated: 2026-08-30 (bank purse, 10 s bleed, MaxRespawns 2, 150/300;
revive still absent)
Depends: voice-coop.md, 1-two-doors-to-hold.md, ../Play Mechanics/Going-Down.md
-->

## Why this beat exists

**Revive does not exist.** A repo-wide grep for `revive` over `Assets/Scripts/`
returns nothing — no type, method, field, RPC or binding.

What ships is a **down state**, a **10 s bleed-out**, and a **priced self-respawn
from the realm bank**. `RunSim.SetSeatDown` freezes that seat's Unbanked (so a
Convert cannot drain a pile whose owner cannot act). `RunSim.TryRespawn` debits
`Respawns.CostFor` — **150** × min(taken + 1, 3) — from `BankedSalvage` via
`SpendBanked`. Cap this Boarding: **2** (`MaxRespawns`). Host and solo see the
DOWN overlay; a guest is `WaitingOnShip` and asks over
`NetPlayerAvatar.RequestRespawn`. Heal is to full, not partial.

The fee is the crew's: the counter is the Boarding's, not the seat's. Nothing
else pays, and the Hatch falling cannot be bought back.

That still asks nothing of the other player as a verb. The stamped teammate
hold sits on top of this path rather than replacing it.

**Ruled 2026-08-20: a downed host does not end the Boarding while another seat
is alive.** Landed. `TryEnterLocalDown` holds the Boarding open while
`AnyRemoteSeatFighting()`; `NotifySeatWentDown` ends it only when nobody left
can stand and nobody waiting can pay. Solo is unchanged in substance: one seat,
no other seat.

## The live verb

A seat at 0 HP is down: overlay (host/solo) or Ship wait (guest). A Boarding-owned
bleed clock starts at **10 s** and is not restarted for a second seat. Confirm
(or NAV RESPAWN) buys the return if `RespawnRefuse` is null: "no run" /
"out of respawns" / "not enough banked".

**Cost is Salvage from the realm pot**, climbing **150 / 300**, then the hard
stop. Exposure-only teammate revive remains the stamp (Gaps).

**Unbanked survives the down.** Freeze, not wipe. A paid return leaves the
carry untouched. Wipe happens when the Boarding ends with nobody able to stand,
or on Hatch breach.

## Functional storyboard

Continues from beat 1. Not a scripted knockdown.

### 0 — Go down

| | |
|---|---|
| **Goal** | Understand instantly that this is a bill, not the end. |
| **Player** | HP reaches 0. Host/solo: DOWN overlay names price, bank, countdown. Guest: waiting on the Ship. |
| **Game** | Downed seat: `demo.coop.you_are_down` · Subtitle · Local · Game. **You are down. They can bring you up.** Other seats: `demo.coop.they_are_down` · Subtitle · Local · Game. **<name> is down.** |
| **Standing** | Downed: Buy back. Others: Keep fighting |
| **Prompt** | DOWN: price + "of your N banked" + bleed clock (`HudController.Fight.DownText`). Guest: NAV RESPAWN row. |
| **Log** | `[Run] bled out — nobody stood up inside 10s` on expiry; `respawn taken — slot=… cost=… bank …` on success |
| **Have** | `GameController.Down`, `RunSim.SetSeatDown`, `BleedOutSeconds = 10`. M86: `Dead` animator bool from `Hp <= 0`; `Anim_Down.fbx` unsourced. |
| **Build** | Immobile-but-alive body in the Bastion (guest already leaves for the Ship). The "they can bring you up" line is the revive stamp — live copy should name the bank until revive lands. HOST-ONLY authority on down/up. **Do not** wipe unbanked here. **Do not** call the host's death path while a remote seat fights. |

### 1 — The priced return

| | |
|---|---|
| **Goal** | Pay the realm pot, stand up. |
| **Player** | Confirm (host/solo) or NAV RESPAWN (guest) inside the 10 s. Body resets to spawn, healed to full. Carry untouched. |
| **Game** | On completion, returned seat: `demo.coop.up` · Subtitle · Local · Game. **Up. Get back to your hatch.** |
| **Standing** | Back to the fight |
| **Prompt** | — |
| **Log** | `respawn taken — slot=<n> cost=<n> bank <a>-><b> taken=<n>` |
| **Have** | `TryRespawn` → `SpendBanked`. Shared `RespawnsTaken`. FOLLOW is free and is not this verb (never-embarked seat). |
| **Build** | Teammate hold-Interact revive: any living seat holds on them, interrupt by move/fire/hit, partial HP. Routes guest → host through `NetVerbs`. |

### 2 — Bleed-out

| | |
|---|---|
| **Goal** | Time runs out. |
| **Player** | Nobody pays in 10 s. If a remote seat still fights, the host overlay stays and the Boarding continues. If nobody can stand and nobody waiting can pay, the Boarding ends Lost. |
| **Game** | `demo.coop.bled_out` · Subtitle · Party · Game. **<name> is out. They come back when you next dock.** |
| **Standing** | Unchanged for the living |
| **Prompt** | — |
| **Log** | `bled out — nobody stood up inside 10s` |
| **Have** | Sim reports expiry; Game layer decides. Guest is already on the Ship at 0 HP. |
| **Build** | Align `demo.coop.bled_out` with mid-Boarding bank-priced return. The stamped "next dock" copy contradicts `TryRespawn`. |

### 3 — The host goes down

| | |
|---|---|
| **Goal** | Prove the ruling in play. |
| **Player** | Host is down. Guest is still fighting. Boarding continues. Host Confirm pays the bank, or the guest keeps the floor until the sweep. |
| **Game** | Same lines as steps 0 and 1. No special-casing in copy. |
| **Standing** | Unchanged |
| **Log** | existing down / respawn lines |
| **Have** | Landed. `TryEnterLocalDown` + `NotifySeatWentDown`. |
| **Build** | Optional `[Run] host down, run continues seats alive=<n>` log. |

## Acceptance shape

Player action: one player is downed mid-Wave and buys back from the realm bank
inside 10 s, or the Boarding continues on the other seat. Reads back: the
respawn log naming cost and bank, and a Boarding that survives the host being
downed.

Teammate hold-Interact is the stamped extra; it does not ship.

## Verification

The PlayMode net fixture can hold: down freezes unbanked, `TryRespawn` debits
bank not carry, a downed host with a living guest does not end the Boarding,
bleed-out expiry is reported. The hold-Interact revive, camera, and whether a
rescue under pressure feels possible wait on that verb existing.

## Gaps

- **Revive cost.** Exposure only is stamped. Salvage from the pot is what
  already ships for self-return. Unknown which teammate-rescue should use;
  needs play data.
- **Revive against the priced respawn.** Both would put the same seat back.
  Keep both, hide the priced one behind bleed-out, or drop the price in a
  party: unruled.
- **Partial HP on revive.** Not specced. Live self-return is full HP.
- Whether a downed seat can be revived during a Convert window: stamped
  permissive, because Master Control has no between.
- Ship wait labels that still price against `avatar.Unbanked`
  (`ShipController.cs`, `MapStation.cs`) while the sim spends bank — UI drift,
  not this beat's to invent.
