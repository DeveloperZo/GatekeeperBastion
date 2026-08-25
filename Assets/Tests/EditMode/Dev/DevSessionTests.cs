using System.IO;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;
using UnityEngine;

namespace GatekeepersProtocol.Tests
{
    /// <summary>M13 Watch + criterion 5/6: Dev actions latch DevTainted and Save becomes a
    /// no-op so a real campaign file cannot be overwritten by a scratch loadout/grant.</summary>
    public class DevSessionTests
    {
        [SetUp]
        public void SetUp()
        {
            // The developer's campaign is a DIRECTORY now, and EditModeSaveFixture has already
            // moved it out of reach for the whole assembly. All this has to do is start each
            // test from an empty one.
            EditModeSaveFixture.ClearGuardedSaveRoot();
        }

        [TearDown]
        public void TearDown()
        {
            // Domain-reload-disabled: clear DevTainted / Meta so the next test does not inherit
            // a tainted session. The write this makes lands in the guarded directory and the
            // fixture throws it away at the end of the run.
            GameSession.StartNew();
            EditModeSaveFixture.ClearGuardedSaveRoot();
        }

        /// <summary>The realm this session is playing, read back off disk.</summary>
        static RealmState RealmOnDisk()
        {
            return SaveService.LoadRealm(GameSession.CurrentRealmId);
        }

        /// <summary>The Boarder this session is playing, read back off disk.</summary>
        static BoarderState BoarderOnDisk()
        {
            return SaveService.LoadBoarder(GameSession.CurrentBoarderId);
        }

        [Test]
        public void NotifyDevAction_LatchesTaint_AndSuppressesSave()
        {
            GameSession.StartNew();
            Assert.IsFalse(GameSession.DevTainted);
            long banked = GameSession.Meta.BankedSalvage;

            GameSession.NotifyDevAction();
            Assert.IsTrue(GameSession.DevTainted);

            GameSession.Meta.EarnSalvage(999);
            GameSession.Save(); // must no-op
            Assert.IsTrue(GameSession.DevTainted);

            // Disk must still hold the pre-taint StartNew state, not the +999 mutation.
            // Salvage lives on the realm, so that is the document to read back.
            var fromDisk = RealmOnDisk();
            Assert.IsNotNull(fromDisk, "StartNew wrote a realm this build can read");
            Assert.AreEqual(banked, fromDisk.BankedSalvage,
                "Dev-tainted Save must not overwrite the campaign");
        }

        [Test]
        public void StartNew_ClearsTaint_AndPersistsAgain()
        {
            GameSession.StartNew();
            GameSession.NotifyDevAction();
            Assert.IsTrue(GameSession.DevTainted);

            GameSession.StartNew();
            Assert.IsFalse(GameSession.DevTainted, "StartNew must clear DevTainted");
            GameSession.Meta.EarnSalvage(50);
            GameSession.Save();

            var fromDisk = RealmOnDisk();
            Assert.IsNotNull(fromDisk);
            Assert.AreEqual(50L, fromDisk.BankedSalvage);
        }

        [Test]
        public void GrantSalvage_GoesThroughGameController_AndTaints()
        {
            GameSession.StartNew();
            Assert.AreEqual(0L, GameSession.Meta.BankedSalvage);

            GameController.GrantSalvage(5000L);
            Assert.IsTrue(GameSession.DevTainted);
            Assert.AreEqual(5000L, GameSession.Meta.BankedSalvage);

            GameSession.Save();
            var fromDisk = RealmOnDisk();
            Assert.IsNotNull(fromDisk);
            Assert.AreEqual(0L, fromDisk.BankedSalvage, "granted salvage must not land on disk");
        }

        [Test]
        public void SetLoadout_OwnsTiersAndEquips_WithoutTouchingDisk()
        {
            GameSession.StartNew();
            Assert.IsFalse(GameSession.Meta.Owns(WeaponId.Mr35Rifle));

            GameController.SetLoadout((int)WeaponSlot.Primary, (int)WeaponId.Mr35Rifle, 3);
            Assert.IsTrue(GameSession.DevTainted);
            Assert.IsTrue(GameSession.Meta.Owns(WeaponId.Mr35Rifle));
            Assert.AreEqual(3, GameSession.Meta.TierOf(WeaponId.Mr35Rifle));
            Assert.AreEqual(WeaponId.Mr35Rifle, GameSession.Meta.EquippedPrimary);

            GameSession.Save();
            // Weapons ride the Boarder, so the loadout's absence is proved on that document.
            var fromDisk = BoarderOnDisk();
            Assert.IsNotNull(fromDisk);
            Assert.IsFalse(fromDisk.Owns(WeaponId.Mr35Rifle),
                "Dev SetLoadout must not persist over a real campaign");
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Test]
        public void DevCommands_TableCoversF1ThroughF12()
        {
            // 12 F-keys plus the faucet toggle on Home and the editor net-smoke join on End.
            // F11 is the co-op checkpoint surface, F12 the solo one; the join it displaced
            // moved to End inside the editor-only block.
            Assert.AreEqual(14, DevCommands.All.Length);
            Assert.AreEqual(KeyCode.F1, DevCommands.All[0].Key);
            Assert.AreEqual(KeyCode.F9, DevCommands.All[8].Key);
            Assert.AreEqual(KeyCode.F10, DevCommands.All[9].Key);
            Assert.AreEqual(KeyCode.F11, DevCommands.All[10].Key);
            Assert.AreEqual(KeyCode.F12, DevCommands.All[11].Key);
            Assert.AreEqual(KeyCode.Home, DevCommands.All[12].Key);
            Assert.AreEqual(KeyCode.End, DevCommands.All[13].Key);
            Assert.AreEqual(DevCommands.Kind.ArmJumpToWave, DevCommands.All[2].Action);
            Assert.AreEqual(DevCommands.Kind.LoadoutPicker, DevCommands.All[6].Action);
            Assert.AreEqual(DevCommands.Kind.StartCoopHost, DevCommands.All[9].Action);
            Assert.AreEqual(DevCommands.Kind.ArmCoopCheckpoint, DevCommands.All[10].Action);
            Assert.AreEqual(DevCommands.Kind.ArmDemoCheckpoint, DevCommands.All[11].Action);
            Assert.AreEqual(DevCommands.Kind.ToggleFaucetLoop, DevCommands.All[12].Action);
            Assert.AreEqual(DevCommands.Kind.StartCoopClient, DevCommands.All[13].Action);
            Assert.IsTrue(ShortcutPanel.IsEditorOnlyDevKey(KeyCode.F12),
                "the checkpoint jump is a cheat — player and dev builds never bind it");
            Assert.IsTrue(ShortcutPanel.IsEditorOnlyDevKey(KeyCode.F11),
                "the co-op checkpoint is the same cheat on the other hull");
            Assert.IsFalse(ShortcutPanel.IsEditorOnlyDevKey(KeyCode.F10),
                "the net smoke stays bindable in a dev build");
        }
#endif
    }
}
