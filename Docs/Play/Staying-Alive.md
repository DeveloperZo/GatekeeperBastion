<!-- Purpose: tactics that follow from verified mechanics — how the AI, sprint, repair and weapon systems actually behave. / Status: working / Update trigger: when gatekeeper targeting, provocation, sprint/stamina, repair costs or weapon slot handling change. / Last updated: 2026-08-16 -->

# Staying Alive

## Game

Your health does not come back during a Run. The Hatch comes back only when you repair it with the Repair Rig and banked Salvage. Every tactic here is about spending as little of those two pools as possible per kill.

On a Hatch-bank, hurt Defaults on the approach — a pull is permanent, and once they commit to the Hatch they will not leave. On a field-terminal Wave they are already on you. One shot shouts one group, not the whole Bastion. Sprint outruns a chase; walking does not.

Triage by refusal: Hunters cost health, Thieves cost the door on a Hatch-bank and your attention on a field Wave (they never hurt you), Defaults are the only kind you can steer. Bindings and numbers: [Field Manual](Field-Manual.md).

## Reference

Every tactic here is a rule the room already runs. Bindings and numbers are in the [Field Manual](Field-Manual.md).

The two things that end a Run are your health and the Hatch's, and neither comes back for free. Your health does not come back **at all** during a Run. The Hatch's comes back only when you stand at it with the Repair Rig equipped and spend banked Salvage. So every tactic below is really about the same thing: spending as little of those two pools as possible per Gatekeeper killed.

### Pull them off the door on a Hatch-bank, because a pull is permanent

On a **field-terminal Wave** nobody is walking to the Hatch. They are walking at you. Pulling is not a door trade there.

On a **Hatch-bank**, a basic Gatekeeper marches on the Hatch and ignores you — until you hurt it. Hurting it flips it onto you **for the rest of its life**. It does not calm down, it does not go back to the door, it does not lose interest.

That makes damage a steering tool. A marcher you shoot on its way in is a marcher that will never touch the Hatch, even if you then leave it alone to deal with something else.

But there is a deadline. Once a marcher gets close enough to the Hatch it **commits to it permanently**, and from then on shooting it does nothing to its mind — it is a door problem until it dies. The latch is one-way and it is the single most useful piece of timing in the game: hurt them on the approach and they're yours, hurt them at the door and you have to kill them there.

### One shot pulls a group, but only one layer deep

Damaging a Gatekeeper also alerts nearby ones and flips them onto you as well. Those alerted Gatekeepers do **not** alert their own neighbours — the shout does not relay, so one shot pulls one clump, not the whole Bastion.

Use it deliberately: a single shot into the middle of a fresh group is often better than carefully killing one Gatekeeper, because it turns the whole clump into a problem you control the position of instead of a clump walking at the door.

Do not use it on a group that is already spread across the room, because you get the pull without getting them bunched.

### You can be seen without firing a shot

An un-provoked Gatekeeper that has a clear line of sight to you at a distance will notice you and come. Line of sight is a real raycast — anything solid between you breaks it.

So standing in a long open sightline is a decision, not a neutral position. Fighting from a spot with cover between you and the spawn points means you choose when the pull happens instead of the geometry choosing for you.

### Sprint outruns a chase; walking does not

A provoked Gatekeeper chases faster than you walk and slower than you sprint. That is deliberate and it means:

- **Backing away while shooting does not work.** They close on you the whole time. It is not a stalemate, it is a slow loss.
- **Sprinting does work, until the stamina runs out.** Sprint is for breaking contact, crossing the room, or getting back to the Hatch — not for kiting indefinitely.

Three details about the stamina that are not shown to you:

- **Bottoming out locks sprint**, and it stays locked until a decent chunk of the pool is back — not the instant the bar starts refilling. If you empty the tank at the wrong moment you are walking for meaningfully longer than the bar suggests. Leave a reserve.
- **Regeneration has a delay after you stop sprinting.** Tapping sprint repeatedly keeps resetting that delay and refills nothing.
- **Holding sprint while standing still costs nothing.** Sprint only engages when you are actually moving.

### Triage by what each kind refuses to do

Each kind is defined by something it will not do, and that refusal tells you what ignoring it costs.

