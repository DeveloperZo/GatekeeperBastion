using System.IO;
using GatekeeperBastion.Game;
using NUnit.Framework;

namespace GatekeeperBastion.PlayTests
{
    /// <summary>M22 criterion 8: the Dev-taint banner's condition is live after a Dev command
    /// and absent on a clean session. Reads <see cref="DevTaintBanner.IsVisible"/> — the same
    /// flag the Bastion HUD and station panels draw from. No scene load: the condition is
    /// <see cref="GameSession.DevTainted"/>, latched the moment NotifyDevAction runs.</summary>
    public class DevTaintBannerTests
    {
        string _backupPath;
        bool _hadSave;
        string _originalJson;

        [SetUp]
        public void SetUp()
        {
            _backupPath = SaveService.SavePath + ".m22banner.bak";
            _hadSave = File.Exists(SaveService.SavePath);
            _originalJson = _hadSave ? File.ReadAllText(SaveService.SavePath) : null;
            if (File.Exists(SaveService.SavePath)) File.Delete(SaveService.SavePath);
            if (File.Exists(_backupPath)) File.Delete(_backupPath);
            GameSession.StartNew();
            if (File.Exists(SaveService.SavePath)) File.Delete(SaveService.SavePath);
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(SaveService.SavePath)) File.Delete(SaveService.SavePath);
            GameSession.StartNew();
            if (File.Exists(SaveService.SavePath)) File.Delete(SaveService.SavePath);
            if (_hadSave && _originalJson != null)
                File.WriteAllText(SaveService.SavePath, _originalJson);
            if (File.Exists(_backupPath)) File.Delete(_backupPath);
        }

        [Test]
        public void CleanSession_BannerConditionAbsent()
        {
            Assert.IsFalse(GameSession.DevTainted);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Assert.IsFalse(DevTaintBanner.IsVisible,
                "A clean session must not show the Dev-taint banner.");
#endif
        }

        [Test]
        public void DevCommand_BannerConditionLiveImmediately()
        {
            Assert.IsFalse(GameSession.DevTainted, "fixture must start clean");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Assert.IsFalse(DevTaintBanner.IsVisible);
#endif

            // GrantSalvage is the Dev command path — NotifyDevAction latches before the grant.
            GameController.GrantSalvage(5000L);

            Assert.IsTrue(GameSession.DevTainted,
                "GrantSalvage must latch DevTainted via NotifyDevAction.");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Assert.IsTrue(DevTaintBanner.IsVisible,
                "Banner condition must be live the moment the first Dev command runs, " +
                "not deferred to the next scene load.");
#endif
        }
    }
}
