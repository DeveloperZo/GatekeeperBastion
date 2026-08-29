<!--
Purpose: Coming Soon marker for shared realms — entering a friend's world without its owner online; builder sections cover only what the code already holds.
Status: teaser; the machinery is built and wired into the chooser, and is awaiting cross-machine verification.
in_build: true
coming_soon: true
Update trigger: replace this page when shared realms ship.
Last updated: 2026-08-29
-->

# Shared Realms — Coming Soon

A shared realm is a friend's world a Boarder can open from the chooser even when its owner
is not online. Hosting a session is what shares a realm and admits a guest, and afterwards
that world shows on the guest's chooser as a borrowed row. Opening one takes a lock, so
two members cannot hold the same world at once; a member playing a borrowed realm banks
into and spends from the owner's one pot, and the owner finds the world advanced when they
next open it. The machinery is built and wired to the chooser, but it has never been run
between two real machines, and there is no way yet to take a member off a realm or to
unshare it.

## Game

Coming Soon.

Today a friend's world exists for me only while they hold the door open. The rest is on its way: worlds with members, a door that answers when the owner is not home, their Ship walked alone, my conversions waiting in their pot when they wake.

When it works, these notes will say how it works. Not before.

## The interaction

**The rules.** A shared realm lives in the platform store as one self-written copy per member — an envelope (owner, admitted members, revision, lock claim) beside the realm body — because the store lets a player write only their own data. A reader fetches every member's copy and the freshest one is the realm. A holder heartbeats the live body into its copy and releases on close, advancing the revision.

Opening takes a lock, which settles three ways:

- a live claim refuses by name;
- a stale-looking claim is observed for a fixed window before it may be walked past;
- two simultaneous claims settle by one deterministic rule, so both sides cannot proceed.

**The way in is wired, and this is where it runs.** Hosting a session over Relay is the invitation: it makes the current realm shared, uploads the first copy, admits each guest as they connect, and publishes the realm's identity on the wire, so a guest's Boarder remembers the borrowed realm. That remembered row is then drawn on the chooser under the picked Boarder, marked `[friend's]`, and **ENTER on it runs the whole owner-offline open** — fetch every member's copy, decide, observe a stale claim, claim, survive the contest, and sail to the co-op Ship on the fetched realm.

An owned realm that has been shared takes the same lock before ENTER, so the owner is one member among the members where the lock is concerned. DELETE on a borrowed row is a two-press FORGET rather than a delete, a row the realm no longer admits is pruned when entry is refused, and a borrowed session's writes go to a separate working copy that never touches the local realms directory.

