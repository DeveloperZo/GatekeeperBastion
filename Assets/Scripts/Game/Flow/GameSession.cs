using System;
using GatekeeperBastion.Game.Ui;
using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;

namespace GatekeeperBastion.Game
{
    /// <summary>HOST-ONLY composition root: the one place that owns the loaded MetaState and all
    /// SaveService IO for the whole app (allowed exception to the no-new-statics rule).</summary>
    /// <remarks>
    /// Scene controllers read GameSession.Meta; every persistence call in the codebase goes
    /// through Save()/StartNew()/ContinueGame() — nothing else may touch SaveService directly.
    /// In future co-op this state lives on the host and is never mutated by remote clients.
    /// Load and save are exception-guarded: a corrupt or unreadable save file must degrade to a
    /// fresh session, never take the game down on the way into or out of a run.
    /// </remarks>
    public static class GameSession
    {
        const string Tag = "GameSession";

        /// <summary>The live cross-run progression state, or null before StartNew/ContinueGame.</summary>
        public static MetaState Meta { get; private set; }

        /// <summary>True once any Dev command has run this play session. A tainted session must
        /// never overwrite a real campaign save (M13 Watch) — <see cref="Save"/> becomes a no-op.</summary>
        public static bool DevTainted { get; private set; }

        /// <summary>1-based start wave chosen at the MAP. Consumed by GameController on embark;
        /// resets to 1 after the run begins so a direct-play fallback stays a normal start.</summary>
        public static int PendingStartWave { get; set; } = 1;

        /// <summary>Clears statics at play-mode start (domain-reload-disabled safety).</summary>
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Meta = null;
            DevTainted = false;
            PendingStartWave = 1;
        }

        /// <summary>Mark this play session as Dev-touched. First call logs once; Save is then skipped.</summary>
        public static void NotifyDevAction()
        {
            if (DevTainted) return;
            DevTainted = true;
            Log.Event("Dev", "session tainted — GameSession.Save suppressed for this play session");
        }

        /// <summary>True when a save file exists on disk.</summary>
        public static bool HasSave
        {
            get
            {
                try { return SaveService.Exists(); }
                catch (Exception e)
                {
                    Log.Warn(Tag, $"HasSave check failed ({e.GetType().Name}: {e.Message}); assuming no save.");
                    return false;
                }
            }
        }

        /// <summary>Begin a fresh campaign: new MetaState (0 banked, nothing owned) saved immediately.</summary>
        public static void StartNew()
        {
            DevTainted = false; // intentional new campaign may persist again
            Meta = MetaState.CreateNew();
            Save();
            Log.Event("Ship",
                $"new campaign — {Meta.BankedSalvage} banked to start with, no weapon yet");
        }

        /// <summary>Load the persisted campaign into Meta. Falls back to a fresh session on failure.</summary>
        public static void ContinueGame()
        {
            DevTainted = false; // disk load is a clean slate for save suppression
            try
            {
                Meta = SaveService.Load();
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
                $"{Meta.PodEjections} death(s), {LoadoutLog(Meta)}");

            if (Meta.SalvageStatsWereRepaired)
            {
                // Either a pre-v2 save (no lifetime fields) or a hand-edited one: Earned/Spent were
                // rebuilt from the owned upgrade levels so Earned == Banked + Spent holds again.
                Log.Warn(Tag, $"Lifetime salvage stats rebuilt on load — {Meta.LifetimeSalvageEarned} earned, " +
                              $"{Meta.LifetimeSalvageSpent} spent (reconstructed from owned upgrades).");
            }

            if (!Meta.OwnsAnyWeapon)
            {
                // Guard trip: no weapon. Survivable only because PlayerGun falls back to the
                // improvised cutter — flag it so it is not a silent trap. MK-1 costs 0.
                Log.Warn(Tag, "No weapon owned. Runs will use the improvised salvage cutter " +
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
        /// a real campaign (M13 Watch: skip Save while Dev mode has been used).</summary>
        public static void Save()
        {
            if (DevTainted)
            {
                Log.Event("Dev", "Save skipped — session tainted by Dev mode");
                return;
            }
            if (Meta == null)
            {
                Log.Warn(Tag, "Save called with no session loaded; ignored.");
                return;
            }
            try
            {
                SaveService.Save(Meta);
                Log.Info(Tag, $"Saved (banked {Meta.BankedSalvage}, runs {Meta.TotalRuns}).");
            }
            catch (Exception e)
            {
                // Guard trip: a failed save must not abort the caller — this runs on the death path.
                Log.Error(Tag, $"Save failed ({e.GetType().Name}: {e.Message}); progress this run may be lost.");
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
            // Volumes also apply on Settings.Bind / Splash / MainMenu; re-push for direct-play
            // scenes that skip the menu so AudioSources spawned here see the saved buses.
            AudioBuses.ApplyFromSettings();
        }
    }
}
