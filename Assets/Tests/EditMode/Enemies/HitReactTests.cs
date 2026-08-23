using System.IO;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Disk pins for the shot flinch: HitReact trigger, additive masked layer,
    /// Anim_flinch motion, and both Damaged call sites. The offline harness cannot play
    /// the clip; it can prove the files and the hooks cannot silently revert.</summary>
    public class HitReactTests
    {
        const string Controller = "Assets/Packs/Gatekeepers/Gatekeeper.controller";
        const string FlinchMeta = "Assets/Packs/Gatekeepers/Animations/Anim_flinch.anim.meta";
        const string FlinchClip = "Assets/Packs/Gatekeepers/Animations/Anim_flinch.anim";
        const string MaskMeta = "Assets/Packs/Gatekeepers/UpperBody.mask.meta";
        const string FlinchGuid = "0cd7751cb3144b74fb47045e6061905f";
        const string MaskGuid = "ea4cad77548780048875dbc14ba4b83e";
        const string AttackGuid = "03ede1e3f3df7b14fbb44ae773ed0c02";

        static string Read(string relative)
        {
            string path = Path.GetFullPath(relative);
            Assert.IsTrue(File.Exists(path), path);
            return File.ReadAllText(path).Replace("\r\n", "\n");
        }

        [Test]
        public void Controller_WiresHitReactAndFlinchClip()
        {
            string src = Read(Controller);
            StringAssert.Contains("m_Name: HitReact", src);
            StringAssert.Contains("m_Name: Flinch", src);
            StringAssert.Contains("m_ConditionEvent: HitReact", src);
            StringAssert.Contains("guid: " + FlinchGuid, src);
            StringAssert.Contains("guid: " + AttackGuid, src);
            StringAssert.Contains("guid: " + FlinchGuid, Read(FlinchMeta));
            StringAssert.Contains("guid: " + MaskGuid, Read(MaskMeta));
        }

        [Test]
        public void HitReactLayer_IsAdditiveMaskedAndDoesNotWriteDefaults()
        {
            string src = Read(Controller);
            int layer = src.IndexOf("    m_Name: HitReact");
            Assert.Greater(layer, 0, "HitReact layer missing");
            int mask = src.IndexOf("guid: " + MaskGuid, layer);
            Assert.Greater(mask, layer);
            int blend = src.IndexOf("m_BlendingMode: 1", layer);
            Assert.Greater(blend, layer);
            Assert.Less(blend - layer, 400, "additive blend must sit on the HitReact layer");

            int flinch = src.IndexOf("m_Name: Flinch");
            Assert.Greater(flinch, 0);
            StringAssert.Contains("m_WriteDefaultValues: 0",
                src.Substring(flinch, 400));

            int empty = src.IndexOf("m_Name: Empty");
            Assert.Greater(empty, 0);
            StringAssert.Contains("m_WriteDefaultValues: 0",
                src.Substring(empty, 400));
        }

        [Test]
        public void AnyStateToFlinch_CanTransitionToSelf()
        {
            string src = Read(Controller);
            int cond = src.IndexOf("m_ConditionEvent: HitReact");
            Assert.Greater(cond, 0);
            string window = src.Substring(cond, 800);
            StringAssert.Contains("m_CanTransitionToSelf: 1", window);
            StringAssert.Contains("m_HasExitTime: 0", window);
            StringAssert.Contains("m_TransitionDuration: 0.05", window);
        }

        [Test]
        public void FlinchClip_IsQuarterSecondAndDoesNotLoop()
        {
            string src = Read(FlinchClip);
            StringAssert.Contains("m_LoopTime: 0", src);
            StringAssert.Contains("time: 0.25", src);
            StringAssert.Contains("time: 0.06", src);
        }

        [Test]
        public void OnDamaged_FiresHitReactBeforeProvoke()
        {
            string src = Read("Assets/Scripts/Game/Enemies/EnemyGatekeeper.cs");
            StringAssert.Contains("AnimHitReact", src);

            string targeting = Read("Assets/Scripts/Game/Enemies/EnemyGatekeeper.Targeting.cs");
            int trigger = targeting.IndexOf("_animator.SetTrigger(AnimHitReact)");
            int provoke = targeting.IndexOf("Provoke(source, shout: true)");
            Assert.Greater(trigger, 0);
            Assert.Greater(provoke, trigger, "SetTrigger must run before Provoke");
        }

        [Test]
        public void ShipIntruder_SubscribesAndFiresHitReact()
        {
            string src = Read("Assets/Scripts/Game/Ship/ShipIntruder.cs");
            StringAssert.Contains("AnimHitReact", src);
            StringAssert.Contains("_damageable.Damaged += OnDamaged", src);
            StringAssert.Contains("_damageable.Damaged -= OnDamaged", src);
            StringAssert.Contains("_animator.SetTrigger(AnimHitReact)", src);
        }

        [Test]
        public void SceneBuilder_EnsuresHitReactLayerOnExistingController()
        {
            string src = Read("Assets/Scripts/Editor/Scenes/SceneBuilder.Enemies.cs");
            StringAssert.Contains("EnsureHitReactLayer", src);
            StringAssert.Contains("Anim_flinch", src);
            int existing = src.IndexOf("if (existing != null)");
            int ensure = src.IndexOf("EnsureHitReactLayer(existing)", existing);
            Assert.Greater(ensure, existing, "existing-controller path must patch HitReact");
        }
    }
}
