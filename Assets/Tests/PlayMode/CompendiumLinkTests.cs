using System.Collections;
using System.Reflection;
using NUnit.Framework;
using GatekeepersProtocol.Game;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>The pause Compendium's body links, exercised in the authored Ship scene:
    /// the rendered TMP text must carry link regions, the TMP hit-test must find one at
    /// its own character geometry, the EventSystem raycast at that point must route to
    /// the view's click handler, and the click must open the linked page.</summary>
    public class CompendiumLinkTests
    {
        static readonly FieldInfo BodyLabelField = typeof(CompendiumView).GetField(
            "_bodyLabel", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo OpenStemField = typeof(CompendiumView).GetField(
            "_openStem", BindingFlags.Instance | BindingFlags.NonPublic);

        CompendiumView _view;
        TextMeshProUGUI _body;
        bool _restoreComingSoon;
        bool _comingSoonPrev;

        [TearDown]
        public void RestoreComingSoonSetting()
        {
            if (!_restoreComingSoon) return;
            Settings.SetBool(SettingId.ShowComingSoonEntries, _comingSoonPrev);
            _restoreComingSoon = false;
        }

        IEnumerator OpenReaderOnShip()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.ShipCoop);
            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.ShipCoop);

            // The game's entry point: pause shell first, then the reader — Open() on an
            // inactive pause hierarchy would leave TMP with no geometry to hit-test.
            var ships = PlayTestUtil.FindAll<ShipController>();
            Assert.IsNotEmpty(ships, "No ShipController in " + SceneFlow.ShipCoop + ".");
            var views = PlayTestUtil.FindAll<CompendiumView>();
            Assert.IsNotEmpty(views, "No CompendiumView authored in " + SceneFlow.ShipCoop + ".");
            _view = views[0];

            ships[0].OpenCompendium();
            Assert.IsTrue(_view.IsOpen,
                "OpenCompendium left the reader closed synchronously (settings open: " +
                ships[0].IsSettingsOpen + ", station: " +
                (ships[0].OpenStation != null ? ships[0].OpenStation.name : "none") + ").");
            yield return PlayTestUtil.Frames(3);
            Assert.IsTrue(_view.IsOpen,
                "Reader opened and then closed within three frames (settings open: " +
                ships[0].IsSettingsOpen + ", station: " +
                (ships[0].OpenStation != null ? ships[0].OpenStation.name : "none") + ").");

            Assert.IsNotNull(BodyLabelField, "CompendiumView._bodyLabel field moved.");
            _body = (TextMeshProUGUI)BodyLabelField.GetValue(_view);
            Assert.IsNotNull(_body, "Body label not authored on the Ship reader.");
            Assert.IsTrue(_body.gameObject.activeInHierarchy,
                "Body label inactive with the reader open — pause shell not active.");
            _body.ForceMeshUpdate();
        }

        /// <summary>Screen point at the centre of the first character of link 0.</summary>
        Vector2 FirstLinkScreenPoint()
        {
            StringAssert.Contains("<link=", _body.text,
                "Body text carries no <link> tags — markdown link conversion broken.");
            TMP_TextInfo info = _body.textInfo;
            Assert.Greater(info.linkCount, 0,
                "Body text has <link> tags but TMP parsed no link regions — rich text " +
                "or geometry generation broken.");
            TMP_LinkInfo link = info.linkInfo[0];
            TMP_CharacterInfo ch = info.characterInfo[link.linkTextfirstCharacterIndex];
            Vector3 mid = (ch.bottomLeft + ch.topRight) * 0.5f;
            Vector3 world = _body.transform.TransformPoint(mid);
            return RectTransformUtility.WorldToScreenPoint(null, world);
        }

        [UnityTest]
        public IEnumerator BodyLink_TmpHitTest_FindsLinkAtItsOwnGeometry()
        {
            yield return OpenReaderOnShip();
            Vector2 screen = FirstLinkScreenPoint();
            int hit = TMP_TextUtilities.FindIntersectingLink(_body, screen, null);
            Assert.GreaterOrEqual(hit, 0,
                "TMP_TextUtilities.FindIntersectingLink misses at the exact geometry of " +
                "link 0 (screen " + screen + ") — the hit-test path the click handler uses.");
        }

        [UnityTest]
        public IEnumerator BodyLink_EventSystemRaycast_RoutesToClickHandler()
        {
            yield return OpenReaderOnShip();
            Vector2 screen = FirstLinkScreenPoint();

            EventSystem es = EventSystem.current;
            if (es == null)
            {
                var go = new GameObject("TestEventSystem");
                es = go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();
                yield return null;
            }

            var ev = new PointerEventData(es) { position = screen };
            var results = new System.Collections.Generic.List<RaycastResult>();
            es.RaycastAll(ev, results);
            Assert.IsNotEmpty(results,
                "Nothing under the pointer at a body link — no raycast target covers the text.");

            GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(
                results[0].gameObject);
            Assert.AreEqual(_view.gameObject, handler,
                "Topmost raycast target at a body link ('" + results[0].gameObject.name +
                "') does not route clicks to CompendiumView.");
        }

        int ActiveNavRows(string prefix)
        {
            int n = 0;
            var buttons = PlayTestUtil.FindAll<Button>();
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i].name.StartsWith(prefix, System.StringComparison.Ordinal)
                    && buttons[i].gameObject.activeInHierarchy)
                    n++;
            }
            return n;
        }

        bool NavRowPresent(string goName)
        {
            var buttons = PlayTestUtil.FindAll<Button>();
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i].name == goName) return true;
            }
            return false;
        }

        [UnityTest]
        public IEnumerator Nav_ComingSoonToggleOff_RemovesTeaserRowsAndKeepsShippedOnes()
        {
            _comingSoonPrev = Settings.GetBool(SettingId.ShowComingSoonEntries);
            _restoreComingSoon = true;
            Settings.SetBool(SettingId.ShowComingSoonEntries, true);

            yield return OpenReaderOnShip();
            Assert.IsTrue(NavRowPresent("Entry_Protocol-AEGIS"),
                "With the toggle on, the AEGIS teaser row should be built.");

            Settings.SetBool(SettingId.ShowComingSoonEntries, false);
            _view.Close();
            _view.Open();
            yield return null;

            Assert.IsFalse(NavRowPresent("Entry_Protocol-AEGIS"),
                "With the toggle off, teaser rows should not be built.");
            Assert.IsTrue(NavRowPresent("Entry_Protocol-VEIL"),
                "Shipped entries must survive the Coming Soon filter.");
        }

        [UnityTest]
        public IEnumerator Nav_OpensConcise_AndUnfoldsTheVolumeOfTheOpenPage()
        {
            // Row counts below assume the full set, so pin the Coming Soon toggle on.
            _comingSoonPrev = Settings.GetBool(SettingId.ShowComingSoonEntries);
            _restoreComingSoon = true;
            Settings.SetBool(SettingId.ShowComingSoonEntries, true);

            yield return OpenReaderOnShip();

            Assert.AreEqual(CompendiumCatalog.Volumes.Length, ActiveNavRows("Volume_"),
                "Every volume header should be visible.");
            // The reader opens on the first entry, so exactly volume I is unfolded.
            Assert.AreEqual(CompendiumCatalog.Volumes[0].EntryStems.Length,
                ActiveNavRows("Entry_"),
                "A fresh open should show only the open entry's volume unfolded.");

            // Opening a page in a collapsed volume unfolds that volume too.
            _view.Open("Gatekeeper-Thief");
            yield return null;
            Assert.AreEqual(
                CompendiumCatalog.Volumes[0].EntryStems.Length
                + CompendiumCatalog.Volumes[3].EntryStems.Length,
                ActiveNavRows("Entry_"),
                "Opening a Garrison page should unfold the Garrison volume.");
        }

        [UnityTest]
        public IEnumerator BodyLink_Click_OpensTheLinkedPage()
        {
            yield return OpenReaderOnShip();
            string target = _body.textInfo.linkInfo[0].GetLinkID();
            Vector2 screen = FirstLinkScreenPoint();

            EventSystem es = EventSystem.current;
            var ev = es != null ? new PointerEventData(es) : new PointerEventData(null);
            ev.position = screen;
            ((IPointerClickHandler)_view).OnPointerClick(ev);
            yield return null;

            Assert.IsNotNull(OpenStemField, "CompendiumView._openStem field moved.");
            string open = (string)OpenStemField.GetValue(_view);
            Assert.AreEqual(target, open,
                "Clicking link '" + target + "' did not open its page (open stem: '" +
                open + "').");
        }
    }
}
