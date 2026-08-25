using System.IO;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Source pins for the shared viewmodel controller. The clips and
    /// Animator are Unity assets the offline harness cannot play; what it can prove
    /// is that the files exist, the parameter names match WeaponViewModel, the
    /// curves key wrapper roots, and Bastion has no controller-less Animator.</summary>
    public class ViewModelAnimTests
    {
        static readonly string[] Wrappers = { "Mk1", "SecD", "Br12", "Mr35", "PriC", "Cutter" };
        static readonly string[] Clips =
        {
            "Assets/Packs/Weapons/ViewModel_Idle.anim",
            "Assets/Packs/Weapons/ViewModel_Move.anim",
            "Assets/Packs/Weapons/ViewModel_Fire.anim",
            "Assets/Packs/Weapons/ViewModel_Reload.anim"
        };

        const string ControllerGuid = "efc4e1b037af76e4ba240185f08c2c59";
        const string BoarderControllerGuid = "7fc32efc9375141408a6468bebc85272";

        static string Read(string relative)
        {
            string path = Path.GetFullPath(relative);
            Assert.IsTrue(File.Exists(path), path);
            return File.ReadAllText(path).Replace("\r\n", "\n");
        }

        [Test]
        public void SharedControllerAndFourClips_Exist()
        {
            Assert.IsTrue(File.Exists(Path.GetFullPath("Assets/Packs/Weapons/ViewModel.controller")));
            Assert.IsTrue(File.Exists(Path.GetFullPath("Assets/Packs/Weapons/ViewModel_Idle.anim")));
            Assert.IsTrue(File.Exists(Path.GetFullPath("Assets/Packs/Weapons/ViewModel_Move.anim")));
            Assert.IsTrue(File.Exists(Path.GetFullPath("Assets/Packs/Weapons/ViewModel_Fire.anim")));
            Assert.IsTrue(File.Exists(Path.GetFullPath("Assets/Packs/Weapons/ViewModel_Reload.anim")));
        }

        [Test]
        public void Controller_HasSpeedFireReloadParameters()
        {
            string src = Read("Assets/Packs/Weapons/ViewModel.controller");
            StringAssert.Contains("m_Name: Speed", src);
            StringAssert.Contains("m_Name: Fire", src);
            StringAssert.Contains("m_Name: Reload", src);
        }

        [Test]
        public void AllClips_KeyWrapperRoots_NeverFbxChildren()
        {
            for (int c = 0; c < Clips.Length; c++)
            {
                string src = Read(Clips[c]);
                for (int w = 0; w < Wrappers.Length; w++)
                    StringAssert.Contains("path: " + Wrappers[w], src, Clips[c]);
                Assert.IsFalse(src.Contains("path: Mk1/"), Clips[c] + " fbx child would overwrite the barrel correction");
                Assert.IsFalse(src.Contains("path: PriC/"), Clips[c]);
            }
        }

        [Test]
        public void FireClip_HasRearKick()
        {
            string src = Read("Assets/Packs/Weapons/ViewModel_Fire.anim");
            StringAssert.Contains("z: -0.045", src);
            StringAssert.Contains("x: -9", src);
            StringAssert.Contains("m_LoopTime: 0", src);
        }

        [Test]
        public void ReloadClip_IsOneSecondUnlooped()
        {
            string src = Read("Assets/Packs/Weapons/ViewModel_Reload.anim");
            StringAssert.Contains("time: 1", src);
            StringAssert.Contains("m_LoopTime: 0", src);
            StringAssert.Contains("y: -0.07", src);
        }

        [Test]
        public void MoveAndIdle_Loop()
        {
            StringAssert.Contains("m_LoopTime: 1", Read("Assets/Packs/Weapons/ViewModel_Move.anim"));
            StringAssert.Contains("m_LoopTime: 1", Read("Assets/Packs/Weapons/ViewModel_Idle.anim"));
        }

        [Test]
        public void Controller_HasLocomotionFireReload()
        {
            string src = Read("Assets/Packs/Weapons/ViewModel.controller");
            StringAssert.Contains("m_Name: Locomotion", src);
            StringAssert.Contains("m_Name: Fire", src);
            StringAssert.Contains("m_Name: Reload", src);
            // Modern controller serialization writes m_BlendParameter; the bare
            // "blendParameter:" spelling died when the weapons pack re-serialized the asset.
            StringAssert.Contains("m_BlendParameter: Speed", src);
        }

        [Test]
        public void ViewModelFindsGripAnimator_NeverTheBody()
        {
            string src = Read("Assets/Scripts/Game/Player/WeaponViewModel.cs");
            StringAssert.Contains("_grip.GetComponent<Animator>()", src);
            Assert.IsFalse(src.Contains("GetComponentInChildren<Animator>"),
                "body Animator would swallow Fire/Reload");
            StringAssert.Contains("WeaponView.Hold(_shown, _fpc.IsZooming)", src);
            StringAssert.Contains("RestoreReloadSpeed()", src);
            StringAssert.Contains("ResetTrigger(ReloadHash)", src);
        }

        [Test]
        public void ReloadSpeed_UsesPlayerGunDuration_AndRestores()
        {
            string view = Read("Assets/Scripts/Game/Player/WeaponViewModel.cs");
            StringAssert.Contains("_gun.ActiveReloadSeconds", view);
            StringAssert.Contains("_animator.speed = _reloadClipLength / duration", view);
            StringAssert.Contains("_animator.speed = 1f", view);

            string gun = Read("Assets/Scripts/Game/Player/PlayerGun.Reload.cs");
            StringAssert.Contains("ActiveReloadSeconds => EffectiveReloadSeconds(_activeSlot)", gun);
        }

        [Test]
        public void PriCAndMr35_FbxChildLocalZ_IsPinned()
        {
            Assert.AreEqual(0.204f, GatekeepersProtocol.Sim.WeaponView.PriCFbxLocalZ);
            Assert.AreEqual(0.204f, GatekeepersProtocol.Sim.WeaponView.Mr35FbxLocalZ);
            StringAssert.Contains(
                "propertyPath: m_LocalPosition.z\n      value: 0.204",
                Read("Assets/Packs/Weapons/PriC.prefab"));
            StringAssert.Contains("m_LocalScale: {x: 1, y: 1, z: 1}",
                Read("Assets/Packs/Weapons/PriC.prefab"));
            StringAssert.Contains(
                "propertyPath: m_LocalPosition.z\n      value: 0.204",
                Read("Assets/Packs/Weapons/Mr35.prefab"));
        }

        [Test]
        public void OneController_OnGrip_NotOnWeaponPrefabs()
        {
            string boarder = Read("Assets/Packs/Boarder/Boarder.prefab");
            StringAssert.Contains("guid: " + ControllerGuid, boarder);
            StringAssert.Contains("guid: " + BoarderControllerGuid, boarder);

            string[] weapons =
            {
                "Assets/Packs/Weapons/Mk1.prefab",
                "Assets/Packs/Weapons/SecD.prefab",
                "Assets/Packs/Weapons/Br12.prefab",
                "Assets/Packs/Weapons/Mr35.prefab",
                "Assets/Packs/Weapons/PriC.prefab",
                "Assets/Packs/Weapons/Cutter.prefab"
            };
            for (int i = 0; i < weapons.Length; i++)
            {
                string src = Read(weapons[i]);
                Assert.IsFalse(src.Contains(ControllerGuid), weapons[i] + " must not carry the viewmodel controller");
            }
        }

        [Test]
        public void Bastion_HasNoControllerlessAnimator()
        {
            string src = Read("Assets/Scenes/Bastion_Coop.unity");
            Assert.IsFalse(src.Contains("m_Controller: {fileID: 0}"));
        }
    }
}
