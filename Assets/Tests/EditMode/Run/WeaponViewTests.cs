using System.Collections.Generic;
using NUnit.Framework;
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
            Assert.Less(WeaponView.LocalGunRight, 0.5f);
            Assert.Greater(WeaponView.LocalGunDown, -0.5f);
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
