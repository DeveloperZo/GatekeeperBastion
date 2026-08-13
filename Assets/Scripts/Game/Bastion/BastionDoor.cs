using GatekeeperBastion.Sim;
using UnityEngine;
using UnityEngine.AI;

namespace GatekeeperBastion.Game
{
    /// <summary>Which segment edge a progression gate seals. Passages (Door side, no
    /// <see cref="BastionDoor"/>) stay walkable with no gate mark.</summary>
    public enum BastionDoorEdge : byte
    {
        North = 0,
        South = 1,
        East = 2,
        West = 3,
    }

    /// <summary>HOST-ONLY: progression gate — starts locked, opens when its terminal set
    /// completes. Not a passage: Door sides without this component are free explores.</summary>
    public sealed class BastionDoor : MonoBehaviour
    {
        const string SlabName = "GateSlab";

        [SerializeField] string doorId;
        [SerializeField] string[] requiredTerminalIds;
        [SerializeField] bool startsLocked = true;
        [SerializeField] bool opensControlRoom;
        [SerializeField] BastionDoorEdge apertureEdge = BastionDoorEdge.North;
        [SerializeField] SpawnPoint[] sealedSpawns;

        BoxCollider _blockCollider;
        NavMeshObstacle _obstacle;
        Transform _slab;
        BastionLayout _layout;
        bool _open;

        public string DoorId => doorId;
        public string[] RequiredTerminalIds => requiredTerminalIds;
        public bool StartsLocked => startsLocked;
        public bool OpensControlRoom => opensControlRoom;
        public BastionDoorEdge ApertureEdge => apertureEdge;
        public SpawnPoint[] SealedSpawns => sealedSpawns;
        public bool IsOpen => _open;

        /// <summary>Editor/sketch emit — assigns ids and snaps to the segment Door aperture.</summary>
        public void Configure(
            string id,
            string[] requiredTerminals,
            bool controlGate,
            BastionDoorEdge edge,
            bool locked = true)
        {
            doorId = id;
            requiredTerminalIds = requiredTerminals != null
                ? requiredTerminals
                : System.Array.Empty<string>();
            opensControlRoom = controlGate;
            apertureEdge = edge;
            startsLocked = locked;
            SnapToAperture();
            EnsureBlockers();
            SetOpen(!locked);
        }

        public void SetSealedSpawns(SpawnPoint[] spawns)
        {
            sealedSpawns = spawns;
        }

        void Awake()
        {
            SnapToAperture();
            EnsureBlockers();
        }

        /// <summary>Place this gate on the parent segment's Door opening for
        /// <see cref="apertureEdge"/>.</summary>
        public void SnapToAperture()
        {
            BastionSegment segment = GetComponentInParent<BastionSegment>();
            if (segment == null)
                return;
            if (!segment.TryGetDoorAperture(apertureEdge, out Vector3 localPos, out float yawY, out Vector3 blockerSize))
                return;

            transform.localPosition = localPos;
            transform.localRotation = Quaternion.Euler(0f, yawY, 0f);
            ApplyBlockerSize(blockerSize);
        }

        void EnsureBlockers()
        {
            BastionSegment segment = GetComponentInParent<BastionSegment>();
            Vector3 size = new Vector3(3f, BastionKit.StoreyHeight - BastionKit.FloorSlabThickness, BastionKit.WallThickness);
            if (segment != null
                && segment.TryGetDoorAperture(apertureEdge, out _, out _, out Vector3 apertureSize))
                size = apertureSize;

            _blockCollider = GetComponent<BoxCollider>();
            if (_blockCollider == null)
                _blockCollider = gameObject.AddComponent<BoxCollider>();

            _obstacle = GetComponent<NavMeshObstacle>();
            if (_obstacle == null)
                _obstacle = gameObject.AddComponent<NavMeshObstacle>();
            _obstacle.carving = true;

            ApplyBlockerSize(size);
            EnsureSlab(size);
        }

        void ApplyBlockerSize(Vector3 size)
        {
            float y = size.y * 0.5f;
            var center = new Vector3(0f, y, 0f);
            if (_blockCollider != null)
            {
                _blockCollider.size = size;
                _blockCollider.center = center;
            }

            if (_obstacle != null)
            {
                _obstacle.size = size;
                _obstacle.center = center;
            }

            if (_slab != null)
            {
                _slab.localPosition = center;
                _slab.localScale = size;
            }
        }

        void EnsureSlab(Vector3 size)
        {
            Transform existing = transform.Find(SlabName);
            if (existing != null)
            {
                _slab = existing;
                _slab.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
                _slab.localScale = size;
                return;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = SlabName;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = size;

            // Collider lives on the gate root; slab is presentation only.
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(col);
                else
#endif
                    Destroy(col);
            }

            _slab = go.transform;
        }

        void OnEnable()
        {
            _layout = GetComponentInParent<BastionLayout>();
            if (_layout != null)
                _layout.RegisterDoor(this);
        }

        void OnDisable()
        {
            if (_layout != null)
                _layout.UnregisterDoor(this);
        }

        /// <summary>When closed, block walk and show the slab; when open, clear both and unseal spawns.</summary>
        public void SetOpen(bool open)
        {
            _open = open;
            if (_blockCollider != null)
                _blockCollider.enabled = !open;

            if (_obstacle != null)
            {
                _obstacle.enabled = !open;
                _obstacle.carving = !open;
            }

            if (_slab != null)
                _slab.gameObject.SetActive(!open);

            if (sealedSpawns == null)
                return;

            for (int i = 0; i < sealedSpawns.Length; i++)
            {
                SpawnPoint sp = sealedSpawns[i];
                if (sp == null)
                    continue;
                sp.gameObject.SetActive(open);
            }
        }
    }
}
