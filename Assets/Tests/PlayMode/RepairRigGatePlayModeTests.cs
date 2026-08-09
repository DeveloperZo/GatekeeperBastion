using System.Collections;
using GatekeeperBastion.Game;
using GatekeeperBastion.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeeperBastion.PlayTests
{
    /// <summary>M23 criterion 5: a Run embarked without the repair rig cannot move the Hatch's
    /// HP by any player-reachable path.</summary>
    public class RepairRigGatePlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return PlayTestUtil.ReturnToBlankScene();
        }

        [UnityTest]
        public IEnumerator RiglessEmbark_CannotRepairHatch()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.Bastion);

            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.Bastion);
            yield return PlayTestUtil.WaitUntil(
                () => GameController.Instance != null && GameController.Instance.Sim != null, 10f);

            var gc = GameController.Instance;
            Assert.IsNotNull(gc);
            Assert.IsNotNull(gc.Sim);
            Assert.IsNotNull(GameSession.Meta);

            // Strip any migrated/granted rig so this Run is explicitly rigless.
            var meta = GameSession.Meta;
            meta.EquippedEquipment = EquipmentId.None;
            meta.OwnedEquipmentMask = 0;
            meta.EquipmentSlotPurchased = false;
            meta.EarnSalvage(500); // bank enough that salvage is not the blocker
            gc.ApplyMetaEffects();

            Assert.IsFalse(meta.HasRepairRigEquipped);
            Assert.IsNotNull(meta.RepairMissingStep());

            var hatch = gc.Sim.Ship.Hatch;
            float before = hatch.Hp;
            // Leave the hatch damaged so a successful repair would move HP.
            hatch.Damage(40f);
            Assert.Less(hatch.Hp, before);

            float damaged = hatch.Hp;
            var repair = Object.FindFirstObjectByType<RepairInteractable>();
            Assert.IsNotNull(repair, "Bastion scene must have a RepairInteractable");

            // Player-reachable path: hold-interact at the hatch.
            for (int i = 0; i < 30; i++)
                repair.Interact(0.1f);

            Assert.AreEqual(damaged, hatch.Hp, 0.01f,
                "rigless Interact must not move Hatch HP");

            // Direct command path also refuses (Dev Restore uses Subsystem.Repair, not this).
            float applied = gc.RepairHatch(20f);
            Assert.AreEqual(0f, applied, 1e-5f);
            Assert.AreEqual(damaged, hatch.Hp, 0.01f);
        }
    }
}
