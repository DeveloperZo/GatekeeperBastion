using System.IO;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Disk pins for the death presentation. THE LIVE DEATH IS THE AUTHORED
    /// CLIP: BeginDeathPresentation (EnemyGatekeeper.Death.cs) fires the Die trigger
    /// into the controller's Death state, latches the AI off for good, and a
    /// LateUpdate clamp holds the corpse on the walkable floor plane; destroy is
    /// deferred through WaveSpawner.Despawn and clones get the same presentation via
    /// NetEnemy.PlayDeathRpc. The clip only works because EVERY root channel is baked
    /// into pose in the fbx import — earlier imports left channels as root motion,
    /// which applyRootMotion=false discarded (the hips-drop upright sink seen in
    /// play), while applyRootMotion=true on the child Animator buried the mesh and
    /// kicked it a storey up. A controller-less body (fixtures) falls back to a
    /// procedural board-tip. Live shape is GatekeeperDeathAnimTests (PlayMode);
    /// these are grep pins.</summary>
    public class DeathCollapseTests
    {
        const string DeathFile = "Assets/Scripts/Game/Enemies/EnemyGatekeeper.Death.cs";

        static string Read(string relative)
        {
            string path = Path.GetFullPath(relative);
            Assert.IsTrue(File.Exists(path), path);
            return File.ReadAllText(path).Replace("\r\n", "\n");
        }

        [Test]
        public void OnDeath_PlaysDieClipClampsToFloorAndDefersDestroy()
        {
            string src = Read(DeathFile);
            StringAssert.Contains("BeginDeathPresentation", src);
            StringAssert.Contains("SetTrigger(AnimDie)", src);
            StringAssert.Contains("Animator.StringToHash(\"Die\")", src);
            StringAssert.Contains("applyRootMotion = false", src);
            StringAssert.Contains("_collapseFloorY", src);
            StringAssert.Contains("baseOffset", src);
            StringAssert.Contains("Despawn(this, CorpseSeconds)", src);
            StringAssert.Contains("const float CorpseSeconds = 3.7f", src);
            StringAssert.Contains("SpawnDeathBurst", src);
            StringAssert.Contains("cols[i].enabled = false", src);
            // Fallback for a controller-less body only — the clip path returns before it.
            StringAssert.Contains("RotateAround", src);
            StringAssert.DoesNotContain("applyRootMotion = true", src);
        }

        /// <summary>The whole fall must live in the pose: any root channel left as root
        /// motion is DISCARDED under applyRootMotion=false, and the body visibly sinks
        /// upright instead of tipping. A reimport that reverts these flags reintroduces
        /// that silently, so the import settings are pinned on disk.</summary>
        [Test]
        public void DeathClipImport_BakesEveryRootChannelIntoPose()
        {
            string meta = Read("Assets/Packs/Gatekeepers/Animations/Anim_death_falling_back.fbx.meta");
            StringAssert.Contains("loopTime: 0", meta);
            StringAssert.Contains("loopBlendOrientation: 1", meta);
            StringAssert.Contains("loopBlendPositionY: 1", meta);
            StringAssert.Contains("loopBlendPositionXZ: 1", meta);
            StringAssert.Contains("keepOriginalOrientation: 1", meta);
            StringAssert.Contains("keepOriginalPositionY: 1", meta);
            StringAssert.Contains("keepOriginalPositionXZ: 1", meta);
        }

        /// <summary>The permanent latch, not the transient _collapsing flag, is what keeps
        /// Update off a corpse: a clone's local Damageable never takes damage, so IsDead is
        /// no guard there. Behavioural check is GatekeeperDeathAnimTests
        /// (DeathPresentation_StaysLatchedOnAnUndamagedClone).</summary>
        [Test]
        public void BeginDeathPresentation_LatchesAiOffPermanently()
        {
            StringAssert.Contains("_presentationDead = true", Read(DeathFile));
            string src = Read("Assets/Scripts/Game/Enemies/EnemyGatekeeper.cs");
            int update = src.IndexOf("void Update()");
            Assert.Greater(update, 0);
            int latch = src.IndexOf("if (_presentationDead) return;", update);
            int isDead = src.IndexOf("_damageable.IsDead) return;", update);
            Assert.Greater(latch, update, "Update must return on the presentation-death latch");
            Assert.Greater(isDead, latch, "the latch must come before the IsDead check");
        }

        [Test]
        public void Despawn_RemovesFromLedgerBeforeDestroyTail()
        {
            string src = Read("Assets/Scripts/Game/Enemies/WaveSpawner.cs");
            StringAssert.Contains("float corpseSeconds = 0f", src);
            int remove = src.IndexOf("_live.Remove(gatekeeper)");
            int corpses = src.IndexOf("_corpses.Add");
            int destroy = src.IndexOf("DestroyActor(gatekeeper)");
            Assert.Greater(remove, 0);
            Assert.Greater(corpses, remove, "corpse list must be after ledger removal");
            Assert.Greater(destroy, remove, "destroy tail must be after ledger removal");
            StringAssert.Contains("TickCorpses", src);
            StringAssert.Contains("_corpses.Clear()", src);
            StringAssert.Contains("TickCorpses(dt)",
                Read("Assets/Scripts/Game/Enemies/WaveSpawner.Groups.cs"));
        }

        [Test]
        public void NetEnemy_ReplicatesDieAndLeavesDestroyToSpawner()
        {
            string src = Read("Assets/Scripts/Game/Net/NetEnemy.cs");
            StringAssert.Contains("SendTo.NotServer", src);
            StringAssert.Contains("PlayDeathRpc", src);
            StringAssert.Contains("HasLifetimeOwner", src);
            StringAssert.Contains("BeginDeathPresentation", src);
        }

        /// <summary>The Death state the runtime fires is authored by SceneBuilder on both
        /// controller branches, so a Build All can never strip the state the Die trigger
        /// lands in.</summary>
        [Test]
        public void DeathState_IsAuthoredOnBothControllerBranches()
        {
            string builder = Read("Assets/Scripts/Editor/Scenes/SceneBuilder.Enemies.cs");
            StringAssert.Contains("EnsureDeathState", builder);
            StringAssert.Contains("Anim_death_falling_back", builder);
            int existing = builder.IndexOf("if (existing != null)");
            int ensure = builder.IndexOf("EnsureDeathState(existing)", existing);
            Assert.Greater(ensure, existing, "existing-controller path must keep authoring Death");
        }
    }
}
