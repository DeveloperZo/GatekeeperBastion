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
    public class CampaignSpinePlayModeTests
    {
        const string RealmName = "SPINE";

        string _snapshot;

        [SetUp]
        public void CaptureTheDevelopersSession()
        {
            // Snapshot only a session that already exists — never EnsureLoaded, which would
            // pull the developer's campaign into the suite and leave it there.
            _snapshot = GameSession.Meta != null ? GameSession.Meta.ToSnapshotJson() : null;
        }

        [TearDown]
        public void RestoreTheDevelopersSession()
        {
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

            // zone 1 — two terminals and a survived bank open D1
            yield return BastionRunDrive.RunSingleTerminal(gc, sim, layout, "T1");
            yield return BastionRunDrive.RunSingleTerminal(gc, sim, layout, "T2");
            yield return BastionRunDrive.SurviveBank(gc, sim);
            Assert.IsTrue(layout.IsDoorOpen("D1"), "T1+T2 plus a survived bank must open D1");

            // zone 2 — one composite trigger, then the second bank
            yield return BastionRunDrive.RunZoneComposite(gc, sim, layout, 2);
            yield return BastionRunDrive.SurviveBank(gc, sim);
            Assert.IsTrue(layout.IsDoorOpen("D2"), "T3 plus a survived bank must open D2");

            // zone 3 — two more terminals, then the door onto the Control Room
            yield return BastionRunDrive.RunSingleTerminal(gc, sim, layout, "T4");
            yield return BastionRunDrive.RunSingleTerminal(gc, sim, layout, "T5");
            yield return BastionRunDrive.SurviveBank(gc, sim);
            Assert.IsTrue(layout.IsDoorOpen("D3"), "T4+T5 plus a survived bank must open D3");

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
            Assert.GreaterOrEqual(reloaded.CompletedTerminalIds.Length, 5,
                "the five terminals cleared this run are not recorded on the realm");
            Assert.GreaterOrEqual(reloaded.OpenDoorIds.Length, 3,
                "the three doors opened this run are not recorded on the realm");
            Assert.IsTrue(reloaded.Owns(WeaponId.Mk1Sidearm),
                "the MK-1 the ship granted did not travel with the Boarder");

            yield return PlayTestUtil.ReturnToBlankScene();
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
