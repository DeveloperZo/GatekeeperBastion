# M45 — Two Runs in one process

<!--
Status: later
Update trigger: when a netcode library is chosen, when an ownership rule changes, or when a new singleton or Instantiate call site appears
Last updated: 2026-08-10
-->

Everything that becomes ruinously expensive after co-op starts, done now, while the game is
still single-player. **No netcode library, no second player, no transport.** The gate is one
EditMode test: two independent Run simulations advance in the same process without touching
each other.

## Story

As the developer, I want the single-player codebase to already have the shape co-op needs, so
that adding co-op is integration work rather than a rewrite.

## Why it matters

**This is the thing that kills co-op retrofits, and it is not the netcode.** Unknown Worlds on
Subnautica: adding multiplayer *"would take us many months of work. We almost have to start
over from the beginning, as it affects everything"*
([statement](https://unknownworlds.com/en/news/subnautica-multiplayer-when-if-how-and-why)).
They did not say the networking was hard. They said it touched everything.

**The existence proof is one person.** Lethal Company is solo-developed in Unity, 4-player PvE
co-op, 10M+ copies, host-authoritative, off-the-shelf netcode, no prediction, no dedicated
servers, no host migration ([networking notes](https://lethal.wiki/dev/advanced/networking)).
The architecture that ships this genre is not elaborate.

**The tree is close already.** Measured 2026-08-10: three static singletons, three `Find*` call
sites, four `Camera.main`, five `Instantiate`, three files using `Random`, seven reading
`Input` directly, and **zero `Time.` references in `Sim/`**. This is a week of work now and a
month of work after co-op starts.

Leverage tier (`../../Working-Rules.md`): **#10, structure.** No constant moves and nothing is
player-visible; what changes is what is possible later.

## The correction this milestone carries

**`noEngineReferences: true` is dropped, and M39 is cancelled.** The strict rule is load-bearing
for exactly one thing — cross-platform bitwise determinism for lockstep or deterministic
rollback — and this project is doing neither.

- Unity's own docs: NGO *"doesn't support full client-side prediction and reconciliation"*
  ([client anticipation](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.7/manual/advanced-topics/client-anticipation.html)).
- NGO's documented physics model runs Unity physics **on the server**
  ([physics](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.5/manual/advanced-topics/physics.html)),
  which a pure sim cannot call.
- Unity's own co-op reference sample puts game logic in `NetworkBehaviour` and uses
  `NavMeshAgent` server-side
  ([Boss Room architecture](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.7/manual/samples/bossroom/architecture.html)).
- The one architecture that mandates engine-freedom, Photon Quantum, says why in its own docs:
  fixed-point math for cross-platform determinism
  ([Fixed Point](https://doc.photonengine.com/quantum/current/manual/quantum-ecs/fixed-point)).

**Overwatch is the citation to stop using.** Its simulation is its engine, and the separation
it describes is by tick rate, not by type purity.

**What survives is the dependency direction, not the type ban.** `Sim/` may use `Vector3`,
`Quaternion` and `Mathf`. It may not touch a MonoBehaviour, a scene, or ambient time. The cost
of the strict version is concrete: no `Physics.Raycast` and no NavMesh inside the sim, and
`INetworkSerializable` lives in an assembly that references UnityEngine, so strict purity buys a
hand-written mapping layer for every replicated field, permanently.

## Acceptance criteria

### A — the seam is restated with the reason it actually has

1. **`Assets/Scripts/Sim/GatekeeperBastion.Sim.asmdef` exists with
   `"noEngineReferences": false`** and references only `UnityEngine.CoreModule`.
   `GatekeeperBastion`, `GatekeeperBastion.Editor`, `GatekeeperBastion.Tests` and
   `GatekeeperBastion.PlayTests` all reference it. **The two test assemblies set
   `"overrideReferences": true`** and inherit nothing, so a missing line there fails every test
   rather than one.
2. **The rule in `../../Architecture/1-Boundaries.md` reads: no `MonoBehaviour`, no
   `GameObject`, no scene lookup, no `Time`, no `Random`, no `SerializeField`, no
   `UnityEditor`** — and says nothing about `Vector3`. **The reason is rewritten from "the
   multiplayer seam" to testability and one-directional dependency**, in the same edit, with the
   sources above.
3. `../../Decisions.md` records the netcode model as **host-client, server-authoritative, over
   Steam's relay**, and records **client-authoritative own-movement** as correct for PvE with no
   anti-cheat requirement.
4. **Nothing in this milestone names a netcode library.** All of the work below transfers
   unchanged between NGO, Mirror and FishNet, and choosing one before the shape is right is how
   the choice gets made for the wrong reasons.

### B — nothing singular that must become plural

5. **No static instance holds Run or player state.** Three exist:
   `GameController.Instance`, `HatchStation.Instance`, `DecoyBay.Instance`. Each either becomes
   a field on an explicitly-passed context, or keeps its static accessor **and holds no
   per-player state**, stated per type. **`Player` must never become a singleton** — there is
   about to be more than one.
6. **No `FindObjectOfType`, `FindAnyObjectByType`, `GameObject.Find` or `Camera.main` in
   gameplay code.** Seven sites: `HudController.cs:121`, `MainMenuView.cs:140`,
   `UiPitchRandomizer.cs:17`, `FirstPersonController.cs:45,94,95`, `ShipHud.cs:168`. Replaced by
   a reference resolved once at spawn or a registry keyed by owner. **UI and menu code is not
   exempt** — those are the sites that silently return *a* player rather than *the* player.
7. **All gameplay `Instantiate` and `Destroy` go through one spawner with a stable ID.**
   `WaveSpawner.Groups.cs:169` is the only gameplay site; the four UI sites
   (`OptionsView.cs:305`, `ShortcutPanelView.cs:131,146`, `StationPanelView.cs:234`) are
   local-only and **explicitly exempt, named here so the exemption is deliberate.** VFX, decals
   and shells stay local forever.

### C — input, time and randomness become data

8. **Input is one serializable struct, produced in exactly one place.** Fields cover move, look
   and the action bits the game already binds. **No gameplay file calls `Input.` directly.**
   Seven do today; `Game/Input/Controls.cs` becomes the only one, plus `Dev/DevConsole.cs` which
   is dev-only and exempt.
9. **Gameplay time comes from one accessor, not from `Time.` inline.** Presentation keeps using
   `Time.deltaTime` freely. `Sim/` already has zero `Time.` references and must keep them.
10. **Every gameplay-affecting random draw comes from a seeded generator passed in**, and the
    seed is logged on `run begins`. Two sites: `WaveSpawner.Groups.cs` and
    `PlayerGun.Firing.cs`. **`UiPitchRandomizer.cs` is cosmetic and exempt.** Recoil spread and
    spawn placement disagreeing between machines is the class of bug this prevents, and it
    reproduces badly.
11. **The Run advances from one explicit `Tick(dt)` with a written system order**, called from
    one place, rather than several `Update()` methods relying on Unity's script order.

### D — the gate

12. **An EditMode test constructs two independent Run simulations in one process, advances both
    with different inputs, and asserts their states differ and neither affected the other.**
    **This is the milestone.** It passes only if criteria 5 through 11 all hold, and it fails
    the moment anyone reintroduces shared static state.
13. **`Tools/verify/run.sh` and the Unity suite both pass with the same test names as before,
    plus the new one.**
14. **No player-visible change.** A Run plays identically, and the session log is unchanged
    apart from the seed line added by criterion 10.

## Implementation

**Change** the asmdef, `1-Boundaries.md`, `Decisions.md` (criteria 1 to 4).

**Change** the three singletons, then the seven `Find*`/`Camera.main` sites, then
`WaveSpawner.Groups.cs`'s spawn, then the input struct, then the two random sites, then the
tick. **In that order, one per commit** — each is behaviour-preserving and a bisect should find
which one broke something.

**Create** the two-Run EditMode test last. It will fail until the rest lands; that is the point.

Rejected, and recorded so none of it is re-proposed:

| Not building | Why not, and when it would change |
|---|---|
| Client prediction and rollback | Solves own-character input lag under server authority. Client-authoritative own-movement is free and correct for PvE. Revisit only for PvP |
| Lag compensation | Server rewinding hitboxes. In PvE the shooting client can decide its own hits |
| Dedicated server | Costs money and a second build target forever. Host-client is standard at 2-4 players. If ever wanted, ship the binary and host nothing |
| Host migration | A product feature, and expensive. Lethal Company shipped without it |
| Anti-cheat | Players invite their friends |
| Custom serialization, interest management | Bandwidth problems that do not exist at four players |
| An ECS rewrite | A scope decision about entity counts, not a networking one. Do not let co-op be the excuse |
| Deterministic lockstep, fixed-point math | The requirement this project explicitly does not have |

Rejected: choosing the netcode library here. It is a `Decisions.md` entry when the first slice
is about to be built, not a prerequisite for any criterion above.

## Verification

1. Criteria 5 to 10 — the greps, expecting the named exemptions and nothing else.
2. Criterion 12 — the two-Run test. **Then seed a static back into `GameController` and confirm
   the test fails.** A gate that cannot fail is not a gate.
3. Criterion 13 — both suites, same names.
4. Criterion 14 — play one Wave and diff the session log against the previous session's; only
   the seed line is new.

## Watch

- **Criterion 12 is the whole milestone.** Everything above it is a means. If the two-Run test
  is hard to write, that is the finding, not an obstacle to route around.
- **The exemptions are the risk.** Every one of them is named here on purpose — UI
  `Instantiate`, the dev console's input, cosmetic random. An exemption added later without
  being named is how the rule dissolves.
- **Do not start the netcode integration inside this milestone.** The next slice is small and
  specific: *the client shoots one server-spawned enemy, it dies, and the wave counter reads the
  same number on both screens.* It gets its own file.
- **Networked physics is the part everyone underestimates.** The R.E.P.O. developers, on a
  Unity co-op game that sold millions: *"making all the physics feel smooth for clients was
  something we struggled with"*
  ([Photon case study](https://blog.photonengine.com/r-e-p-o-multiplayer-success-powered-by-photon/)).
  Budget for it when it arrives; do not pre-solve it here.

## Depends on / Unblocks

Depends on nothing. **Replaces M39**, which was premised on `noEngineReferences: true`.

**Unblocks the first co-op slice**, and is the reason it can be a slice rather than a rewrite.
