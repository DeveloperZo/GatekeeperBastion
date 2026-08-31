using System.Collections;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>
    /// The realm chooser, driven. Covers criteria 1 and 6 of Docs/Milestones — the realm and
    /// the Boarder: a player creates a realm, names it, enters it with an existing Boarder, and
    /// coming back to it later finds its world exactly as it was left.
    /// </summary>
    /// <remarks>
    /// This also serves as the first thing that ever LOADS RealmSelect.unity. The scene was
    /// hand-authored offline, so "it imports" and "it works" are different claims — a missing
    /// serialized reference or a mis-shaped TMP_InputField would show up here and nowhere else
    /// in the suite.
    ///
    /// The milestone originally asked for a playthrough. It does not need one: nothing here
    /// depends on having earned anything, so the world state a realm has to remember is set
    /// directly rather than played for (Zo, 2026-08-21; Docs/Decisions.md permits a
    /// dev-tainted session to close a gate, and this does not even need the taint).
    /// </remarks>
    public class RealmChooserTests
    {
        string _snapshot;

        [SetUp]
        public void CaptureTheDevelopersSession()
        {
            _snapshot = GameSession.Meta != null ? GameSession.Meta.ToSnapshotJson() : null;
        }

        [TearDown]
        public void RestoreTheDevelopersSession()
        {
            if (_snapshot == null) return;
            var back = MetaState.FromSnapshotJson(_snapshot);
            if (back != null) GameSession.AdoptDevState(back);
        }

        static RealmSelectView View()
        {
            return Object.FindAnyObjectByType<RealmSelectView>();
        }

        /// <summary>Type into the chooser's name field and prove it took. Reading it back is
        /// the point: the field is hand-authored, so "bound and working" is a different claim
        /// from "present in the scene".</summary>
        static void TypeName(string name)
        {
            var view = View();
            Assert.IsNotNull(view, "the chooser is loaded");
            view.SetName(name);
            Assert.AreEqual(name, view.NameText, "the name field is wired both ways");
        }

        static void TypeBoarderName(string name)
        {
            var view = View();
            Assert.IsNotNull(view, "the chooser is loaded");
            view.SetBoarderName(name);
            Assert.AreEqual(name, view.BoarderNameText, "the boarder name field is wired both ways");
        }

        [UnityTest]
        public IEnumerator TheChooserScene_LoadsAndBindsEverything()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.RealmSelect);
            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.RealmSelect);

            var view = View();
            Assert.IsNotNull(view, "RealmSelect.unity carries a RealmSelectView");
            Assert.IsNotNull(Object.FindAnyObjectByType<RealmSelectController>(),
                "and a controller to drive it");
            TypeName("BINDING CHECK");
            TypeBoarderName("BOARDER BIND");
            view.ClearName();
            Assert.AreEqual("", view.NameText,
                "the hand-authored name field reads and writes — the part of that scene most "
                + "likely to have come out wrong");
            Assert.AreEqual("", view.BoarderNameText,
                "and the boarder name field clears with it");
            Assert.IsNotNull(Object.FindAnyObjectByType<Canvas>(), "and a canvas to draw on");

            // A missing serialized reference logs one error naming the field, so a clean load
            // is the binding check. Start() mints a Boarder when the machine has none, which
            // is what stops a first-time player facing a chooser they cannot leave.
            Assert.Greater(RealmRegistry.Boarders().Length, 0,
                "the chooser guarantees a Boarder to take in");

            yield return PlayTestUtil.ReturnToBlankScene();
        }

        /// <summary>Hosting lives here now, and it opens closed. A player arriving at the
        /// chooser is picking a realm, not already handing out a code.</summary>
        [UnityTest]
        public IEnumerator TheChooser_OpensOnTheLists_NotOnTheJoinCode()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.RealmSelect);
            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.RealmSelect);

            var panel = FindByName("Session Panel");
            Assert.IsNotNull(panel, "RealmSelect.unity carries the open-to-friends panel");
            Assert.IsFalse(panel.activeSelf,
                "the chooser shows its lists first — the code panel is opened deliberately");

            var open = FindByName("OPENTOFRIENDSButton");
            Assert.IsNotNull(open, "and the button that opens it");

            yield return PlayTestUtil.ReturnToBlankScene();
        }

        /// <summary>Inactive objects are invisible to the usual finds, and the panel under test
        /// is inactive by design, so this walks the hierarchy including what is switched
        /// off.</summary>
        static GameObject FindByName(string name)
        {
            var roots = UnityEngine.SceneManagement.SceneManager
                .GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var all = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < all.Length; j++)
                    if (all[j].name == name) return all[j].gameObject;
            }
            return null;
        }

        [UnityTest]
        public IEnumerator CreatingARealm_NamesIt_AndListsIt()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.RealmSelect);
            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.RealmSelect);

            int before = RealmRegistry.Realms().Length;
            TypeName("HOLLOW REACH");
            View().onCreate.Invoke();
            yield return null;

            var realms = RealmRegistry.Realms();
            Assert.AreEqual(before + 1, realms.Length, "the realm was created");
            bool found = false;
            for (int i = 0; i < realms.Length; i++)
                if (realms[i].Name == "HOLLOW REACH") found = true;
            Assert.IsTrue(found, "and it is listed under the name that was typed");

            yield return PlayTestUtil.ReturnToBlankScene();
        }

        /// <summary>One press arms and says so; it does not delete. This is the test for a
        /// defect that shipped: the prompt was written and then immediately painted over by
        /// Repaint, which ends by writing the enter-refusal over the status line. The button
        /// label still changed, so the only visible sign was one word — and the realm survived
        /// either way, which is exactly what makes it easy to miss.</summary>
        [UnityTest]
        public IEnumerator DeletingARealm_AsksFirst_AndSaysSo()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.RealmSelect);
            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.RealmSelect);

            TypeName("SPENT LANTERN");
            View().onCreate.Invoke();
            yield return null;
            int before = RealmRegistry.Realms().Length;

            View().onDeleteRealm.Invoke();
            yield return null;

            Assert.AreEqual(before, RealmRegistry.Realms().Length,
                "one press must never delete a world");
            StringAssert.Contains("Press again", View().StatusText,
                "and the player has to be told that a second press will");
            StringAssert.Contains("SPENT LANTERN", View().StatusText,
                "naming what is about to go");

            yield return PlayTestUtil.ReturnToBlankScene();
        }

        /// <summary>The second press deletes, and the document is moved rather than unlinked,
        /// so a mispress is recoverable outside the game.</summary>
        [UnityTest]
        public IEnumerator DeletingARealm_Twice_RetiresIt()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.RealmSelect);
            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.RealmSelect);

            TypeName("COLD FURROW");
            View().onCreate.Invoke();
            yield return null;
            string id = null;
            var realms = RealmRegistry.Realms();
            for (int i = 0; i < realms.Length; i++)
                if (realms[i].Name == "COLD FURROW") id = realms[i].Id;
            Assert.IsNotNull(id, "the realm under test was created");

            View().onDeleteRealm.Invoke();
            View().onDeleteRealm.Invoke();
            yield return null;

            realms = RealmRegistry.Realms();
            for (int i = 0; i < realms.Length; i++)
                Assert.AreNotEqual(id, realms[i].Id, "the realm is gone from the chooser");
            Assert.IsFalse(System.IO.File.Exists(SaveService.RealmPath(id)),
                "and out of the realms directory");
            Assert.IsTrue(System.IO.Directory.Exists(SaveService.RetiredDirectory),
                "moved, not unlinked — a mispress stays recoverable by hand");

            yield return PlayTestUtil.ReturnToBlankScene();
        }

        [UnityTest]
        public IEnumerator ReEnteringARealm_FindsItsWorldExactlyAsItWasLeft()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.RealmSelect);
            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.RealmSelect);

            TypeName("ASH LADDER");
            View().onCreate.Invoke();
            yield return null;

            string realmId = null;
            var realms = RealmRegistry.Realms();
            for (int i = 0; i < realms.Length; i++)
                if (realms[i].Name == "ASH LADDER") realmId = realms[i].Id;
            Assert.IsNotNull(realmId);
            string boarderId = RealmRegistry.Boarders()[0].Id;

            Assert.IsTrue(GameSession.EnterRealm(realmId, boarderId), "the realm opens");

            // What a session does to a world, set directly rather than played for.
            GameSession.Meta.EarnSalvage(1234L);
            GameSession.Meta.SetOpenDoorIds(new[] { "D1", "D2" });
            GameSession.Meta.SetCompletedTerminalIds(new[] { "T1" });
            GameSession.Meta.HatchArmorLevel = 2;
            GameSession.Meta.DeepestWaveReached = 4;
            GameSession.SaveRealm();

            // Leave for somewhere else entirely, so nothing can be left over in memory.
            var elsewhere = RealmRegistry.CreateRealm("SOMEWHERE ELSE");
            Assert.IsTrue(GameSession.EnterRealm(elsewhere.Id, boarderId));
            Assert.AreEqual(0L, GameSession.Meta.BankedSalvage, "a different world, untouched");
            Assert.AreEqual(0, GameSession.Meta.OpenDoorIds.Length);

            // And come back.
            Assert.IsTrue(GameSession.EnterRealm(realmId, boarderId), "the realm reopens");
            Assert.AreEqual(1234L, GameSession.Meta.BankedSalvage, "the pot is as it was left");
            Assert.AreEqual(2, GameSession.Meta.OpenDoorIds.Length, "so are the doors");
            Assert.AreEqual(1, GameSession.Meta.CompletedTerminalIds.Length, "and the clears");
            Assert.AreEqual(2, GameSession.Meta.HatchArmorLevel, "and the ship upgrade");
            Assert.AreEqual(4, GameSession.Meta.DeepestWaveReached);

            yield return PlayTestUtil.ReturnToBlankScene();
        }

        [UnityTest]
        public IEnumerator ABoardersKitTravels_AndTheWorldsProgressDoesNot()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.RealmSelect);
            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.RealmSelect);

            string boarderId = RealmRegistry.Boarders()[0].Id;
            var first = RealmRegistry.CreateRealm("FIRST");
            Assert.IsTrue(GameSession.EnterRealm(first.Id, boarderId));

            GameSession.Meta.SetOwned(WeaponId.Br12Shotgun, true);
            GameSession.Meta.SetEquipped(WeaponId.Br12Shotgun);
            GameSession.Meta.EarnSalvage(500L);
            GameSession.Meta.SetCompletedTerminalIds(new[] { "T1" });
            GameSession.Save();

            var second = RealmRegistry.CreateRealm("SECOND");
            Assert.IsTrue(GameSession.EnterRealm(second.Id, boarderId));

            Assert.IsTrue(GameSession.Meta.Owns(WeaponId.Br12Shotgun),
                "the weapon bought in one world is in your hands in the next");
            Assert.AreEqual(WeaponId.Br12Shotgun, GameSession.Meta.EquippedPrimary);
            Assert.AreEqual(0L, GameSession.Meta.BankedSalvage,
                "but the salvage stayed in the world that earned it");
            Assert.AreEqual(0, GameSession.Meta.CompletedTerminalIds.Length,
                "and so did the bastion it cleared");

            yield return PlayTestUtil.ReturnToBlankScene();
        }
    }
}