**What is genuinely not built.** There is no way to revoke a member or to unshare a realm from inside the game: `RealmShare.RemoveMember` and the store's `DeleteOwnCopy` have no caller anywhere in `Assets/Scripts`. Nothing shows a realm's member list, and the borrowed row's `[friend's]` mark is the only thing on any surface that says a realm is shared at all.

**What is not verified.** None of the above has been exercised across two real machines against the live store: `M100` landed with its rules tested offline and its flows tested against a faked store, and its two-machine criteria are owed. How any of it behaves between two players is *unknown, needs play data*. So this page states what the code does, not what the feature does in a player's hands. The Coming Soon marker is not a claim that the machinery is missing.

Driven by ENTER on a `[friend's]` row at the chooser, by ENTER on an owned realm
that has been shared (same lock), and by hosting over Relay, which is what shares a
realm and admits a guest. Shown by the borrowed row and its `[friend's]` mark, the
"Opening … from the store…" status line, and the named refusals — open right now by
whom, no longer admits this player, opened by someone else at the same moment.
Reads back as the `entered SHARED realm "…" as "…"` line and the `RealmShare`
events: `realm … is now SHARED`, `admitted …`, `BROKE stale claim by …`,
`released realm … at rev N`.

## Consequences

Where a conversion lands and whose world a session changes stop depending on who is
online: a member playing a borrowed realm banks into and spends from the owner's one
pot ([Realms](Realms.md)), and the owner finds their world advanced when they next
open it — the store's freshest copy becomes their local file, under the lock, before
they enter.

The lock is the other thing to account for: two members cannot hold one
realm at once, so a friend already inside refuses the next member by name, and a realm
whose holder crashed is openable only once its claim has gone stale and been observed.

Removal test: take the machinery out and a friend's world is reachable only while
its owner hosts, and the Boarder's kit is the only half that crosses machines —
decisions change, so it is a mechanic.

## Classification

Secondary, loop. Serves Fellowship (`../Design/1-Experience.md` §1.2, 2026-08-16 rank
stamp). Built and wired to the chooser; unverified across two machines — how it behaves
between two players is *unknown, needs play data* — and with no revoke or unshare surface.

## Interacts with

- [Realms](Realms.md) — the same realm/Boarder split and the same one pot; a member playing a borrowed realm banks into and spends from the owner's world. That page owns the chooser and the one-pot coupling.
- [The Crew](The-Crew.md) — hosting over Relay is what shares a realm and what admits a member; a session is how a Boarder earns the row it can later open alone. That page owns the session.

## Where

- Envelope, membership, freshest-copy pick, lock liveness, claim/beat/release/contest rules (pure, testable) — `Assets/Scripts/Sim/Meta/RealmShare.cs` (`RealmShareEnvelope`, `Admits`, `AddMember`, `PickFreshest`, `ClaimLooksLive`, `DecideOpen`, `Claim`, `Beat`, `Release`, `LosesContest`)
- This machine's held lock, owned-shared acquire before ENTER, host-share flow — `Assets/Scripts/Game/Flow/RealmShareService.cs` (`TryAcquireOwned`, `EnsureSharedForHost`, `InstallHold`)
- The remote open dance: fetch every copy, decide, observe a stale claim, claim, survive the contest — `Assets/Scripts/Game/Flow/RealmShareRemote.cs` (`OpenRemote`, `FetchAndDecide`, `ClaimAndConfirm`)
- The real store: Cloud Save player data, public access class, per-member copies — `Assets/Scripts/Game/Flow/CloudRealmStore.cs` (`WriteOwnCopy`, `ReadCopyOf`)
- Heartbeat and release on close — `Assets/Scripts/Game/Flow/RealmShareBeacon.cs`
- Hosting over Relay is what shares the realm, and a connecting guest is admitted — `Assets/Scripts/Game/Flow/RealmSelectController.Hosting.cs` (the `relay` branch calling `EnsureSharedForHost`); `Assets/Scripts/Game/Net/NetSession.Internal.cs` (`OnConnectionApproval` → `RealmShareService.AdmitGuest`)
- A guest's chooser remembers the borrowed realm off the wire — `Assets/Scripts/Game/Net/NetPartyLobby.cs` (`SharedRealmId`, `SharedRealmOwner`, `SharedRealmName`, `PublishSharedRealm`, `RememberIfShared`); remembered rows live on the Boarder — `Assets/Scripts/Sim/Meta/BoarderState.cs` (`RememberRealm`, `KnownRealmAt`, `ForgetRealm`)
- Borrowed rows are built onto the chooser under the picked Boarder and marked, and ENTER on one runs the remote open — `Assets/Scripts/Game/Flow/RealmSelectShared.cs` (`BuildRows`, `IsRemote`, `Enter`, `AcquireIfShared`); `Assets/Scripts/Game/Flow/RealmSelectController.cs` (`OnEnter`, the `_shared.IsRemote` branch and the `AcquireIfShared` call before a local ENTER); the row's mark — `Assets/Scripts/Game/Ui/RealmSelectView.cs` (`[friend's]` / `[co-op]` labels)
- A borrowed session adopts the fetched realm in memory and its writes never touch the local realms directory — `Assets/Scripts/Game/Flow/GameSession.cs` (`EnterRemoteRealm`, `CurrentRealmIsRemote`, the `SaveRemoteRealm` branch in `Save`)
- No revoke and no unshare: these have no caller anywhere in `Assets/Scripts` — `Assets/Scripts/Sim/Meta/RealmShare.cs` (`RemoveMember`), `Assets/Scripts/Game/Flow/IRealmStore.cs` and `Assets/Scripts/Game/Flow/CloudRealmStore.cs` (`DeleteOwnCopy`)
- The flows are exercised against a substituted store and a shortened observation window, never against the live service in a test — `Assets/Scripts/Game/Flow/RealmShareService.cs` (`UseStore`, "Test seam. Production never calls it."); `Assets/Scripts/Game/Flow/RealmShareRemote.cs` (`ConfirmDelayMsOverride`); the two-machine criteria owed are the milestone's own record, [`M100`](../Milestones/3.%20completed/M100-a-realm-you-can-open-while-its-owner-is-offline.md)
