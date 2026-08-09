using System.IO;
using GatekeeperBastion.Game;
using GatekeeperBastion.Sim;
using NUnit.Framework;
using UnityEngine;

namespace GatekeeperBastion.Tests
{
    /// <summary>M13 Watch + criterion 5/6: Dev actions latch DevTainted and Save becomes a
    /// no-op so a real campaign file cannot be overwritten by a scratch loadout/grant.</summary>
    public class DevSessionTests
    {
        string _backupPath;
        bool _hadSave;
        string _originalJson;

        [SetUp]
        public void SetUp()
        {
            _backupPath = SaveService.SavePath + ".m13test.bak";
            _hadSave = File.Exists(SaveService.SavePath);
            _originalJson = _hadSave ? File.ReadAllText(SaveService.SavePath) : null;
            if (File.Exists(SaveService.SavePath)) File.Delete(SaveService.SavePath);
            if (File.Exists(_backupPath)) File.Delete(_backupPath);
        }

        [TearDown]
        public void TearDown()
        {
            // Wipe whatever the test wrote, then restore the caller's campaign file.
            if (File.Exists(SaveService.SavePath)) File.Delete(SaveService.SavePath);
            if (_hadSave && _originalJson != null)
                File.WriteAllText(SaveService.SavePath, _originalJson);
            if (File.Exists(_backupPath)) File.Delete(_backupPath);

            // Domain-reload-disabled: clear DevTainted / Meta without leaving a test save.
            GameSession.StartNew();
            if (File.Exists(SaveService.SavePath)) File.Delete(SaveService.SavePath);
            if (_hadSave && _originalJson != null)
                File.WriteAllText(SaveService.SavePath, _originalJson);
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

            // Disk must still hold the pre-taint StartNew snapshot, not the +999 mutation.
            var fromDisk = MetaState.FromJson(File.ReadAllText(SaveService.SavePath));
            Assert.AreEqual(banked, fromDisk.BankedSalvage,
                "Dev-tainted Save must not overwrite the campaign file");
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

            var fromDisk = MetaState.FromJson(File.ReadAllText(SaveService.SavePath));
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
            var fromDisk = MetaState.FromJson(File.ReadAllText(SaveService.SavePath));
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
            var fromDisk = MetaState.FromJson(File.ReadAllText(SaveService.SavePath));
            Assert.IsFalse(fromDisk.Owns(WeaponId.Mr35Rifle),
                "Dev SetLoadout must not persist over a real campaign");
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Test]
        public void DevCommands_TableCoversF1ThroughF9()
        {
            Assert.AreEqual(9, DevCommands.All.Length);
            Assert.AreEqual(KeyCode.F1, DevCommands.All[0].Key);
            Assert.AreEqual(KeyCode.F9, DevCommands.All[8].Key);
            Assert.AreEqual(DevCommands.Kind.ArmJumpToWave, DevCommands.All[2].Action);
            Assert.AreEqual(DevCommands.Kind.LoadoutPicker, DevCommands.All[6].Action);
        }
#endif
    }
}
