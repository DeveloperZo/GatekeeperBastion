using System.IO;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Disk pins for the death collapse: Die trigger, falling-back clip,
    /// ledger-then-destroy, and the co-op Rpc. The offline harness cannot play the
    /// clip; it can prove the files and the call sites cannot silently revert.</summary>
    public class DeathCollapseTests
    {
        const string Controller = "Assets/Packs/Gatekeepers/Gatekeeper.controller";
        const string DeathMeta = "Assets/Packs/Gatekeepers/Animations/Anim_death_falling_back.fbx.meta";
        const string DeathGuid = "09d7b63af6247d34281bf977e47d7eb0";
        const string AttackGuid = "03ede1e3f3df7b14fbb44ae773ed0c02";

        static string Read(string relative)
        {
            string path = Path.GetFullPath(relative);
            Assert.IsTrue(File.Exists(path), path);
            return File.ReadAllText(path).Replace("\r\n", "\n");
        }

        [Test]
        public void Controller_WiresDieAndDeathToFallingBack()
        {
            string src = Read(Controller);
            StringAssert.Contains("m_Name: Die", src);
            StringAssert.Contains("m_Name: Death", src);
            StringAssert.Contains("m_ConditionEvent: Die", src);
            StringAssert.Contains("guid: " + DeathGuid, src);
            StringAssert.Contains("guid: " + AttackGuid, src);
            string meta = Read(DeathMeta);
            StringAssert.Contains("guid: " + DeathGuid, meta);
            // Bake root into pose: the Animator has applyRootMotion off (locomotion
            // must not slide), so an unbaked Mixamo fall is discarded and they die standing.
            StringAssert.Contains("keepOriginalPositionY: 1", meta);
            StringAssert.Contains("keepOriginalPositionXZ: 1", meta);
            StringAssert.Contains("loopTime: 0", meta);
        }

        [Test]
        public void DeathState_HasNoExitTransition()
        {
            string src = Read(Controller);
            int death = src.IndexOf("m_Name: Death");
            Assert.Greater(death, 0, "Death state missing");
            int transitions = src.IndexOf("m_Transitions:", death);
            Assert.Greater(transitions, death);
            StringAssert.StartsWith(" []", src.Substring(transitions + "m_Transitions:".Length));
        }

        [Test]
        public void OnDeath_FiresDieDisablesCollidersAndDefersDestroy()
        {
            string src = Read("Assets/Scripts/Game/Enemies/EnemyGatekeeper.cs");
            StringAssert.Contains("AnimDie", src);
            StringAssert.Contains("BeginDeathPresentation", src);
            StringAssert.Contains("Despawn(this, CorpseSeconds)", src);
            StringAssert.Contains("SpawnDeathBurst", src);
            StringAssert.Contains("cols[i].enabled = false", src);
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

        [Test]
        public void SceneBuilder_EnsuresDeathStateOnExistingController()
        {
            string src = Read("Assets/Scripts/Editor/Scenes/SceneBuilder.Enemies.cs");
            StringAssert.Contains("EnsureDeathState", src);
            StringAssert.Contains("Anim_death_falling_back", src);
            int existing = src.IndexOf("if (existing != null)");
            int ensure = src.IndexOf("EnsureDeathState(existing)", existing);
            Assert.Greater(ensure, existing, "existing-controller path must patch Death");
        }
    }
}
