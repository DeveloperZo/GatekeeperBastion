using System.Collections;
using System.Reflection;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>
    /// THE SPINE. One traversal, end to end, on a save that did not exist when the test
    /// started: a fresh realm clears the ship's demo gates, embarks through the NAV MAP,
    /// runs the whole bastion — three zones, three doors, Master Control — wins, comes
    /// home to the ship, and is still there when the realm is re-opened from disk.
    ///
    /// If this passes, the game can be played from nothing to the end of a bastion and
    /// back. Every other PlayMode fixture proves one joint of that; this proves they are
    /// connected, which is the failure no per-joint test can see. It is deliberately the
    /// slowest test in the suite.
    ///
    /// TWO SEAMS, both stated rather than hidden:
    ///   * Waves are stripped, not fought (<see cref="BastionRunDrive"/>) — this is a
    ///     traversal test, not a difficulty one.
    ///   * The ship tutorial is SKIPPED rather than played, through the very
    ///     DemoProgress.SkipShipTutorial a player's skip takes — aiming at two intruders
    ///     needs a cursor lock an unfocused editor will not hold. The gate that skip lifts
    ///     is asserted on both sides, so the skip is proven, not assumed.
    /// </summary>
    public class CampaignSpineTests
    {
        const string RealmName = "SPINE";

        string _snapshot;
        bool _skipPref;

        [SetUp]
        public void CaptureTheDevelopersSession()
        {
            // Snapshot only a session that already exists — never EnsureLoaded, which would
            // pull the developer's campaign into the suite and leave it there.
            _snapshot = GameSession.Meta != null ? GameSession.Meta.ToSnapshotJson() : null;
            // The spine drives beat 0 from a virgin state. On a machine where the developer
            // has finished the ship tutorial, the machine-wide SkipShipTutorial preference
            // makes Adopt pre-pass every gate and grant the MK-1 — clear it for the test.
            _skipPref = Settings.GetBool(SettingId.SkipShipTutorial);
            Settings.SetBool(SettingId.SkipShipTutorial, false);
        }

        [TearDown]
        public void RestoreTheDevelopersSession()
        {
            Settings.SetBool(SettingId.SkipShipTutorial, _skipPref);
            if (_snapshot == null) return;
            MetaState back = MetaState.FromSnapshotJson(_snapshot);
            if (back != null) GameSession.AdoptDevState(back);
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator FreshRealm_ClearsTheShipGates_RunsTheBastion_AndComesHome()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.ShipCoop);
            PlayTestUtil.RequireSceneInBuild(SceneFlow.BastionCoop);

            // ---- 1. a save that did not exist a moment ago ---------------------------
            string boarderId = FirstBoarderId();
            RealmState realm = RealmRegistry.CreateRealm(RealmName);
            Assert.IsNotNull(realm, "the realm was not created");
            Assert.IsTrue(GameSession.EnterRealm(realm.Id, boarderId),
                "a realm created this second must be enterable");

            MetaState meta = GameSession.Meta;
            Assert.IsNotNull(meta, "entering a realm left no session");
            Assert.IsTrue(meta.DemoActive, "a fresh realm starts inside the demo");
            Assert.AreEqual(0L, meta.BankedSalvage, "a fresh realm's pot is empty");
            Assert.AreEqual(0, meta.OpenDoorIds.Length, "and every door is shut");
            Assert.AreEqual(0, meta.CompletedTerminalIds.Length, "and nothing is cleared");

            // ---- 2. the ship's gates refuse, in their authored order -----------------
            Assert.AreEqual(DemoEmbarkRefusal.ShipTerminal, DemoProgress.EmbarkRefusal(meta),
                "a fresh realm may not embark: the ship terminal comes first");

            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.ShipCoop);
            yield return PlayTestUtil.WaitUntil(() => PlayerRegistry.Local != null, 10f);
            Assert.IsNotNull(PlayerRegistry.Local,
                SceneFlow.ShipCoop + " never registered a player — the realm has no one to play");

            // Take the skip a player takes. The lesson is an arc: this says it is behind us.
            Assert.IsTrue(DemoProgress.SkipShipTutorial(meta),
                "a fresh realm must have a ship tutorial available to skip");
            Assert.AreEqual(DemoEmbarkRefusal.None, DemoProgress.EmbarkRefusal(meta),
                "skipping the tutorial must lift every embark gate that tutorial owns");
            Assert.IsTrue(meta.Owns(WeaponId.Mk1Sidearm),
                "the skip must still hand over the MK-1 the tutorial's terminal grants — "
                + "skipping the lesson must not embark the player unarmed");
            Assert.IsFalse(DemoProgress.SkipShipTutorial(meta),
                "and a second skip must be a no-op");

            // ---- 3. embark through the real NAV MAP ----------------------------------
            var map = Object.FindFirstObjectByType<MapStation>();
            Assert.IsNotNull(map, SceneFlow.ShipCoop + " has no NAV MAP station to embark from");
            MethodInfo embark = typeof(MapStation).GetMethod(
                "TryEmbark", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(embark,
                "MapStation.TryEmbark is gone — the embark this test drives no longer exists");
            embark.Invoke(map, new object[] { meta });

            yield return PlayTestUtil.WaitUntil(
                () => SceneManager.GetActiveScene().name == SceneFlow.BastionCoop, 30f);
            Assert.AreEqual(SceneFlow.BastionCoop, SceneManager.GetActiveScene().name,
                "EMBARK did not reach the bastion within 30 s (active scene: '"
                + SceneManager.GetActiveScene().name + "')");

            // ---- 4. the whole bastion ------------------------------------------------
            yield return PlayTestUtil.WaitUntil(
                () =>
                {
                    var g = Object.FindFirstObjectByType<GameController>();
                    return g != null && g.Sim != null;
                }, 15f);

            GameController gc = Object.FindFirstObjectByType<GameController>();
            BastionLayout layout = Object.FindFirstObjectByType<BastionLayout>();
            Assert.IsNotNull(gc, "the bastion has no GameController — the run never starts");
            Assert.IsNotNull(layout, "the bastion has no BastionLayout");
            RunSim sim = gc.Sim;
            Assert.AreEqual(RunPhase.Tethered, sim.Phase, "arriving at the bastion auto-boards");

            // The spine (Zo, 2026-08-25) gives each zone ONE terminal running that zone's
            // whole wave span, so both zones are composites and there is no third zone. The
            // loop is Hatch -> lane -> terminal (Zo, 2026-08-26; Docs/Design/0-Gameplay.md):
            // RunZoneComposite starts at the Hatch and walks it down, and the shutoff is what
            // opens the door. No Convert window stands between the zones any more — the
            // Convert left the demo, and once a zone's door opens the Hatch-bank is correctly
            // refused again because the NEXT zone's terminal is unfinished.
            // zone 1 — one composite, closed at T2, which opens D1
            yield return BastionRunDrive.RunZoneComposite(gc, sim, layout, 1);
            Assert.IsTrue(layout.IsDoorOpen("D1"),
                "closing zone 1 at its terminal must open D1");

            // zone 2 — the same loop again, and its shutoff opens the control door
            yield return BastionRunDrive.RunZoneComposite(gc, sim, layout, 2);
            Assert.IsTrue(layout.IsDoorOpen("D3"),
                "closing zone 2 at its terminal must open the control door");

            // Master Control — the win
            long bankedBefore = meta.BankedSalvage;
            int cleanBefore = meta.CleanExits;
            yield return BastionRunDrive.WinThroughMasterControl(gc, sim, layout);

            Assert.IsTrue(gc.RunIsOver, "the controller does not read the Run as over");
            Assert.AreEqual(0L, sim.TotalUnbanked, "the finale dump left salvage stranded");
            long bankedAfter = meta.BankedSalvage;
            Assert.Greater(bankedAfter, bankedBefore, "the run banked nothing into the realm");
            Assert.AreEqual(cleanBefore + 1, meta.CleanExits, "the clean exit was not recorded");

            // ---- 5. home ------------------------------------------------------------
            gc.ReturnToShip();
            yield return PlayTestUtil.WaitUntil(
                () => SceneManager.GetActiveScene().name == SceneFlow.ShipCoop, 30f);
            Assert.AreEqual(SceneFlow.ShipCoop, SceneManager.GetActiveScene().name,
                "RETURN did not land back on the ship within 30 s (active scene: '"
                + SceneManager.GetActiveScene().name + "')");
            Assert.AreEqual(bankedAfter, GameSession.Meta.BankedSalvage,
                "the pot changed on the trip home");

            // ---- 6. and it is still true on disk -------------------------------------
            // ReturnToShip saves on the way out, so re-opening the realm reads what a
            // player would find next session. This is the difference between "the run
            // finished" and "the run counted".
            Assert.IsTrue(GameSession.EnterRealm(realm.Id, boarderId),
                "the realm this run was played in must re-open");
            MetaState reloaded = GameSession.Meta;
            Assert.AreEqual(bankedAfter, reloaded.BankedSalvage,
                "the banked pot did not survive the trip to disk");
            Assert.AreEqual(cleanBefore + 1, reloaded.CleanExits,
                "the clean exit did not survive the trip to disk");
            // The spine runs two field terminals and two doors (D1, then the control door).
            Assert.GreaterOrEqual(reloaded.CompletedTerminalIds.Length, 2,
                "the terminals cleared this run are not recorded on the realm");
            Assert.GreaterOrEqual(reloaded.OpenDoorIds.Length, 2,
                "the doors opened this run are not recorded on the realm");
            Assert.IsTrue(reloaded.Owns(WeaponId.Mk1Sidearm),
                "the MK-1 the ship granted did not travel with the Boarder");

            yield return PlayTestUtil.ReturnToBlankScene();
        }

        /// <summary>Beat 0's LAST gate, driven through the real panel the player clicks: the
        /// ARMORY rack sells the free MK-1, and that one purchase is what arms the Boarder,
        /// clears EMBARK and opens co-op (2026-08-25 recut). The spine test above SKIPS the
        /// ship tutorial, so without this nothing proves a player can actually get a weapon —
        /// and an unarmed player cannot embark, which would soft-lock the demo at its start.</summary>
        [UnityTest]
        [Timeout(180000)]
        public IEnumerator Beat0_ArmoryRack_SellsTheFreeMk1_ArmsTheBoarder_AndOpensCoop()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.ShipCoop);

            // A Boarder minted for this test: weapons live on the BOARDER, so reusing the
            // machine's existing one would arrive already armed and prove nothing.
            BoarderState boarder = RealmRegistry.CreateBoarder("ARMORY BOARDER");
            Assert.IsNotNull(boarder, "could not mint a Boarder");
            RealmState realm = RealmRegistry.CreateRealm("ARMORY");
            Assert.IsTrue(GameSession.EnterRealm(realm.Id, boarder.Id), "a fresh realm must open");
            MetaState meta = GameSession.Meta;
            Assert.IsNotNull(meta, "entering a realm left no session");

            // Stand exactly where the ship terminal leaves the player: power routed, unarmed.
            meta.DemoNavPowered = true;
            meta.DemoArmoryPowered = true;
            Assert.IsFalse(meta.OwnsAnyWeapon, "the recut leaves beat 0 unarmed until the rack");
            Assert.AreEqual(DemoEmbarkRefusal.NoWeapon, DemoProgress.EmbarkRefusal(meta),
                "the weapon is the only gate the terminal leaves behind");
            Assert.IsFalse(DemoProgress.MultiplayerUnlocked(meta), "co-op waits for the MK-1");

            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.ShipCoop);
            yield return PlayTestUtil.WaitUntil(() => PlayerRegistry.Local != null, 10f);

            var armory = Object.FindFirstObjectByType<ArmoryStation>(FindObjectsInactive.Include);
            Assert.IsNotNull(armory, "the ARMORY station is authored in the ship");
            Assert.AreEqual(DemoStationVerdict.Open, DemoProgress.ArmoryVerdict(meta),
                "the terminal's power must surface the rack");
            Assert.IsFalse(DemoStationGate.TryHandleInteractDown(armory),
                "the demo gate must not swallow the interact — the panel has to open");

            var ship = Object.FindFirstObjectByType<ShipController>();
            Assert.IsNotNull(ship, "no ShipController in the ship scene");
            Assert.IsTrue(ship.TryOpenStationFromInteract(armory), "the rack refused to open");
            yield return null;
            yield return null;

            var view = Object.FindFirstObjectByType<ArmoryPanelView>(FindObjectsInactive.Include);
            Assert.IsNotNull(view, "no ArmoryPanelView is bound to the rack");

            UnityEngine.UI.Button buy = null;
            for (int i = 0; i < view.CatalogRowCount; i++)
            {
                PanelRowView row = view.CatalogRow(i);
                if (row == null || !row.gameObject.activeInHierarchy) continue;
                bool isMk1 = false;
                foreach (TMPro.TMP_Text t in row.GetComponentsInChildren<TMPro.TMP_Text>(true))
                {
                    if (t != null && t.text != null && t.text.Contains("MK-1")) { isMk1 = true; break; }
                }
                if (!isMk1) continue;
                foreach (var b in row.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                {
                    if (b != null && b.gameObject.activeInHierarchy) { buy = b; break; }
                }
                break;
            }
            Assert.IsNotNull(buy,
                "the MK-1 has no live buy button on the rack — beat 0 would soft-lock here");
            Assert.IsTrue(buy.interactable, "the MK-1's buy button is not clickable");

            buy.onClick.Invoke();
            yield return null;

            Assert.IsTrue(meta.Owns(WeaponId.Mk1Sidearm), "clicking BUY did not hand over the MK-1");
            Assert.AreEqual(WeaponId.Mk1Sidearm, meta.EquippedSecondary, "and it must be equipped");
            Assert.IsTrue(meta.DemoWeaponGranted && meta.DemoShipFightDone,
                "the rack's buy IS the arming beat — both remaining gates ride it");
            Assert.AreEqual(DemoEmbarkRefusal.None, DemoProgress.EmbarkRefusal(meta),
                "EMBARK must be clear the moment the player is armed");
            Assert.IsTrue(DemoProgress.MultiplayerUnlocked(meta),
                "co-op unlocks at the MK-1 (Zo, 2026-08-25)");
        }

        /// <summary>A Boarder to take in. The chooser mints one on a machine that has none;
        /// this fixture may run without ever loading it, so it does the same thing.</summary>
        static string FirstBoarderId()
        {
            var boarders = RealmRegistry.Boarders();
            if (boarders.Length > 0)
                return boarders[0].Id;
            BoarderState made = RealmRegistry.CreateBoarder("SPINE BOARDER");
            Assert.IsNotNull(made, "no Boarder exists and one could not be created");
            return made.Id;
        }
    }
}
