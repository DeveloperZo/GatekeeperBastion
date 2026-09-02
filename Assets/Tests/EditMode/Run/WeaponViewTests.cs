using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    public class WeaponViewTests
    {
        [Test]
        public void FovConstants_AreHip60_Zoom40()
        {
            Assert.AreEqual(60f, WeaponView.HipFov);
            Assert.AreEqual(40f, WeaponView.ZoomFov);
        }

        [Test]
        public void FootstepClock_IsWalk045_Sprint032()
        {
            Assert.AreEqual(0.45f, WeaponView.WalkFootstepSeconds);
            Assert.AreEqual(0.32f, WeaponView.SprintFootstepSeconds);
        }

        [Test]
        public void MeshPrefabs_SixDistinctPaths()
        {
            string[] paths = WeaponView.AllMeshPrefabs();
            Assert.AreEqual(6, paths.Length);
            var seen = new HashSet<string>();
            for (int i = 0; i < paths.Length; i++)
            {
                Assert.IsFalse(string.IsNullOrEmpty(paths[i]), "path " + i);
                Assert.IsTrue(seen.Add(paths[i]), "duplicate " + paths[i]);
            }
        }

        [Test]
        public void ShopWeaponsPlusCutter_MapToTheSixPrefabs()
        {
            Assert.AreEqual(WeaponView.Mk1Prefab, WeaponView.MeshPrefab(WeaponId.Mk1Sidearm));
            Assert.AreEqual(WeaponView.SecDPrefab, WeaponView.MeshPrefab(WeaponId.SecD));
            Assert.AreEqual(WeaponView.Br12Prefab, WeaponView.MeshPrefab(WeaponId.Br12Shotgun));
            Assert.AreEqual(WeaponView.Mr35Prefab, WeaponView.MeshPrefab(WeaponId.Mr35Rifle));
            Assert.AreEqual(WeaponView.PriCPrefab, WeaponView.MeshPrefab(WeaponId.PriC));
            Assert.AreEqual(WeaponView.CutterPrefab, WeaponView.MeshPrefab(WeaponId.None));
        }

        [Test]
        public void UnownedOnShip_HidesTheMesh()
        {
            WeaponId id;
            Assert.IsFalse(WeaponView.TryShownWeapon(false, false, WeaponId.Mk1Sidearm, out id));
            Assert.AreEqual(WeaponId.None, id);
        }

        [Test]
        public void UnownedInFight_ShowsCutter()
        {
            WeaponId id;
            Assert.IsTrue(WeaponView.TryShownWeapon(false, true, WeaponId.None, out id));
            Assert.AreEqual(WeaponId.None, id);
        }

        [Test]
        public void OwnedEquipped_ShowsThatWeapon()
        {
            WeaponId id;
            Assert.IsTrue(WeaponView.TryShownWeapon(true, false, WeaponId.Mk1Sidearm, out id));
            Assert.AreEqual(WeaponId.Mk1Sidearm, id);
        }

        [Test]
        public void OwnedEmptySlot_HidesTheMesh()
        {
            WeaponId id;
            Assert.IsFalse(WeaponView.TryShownWeapon(true, true, WeaponId.None, out id));
        }

        [Test]
        public void SfxOneShotScale_IsHalf()
        {
            Assert.AreEqual(0.5f, WeaponView.SfxOneShotScale);
        }

        [Test]
        public void LocalGunOffset_SitsInFrontOfTheCamera()
        {
            Assert.Greater(WeaponView.LocalGunForward, 0.05f);
            Assert.Less(WeaponView.LocalGunForward, 1f);
            Assert.Less(Mathf.Abs(WeaponView.LocalGunRight), 0.5f);
            Assert.Greater(WeaponView.LocalGunDown, -0.5f);
        }

        [Test]
        public void Hold_Mk1KeepsTheHipConstants()
        {
            var h = WeaponView.Hold(WeaponId.Mk1Sidearm);
            Assert.AreEqual(WeaponView.LocalGunRight, h.Right);
            Assert.AreEqual(WeaponView.LocalGunDown, h.Down);
            Assert.AreEqual(WeaponView.LocalGunForward, h.Forward);
            Assert.AreEqual(0f, h.Pitch);
            // Mirrored 2026-09-02: the Boarder is left-handed (M113 posed the gun in hand.l),
            // so the pocket sits lower-LEFT and yaw/roll flip sign with LocalGunRight.
            Assert.AreEqual(-2f, h.Yaw);
            Assert.AreEqual(-10f, h.Roll);
            Assert.Less(WeaponView.LocalGunRight, 0f, "left-hand pocket");
        }

        [Test]
        public void Hold_PocketFollowsArmsClass()
        {
            // Two pockets since 2026-09-02 (M113 MR-35 row): sidearms in the MK-1 pocket, long
            // guns (rifle and shotgun classes) in one shared long-gun pocket. Within a class
            // every weapon holds identically; zoom does not move either pocket.
            var mk1 = WeaponView.Hold(WeaponId.Mk1Sidearm);
            var rifle = WeaponView.Hold(WeaponId.Mr35Rifle);
            WeaponId[] longGuns = { WeaponId.SecD, WeaponId.Br12Shotgun, WeaponId.Mr35Rifle, WeaponId.PriC };
            for (int i = 0; i < longGuns.Length; i++)
            {
                var h = WeaponView.Hold(longGuns[i]);
                Assert.AreEqual(rifle.Right, h.Right, longGuns[i].ToString());
                Assert.AreEqual(rifle.Down, h.Down, longGuns[i].ToString());
                Assert.AreEqual(rifle.Forward, h.Forward, longGuns[i].ToString());
                Assert.AreEqual(rifle.Pitch, h.Pitch, longGuns[i].ToString());
                Assert.AreEqual(rifle.Yaw, h.Yaw, longGuns[i].ToString());
                Assert.AreEqual(rifle.Roll, h.Roll, longGuns[i].ToString());
                Assert.AreEqual(rifle.Right, WeaponView.Hold(longGuns[i], true).Right);
            }
            Assert.AreEqual(mk1.Right, WeaponView.Hold(WeaponId.ThrowawayComposite).Right, "pistol class shares the MK-1 pocket");
            Assert.AreNotEqual(mk1.Right, rifle.Right, "the two pockets are two pockets");
        }

        [Test]
        public void Hold_CutterKeepsTheMeleeCant()
        {
            var mk1 = WeaponView.Hold(WeaponId.Mk1Sidearm);
            var cutter = WeaponView.Hold(WeaponId.None);
            // The melee cant is stronger than the hip roll, whichever hand holds it.
            Assert.Greater(Mathf.Abs(cutter.Roll), Mathf.Abs(mk1.Roll));
            Assert.Greater(cutter.Right * mk1.Right, 0f, "same hand as the gun");
            AssertInFrame(cutter);
        }

        [Test]
        public void ArmsClass_SortsTheCatalogLikeFireClip()
        {
            Assert.AreEqual(WeaponView.ArmsClass.Pistol, WeaponView.ArmsClassFor(WeaponId.Mk1Sidearm));
            Assert.AreEqual(WeaponView.ArmsClass.Pistol, WeaponView.ArmsClassFor(WeaponId.ThrowawayComposite));
            Assert.AreEqual(WeaponView.ArmsClass.Rifle, WeaponView.ArmsClassFor(WeaponId.Mr35Rifle));
            Assert.AreEqual(WeaponView.ArmsClass.Rifle, WeaponView.ArmsClassFor(WeaponId.Vk7Rifle));
            Assert.AreEqual(WeaponView.ArmsClass.Rifle, WeaponView.ArmsClassFor(WeaponId.PriC));
            Assert.AreEqual(WeaponView.ArmsClass.Rifle, WeaponView.ArmsClassFor(WeaponId.SecD));
            Assert.AreEqual(WeaponView.ArmsClass.Shotgun, WeaponView.ArmsClassFor(WeaponId.Br12Shotgun));
            Assert.AreEqual(WeaponView.ArmsClass.Melee, WeaponView.ArmsClassFor(WeaponId.None));
            // Child names are the const strings the viewmodel compares against.
            Assert.AreEqual(WeaponView.ArmsPistol, WeaponView.ArmsClassName(WeaponView.ArmsClass.Pistol));
            Assert.AreEqual(WeaponView.ArmsRifle, WeaponView.ArmsClassName(WeaponView.ArmsClass.Rifle));
            Assert.AreEqual(WeaponView.ArmsShotgun, WeaponView.ArmsClassName(WeaponView.ArmsClass.Shotgun));
            Assert.AreEqual(WeaponView.ArmsMelee, WeaponView.ArmsClassName(WeaponView.ArmsClass.Melee));
        }

        [Test]
        public void Hold_LongGunKeepsThePocketConstants()
        {
            // The three LongGun* constants are the tuning surface; Hold returns them as-is.
            // Change the constants, not this test.
            var h = WeaponView.Hold(WeaponId.Mr35Rifle);
            Assert.AreEqual(WeaponView.LongGunRight, h.Right);
            Assert.AreEqual(WeaponView.LongGunDown, h.Down);
            Assert.AreEqual(WeaponView.LongGunForward, h.Forward);
            Assert.AreEqual(0f, h.Pitch);
            Assert.AreEqual(0f, h.Yaw);
            Assert.AreEqual(0f, h.Roll);
            Assert.AreEqual(WeaponView.Hold(WeaponId.Br12Shotgun).Forward, h.Forward, "shotgun class shares the pocket");
        }

        [Test]
        public void Hold_LongGunsSitNearerCentreAndCloser()
        {
            var mk1 = WeaponView.Hold(WeaponId.Mk1Sidearm);
            foreach (var id in new[] { WeaponId.Mr35Rifle, WeaponId.PriC, WeaponId.SecD, WeaponId.Br12Shotgun })
            {
                var h = WeaponView.Hold(id);
                Assert.Less(Mathf.Abs(h.Right), Mathf.Abs(mk1.Right), id + " nearer the centreline than the sidearm");
                Assert.Less(h.Forward, mk1.Forward, id + " pulled back so the arms' elbow cuts leave the frame");
                Assert.Greater(h.Right * mk1.Right, 0f, id + " same hand as the sidearm");
                AssertInFrame(h);
            }
        }

        static void AssertInFrame(WeaponView.GunHold h)
        {
            Assert.Greater(h.Forward, 0.05f);
            Assert.Less(h.Forward, 1f);
            Assert.Less(Mathf.Abs(h.Right), 0.5f);
            Assert.Greater(h.Down, -0.5f);
        }

        [Test]
        public void FireClip_SplitsPistolShotgunRifle()
        {
            Assert.AreEqual(WeaponView.FirePistol, WeaponView.FireClip(WeaponId.Mk1Sidearm));
            Assert.AreEqual(WeaponView.FirePistol, WeaponView.FireClip(WeaponId.SecD));
            Assert.AreEqual(WeaponView.FirePistol, WeaponView.FireClip(WeaponId.None));
            Assert.AreEqual(WeaponView.FireShotgun, WeaponView.FireClip(WeaponId.Br12Shotgun));
            Assert.AreEqual(WeaponView.FireRifle, WeaponView.FireClip(WeaponId.Mr35Rifle));
            Assert.AreEqual(WeaponView.FireRifle, WeaponView.FireClip(WeaponId.PriC));
        }
    }
}
