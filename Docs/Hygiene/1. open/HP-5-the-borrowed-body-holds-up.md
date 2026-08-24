# HP-5 — The borrowed body holds up

<!--
Status: open
Closable by: Zo at the keyboard, two editors (the remote body and the gun need a second seat)
Update trigger: when an item is added, settled or evicted
Last updated: 2026-08-24
-->

One fight with a second seat: watch the Tactical-dressed Boarder move, hold its gun, and
read apart from the Cloaker; sweep the mixamo remnant.

The Boarder was re-dressed onto `SK_Tactical` on 2026-08-24 (blue `_PaintColor`, cyan
visor via `Packs/Boarder/BoarderBot.mat`) as a placeholder until a real Boarder model
exists. The swap was verified structurally — `Animator.Rebind()` maps hips, head and
right hand, and the prefab kept every fileID — but nothing about it has been watched
running, and `M63-3` put the Cloaker on the same robot the same day.

## Items

1. **TH-21** — the held gun on a remote avatar. `Grip`'s local offsets were tuned for the
   mixamo hand and were carried unchanged onto `hand.r`. From the Boarder redress,
   2026-08-24. Settles when a second seat sees the other Boarder's gun sitting in its
   hand, not floating beside it.
2. **TH-22** — the retarget in motion. The `Boarder.controller` clips (idle / walk / run,
   Fire, Reload, Dead) have only been proven to bind, never watched on the Tactical rig.
   From the Boarder redress, 2026-08-24. Settles on eyes-on of a moving remote or iso
   body animating without distortion.
3. **TH-23** — the Boarder-vs-Cloaker read. Boarder and Cloaker now share the
   `SK_Tactical` silhouette (`M63-3`, same day); the blue-paint / cyan-visor tell has
   never been observed against a revealed Cloaker. Settles when a fight with a Cloaker
   on screen shows the two read apart at a glance.
4. **TH-24** — the mixamo remnant. `WeaponView.RightHandBone` still names
   `mixamorig:RightHand`, which matches no bone since the redress; its one consumer fills
   `WeaponViewModel._rightHand`, which nothing reads. Settles when the constant and the
   dead field are retired or repointed in an edit that says so.

## Not this pass

- The real Boarder model — the placeholder exists because that model does not; replacing
  it is a milestone when it is cut, not an item here.
- The Cloaker's own eyes-on — owed by `M63-3`'s close, not by this pass.
- Whether blue-on-Tactical is the *right* friendly read — a design call for the model
  replacement; this pass only observes that the placeholder tells apart.
