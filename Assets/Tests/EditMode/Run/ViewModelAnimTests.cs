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

        const string ArmsPrefab = "Assets/Packs/Boarder/Arms.prefab";
        const string ArmsPrefabMeta = "Assets/Packs/Boarder/Arms.prefab.meta";

        static string Read(string relative)
        {
            string path = Path.GetFullPath(relative);
            Assert.IsTrue(File.Exists(path), path);
            return File.ReadAllText(path).Replace("\r\n", "\n");
        }

        /// <summary>Read a file the arms mesh work has not authored yet. The seam ships before
        /// the mesh, so these rows skip rather than fail until the asset lands.</summary>
        static string ReadOrIgnore(string relative)
        {
            string path = Path.GetFullPath(relative);
            if (!File.Exists(path))
                Assert.Ignore(relative + " does not exist yet — the arms mesh is cut later");
            return File.ReadAllText(path).Replace("\r\n", "\n");
        }

        /// <summary>A method body, from its signature to the first method-level closing brace.
        /// Nested blocks close at deeper indentation, so "\n        }" ends the method.</summary>
        static string Block(string src, string signature)
        {
            int start = src.IndexOf(signature);
            Assert.IsTrue(start >= 0, "not in source: " + signature);
            int end = src.IndexOf("\n        }", start);
            Assert.IsTrue(end > start, "unterminated: " + signature);
            return src.Substring(start, end - start);
        }

        static int CountOf(string src, string needle)
        {
            int n = 0;
            int at = src.IndexOf(needle);
            while (at >= 0)
            {
                n++;
                at = src.IndexOf(needle, at + needle.Length);
            }
            return n;
        }

        /// <summary>The 32-char guid out of a .meta, or a skip when the asset is not authored.</summary>
        static string GuidOf(string metaRelative)
        {
            string src = ReadOrIgnore(metaRelative);
            int at = src.IndexOf("guid: ");
            Assert.IsTrue(at >= 0, metaRelative + " has no guid");
            return src.Substring(at + 6, 32);
        }

        static void AssertSameHold(GatekeepersProtocol.Sim.WeaponView.GunHold expected,
            GatekeepersProtocol.Sim.WeaponView.GunHold actual, string what)
        {
            Assert.AreEqual(expected.Right, actual.Right, what + " Right");
            Assert.AreEqual(expected.Down, actual.Down, what + " Down");
            Assert.AreEqual(expected.Forward, actual.Forward, what + " Forward");
            Assert.AreEqual(expected.Pitch, actual.Pitch, what + " Pitch");
            Assert.AreEqual(expected.Yaw, actual.Yaw, what + " Yaw");
            Assert.AreEqual(expected.Roll, actual.Roll, what + " Roll");
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

        [Test]
        public void Arms_ParentToShownWrapper_NotToGrip()
        {
            string src = Read("Assets/Scripts/Game/Player/WeaponViewModel.cs");
            string enableOnly = Block(src, "void EnableOnly(GameObject keep)");
            StringAssert.Contains("Transform root = keep.transform;", enableOnly,
                "arms must ride the shown wrapper root");
            StringAssert.Contains("arms.SetParent(root, false)", enableOnly);
            Assert.IsFalse(src.Contains("SetParent(_grip"),
                "a child of the Grip sits still while the gun kicks out of its hands");
        }

        [Test]
        public void ArmsOffset_IsWrittenPerFrame_NotAtSwitchTime()
        {
            string src = Read("Assets/Scripts/Game/Player/WeaponViewModel.cs");
            string pose = Block(src, "void PoseArms()");
            StringAssert.Contains("WeaponView.ArmsOffset(_shown", pose,
                "the stance has to be re-read every frame");
            StringAssert.Contains("IsZooming", pose);
            StringAssert.Contains("PoseArms();", Block(src, "void LateUpdate()"));

            // EnableOnly re-runs only when the shown weapon changes. Reading IsZooming there
            // samples the stance once at switch time, so the zoom pose could never differ
            // from the hip pose and criterion 3's six zoom rows could not pass.
            Assert.IsFalse(Block(src, "void EnableOnly(GameObject keep)").Contains("ArmsOffset"),
                "sampling the stance at switch time freezes it");
        }

        [Test]
        public void Arms_ShowOnlyTheWeaponsClassChild()
        {
            string src = Read("Assets/Scripts/Game/Player/WeaponViewModel.cs");
            string enableOnly = Block(src, "void EnableOnly(GameObject keep)");
            StringAssert.Contains("WeaponView.ArmsClassName(WeaponView.ArmsClassFor(_shown))", enableOnly,
                "the class child is chosen from the shown weapon, in Sim");
            StringAssert.Contains("child.name == cls", enableOnly,
                "one class child on, every other class child off - an unbaked class shows no arms");

            // Off disk: the prefab carries at least the Pistol child (the MK-1 bake).
            string prefab = ReadOrIgnore(ArmsPrefab);
            StringAssert.Contains("m_Name: " + GatekeepersProtocol.Sim.WeaponView.ArmsPistol + "\n", prefab,
                "Arms.prefab has no Pistol child - run the cutter's migrate step");
        }

        [Test]
        public void Arms_ActivateAndDeactivate_ShareOneChokePoint()
        {
            string src = Read("Assets/Scripts/Game/Player/WeaponViewModel.cs");
            string enableOnly = Block(src, "void EnableOnly(GameObject keep)");
            StringAssert.Contains("SetActive(_arms, true)", enableOnly);
            StringAssert.Contains("SetActive(_arms, false)", enableOnly);
            // The prop is a stand-in for culled arms: with the iso rig live the body is drawn
            // (BoarderPresentation skips the cull), so the prop must stay off or it is a third arm.
            StringAssert.Contains("_isoRig.isActiveAndEnabled", enableOnly, "arms off while the body is visible");
            StringAssert.Contains("keep == null || bodyVisible", enableOnly);

            string awake = Block(src, "void Awake()");
            Assert.IsFalse(awake.Contains("SetActive(_arms"),
                "Awake runs once — a Drone-slot episode would leave the arms off for the session");
            Assert.IsFalse(awake.Contains("_arms.SetActive"), "same");

            string hidden = Block(src, "void ShowHidden()");
            Assert.IsFalse(hidden.Contains("_arms"),
                "hiding is EnableOnly's job; two writers drift apart");
        }

        [Test]
        public void Arms_AreNotSkinned()
        {
            string src = ReadOrIgnore(ArmsPrefab);
            StringAssert.Contains("MeshRenderer:", src);
            Assert.IsFalse(src.Contains("SkinnedMeshRenderer:"),
                "BoarderPresentation moves every SkinnedMeshRenderer onto LocalHead and culls it");
        }

        [Test]
        public void Arms_AuthoredInactive()
        {
            // The prefab root is authored ACTIVE so it previews in the Project window; what
            // keeps arms off a remote avatar is the m_IsActive override on the instance inside
            // Boarder.prefab, which is what this asserts.
            ReadOrIgnore(ArmsPrefab);

            string guid = GuidOf(ArmsPrefabMeta);
            string boarder = Read("Assets/Packs/Boarder/Boarder.prefab");
            StringAssert.Contains("propertyPath: m_IsActive\n      value: 0", boarder,
                "the Arms instance in Boarder.prefab must override m_IsActive to 0");
            // The override must target a GameObject that still exists in Arms.prefab. A
            // re-bake that re-saves the prefab can change the root's fileID; a dangling
            // override still contains the text above but no longer keeps the arms off a
            // remote avatar.
            var armsPrefab = ReadOrIgnore(ArmsPrefab);
            var overrideTarget = System.Text.RegularExpressions.Regex.Match(boarder,
                @"target: \{fileID: (\d+), guid: " + guid + @", type: 3\}\s+propertyPath: m_IsActive\s+value: 0");
            Assert.IsTrue(overrideTarget.Success, "m_IsActive override must target an Arms.prefab object");
            StringAssert.Contains("--- !u!1 &" + overrideTarget.Groups[1].Value + "\n", armsPrefab,
                "the m_IsActive override in Boarder.prefab targets a fileID that no longer exists in Arms.prefab - re-run Step 4");
            // Count nested-prefab instances, not guid hits: a PrefabInstance block repeats the
            // source guid once per overridden property (13 for name + TRS), so a raw count
            // reads "one instance" as thirteen.
            Assert.AreEqual(1, CountOf(boarder, "m_SourcePrefab: {fileID: 100100000, guid: " + guid),
                "exactly one arms instance — weapon switching moves it, never duplicates it");
        }

        [Test]
        public void ArmsOffset_IsSharedAcrossWeapons()
        {
            var mk1 = GatekeepersProtocol.Sim.WeaponView.ArmsOffset(
                GatekeepersProtocol.Sim.WeaponId.Mk1Sidearm, false);

            var mapped = new[]
            {
                GatekeepersProtocol.Sim.WeaponId.SecD,
                GatekeepersProtocol.Sim.WeaponId.Br12Shotgun,
                GatekeepersProtocol.Sim.WeaponId.Mr35Rifle,
                GatekeepersProtocol.Sim.WeaponId.PriC
            };
            for (int i = 0; i < mapped.Length; i++)
                AssertSameHold(mk1, GatekeepersProtocol.Sim.WeaponView.ArmsOffset(mapped[i]),
                    mapped[i].ToString());

            // One pose ships. A per-class branch cannot be added without editing this test.
            var fallback = new[]
            {
                GatekeepersProtocol.Sim.WeaponId.None,
                GatekeepersProtocol.Sim.WeaponId.Vk7Rifle,
                GatekeepersProtocol.Sim.WeaponId.ThrowawayComposite
            };
            for (int i = 0; i < fallback.Length; i++)
                AssertSameHold(mk1, GatekeepersProtocol.Sim.WeaponView.ArmsOffset(fallback[i]),
                    fallback[i].ToString());

            // Deliberately no hip-vs-zoom assertion. Criterion 3 checks six zoom rows, so the
            // stances are expected to diverge; pinning them equal would go red exactly when
            // step 4 does the right thing. The zoom argument reaching ArmsOffset is proven by
            // ArmsOffset_IsWrittenPerFrame_NotAtSwitchTime instead.
        }

        [Test]
        public void RightHandBone_IsGone()
        {
            string view = Read("Assets/Scripts/Sim/Run/WeaponView.cs");
            Assert.IsFalse(view.Contains("mixamorig:RightHand"),
                "the mixamo bone matches nothing since the Boarder redress");
            Assert.IsFalse(view.Contains("RightHandBone"));

            string model = Read("Assets/Scripts/Game/Player/WeaponViewModel.cs");
            Assert.IsFalse(model.Contains("_rightHand"));
            Assert.IsFalse(model.Contains("RightHandBone"));
        }
    }
}
