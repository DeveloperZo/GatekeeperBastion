using System.Collections;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>F12 fight digits land on the authored spine state, and the Hatch can walk
    /// zone 1 → zone 2 → Master from those beats. Not a full combat playthrough — host verbs
    /// and layout facts only.</summary>
    public class BastionSpinePlayModeTests
    {
        string _campaignJson;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _campaignJson = GameSession.Meta == null ? null : GameSession.Meta.ToSnapshotJson();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            MetaState restored;
            if (_campaignJson == null)
            {
                restored = MetaState.CreateNew();
                restored.DemoActive = false;
            }
            else
            {
                restored = MetaState.FromSnapshotJson(_campaignJson);
            }
            GameSession.AdoptDevState(restored);
            _campaignJson = null;
            yield return PlayTestUtil.ReturnToBlankScene();
        }

        [UnityTest]
        public IEnumerator DigitB_ZoneOneDone_ControlGateShut_HatchDoesNotOfferMaster()
        {
            yield return LoadFightDigit(2);
            GameController gc = Object.FindFirstObjectByType<GameController>();
            BastionLayout layout = Object.FindFirstObjectByType<BastionLayout>();
            Assert.IsTrue(layout.IsTerminalDone(DemoZones.TerminalIdAt(1, 0)));
            Assert.IsFalse(layout.IsDoorOpen(DemoZones.DoorId(1)),
                "digit B is pre-Convert — D1 stays shut so Convert can answer");
            Assert.IsFalse(layout.ControlRoomReachable);
            Assert.IsFalse(gc.HatchOffersWaveStart,
                "Master must not steal the Hatch while the control gate is shut");
            Assert.Greater(gc.Sim.Unbanked, 0L, "digit B seeds the zone-1 bank pile");
        }

        [UnityTest]
        public IEnumerator DigitD_ZoneTwoDone_D1Open_ControlGateShut()
        {
            yield return LoadFightDigit(4);
            BastionLayout layout = Object.FindFirstObjectByType<BastionLayout>();
            GameController gc = Object.FindFirstObjectByType<GameController>();
            Assert.IsTrue(layout.IsDoorOpen(DemoZones.DoorId(1)));
            Assert.IsFalse(layout.IsDoorOpen(DemoZones.DoorId(2)));
            Assert.IsTrue(layout.IsTerminalDone(DemoZones.TerminalIdAt(2, 0)));
            Assert.IsFalse(gc.HatchOffersWaveStart);
        }

        [UnityTest]
        public IEnumerator DigitF_BothDoors_HatchOffersMaster()
        {
            yield return LoadFightDigit(6);
            BastionLayout layout = Object.FindFirstObjectByType<BastionLayout>();
            GameController gc = Object.FindFirstObjectByType<GameController>();
            Assert.IsTrue(layout.ControlRoomReachable);
            Assert.IsTrue(gc.HatchOffersWaveStart);
            Assert.IsTrue(gc.StartZoneWaveFromHatch());
            Assert.IsTrue(gc.Sim.MasterSequenceLive);
        }

        [UnityTest]
        public IEnumerator DigitG_StandsAtHatch_AndStartsMaster()
        {
            yield return LoadFightDigit(7);
            GameController gc = Object.FindFirstObjectByType<GameController>();
            Assert.IsTrue(gc.HatchOffersWaveStart);
            Assert.IsTrue(gc.StartZoneWaveFromHatch());
            Assert.IsTrue(gc.Sim.MasterSequenceLive);
        }

        [UnityTest]
        public IEnumerator FieldRecallPads_AreShipTeleporterGraybox_NotCyanSquares()
        {
            yield return LoadFightDigit(2);
            var recalls = Object.FindObjectsByType<HatchRecallPad>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            Assert.AreEqual(2, recalls.Length, "T2 and T5 each need a HatchRecallPad");
            for (int i = 0; i < recalls.Length; i++)
            {
                StringAssert.Contains("Portal", recalls[i].name);
                Assert.IsNull(recalls[i].GetComponent<MeshFilter>(),
                    "recall pad root must not carry a procedural mesh");
                Assert.IsNotNull(recalls[i].transform.Find("Pad"),
                    recalls[i].name + " missing Pad (Ship teleporter graybox)");
            }
        }

        [UnityTest]
        public IEnumerator Spine_FromZoneOneMouth_ThroughControlRoomReady()
        {
            // Zone-2 mouth (D1 open, T5 live) → finish zone-2 terminals in Meta terms →
            // control room ready → Hatch starts Master.
            PlayTestUtil.RequireSceneInBuild(SceneFlow.BastionCoop);
            MetaState mouth = GameController.BuildZoneTwoReadyCheckpoint();
            GameSession.AdoptDevState(mouth);
            GameSession.PendingStartWave = 1;
            GameSession.PendingDemoFight = 0;
            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.BastionCoop);
            yield return WaitForGc();

            GameController gc = Object.FindFirstObjectByType<GameController>();
            BastionLayout layout = Object.FindFirstObjectByType<BastionLayout>();
            Assert.IsTrue(layout.IsDoorOpen(DemoZones.DoorId(1)));
            Assert.IsFalse(layout.IsTerminalDone(DemoZones.TerminalIdAt(2, 0)));
            Assert.IsTrue(gc.HatchOffersWaveStart, "zone 2 unfinished — Hatch starts that zone");

            // Jump the layout to post-zone-2 Convert (digit F meta) without a reload of
            // combat — prove the offer chain ends at Master once both gates are open.
            MetaState ready = GameController.BuildDemoCheckpoint(6);
            layout.RestoreCompletedTerminals(ready.CompletedTerminalIds);
            layout.RestoreOpenDoors(ready.OpenDoorIds);
            Assert.IsTrue(layout.ControlRoomReachable);
            Assert.IsTrue(gc.HatchOffersWaveStart);
            Assert.IsTrue(gc.StartZoneWaveFromHatch());
            Assert.IsTrue(gc.Sim.MasterSequenceLive,
                "beginning-to-control-room path does not reach Master from the Hatch");
        }

        static IEnumerator LoadFightDigit(int digit)
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.BastionCoop);
            MetaState checkpoint = GameController.BuildDemoCheckpoint(digit);
            Assert.IsNotNull(checkpoint);
            GameSession.AdoptDevState(checkpoint);
            GameSession.PendingStartWave = 1;
            GameSession.PendingDemoFight = digit;
            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.BastionCoop);
            yield return WaitForGc();
        }

        static IEnumerator WaitForGc()
        {
            yield return PlayTestUtil.WaitUntil(
                () =>
                {
                    GameController g = Object.FindFirstObjectByType<GameController>();
                    return g != null && g.Sim != null;
                }, 10f);
        }
    }
}
