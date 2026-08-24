using System.Reflection;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;
using UnityEngine;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The hull's record surfaces render what RealmState says, fresh on every paint:
    /// after a clear, a purchase and a deeper run, the marks, the rack and the plate all read
    /// the record's new values (Docs/Milestones — the ship remembers, criteria 2, 3, 5, 8).
    /// The view keeps a packed value for change detection; these mutation asserts are what
    /// keep that cache honest — a repaint that trusted a stale copy fails every one of them.</summary>
    /// <remarks>
    /// EditMode runs no MonoBehaviour lifecycle, so Awake — which collects the authored
    /// children — is invoked by reflection, and the serialized surface fields are bound the
    /// same way. The session is adopted and handed back exactly as the PlayMode fixtures do:
    /// AdoptDevState taints, which makes GameSession.Save a no-op, and teardown restores the
    /// snapshot — or an inert session where there was nothing to find.
    /// </remarks>
    public class RealmRecordViewTests
    {
        const int MarkCount = 12;

        string _snapshot;
        GameObject _rig;
        RealmRecordView _view;
        Transform _marksRoot;
        Transform _rackRoot;
        TextMesh _plate;
        MetaState _meta;

        [SetUp]
        public void AdoptAScratchSessionAndBuildTheSurfaces()
        {
            _snapshot = GameSession.Meta != null ? GameSession.Meta.ToSnapshotJson() : null;
            _meta = MetaState.Compose(
                BoarderState.CreateNew("b-view-test", "TESTER"),
                RealmState.CreateNew("r-view-test", "TESTREALM"));
            GameSession.AdoptDevState(_meta);

            _rig = new GameObject("RealmRecordViewTests rig");
            // Inactive so AddComponent can never run Awake ahead of the field binding below —
            // the view logs an error for unbound surfaces, and a logged error fails the test.
            // Every assert below reads activeSelf, which an inactive parent does not disturb.
            _rig.SetActive(false);

            _marksRoot = new GameObject("Marks").transform;
            _marksRoot.SetParent(_rig.transform);
            for (int i = 0; i < MarkCount; i++)
            {
                var mark = new GameObject("Mark_" + i);
                mark.transform.SetParent(_marksRoot);
                mark.SetActive(false);
            }

            _rackRoot = new GameObject("Mounts").transform;
            _rackRoot.SetParent(_rig.transform);
            var prefabs = new GameObject[WeaponCatalog.ArraySize];
            for (int i = 0; i < WeaponCatalog.ArraySize; i++)
            {
                var mount = new GameObject("Mount_" + i);
                mount.transform.SetParent(_rackRoot);
                // An inactive template stands in for the weapon prefab: what matters here is
                // whether the view stands something on the mount, not what it looks like.
                var template = new GameObject("Weapon_" + i);
                template.transform.SetParent(_rig.transform);
                template.SetActive(false);
                prefabs[i] = template;
            }

            var plateGo = new GameObject("Plate");
            plateGo.transform.SetParent(_rig.transform);
            _plate = plateGo.AddComponent<TextMesh>();

            _view = _rig.AddComponent<RealmRecordView>();
            Bind("_marksRoot", _marksRoot);
            Bind("_rackRoot", _rackRoot);
            Bind("_weaponPrefabs", prefabs);
            Bind("_plate", _plate);
            typeof(RealmRecordView)
                .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_view, null);
        }

        [TearDown]
        public void HandTheSessionBackAsFound()
        {
            if (_rig != null) Object.DestroyImmediate(_rig);
            // Where there was nothing to find, leave an inert session rather than this test's
            // scratch realm — arriving first must not decide what everyone else sees.
            var back = _snapshot != null ? MetaState.FromSnapshotJson(_snapshot) : Inert();
            if (back != null) GameSession.AdoptDevState(back);
            _snapshot = null;
        }

        static MetaState Inert()
        {
            var m = MetaState.CreateNew();
            m.DemoActive = false;
            return m;
        }

        void Bind(string field, object value)
        {
            typeof(RealmRecordView)
                .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_view, value);
        }

        int LitMarks()
        {
            int lit = 0;
            for (int i = 0; i < _marksRoot.childCount; i++)
                if (_marksRoot.GetChild(i).gameObject.activeSelf) lit++;
            return lit;
        }

        bool MountShows(int index)
        {
            var mount = _rackRoot.GetChild(index);
            for (int c = 0; c < mount.childCount; c++)
                if (mount.GetChild(c).gameObject.activeSelf) return true;
            return false;
        }

        // ------------------------------------------------------------------ criterion 6

        [Test]
        public void AFreshRealmReadsAsEmpty_NotAsBroken()
        {
            _view.Repaint();

            Assert.AreEqual(0, LitMarks(), "an unplayed realm lights nothing");
            for (int i = 0; i < WeaponCatalog.ArraySize; i++)
                Assert.IsFalse(MountShows(i), "an empty kit leaves every mount bare");
            Assert.AreEqual("NO RECORD YET", _plate.text,
                "an absence reads as an absence, never as a zero-count label");
        }

        // ------------------------------------------------------------------ criteria 2, 8

        [Test]
        public void AClearLightsAMark_AndTheCountIsAlwaysTheRecords()
        {
            _view.Repaint();
            Assert.AreEqual(0, LitMarks());

            _meta.Realm.CleanExits = 1;
            _view.Repaint();
            Assert.AreEqual(1, LitMarks(), "one run survived, one mark on the wall");
            StringAssert.Contains("SURVIVED 1", _plate.text);

            // A second mutation after a successful paint is what a stale cached copy fails.
            _meta.Realm.CleanExits = 3;
            _view.Repaint();
            Assert.AreEqual(3, LitMarks(), "the wall follows the record, not a copy of it");
            StringAssert.Contains("SURVIVED 3", _plate.text);
        }

        [Test]
        public void TheWallCapsAtItsAuthoredMarks_RatherThanOverrunning()
        {
            _meta.Realm.CleanExits = 100;
            _view.Repaint();

            Assert.AreEqual(MarkCount, LitMarks(),
                "a realm past the wall's capacity lights the whole wall and stops");
        }

        // ------------------------------------------------------------------ criterion 3

        [Test]
        public void APurchaseStandsOnTheRack_BeforeTheNextEmbark()
        {
            _view.Repaint();
            Assert.IsFalse(MountShows((int)WeaponId.Br12Shotgun));

            _meta.Boarder.SetOwned(WeaponId.Br12Shotgun, true);
            _view.Repaint();

            Assert.IsTrue(MountShows((int)WeaponId.Br12Shotgun),
                "a weapon bought this session appears without a scene reload");
            Assert.IsFalse(MountShows((int)WeaponId.Mk1Sidearm),
                "and only the owned row fills — the rack is the kit, not the catalog");
        }

        // ------------------------------------------------------------------ criterion 5

        [Test]
        public void ADeeperRunRaisesThePlate()
        {
            _meta.Realm.CleanExits = 1;
            _meta.Realm.DeepestWaveReached = 5;
            _view.Repaint();
            StringAssert.Contains("WAVE 5", _plate.text);

            _meta.Realm.DeepestWaveReached = 9;
            _view.Repaint();
            StringAssert.Contains("WAVE 9", _plate.text);
        }

        // ------------------------------------------------------------------ criterion 8

        [Test]
        public void EverySurfaceFollowsTheRecord_NoSurfaceHoldsItsOwnNumber()
        {
            _view.Repaint();

            _meta.Realm.CleanExits = 2;
            _meta.Realm.PodEjections = 1;
            _meta.Realm.DeepestWaveReached = 4;
            _meta.Realm.HatchArmorLevel = 1;
            _meta.Boarder.SetOwned(WeaponId.Mk1Sidearm, true);
            _view.Repaint();

            Assert.AreEqual(2, LitMarks());
            Assert.IsTrue(MountShows((int)WeaponId.Mk1Sidearm));
            StringAssert.Contains("SURVIVED 2", _plate.text);
            StringAssert.Contains("LOST 1", _plate.text);
            StringAssert.Contains("WAVE 4", _plate.text);
            StringAssert.Contains("HATCH +1", _plate.text);
        }
    }
}
