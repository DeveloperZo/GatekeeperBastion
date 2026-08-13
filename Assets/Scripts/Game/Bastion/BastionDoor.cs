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

    /// <summary>HOST-ONLY: progression gate — a visible slab that fills the doorway while
    /// locked, then hides when the unlock graph opens it. Not a passage.</summary>
    public sealed class BastionDoor : MonoBehaviour
    {
        public const string SlabName = "GateSlab";

        [SerializeField] string doorId;
        [SerializeField, Tooltip("Also filled by Field terminals that reference this door.")]
        string[] requiredTerminalIds;
        [SerializeField] bool startsLocked = true;
        [SerializeField] bool opensControlRoom;
        [SerializeField] BastionDoorEdge apertureEdge = BastionDoorEdge.North;
        [SerializeField] SpawnPoint[] sealedSpawns;
        [SerializeField] Material gateMat;

        BoxCollider _blockCollider;
        NavMeshObstacle _obstacle;
        Transform _slab;
        BastionLayout _layout;
        bool _open;
        bool _blockersReady;

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
            EnsureGatePresentation();
            SetOpen(!locked);
        }

        public void SetSealedSpawns(SpawnPoint[] spawns)
        {
            sealedSpawns = spawns;
        }

        public void SetGateMaterial(Material mat)
        {
            gateMat = mat;
            if (_slab != null)
                ApplySlabMaterial(_slab.gameObject);
        }

        void Awake()
        {
            EnsureGatePresentation();
        }

        /// <summary>Size blockers and ensure GateSlab. Does not move the transform —
        /// hand-authored placement wins; use <see cref="SnapToAperture"/> for sketch emit.</summary>
        public void EnsureGatePresentation()
        {
            EnsureBlockers();
            _blockersReady = true;
        }

        /// <summary>Place this gate on the parent segment's Door opening for
        /// <see cref="apertureEdge"/>. Sketch emit only — does not rewrite authored sides.</summary>
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
            Vector3 size = ResolveBlockerSize();

            _blockCollider = GetComponent<BoxCollider>();
            if (_blockCollider == null)
                _blockCollider = gameObject.AddComponent<BoxCollider>();

            _obstacle = GetComponent<NavMeshObstacle>();
            if (_obstacle == null)
                _obstacle = gameObject.AddComponent<NavMeshObstacle>();
            _obstacle.carving = true;
            _obstacle.shape = NavMeshObstacleShape.Box;

            ApplyBlockerSize(size);
            EnsureSlab(size);
        }

        Vector3 ResolveBlockerSize()
        {
            BastionSegment segment = GetComponentInParent<BastionSegment>();
            if (segment != null
                && segment.TryGetDoorAperture(apertureEdge, out _, out _, out Vector3 apertureSize))
                return apertureSize;
            return new Vector3(3f, 3.5f, BastionKit.WallThickness * 2f);
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
                _slab.localRotation = Quaternion.identity;
                _slab.localScale = size;
                ApplySlabMaterial(_slab.gameObject);
                return;
            }

#if UNITY_EDITOR
            // GateSlab is scene-authored (terminals wire). Only create when missing in Edit mode.
            if (!Application.isPlaying)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = SlabName;
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = size;

                var col = go.GetComponent<Collider>();
                if (col != null)
                    DestroyImmediate(col);

                ApplySlabMaterial(go);
                _slab = go.transform;
                return;
            }
#endif
            Debug.LogError(
                "[BastionDoor] Missing GateSlab on " + name
                + " — drop gkb_bastion_terminals_request.txt to author sealed doors.",
                this);
        }

        void ApplySlabMaterial(GameObject slab)
        {
            var renderer = slab.GetComponent<MeshRenderer>();
            if (renderer == null)
                return;
            if (gateMat != null)
            {
                renderer.sharedMaterial = gateMat;
                return;
            }

            BastionSegment segment = GetComponentInParent<BastionSegment>();
            if (segment != null && segment.WallMaterial != null)
            {
                renderer.sharedMaterial = segment.WallMaterial;
                return;
            }
        }

        void OnEnable()
        {
            if (!_blockersReady)
                EnsureGatePresentation();
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
            if (!_blockersReady)
                EnsureGatePresentation();

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
