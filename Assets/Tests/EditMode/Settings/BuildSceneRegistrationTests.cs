using System;
using System.Collections.Generic;
using GatekeepersProtocol.EditorTools;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Build All used to assign EditorBuildSettings a fresh four-entry array, which
    /// silently unregistered every other scene. A scene loaded by name then failed to load with
    /// nothing pointing at Build All as the cause — that is exactly how the isometric probe
    /// scene's fixture broke. These pin the merge instead: the boot scenes lead, anything
    /// else registered survives, and only rows whose file is gone are removed.</summary>
    public sealed class BuildSceneRegistrationTests
    {
        static readonly string[] Boot =
        {
            "Splash", "MainMenu", "RealmSelect", "Ship - CoOp", "Bastion_Coop"
        };

        static List<SceneBuilder.SceneEntry> Merge(
            List<SceneBuilder.SceneEntry> existing, Func<string, bool> exists,
            out List<string> kept, out List<string> dropped)
        {
            return SceneBuilder.MergeBuildScenes(existing, Boot, exists, out kept, out dropped);
        }

        static List<SceneBuilder.SceneEntry> Entries(params (string path, bool on)[] rows)
        {
            var list = new List<SceneBuilder.SceneEntry>();
            foreach ((string path, bool on) in rows)
                list.Add(new SceneBuilder.SceneEntry(path, on));
            return list;
        }

        [Test]
        public void Merge_KeepsASceneRegisteredOutsideTheBootSet()
        {
            List<SceneBuilder.SceneEntry> r = Merge(
                Entries(("Splash", true), ("Some_Probe", false)),
                _ => true, out List<string> kept, out _);

            Assert.AreEqual(Boot.Length + 1, r.Count, "a non-boot scene must survive Build All");
            Assert.AreEqual("Some_Probe", r[Boot.Length].Path);
            Assert.IsFalse(r[Boot.Length].Enabled, "its enabled flag must carry over, not be reset");
            CollectionAssert.Contains(kept, "Some_Probe (disabled)");
        }

        [Test]
        public void Merge_PutsTheBootScenesFirstInFlowOrder()
        {
            List<SceneBuilder.SceneEntry> r = Merge(
                Entries(("Some_Probe", true)), _ => true, out _, out _);

            for (int i = 0; i < Boot.Length; i++)
            {
                Assert.AreEqual(Boot[i], r[i].Path, "boot order is the load order");
                Assert.IsTrue(r[i].Enabled, "boot scenes are always enabled");
            }
        }

        [Test]
        public void Merge_DropsAndReportsARowWhoseSceneFileIsGone()
        {
            List<SceneBuilder.SceneEntry> r = Merge(
                Entries(("Deleted_Scene", true)),
                p => p != "Deleted_Scene", out _, out List<string> dropped);

            Assert.AreEqual(Boot.Length, r.Count);
            CollectionAssert.AreEqual(new[] { "Deleted_Scene" }, dropped,
                "a removal must be reported, never silent");
        }

        [Test]
        public void Merge_DoesNotDuplicateOrDisableABootSceneAlreadyListed()
        {
            List<SceneBuilder.SceneEntry> r = Merge(
                Entries(("Bastion_Coop", false), ("Bastion_Coop", false)), _ => true, out _, out _);

            Assert.AreEqual(Boot.Length, r.Count, "a boot scene listed twice stays one row");
            Assert.IsTrue(r[Boot.Length - 1].Enabled, "boot scenes are always enabled");
            Assert.AreEqual("Bastion_Coop", r[Boot.Length - 1].Path);
        }

        [Test]
        public void Merge_FromAnEmptyListYieldsExactlyTheBootScenes()
        {
            List<SceneBuilder.SceneEntry> r = Merge(
                new List<SceneBuilder.SceneEntry>(), _ => true, out _, out _);

            Assert.AreEqual(Boot.Length, r.Count);
        }
    }
}