- **Hunters never touch the Hatch.** Ignoring one costs you health and nothing else. If the door is the emergency, a Hunter on your back is the cheaper problem — right up until your health is the emergency instead.
- **Thieves hunt you on a field-terminal Wave and cannot be provoked.** Shooting one in the back changes nothing about it: not its speed, and it does not shout. It never hurts you. On a Hatch-bank, ignoring one costs the door a slow, heavy bite.
- **Marchers are the only kind you can steer**, per the pull rule above — and only on a Hatch-bank.

The practical read: when everything arrives at once on a Hatch-bank, the Thieves are on a clock against the door and the Hunters are on a clock against you. You almost always have more slack against the Hunters, because your health can absorb a swing and the Hatch's absorbs the bite badly. On a field-terminal Wave the Hatch is invulnerable; the clock is only yours.

### Repair in Prep, not in Assault

Repairing means standing still at the Hatch holding repair — which is exactly the spot and exactly the posture that everything in the room is heading toward. Prep exists for this. It is untimed and nothing spawns in it.

Repairing during an Assault is sometimes right, but understand what you are paying: you are stationary, at the convergence point, with no way to shoot, while Gatekeepers are walking to your position specifically.

It needs the Repair Rig equipped — without it, holding repair does nothing — and it costs banked Salvage per point of health, from the same purse you buy upgrades with. It will spend the bank down to exactly zero if you hold long enough. The trade is usually worth it — a lost run stops all income — but make it a decision.

### Weapon handling that follows from the slot rules

- **Each slot keeps its own clip.** Switching weapons is not a free reload, but it is faster than one: if your primary runs dry mid-group, switching to the sidearm and firing is quicker than reloading, and you can reload the primary afterwards in the quiet.
- **Switching cancels a reload in progress, and the progress is lost** (default). The HUD warns while you reload if another slot is occupied; the log names what was discarded. Do not switch mid-reload unless you mean it.
- **Ammo is unlimited.** There is no reserve. Reload at every opportunity — the only cost is the time.
- **Crits do not happen.** Chance is frozen at nothing. There is no Luck track at CREW QUARTERS and nothing else grants crit chance. Do not play for burst you have not bought.

### Positioning, in one paragraph

Groups arrive at one spawn point at a time and the point rotates group by group, so pressure moves around the room predictably once you have watched a Wave. Fight where you can see the approach with something solid between you and the far sightlines, close enough to the Hatch to intercept marchers before they commit to it, far enough that you are not the thing standing where every path converges. That spot exists in the room; find it in your first Prep rather than during a Wave.

## See also

[Gatekeepers](Gatekeepers.md). [The Hatch](The-Hatch.md). [Weapons](Weapons.md). [A Run](A-Run.md). [Upgrades](Upgrades.md).

## Gaps

Tactics on this page that only work because of behaviour the game never tells you about:

- **The permanence of a pull.** Nothing states that provoking a Gatekeeper is permanent for its life. Steering marchers away from the door with a single shot is only reliable because of this, and it is invisible.
- **The Hatch commit latch.** Nothing states that a Gatekeeper close to the Hatch stops being provokable. Players who learn to pull will keep trying it at the door and quietly get nothing.
- **The shout not relaying.** Nothing states that alerted Gatekeepers do not alert others. The "one shot pulls one clump" tactic depends entirely on this bound and there is no way to observe it except by inference.
- **Line-of-sight noticing.** Nothing tells you that an un-provoked Gatekeeper can spot you at range through a clear sightline. Being pulled without firing reads as random.
- **The exhaustion latch.** The stamina bar refills visibly while sprint is still locked out. Nothing on screen distinguishes "recovering" from "usable", so the sprint you were counting on can simply not engage.
- **The stamina regeneration delay**, and that it runs even while a panel is open. Neither is shown.
- **That sprint outruns a chase and walking does not.** The relationship between your speeds and theirs is the single most important combat fact in the game and it appears nowhere on the Bastion.
- **That ammo is unlimited.** Never stated, so players ration for no reason.
- **That Thieves cannot be provoked at all.** The HUD gives no feedback distinguishing "this thing is ignoring me" from "I am not doing enough damage", so players empty clips into Thieves expecting them to turn.
- **That repair drains the bank to zero if held.** The prompt shows the rate and the running total, but there is no warning at the point the purse is about to empty.
- **That repair needs the Repair Rig equipped.** Without it the key does nothing, at any bank balance, and nothing at the door says so.
- **Spawn-point rotation.** Learnable by watching, never marked.
