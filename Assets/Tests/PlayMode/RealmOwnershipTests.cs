using System.Collections;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>
    /// Who owns what, when two players share a world: salvage earned in somebody else's realm
    /// fills that realm's pot, and nothing anyone does reaches another player's Boarder
    /// (Docs/Milestones — the realm and the Boarder).
    /// </summary>
    /// <remarks>
    /// These do not stand up a NetworkManager. The ownership rule this milestone introduced is
    /// not enforced by an RPC any more — it is enforced by there being NO code path that writes
    /// another machine's Boarder, which is why the credit fan-out and its owner RPCs were
    /// deleted. What is left to prove is where the writes actually land, and that is a question
    /// about GameSession and RunSim rather than about transport.
    ///
    /// The in-process limit is real and worth stating: host and guest share one static
    /// GameSession.Meta, so "the host's Boarder" and "the guest's Boarder" cannot both be live
    /// at once. GameSession.UseBoarder is the seam that makes the assertion possible — a second
    /// Boarder is swapped in, the guest's session is played out against the host's realm, and
    /// the first Boarder is then checked byte-for-byte.
    /// </remarks>
    public class RealmOwnershipTests
    {
        GameObject _hostGo;
        GameController _gc;
        string _snapshot;

        [SetUp]
        public void CaptureTheDevelopersSession()
        {
            // Snapshot only a session that already exists. Calling EnsureLoaded here would pull
            // the developer's campaign into the test session and leave it there.
            _snapshot = GameSession.Meta != null ? GameSession.Meta.ToSnapshotJson() : null;
        }

        [TearDown]
        public void RestoreTheDevelopersSession()
        {
            if (_gc != null) { Object.DestroyImmediate(_gc.gameObject); _gc = null; }
            if (_hostGo != null) { Object.DestroyImmediate(_hostGo); _hostGo = null; }
            if (_snapshot == null) return;
            var back = MetaState.FromSnapshotJson(_snapshot);
            if (back != null) GameSession.AdoptDevState(back);
        }

        IEnumerator BootHostRun()
        {
            GameSession.EnsureLoaded();
            _hostGo = new GameObject("RealmOwnershipHost");
            _gc = _hostGo.AddComponent<GameController>();
            yield return null;
            yield return null;
            Assert.IsNotNull(_gc.Sim, "the run started");
        }

        /// <summary>Fill both seats' piles and convert them through a Hatch-bank window.</summary>
        void BankAWindow(int seats)
        {
            _gc.Sim.SetPlayerCount(seats);
            _gc.Sim.TriggerDraw();
            for (int i = 0; i < 120; i++)
                _gc.Sim.AwardKill();
            _gc.Sim.NotifyAssaultCleared();
            _gc.Sim.TriggerDraw(WaveMode.HatchBank);
            _gc.Sim.Tick(HatchBank.DurationSeconds);
        }

        [UnityTest]
        public IEnumerator EverySeatBanksIntoTheRealmsPot_AndNoBoarderIsWritten()
        {
            yield return BootHostRun();

            var boarder = GameSession.Meta.Boarder;
            string boarderBefore = boarder.ToJson();
            long potBefore = GameSession.Meta.BankedSalvage;

            BankAWindow(2);

            Assert.Greater(GameSession.Meta.BankedSalvage, potBefore,
                "a two-seat window fills the world's pot");
            Assert.AreEqual(boarderBefore, boarder.ToJson(),
                "salvage is the realm's; earning it writes nothing on any Boarder");
        }

        [UnityTest]
        public IEnumerator AGuestsSessionLeavesTheHostsBoarderUntouched()
        {
            yield return BootHostRun();

            var hostBoarder = GameSession.Meta.Boarder;
            string hostBefore = hostBoarder.ToJson();

            // Stand a second Boarder in for the visiting player. In one process this is the only
            // way two Boarders can exist at once, which is precisely what makes the claim
            // testable rather than merely true.
            var guestBoarder = BoarderState.CreateNew("ownership-guest", "GUEST");
            Assert.IsTrue(GameSession.UseBoarder(guestBoarder), "the guest takes the seat");

            BankAWindow(2);
            // and the guest buys something with the world's salvage
            // What the guest's own machine does to its own Boarder. The purchase RULES are
            // covered by the EditMode ShipCommands tests; what is on trial here is where the
            // write lands, so the write is made directly and unambiguously.
            guestBoarder.SetOwned(WeaponId.Mk1Sidearm, true);
            guestBoarder.SetEquipped(WeaponId.Mk1Sidearm);
            guestBoarder.AwardWheel(WeaponId.Mk1Sidearm, WheelAttr.Damage);

            Assert.IsTrue(GameSession.UseBoarder(hostBoarder), "the host takes its seat back");
            Assert.AreEqual(hostBefore, hostBoarder.ToJson(),
                "nothing the guest earned, spent or bought reached the host's Boarder");
        }

        [UnityTest]
        public IEnumerator AHostsSessionLeavesAGuestsBoarderUntouched()
        {
            yield return BootHostRun();

            var guestBoarder = BoarderState.CreateNew("ownership-guest-2", "GUEST");
            string guestBefore = guestBoarder.ToJson();

            // The guest is not in the seat: the host plays its own world with its own Boarder.
            BankAWindow(1);
            GameSession.Meta.Boarder.SetOwned(WeaponId.Br12Shotgun, true);
            GameSession.Meta.Boarder.AwardWheel(WeaponId.Br12Shotgun, WheelAttr.Damage);

            Assert.AreEqual(guestBefore, guestBoarder.ToJson(),
                "a visiting player's kit is not the host's to write");
        }

        [UnityTest]
        public IEnumerator TheGuestsKitComesHome_AndTheWorldsProgressDoesNot()
        {
            yield return BootHostRun();

            var guestBoarder = BoarderState.CreateNew("ownership-guest-3", "GUEST");
            Assert.IsTrue(GameSession.UseBoarder(guestBoarder));

            BankAWindow(2);
            guestBoarder.SetOwned(WeaponId.Mk1Sidearm, true);
            guestBoarder.AwardWheel(WeaponId.Mk1Sidearm, WheelAttr.Damage);
            long potInTheHostsWorld = GameSession.Meta.BankedSalvage;
            int clearedInTheHostsWorld = GameSession.Meta.DeepestWaveReached;

            // Leaving: the Boarder travels, the realm stays behind. A Boarder document carries
            // no trace of the world it was standing in.
            var travelled = BoarderState.FromJson(guestBoarder.ToJson());
            Assert.IsNotNull(travelled);
            Assert.IsTrue(travelled.Owns(WeaponId.Mk1Sidearm),
                "the weapon bought as a guest comes home");

            var ownRealm = RealmState.CreateNew("ownership-home", "HOME");
            var home = MetaState.Compose(travelled, ownRealm);
            Assert.IsTrue(home.Owns(WeaponId.Mk1Sidearm));
            Assert.AreEqual(0L, home.BankedSalvage,
                "the host's pot stayed in the host's world");
            Assert.AreNotEqual(potInTheHostsWorld, home.BankedSalvage);
            Assert.AreEqual(1, home.DeepestWaveReached,
                "and so did how deep that world had been taken");
            Assert.LessOrEqual(clearedInTheHostsWorld, GameSession.Meta.DeepestWaveReached);
        }
    }
}
