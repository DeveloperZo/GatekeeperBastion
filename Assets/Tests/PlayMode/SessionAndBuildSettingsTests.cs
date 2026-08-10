using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GatekeeperBastion.Game;
using GatekeeperBastion.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace GatekeeperBastion.PlayTests
{
    /// <summary>
    /// Two cheap, fast contracts that sit underneath the scene tests:
    ///   * every scene the flow can navigate to is actually registered in Build Settings, and
    ///   * buying the free MK-1 through the real command layer survives a save/reload.
    /// </summary>
    public class SessionAndBuildSettingsTests
    {
        // Save isolation is assembly-wide via PlayModeSaveFixture — see BastionCombatAndDeathTests.

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return PlayTestUtil.ReturnToBlankScene();
        }

        [Test]
        public void EveryFlowScene_IsRegisteredInBuildSettings()
        {
            // Boot path is Splash → MainMenu → Ship → Bastion (M35 criterion 3).
            string[] flow = { SceneFlow.Splash, SceneFlow.MainMenu, SceneFlow.Ship, SceneFlow.Bastion };

            var missing = new List<string>();
            var present = new List<string>();
            foreach (string scene in flow)
            {
                if (PlayTestUtil.IsSceneInBuild(scene)) present.Add(scene);
                else missing.Add(scene);
            }

            if (present.Count == 0)
            {
                Assert.Inconclusive(
                    "SKIPPED: no flow scene is in Build Settings at all — this is a fresh clone with no " +
                    "generated scenes. Run menu GatekeeperBastion > Build All Scenes.");
            }

            Assert.IsEmpty(missing,
                $"Scene(s) [{string.Join(", ", missing)}] are missing from Build Settings while " +
                $"[{string.Join(", ", present)}] are registered. SceneFlow needs Splash, MainMenu, Ship, Bastion.");
        }

        /// <summary>The build-order smoke assertion that lived in the deleted
        /// ShiftFrontEndMutate.Smoke, updated to the four-scene list (M35 criterion 3).</summary>
        [Test]
        public void BuildSettings_OrderIs_Splash_MainMenu_Ship_Bastion()
        {
            string[] expected = { SceneFlow.Splash, SceneFlow.MainMenu, SceneFlow.Ship, SceneFlow.Bastion };

            // The PlayMode harness injects its own InitTestScene into the build list for the
            // duration of the run (and it lands at slot 0, shifting every real scene) — filter
            // it out or this test can only pass outside the harness.
            var names = new System.Collections.Generic.List<string>();
            int count = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < count; i++)
            {
                string path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
                if (path.Contains("InitTestScene")) continue;
                names.Add(System.IO.Path.GetFileNameWithoutExtension(path));
            }

            Assert.AreEqual(expected.Length, names.Count,
                $"Build Settings holds [{string.Join(", ", names)}]; expected exactly the four flow scenes.");
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], names[i],
                    $"Build slot {i} is '{names[i]}'; expected '{expected[i]}'. " +
                    "Order must be Splash, MainMenu, Ship, Bastion.");
            }
        }

        [UnityTest]
        public IEnumerator MainMenuAndShip_HaveEventSystemForUgui()
        {
            // Splash is deliberately not checked: it has no clickable UI (M35 criterion 2) —
            // the any-key skip polls Input directly, so it needs no EventSystem.
            PlayTestUtil.RequireSceneInBuild(SceneFlow.MainMenu);
            PlayTestUtil.RequireSceneInBuild(SceneFlow.Ship);

            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.MainMenu);
            AssertEventSystemPresent(SceneFlow.MainMenu);

            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.Ship);
            AssertEventSystemPresent(SceneFlow.Ship);
        }

        [UnityTest]
        public IEnumerator Splash_HostsSplashController_AndSkipLandsInMainMenu()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.Splash);
            PlayTestUtil.RequireSceneInBuild(SceneFlow.MainMenu);

            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.Splash, settleFrames: 1);
            var splash = UnityEngine.Object.FindFirstObjectByType<SplashController>(
                FindObjectsInactive.Include);
            Assert.IsNotNull(splash, "Splash must host SplashController (the boot title card).");
            Assert.IsNull(UnityEngine.Object.FindFirstObjectByType<MainMenuController>(
                    FindObjectsInactive.Include),
                "Splash must NOT host MainMenuController — the menu lives in MainMenu.unity.");

            // Drive the any-key skip (Input cannot be injected). This also drains the parked
            // async MainMenu load — a held allowSceneActivation=false op would otherwise block
            // every later scene load in the suite.
            splash.Skip();
            yield return PlayTestUtil.WaitUntil(
                () => SceneFlow.ActiveSceneName == SceneFlow.MainMenu, 10f);
            Assert.AreEqual(SceneFlow.MainMenu, SceneFlow.ActiveSceneName,
                "Skipping the splash must land in MainMenu (criterion 1).");
        }

        [UnityTest]
        public IEnumerator MainMenu_HostsCampaignMenu_AndLoadsShip()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.MainMenu);
            PlayTestUtil.RequireSceneInBuild(SceneFlow.Ship);

            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.MainMenu);
            var menu = UnityEngine.Object.FindFirstObjectByType<MainMenuController>(
                FindObjectsInactive.Include);
            Assert.IsNotNull(menu,
                "MainMenu must host MainMenuController (campaign Continue/Start/Options).");

            Assert.AreEqual(SceneFlow.MainMenu, SceneFlow.FrontEndScene);
            Assert.IsTrue(SceneFlow.CanLoad(SceneFlow.Ship), "Ship must be in build settings.");

            bool ok = SceneFlow.LoadShip();
            Assert.IsTrue(ok, "LoadShip from MainMenu failed.");
            yield return null;
            yield return null;
            Assert.AreEqual(SceneFlow.Ship, SceneFlow.ActiveSceneName,
                "Continue/Start path must land on Ship.");
        }

        [UnityTest]
        public IEnumerator Ship_CanLoadBastion_EmbarkPath()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.Ship);
            PlayTestUtil.RequireSceneInBuild(SceneFlow.Bastion);

            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.Ship);
            Assert.IsTrue(SceneFlow.LoadBastion(), "Embark LoadBastion failed.");
            yield return null;
            yield return null;
            Assert.AreEqual(SceneFlow.Bastion, SceneFlow.ActiveSceneName,
                "Embark must land on Bastion.");
        }

        static void AssertEventSystemPresent(string sceneName)
        {
            var systems = UnityEngine.Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.GreaterOrEqual(systems.Length, 1,
                $"{sceneName} has no EventSystem — uGUI buttons cannot receive clicks.");
            Assert.IsNotNull(systems[0].GetComponent<StandaloneInputModule>(),
                $"{sceneName} EventSystem is missing StandaloneInputModule " +
                "(project activeInputHandler is legacy Input Manager).");
        }

        [Test]
        public void BuyingTheFreeMk1_PersistsAcrossASaveReload()
        {
            Type sessionType = PlayTestUtil.FindType("GatekeeperBastion.Game.GameSession");
            Assert.IsNotNull(sessionType, "Type GatekeeperBastion.Game.GameSession does not exist.");

            MethodInfo startNew = StaticNoArgMethod(sessionType, "StartNew");
            MethodInfo continueGame = StaticNoArgMethod(sessionType, "ContinueGame");
            if (startNew == null || continueGame == null)
            {
                Assert.Inconclusive(
                    "SKIPPED: GameSession.StartNew()/ContinueGame() are not both reachable.");
            }

            startNew.Invoke(null, null);
            MetaState fresh = GameSession.Meta;
            Assert.IsNotNull(fresh, "GameSession.StartNew() left Meta null.");
            Assert.IsFalse(fresh.OwnsAnyWeapon,
                "A brand-new campaign must start WITHOUT the MK-1 — it is bought (free) at the ARMORY.");
            Assert.AreEqual(0L, fresh.BankedSalvage, "Fresh save starts at 0 banked.");
            Assert.AreEqual(0L, WeaponCatalog.Def(WeaponId.Mk1Sidearm).cost);

            long bankedBefore = fresh.BankedSalvage;

            Type shipCommands = PlayTestUtil.FindType("GatekeeperBastion.Game.ShipCommands");
            MethodInfo buyCommand = null;
            if (shipCommands != null)
            {
                try
                {
                    buyCommand = shipCommands.GetMethod("BuyWeapon",
                        BindingFlags.Static | BindingFlags.Public,
                        null, new[] { typeof(WeaponId) }, null);
                }
                catch (AmbiguousMatchException) { buyCommand = null; }
            }

            Assert.IsNotNull(buyCommand, "ShipCommands.BuyWeapon(WeaponId) must exist.");
            object result = buyCommand.Invoke(null, new object[] { WeaponId.Mk1Sidearm });
            bool bought = !(result is bool) || (bool)result;

            Assert.IsTrue(bought, "Buying the free MK-1 failed on a brand-new campaign.");
            Assert.IsTrue(fresh.Owns(WeaponId.Mk1Sidearm), "BuyWeapon did not set ownership.");
            Assert.AreEqual(WeaponId.Mk1Sidearm, fresh.EquippedSecondary);
            Assert.AreEqual(bankedBefore, fresh.BankedSalvage, "Free MK-1 must not debit salvage.");

            GameSession.Save();

            continueGame.Invoke(null, null);
            MetaState reloaded = GameSession.Meta;
            Assert.IsNotNull(reloaded, "GameSession.ContinueGame() left Meta null.");
            Assert.IsTrue(reloaded.Owns(WeaponId.Mk1Sidearm),
                "MK-1 ownership did NOT survive save + reload.");
            Assert.AreEqual(fresh.BankedSalvage, reloaded.BankedSalvage);
        }

        static MethodInfo StaticNoArgMethod(Type type, string name)
        {
            MethodInfo m;
            try { m = type.GetMethod(name, BindingFlags.Static | BindingFlags.Public); }
            catch (AmbiguousMatchException) { return null; }
            return m != null && m.GetParameters().Length == 0 ? m : null;
        }
    }
}
