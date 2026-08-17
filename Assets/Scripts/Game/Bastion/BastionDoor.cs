using GatekeepersProtocol.Sim;
using UnityEngine;
using UnityEngine.AI;

namespace GatekeepersProtocol.Game
{
    /// <summary>Which segment edge a door seals.</summary>
    public enum BastionDoorEdge : byte
    {
        North = 0,
        South = 1,
        East = 2,
        West = 3,
    }

    /// <summary>HOST-ONLY: a sliding door leaf in a doorway. Locked doors (progression gates)
    /// stay shut until the unlock graph opens them; unlocked doors slide open for anyone
    /// within range and shut behind them. NavMesh carving only while locked — unlocked
    /// doorways stay pathable and the leaf outruns an approaching agent.</summary>
    public sealed class BastionDoor : MonoBehaviour
    {
        public const string SlabName = "GateSlab";

        const float LeafThickness = 0.35f;
        const float ProximityRange = 5f;
        const float SlideSpeed = 8f;
        const float PollInterval = 0.15f;

        [SerializeField] string doorId;
        [SerializeField, Tooltip("Also filled by Field terminals that reference this door.")]
        string[] requiredTerminalIds;
        [SerializeField] bool startsLocked = true;
        [SerializeField] bool opensControlRoom;
        [SerializeField] BastionDoorEdge apertureEdge = BastionDoorEdge.North;
        [SerializeField] SpawnPoint[] sealedSpawns;
        [SerializeField] Material gateMat;

        static readonly Collider[] ProximityHits = new Collider[16];

        BoxCollider _blockCollider;
        NavMeshObstacle _obstacle;
        Transform _slab;
        BastionLayout _layout;
        Vector3 _apertureSize;
        bool _unlocked;
        bool _leafOpen;
        float _nextPoll;
        bool _blockersReady;

        public string DoorId => doorId;
        public string[] RequiredTerminalIds => requiredTerminalIds;
        public bool StartsLocked => startsLocked;
        public bool OpensControlRoom => opensControlRoom;
        public BastionDoorEdge ApertureEdge => apertureEdge;
        public SpawnPoint[] SealedSpawns => sealedSpawns;
        public bool IsOpen => _unlocked;

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
            ApplyLeafMaterial();
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
            _apertureSize = size;
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
                _slab.localScale = new Vector3(size.x, size.y, LeafThickness);
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
                _slab.localScale = new Vector3(size.x, size.y, LeafThickness);
                ApplyLeafMaterial();
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
                go.transform.localScale = new Vector3(size.x, size.y, LeafThickness);

                var col = go.GetComponent<Collider>();
                if (col != null)
                    DestroyImmediate(col);

                _slab = go.transform;
                ApplyLeafMaterial();
                return;
            }
#endif
            Debug.LogWarning(
                "[BastionDoor] Missing GateSlab on " + name
                + " — drop gkb_bastion_sketch_request.txt to re-emit doors.",
                this);
        }

        /// <summary>Locked reads as a sealed gate (gate material); unlocked reads as plain
        /// station wall. The swap on unlock is the visible state change.</summary>
        void ApplyLeafMaterial()
        {
            if (_slab == null)
                return;
            var renderer = _slab.GetComponent<MeshRenderer>();
            if (renderer == null)
                return;

            Material wall = null;
            BastionSegment segment = GetComponentInParent<BastionSegment>();
            if (segment != null)
                wall = segment.WallMaterial;

            Material locked = gateMat != null ? gateMat : wall;
            Material open = wall != null ? wall : gateMat;
            Material pick = _unlocked ? open : locked;
            if (pick != null)
                renderer.sharedMaterial = pick;
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

        /// <summary>Unlock state from the graph. Locked: leaf shut, walk and nav blocked.
        /// Unlocked: nav carve off for good; the leaf becomes a proximity door.</summary>
        public void SetOpen(bool open)
        {
            if (!_blockersReady)
                EnsureGatePresentation();

            _unlocked = open;
            if (_obstacle != null)
            {
                _obstacle.enabled = !open;
                _obstacle.carving = !open;
            }

            if (!Application.isPlaying)
                SetLeafInstant(false);
            else if (!open)
                _leafOpen = false;

            ApplyLeafMaterial();

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

        void SetLeafInstant(bool leafOpen)
        {
            _leafOpen = leafOpen;
            if (_slab != null)
            {
                Vector3 p = _slab.localPosition;
                p.y = LeafTargetY(leafOpen);
                _slab.localPosition = p;
            }
            if (_blockCollider != null)
                _blockCollider.enabled = !leafOpen;
        }

        float LeafTargetY(bool leafOpen)
        {
            float shut = _apertureSize.y * 0.5f;
            return leafOpen ? shut + _apertureSize.y : shut;
        }

        void Update()
        {
            if (!Application.isPlaying || !_blockersReady || _slab == null)
                return;

            if (_unlocked && Time.time >= _nextPoll)
            {
                _nextPoll = Time.time + PollInterval;
                _leafOpen = AnyoneNear();
            }

            Vector3 pos = _slab.localPosition;
            float target = LeafTargetY(_leafOpen);
            if (!Mathf.Approximately(pos.y, target))
            {
                pos.y = Mathf.MoveTowards(pos.y, target, SlideSpeed * Time.deltaTime);
                _slab.localPosition = pos;
            }

            // Physical block only while the leaf actually fills the doorway.
            if (_blockCollider != null)
                _blockCollider.enabled = pos.y < LeafTargetY(false) + 0.3f;
        }

        bool AnyoneNear()
        {
            Vector3 center = transform.TransformPoint(0f, _apertureSize.y * 0.5f, 0f);
            var half = new Vector3(_apertureSize.x * 0.5f + 1f, _apertureSize.y * 0.5f, ProximityRange);
            int count = Physics.OverlapBoxNonAlloc(center, half, ProximityHits, transform.rotation);
            for (int i = 0; i < count; i++)
            {
                Collider hit = ProximityHits[i];
                if (hit == null || hit == _blockCollider)
                    continue;
                if (hit.GetComponentInParent<CharacterController>() != null
                    || hit.GetComponentInParent<NavMeshAgent>() != null
                    || hit.GetComponentInParent<BoarderPresentation>() != null)
                    return true;
            }
            return false;
        }
    }
}
