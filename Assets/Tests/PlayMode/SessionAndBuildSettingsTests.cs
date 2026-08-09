using System;
using System.Collections.Generic;
using System.Reflection;
using GatekeeperBastion.Game;
using GatekeeperBastion.Sim;
using NUnit.Framework;

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
