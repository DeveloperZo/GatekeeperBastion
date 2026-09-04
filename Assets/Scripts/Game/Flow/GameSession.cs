using System;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY composition root: the one place that owns the loaded MetaState and all
    /// SaveService IO for the whole app (allowed exception to the no-new-statics rule).</summary>
    /// <remarks>
    /// Scene controllers read GameSession.Meta; every persistence call in the codebase goes
    /// through Save()/SaveBoarder()/SaveRealm()/StartNew()/ContinueGame() — nothing else may
    /// touch SaveService directly.
    ///
    /// A session is a pair: a Boarder that belongs to this player and a realm that belongs to
    /// whoever owns that world. The two are written independently, because the ownership rule
    /// is not symmetric — a client writes its own Boarder, and only a world's host writes that
    /// world. An earlier version of this comment claimed remote clients never mutate any of
    /// this, which was never true of the Boarder half and is the reason the split exists.
    ///
    /// Load and save are exception-guarded: a corrupt or unreadable save file must degrade to a
    /// fresh session, never take the game down on the way into or out of a run.
    /// </remarks>
    public static class GameSession
    {
        const string Tag = "GameSession";

        /// <summary>The live cross-run progression state, or null before StartNew/ContinueGame.</summary>
        public static MetaState Meta { get; private set; }

        /// <summary>Id of the realm this session is playing in, or empty. On a guest this names
        /// the guest's own last realm, not the host's — a visitor does not own the world it is
        /// standing in and must never write it.</summary>
        public static string CurrentRealmId { get; private set; } = "";

        /// <summary>Id of the Boarder this session is playing as, or empty.</summary>
        public static string CurrentBoarderId { get; private set; } = "";

        /// <summary>True while the open realm is a BORROWED one — fetched from another
        /// player's store under the shared-realm lock. Its writes go to the remote working
        /// copy, never into this machine's own realms directory
        /// (Docs/Milestones — a realm you can open while its owner is offline).</summary>
        public static bool CurrentRealmIsRemote { get; private set; }

        /// <summary>True once any Dev command has run this play session. A tainted session must
        /// never overwrite a real campaign save — <see cref="Save"/> becomes a no-op.</summary>
        public static bool DevTainted { get; private set; }

        /// <summary>1-based start wave chosen at the MAP. Consumed by GameController on embark;
        /// resets to 1 after the run begins so a direct-play fallback stays a normal start.</summary>
        public static int PendingStartWave { get; set; } = 1;

        /// <summary>The kit chosen at the practice range door, carried across the scene load
        /// the way <see cref="PendingStartWave"/> carries the MAP's pick. RangeConsole adopts
        /// and clears it on the range's first frame; null means the range was opened directly
        /// and hands out the whole catalogue as before. Transient: never saved.</summary>
        public static RangeLoadout? PendingRangeLoadout { get; set; }

        /// <summary>Set on Escaped when EndDemo flips DemoActive off, consumed on the first
        /// Ship frame so demo.escaped can play after the director's DemoActive-false path
        /// would otherwise skip every line. Transient: never saved.</summary>
        public static bool PendingEscapedLine;

        /// <summary>F12 Bastion-fight digit (2, 4, 6, 7). Consumed by GameController.Start
        /// after the run tethers so the player lands at the Hatch or Master with the
        /// authored Unbanked. 0 = none. Transient: never saved.</summary>
        public static int PendingDemoFight;

        /// <summary>F12 beat digit the CURRENT session state was built from, 0 when this
        /// session is not a checkpoint session. Read by the run-summary Metric line so the
        /// dashboard can tell checkpoint play from a played-through save. Transient.</summary>
        public static int CheckpointBeat;

        /// <summary>Loadout configuration row the checkpoint applied (0 = beat as authored).
        /// Transient.</summary>
        public static int CheckpointConfig;

        /// <summary>Verdict of the beat-relative taint rule
        /// (GameController.CheckpointConfigTainted), judged against the constructed beat
        /// state before the overlay landed. Carried onto the run-summary Metric line as
        /// configTaint. Transient.</summary>
        public static bool CheckpointConfigTaint;

        /// <summary>Clears statics at play-mode start (domain-reload-disabled safety).</summary>
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Meta = null;
            CurrentRealmId = "";
            CurrentBoarderId = "";
            CurrentRealmIsRemote = false;
            DevTainted = false;
            PendingStartWave = 1;
            PendingRangeLoadout = null;
            PendingEscapedLine = false;
            PendingDemoFight = 0;
            CheckpointBeat = 0;
            CheckpointConfig = 0;
            CheckpointConfigTaint = false;
        }

        /// <summary>The current session state is no longer a checkpoint construction —
        /// every path that adopts a real save pair calls this beside DevTainted = false.</summary>
        static void ClearCheckpoint()
        {
            CheckpointBeat = 0;
            CheckpointConfig = 0;
            CheckpointConfigTaint = false;
        }

        /// <summary>Mark this play session as Dev-touched. First call logs once; Save is then skipped.</summary>
        public static void NotifyDevAction()
        {
            if (DevTainted) return;
            DevTainted = true;
            Log.Event("Dev", "session tainted — GameSession.Save suppressed for this play session");
        }

        /// <summary>No realm is open any more. Called when the menu loads: the ids name what
        /// this session is STANDING IN, and once you are back at the menu you are standing in
        /// nothing. Leaving them set made the chooser refuse to delete the realm you had just
        /// played — "go back to the menu first" said to a player already at the menu, with no
        /// action in the game that could satisfy it short of relaunching.</summary>
        public static void ReleaseCurrent()
        {
            if (string.IsNullOrEmpty(CurrentRealmId) && string.IsNullOrEmpty(CurrentBoarderId))
                return;
            CurrentRealmId = "";
            CurrentBoarderId = "";
            CurrentRealmIsRemote = false;
            Log.Event(Tag, "no realm open — back at the menu");
        }

        /// <summary>Dev checkpoints only: replace the live Meta with a constructed state for
        /// this play session. Taints FIRST, so the constructed state can never reach the real
        /// campaign save — quitting or reloading from disk returns the untouched campaign.</summary>
        public static void AdoptDevState(MetaState state)
        {
            if (state == null)
            {
                Log.Warn(Tag, "AdoptDevState ignored — null state.");
                return;
            }
            NotifyDevAction();
            Meta = state;
            // A constructed state is a local pair by definition — never leave a previous
            // borrowed-realm session's write routing behind it.
            CurrentRealmIsRemote = false;
            // The ids name whatever was ENTERED. A constructed state is not on disk under
            // them, so leaving them set would have any later reader pointing at documents
            // this session is not actually playing.
            CurrentBoarderId = state.Boarder == null ? "" : state.Boarder.Id;
            CurrentRealmId = state.Realm == null ? "" : state.Realm.Id;
        }

        /// <summary>True when there is a pair on disk this build can actually open. Not
        /// merely "files exist": offering CONTINUE for documents that will be refused turns
        /// one press into a silent new campaign.</summary>
        public static bool HasSave
        {
            get
            {
                try
                {
                    SaveService.ReportLegacySaveOnce();
                    return RealmRegistry.HasLoadablePair();
                }
                catch (Exception e)
                {
                    Log.Warn(Tag, $"HasSave check failed ({e.GetType().Name}: {e.Message}); assuming no save.");
                    return false;
                }
            }
        }

        /// <summary>Begin a fresh campaign: a new Boarder in a new realm, both written at once.
        /// This is the path a player who never opens the realm list still takes, so it has to
        /// mint both halves rather than assume the list did it.</summary>
        public static void StartNew()
        {
            DevTainted = false; // intentional new campaign may persist again
            ClearCheckpoint();
            bool boarderWritten, realmWritten;
            var boarder = RealmRegistry.CreateBoarder(null, out boarderWritten);
            var realm = RealmRegistry.CreateRealm(null, out realmWritten);
            Adopt(boarder, realm);
            if (!boarderWritten || !realmWritten)
                Log.Error(Tag, "A new campaign could not be written to disk "
                    + "(Boarder " + (boarderWritten ? "saved" : "FAILED")
                    + ", realm " + (realmWritten ? "saved" : "FAILED")
                    + "). Play will continue, but nothing is being kept.");
            Log.Event("Ship",
                $"new campaign — realm \"{realm.Name}\", {Meta.BankedSalvage} banked to start with, no weapon yet");
        }

        /// <summary>Take a Boarder into a realm. The realm list's whole job ends here: it
        /// chooses the two documents, and this makes them the session.</summary>
        public static bool EnterRealm(string realmId, string boarderId)
        {
            var realm = SaveService.LoadRealm(realmId);
            var boarder = SaveService.LoadBoarder(boarderId);
            if (realm == null || boarder == null)
            {
                // SaveService has already named the document that refused and why. What this
                // adds is that nothing was entered, so the caller can stay where it is.
                Log.Warn(Tag, "Could not enter realm — the session was left as it was.");
                return false;
            }
            DevTainted = false;
            ClearCheckpoint();
            CurrentRealmIsRemote = false;
            Adopt(boarder, realm);
            Log.Event("Ship",
                $"entered realm \"{realm.Name}\" as \"{boarder.Name}\" — {Meta.BankedSalvage} banked, {LoadoutLog(Meta)}");
            return true;
        }

        /// <summary>Take a Boarder into a BORROWED realm — one fetched from another player's
        /// store, already holding the shared-realm lock. The realm object arrives in memory
        /// because it has no file of its own here yet; the working copy is written on the
        /// first save. The last-played pointer is deliberately NOT set: CONTINUE resolves
        /// against this machine's own realms, and a borrowed world is not one of them.</summary>
        public static bool EnterRemoteRealm(RealmState realm, string boarderId)
        {
            var boarder = SaveService.LoadBoarder(boarderId);
            if (realm == null || boarder == null)
            {
                Log.Warn(Tag, "Could not enter the shared realm — the session was left as it was.");
                return false;
            }
            DevTainted = false;
            ClearCheckpoint();
            Meta = MetaState.Compose(boarder, realm);
            CurrentBoarderId = boarder.Id;
            CurrentRealmId = realm.Id;
            CurrentRealmIsRemote = true;
            RealmRegistry.LastBoarderId = boarder.Id;
            SaveService.SaveRemoteRealm(realm);
            Log.Event("Ship",
                $"entered SHARED realm \"{realm.Name}\" as \"{boarder.Name}\" — {Meta.BankedSalvage} banked, {LoadoutLog(Meta)}");
            return true;
        }

        /// <summary>Swap which Boarder is playing, leaving the realm exactly as it is. The
        /// chooser uses it to change who goes in without reopening the world, and it is the
        /// seam that lets a co-op test hold two Boarders at once: in-process, host and guest
        /// share this one static, so without a way to put a second Boarder in place there is
        /// nothing for "the host's Boarder was not written" to be measured against.</summary>
        public static bool UseBoarder(BoarderState boarder)
        {
            if (boarder == null)
            {
                Log.Warn(Tag, "UseBoarder ignored — no Boarder given.");
                return false;
            }
            if (Meta == null)
            {
                Log.Warn(Tag, "UseBoarder ignored — no realm is open to take it into.");
                return false;
            }
            Adopt(boarder, Meta.Realm);
            Log.Event("Ship", "Boarder swapped in — \"" + boarder.Name + "\"");
            return true;
        }

        /// <summary>Compose the pair into the live session and remember it as the one to
        /// continue into next time.</summary>
        static void Adopt(BoarderState boarder, RealmState realm)
        {
            Meta = MetaState.Compose(boarder, realm);
            CurrentBoarderId = boarder.Id;
            CurrentRealmId = realm.Id;
            RealmRegistry.LastBoarderId = boarder.Id;
            RealmRegistry.LastRealmId = realm.Id;

            // The ship tutorial is a lesson, and a lesson is learned by the PLAYER once — but
            // its flags live on the realm, so without this every new world would teach it
            // again. The preference is set the first time it is finished; from then on any
            // world opens with it behind you. Untick it in options to be taught again.
            if (Settings.GetBool(SettingId.SkipShipTutorial)
                && DemoProgress.SkipShipTutorial(Meta))
            {
                Log.Event(Tag, "ship tutorial skipped — already learned on this machine");
            }
        }

        /// <summary>Load the persisted campaign into Meta. Falls back to a fresh session on failure.</summary>
        public static void ContinueGame()
        {
            DevTainted = false; // disk load is a clean slate for save suppression
            ClearCheckpoint();
            try
            {
                string realmId, boarderId;
                RealmRegistry.ResolveContinuePair(out realmId, out boarderId);
                if (realmId == null || boarderId == null)
                {
                    // Something is on disk or we would not be here, and none of it can be
                    // opened. Name every refused document before falling through to a new
                    // campaign, or the player presses Continue and simply loses their world.
                    RealmRegistry.ReportUnloadable();
                    Log.Warn(Tag, "Nothing on disk can be opened by this build — "
                        + "starting a new campaign rather than continuing. "
                        + "The documents above have been left exactly where they are.");
                    Meta = null;
                }
                else if (!EnterRealm(realmId, boarderId))
                {
                    Meta = null;
                }
            }
            catch (Exception e)
            {
                // Guard trip: a corrupt save must not brick the boot path.
                Log.Error(Tag, $"Load failed ({e.GetType().Name}: {e.Message}); starting a fresh session.");
                Meta = null;
            }
            if (Meta == null)
            {
                StartNew();
                return;
            }
            Log.Event("Ship",
                $"campaign continued — {Meta.BankedSalvage} banked, {Meta.LifetimeSalvageEarned} earned, " +
                $"{Meta.LifetimeSalvageSpent} spent, {Meta.TotalRuns} run(s), " +
                $"{Meta.PodEjections} run(s) lost, {LoadoutLog(Meta)}");

            if (Meta.SalvageStatsWereRepaired)
            {
                // Either a pre-v2 save (no lifetime fields) or a hand-edited one: Earned/Spent were
                // rebuilt from the owned upgrade levels so Earned == Banked + Spent holds again.
                Log.Warn(Tag, $"Lifetime salvage stats rebuilt on load — {Meta.LifetimeSalvageEarned} earned, " +
                              $"{Meta.LifetimeSalvageSpent} spent (reconstructed from owned upgrades).");
            }

            if (!Meta.OwnsAnyWeapon)
            {
                // Guard trip: no weapon. In the demo this is the intended pre-grant state and
                // the ARMORY is hidden — pointing there would contradict the flow. Outside it,
                // the cutter fallback is survivable but worth flagging. MK-1 costs 0.
                Log.Warn(Tag, Meta.DemoActive
                    ? "No weapon owned yet — the ship terminal's start grants the MK-1."
                    : "No weapon owned. Runs will use the improvised salvage cutter " +
                      "until you take the free MK-1 at the ARMORY.");
            }
        }

        static string LoadoutLog(MetaState m)
        {
            if (!m.OwnsAnyWeapon) return "weapon NONE";
            string p = m.EquippedPrimary == WeaponId.None
                ? "—"
                : $"{WeaponCatalog.Def(m.EquippedPrimary).displayName} T{m.TierOf(m.EquippedPrimary)}";
            string s = m.EquippedSecondary == WeaponId.None
                ? "—"
                : $"{WeaponCatalog.Def(m.EquippedSecondary).displayName} T{m.TierOf(m.EquippedSecondary)}";
            return $"Primary={p}, Secondary={s}";
        }

        /// <summary>Persist the current Meta to disk. No-op (with a warning) when Meta is null.
        /// Also a no-op when <see cref="DevTainted"/> — Dev loadouts / grants must not overwrite
        /// a real campaign (skip Save while Dev mode has been used).</summary>
        public static void Save()
        {
            if (RefusesWrite()) return;
            WriteBoarder();
            WriteRealm();
        }

        /// <summary>Persist only the Boarder. This is the write a guest is allowed to make: its
        /// kit is its own wherever it earned it, and the world it is visiting is not.</summary>
        public static void SaveBoarder()
        {
            if (RefusesWrite()) return;
            WriteBoarder();
        }

        /// <summary>Persist only the realm. Only the owner of a world writes that world.
        /// Deliberately independent of the Boarder write — requiring both would mean a guest
        /// could not save its own kit without also claiming somebody else's world.</summary>
        public static void SaveRealm()
        {
            if (RefusesWrite()) return;
            WriteRealm();
        }

        /// <summary>The refusal both write paths share: a Dev-tainted session and an unloaded
        /// session are each a no-op, and each says so.</summary>
        static bool RefusesWrite()
        {
            if (DevTainted)
            {
                Log.Event("Dev", "Save skipped — session tainted by Dev mode");
                return true;
            }
            if (Meta == null)
            {
                Log.Warn(Tag, "Save called with no session loaded; ignored.");
                return true;
            }
            return false;
        }

        static void WriteBoarder()
        {
            try
            {
                SaveService.SaveBoarder(Meta.Boarder);
            }
            catch (Exception e)
            {
                // Guard trip: a failed save must not abort the caller — this runs on the death path.
                Log.Error(Tag, $"Boarder save failed ({e.GetType().Name}: {e.Message}); kit earned this run may be lost.");
            }
        }

        static void WriteRealm()
        {
            try
            {
                // A borrowed realm writes to its working copy, never into this machine's own
                // realms directory — the chooser lists that directory as "your realms".
                if (CurrentRealmIsRemote) SaveService.SaveRemoteRealm(Meta.Realm);
                else SaveService.SaveRealm(Meta.Realm);
                Log.Info(Tag, $"Saved (banked {Meta.BankedSalvage}, runs {Meta.TotalRuns}).");
            }
            catch (Exception e)
            {
                Log.Error(Tag, $"Realm save failed ({e.GetType().Name}: {e.Message}); progress this run may be lost.");
            }
        }

        /// <summary>
        /// Fallback for direct-play/tests entering a gameplay scene without passing the menu:
        /// continue the save if one exists, otherwise start a fresh session. No-op when loaded.
        /// </summary>
        public static void EnsureLoaded()
        {
            if (Meta != null) return;
            Log.Warn(Tag, "No session loaded (direct play?) — resolving one now.");
            if (HasSave) ContinueGame();
            else StartNew();
            // Volumes also apply on Settings.Bind / Splash; re-push for direct-play
            // scenes that skip the menu so AudioSources spawned here see the saved buses.
            AudioBuses.ApplyFromSettings();
        }
    }
}
