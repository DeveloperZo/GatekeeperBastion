using System.Text;
using GatekeepersProtocol.Sim;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>
    /// REPLICATED-STATE: shows what this realm has done, on the hull, without a menu — a mark
    /// per run survived, the crew's weapons on a rack, and a plate carrying the counts.
    /// </summary>
    /// <remarks>
    /// Read-only. It subscribes to nothing and writes nothing; it reads the record and paints.
    /// The record comes from <see cref="RealmRecordSource"/> rather than straight off
    /// <c>GameSession.Meta</c>, because a guest standing on a host's ship has its OWN realm
    /// loaded — reading that locally would render a confident, wrong number with no error.
    ///
    /// The rack shows the BOARDER's weapons, not the realm's. A realm owns no weapons: kit
    /// travels with the player (Docs/Milestones — the realm and the Boarder), so a rack in the
    /// crew's armoury is the kit of whoever is aboard. That is a correction to this
    /// milestone's original wording, ruled 2026-08-22.
    ///
    /// Surfaces are AUTHORED in the scene and this only switches them on and off. The one
    /// thing built at runtime is the weapon sitting on a mount, because which weapons exist is
    /// data rather than layout.
    /// </remarks>
    public sealed class RealmRecordView : MonoBehaviour
    {
        const string Tag = "RealmRecord";

        [SerializeField, Tooltip("Root of the authored bastion marks. One is switched on per " +
            "run survived, in order. Authored in the Ship scene.")]
        Transform _marksRoot;

        [SerializeField, Tooltip("Root of the authored rack mounts, one per weapon the ARMORY " +
            "can sell. Authored in the Ship scene.")]
        Transform _rackRoot;

        [SerializeField, Tooltip("Weapon prefab per catalog row, in WeaponId order. A row with " +
            "no prefab leaves its mount standing empty rather than drawing nothing at all.")]
        GameObject[] _weaponPrefabs;

        [SerializeField, Tooltip("Plate carrying runs survived, runs lost and deepest wave.")]
        TextMesh _plate;

        [SerializeField, Tooltip("Seconds between checks. The record only changes on a return " +
            "from a Run or a purchase, so this does not want to be every frame.")]
        float _checkInterval = 0.5f;

        Transform[] _marks;
        Transform[] _mounts;
        GameObject[] _mounted;
        readonly StringBuilder _line = new StringBuilder(96);

        // Change detection only — never a display source. The painted values are read fresh
        // from the record every time anything is written, so nothing here is a second copy of
        // a number the player can see (criterion 8).
        long _paintedRecord = -1L;
        int _paintedOwned = -1;
        bool _painted;
        float _nextCheck;

        void Awake()
        {
            _marks = Children(_marksRoot);
            _mounts = Children(_rackRoot);
            _mounted = new GameObject[_mounts.Length];
            if (_marksRoot == null || _rackRoot == null || _plate == null)
                Debug.LogError("[" + Tag + "] '" + name + "' is missing a serialized surface "
                    + "(wire _marksRoot, _rackRoot and _plate in the Ship scene).", this);
        }

        static Transform[] Children(Transform root)
        {
            if (root == null) return new Transform[0];
            var kids = new Transform[root.childCount];
            for (int i = 0; i < kids.Length; i++) kids[i] = root.GetChild(i);
            return kids;
        }

        void OnEnable()
        {
            _painted = false;
            _nextCheck = 0f;
        }

        void Update()
        {
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + _checkInterval;
            Repaint(false);
        }

        /// <summary>Paint if anything moved. Nothing allocates on the unchanged path, which is
        /// almost every check.</summary>
        public void Repaint(bool force)
        {
            RealmRecord record = RealmRecordSource.Current();
            int owned = OwnedMask();
            long packed = record.Pack();
            if (!force && _painted && packed == _paintedRecord && owned == _paintedOwned) return;

            PaintMarks(record);
            PaintRack(owned);
            PaintPlate(record);

            _paintedRecord = packed;
            _paintedOwned = owned;
            _painted = true;
        }

        /// <summary>Which weapons the Boarder aboard owns, as a bit per catalog row. A guest
        /// sees its own kit here on purpose: the rack is the crew's, and a visiting player's
        /// weapons came with them.</summary>
        static int OwnedMask()
        {
            var meta = GameSession.Meta;
            if (meta == null) return 0;
            int mask = 0;
            for (int i = 0; i < WeaponCatalog.ArraySize && i < 31; i++)
                if (meta.Owns((WeaponId)i)) mask |= 1 << i;
            return mask;
        }

        /// <summary>One mark per run this realm survived, in order, capped by how many the
        /// wall was built to hold.</summary>
        void PaintMarks(RealmRecord record)
        {
            for (int i = 0; i < _marks.Length; i++)
            {
                var mark = _marks[i];
                if (mark == null) continue;
                bool lit = i < record.CleanExits;
                if (mark.gameObject.activeSelf != lit) mark.gameObject.SetActive(lit);
            }
        }

        /// <summary>A mount shows its weapon when the Boarder owns it and stands empty
        /// otherwise. An empty mount is a mount, not a hole — nothing is missing, there is
        /// simply nothing on it yet (criterion 6).</summary>
        /// <remarks>
        /// The mounts are authored; the weapon standing on one is built here because WHICH
        /// weapons exist is data, not layout — the same reason a wave's contents are not
        /// authored per wave. Each is built at most once and then switched, so a player
        /// walking past the rack repeatedly does not allocate.
        /// </remarks>
        void PaintRack(int owned)
        {
            for (int i = 0; i < _mounts.Length; i++)
            {
                var mount = _mounts[i];
                if (mount == null) continue;
                bool has = (owned & (1 << i)) != 0;
                if (has && _mounted[i] == null) _mounted[i] = Mount(i, mount);
                if (_mounted[i] == null) continue;
                if (_mounted[i].activeSelf != has) _mounted[i].SetActive(has);
            }
        }

        /// <summary>Put the weapon on its mount. Null when the catalog row has no prefab —
        /// that mount then simply stays bare, which is the honest reading of a weapon this
        /// build has no art for.</summary>
        GameObject Mount(int index, Transform mount)
        {
            if (_weaponPrefabs == null || index >= _weaponPrefabs.Length) return null;
            GameObject prefab = _weaponPrefabs[index];
            if (prefab == null) return null;
            GameObject built = Instantiate(prefab, mount);
            built.transform.localPosition = Vector3.zero;
            built.transform.localRotation = Quaternion.identity;
            StripColliders(built);
            return built;
        }

        /// <summary>A weapon on a wall is scenery. Leaving its colliders live would let a
        /// player walk into a rifle, and the rack is not something you interact with
        /// (criterion 9 — no new verb).</summary>
        static void StripColliders(GameObject built)
        {
            var colliders = built.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;
        }

        void PaintPlate(RealmRecord record)
        {
            if (_plate == null) return;
            _line.Length = 0;
            if (record.IsUntouched)
            {
                // Not "0 / 0 / 1". A realm nobody has played has no record, and saying so
                // reads as a place waiting to be used rather than as a broken readout.
                _line.Append("NO RECORD YET");
            }
            else
            {
                _line.Append("SURVIVED ").Append(record.CleanExits);
                _line.Append("   LOST ").Append(record.RunsLost);
                _line.Append("   DEEPEST WAVE ").Append(record.DeepestWave);
                if (record.HatchArmor > 0)
                    _line.Append("   HATCH +").Append(record.HatchArmor);
            }
            _plate.text = _line.ToString();
        }
    }
}
