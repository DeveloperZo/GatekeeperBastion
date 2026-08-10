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
                $"[{string.Join(", ", present)}] are registered. SceneFlow navigates to all four by name, so " +
                "every missing one is a runtime dead end.");
        }

        [UnityTest]
        public IEnumerator SplashAndMainMenu_HaveEventSystemForUgui()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.Splash);
            PlayTestUtil.RequireSceneInBuild(SceneFlow.MainMenu);

            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.Splash);
            AssertEventSystemPresent(SceneFlow.Splash);

            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.MainMenu);
            AssertEventSystemPresent(SceneFlow.MainMenu);
        }

        [UnityTest]
        public IEnumerator Splash_ReachesMainMenu_WithinBootBudget()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.Splash);
            PlayTestUtil.RequireSceneInBuild(SceneFlow.MainMenu);

            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.Splash);
            var splash = UnityEngine.Object.FindFirstObjectByType<SplashController>();
            Assert.IsNotNull(splash,
                "Splash scene has no SplashController — press-any-key / boot budget cannot run.");

            float deadline = Time.realtimeSinceStartup + 12f;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (SceneFlow.ActiveSceneName == SceneFlow.MainMenu)
                {
                    Assert.IsNotNull(
                        UnityEngine.Object.FindFirstObjectByType<MainMenuController>(),
                        "MainMenu loaded without MainMenuController.");
                    AssertEventSystemPresent(SceneFlow.MainMenu);
                    yield break;
                }
                yield return null;
            }

            Assert.AreEqual(SceneFlow.MainMenu, SceneFlow.ActiveSceneName,
                "Splash did not reach MainMenu within 12s (8s boot budget + settle). " +
                $"Still on '{SceneFlow.ActiveSceneName}'.");
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
