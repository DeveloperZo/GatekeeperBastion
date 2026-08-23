using GatekeepersProtocol.Sim;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY: hung spawn closet. Pivot at the door threshold; local +Z toward
    /// the fight floor, interior down −Z. Gatekeepers walk out; the Boarder cannot open it.</summary>
    public sealed class SpawnRoom : MonoBehaviour
    {
        public const string PrefabPath = "Assets/Prefabs/Bastion/SpawnRoom.prefab";
        public const float InteriorWidth = 4f;
        public const float InteriorDepth = 5f;
        public const float SpawnZ = -3f;

        const string FloorName = "Floor";
        const string CeilingName = "Ceiling";
        const string WallLeftName = "Wall_Left";
        const string WallRightName = "Wall_Right";
        const string WallBackName = "Wall_Back";
        const string SpawnChildName = "S_xx";

        [SerializeField] BastionDoor door;
        [SerializeField] SpawnPoint spawnPoint;
        [SerializeField] int minSeats = 1;

        public BastionDoor Door => door;
        public SpawnPoint Spawn => spawnPoint;
        public int MinSeats => minSeats < 1 ? 1 : minSeats;

        public void AttachTo(BastionSegment segment, BastionDoorEdge edge)
        {
            if (segment == null)
                return;

            transform.SetParent(segment.transform, false);
            segment.SetSideDoor(edge);
            if (!Application.isPlaying)
            {
                if (segment.IsAuthored())
                    segment.PunchDoorEdge(edge);
                else
                    segment.Rebuild();
            }

            if (segment.TryGetDoorAperture(edge, out Vector3 localPos, out float yawY, out _))
            {
                transform.localPosition = localPos;
                transform.localRotation = Quaternion.Euler(0f, yawY + 180f, 0f);
            }

            EnsureShell();
            BindChildren();
            ReassertClosetDoor(edge);
            ApplySegmentMaterials(segment);
        }

        public void NameSpawn(string spawnId)
        {
            BindChildren();
            if (spawnPoint == null)
                return;
            spawnPoint.gameObject.name = "S_" + spawnId;
        }

        public void SetMinSeats(int value)
        {
            minSeats = value < 1 ? 1 : value;
        }

        void Reset()
        {
            EnsureShell();
            BindChildren();
            ReassertClosetDoor(BastionDoorEdge.North);
        }

        void Awake()
        {
            BindChildren();
            ReassertClosetDoor(door != null ? door.ApertureEdge : BastionDoorEdge.North);
        }

        void OnEnable()
        {
            BindChildren();
#if UNITY_EDITOR
            if (!Application.isPlaying)
                PunchAuthoredApertureIfMissing();
#endif
        }

        void OnValidate()
        {
            BindChildren();
            if (door != null && door.transform.IsChildOf(transform))
                door.SetOpensFor(BastionDoorOpensFor.Enemies);
#if UNITY_EDITOR
            QueueAuthoredAperturePunch();
#endif
        }

#if UNITY_EDITOR
        bool _aperturePunchQueued;

        void QueueAuthoredAperturePunch()
        {
            if (Application.isPlaying || _aperturePunchQueued)
                return;
            _aperturePunchQueued = true;
            UnityEditor.EditorApplication.delayCall += PunchAuthoredApertureIfMissing;
        }

        void PunchAuthoredApertureIfMissing()
        {
            _aperturePunchQueued = false;
            if (this == null || Application.isPlaying)
                return;
            BastionSegment segment = GetComponentInParent<BastionSegment>();
            if (segment == null || !segment.IsAuthored())
                return;
            BastionDoorEdge edge = HungEdge();
            ReassertClosetDoor(edge);
            if (segment.HasDoorOpeningOn(edge))
                return;
            segment.PunchDoorEdge(edge);
        }

        /// <summary>Hung edge from local pose, not the door's serialized aperture. A prefab
        /// default of North must not punch the passage south of a west closet.</summary>
        BastionDoorEdge HungEdge()
        {
            Vector3 p = transform.localPosition;
            if (Mathf.Abs(p.x) >= Mathf.Abs(p.z))
                return p.x < 0f ? BastionDoorEdge.West : BastionDoorEdge.East;
            return p.z < 0f ? BastionDoorEdge.South : BastionDoorEdge.North;
        }
#endif

        void ReassertClosetDoor(BastionDoorEdge edge)
        {
            BindChildren();
            if (door == null)
                return;
            door.Configure(
                "",
                System.Array.Empty<string>(),
                controlGate: false,
                edge,
                locked: false,
                BastionDoorOpensFor.Enemies,
                snap: false);
            door.transform.localPosition = Vector3.zero;
            door.transform.localRotation = Quaternion.identity;
        }

        void BindChildren()
        {
            if (door != null && !door.transform.IsChildOf(transform))
            {
                door.SetOpensFor(BastionDoorOpensFor.Anyone);
                door = null;
            }
            if (door == null)
                door = GetComponentInChildren<BastionDoor>(true);
            if (spawnPoint != null && !spawnPoint.transform.IsChildOf(transform))
                spawnPoint = null;
            if (spawnPoint == null)
                spawnPoint = GetComponentInChildren<SpawnPoint>(true);
        }

        void ApplySegmentMaterials(BastionSegment segment)
        {
            if (segment == null)
                return;
            Material wall = segment.WallMaterial;
            Material floor = segment.FloorMaterial;
            SetChildMat(FloorName, floor != null ? floor : wall);
            SetChildMat(CeilingName, wall);
            SetChildMat(WallLeftName, wall);
            SetChildMat(WallRightName, wall);
            SetChildMat(WallBackName, wall);
        }

        void SetChildMat(string childName, Material mat)
        {
            if (mat == null)
                return;
            Transform child = transform.Find(childName);
            if (child == null)
                return;
            var renderer = child.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = mat;
        }

        /// <summary>Box shell when the prefab has no interior yet. Runtime instances keep
        /// serialized children.</summary>
        public void EnsureShell()
        {
            if (transform.Find(FloorName) != null)
                return;

            float h = BastionKit.StoreyHeight;
            float t = BastionKit.WallThickness;
            // Floor spans the closet and a wall-thickness lip through the aperture so NavMesh
            // connects to the Half floor.
            MakeBox(FloorName, new Vector3(0f, t * 0.5f, -InteriorDepth * 0.5f + t * 0.5f),
                new Vector3(InteriorWidth, t, InteriorDepth + t));
            MakeBox(CeilingName, new Vector3(0f, h - t * 0.5f, -InteriorDepth * 0.5f),
                new Vector3(InteriorWidth, t, InteriorDepth));
            MakeBox(WallLeftName, new Vector3(-InteriorWidth * 0.5f, h * 0.5f, -InteriorDepth * 0.5f),
                new Vector3(t, h, InteriorDepth));
            MakeBox(WallRightName, new Vector3(InteriorWidth * 0.5f, h * 0.5f, -InteriorDepth * 0.5f),
                new Vector3(t, h, InteriorDepth));
            MakeBox(WallBackName, new Vector3(0f, h * 0.5f, -InteriorDepth),
                new Vector3(InteriorWidth, h, t));

            if (transform.Find(BastionDoor.SlabName) == null && GetComponentInChildren<BastionDoor>(true) == null)
            {
                var doorGo = new GameObject("Door");
                doorGo.transform.SetParent(transform, false);
                doorGo.AddComponent<BastionDoor>();
            }

            if (GetComponentInChildren<SpawnPoint>(true) == null)
            {
                var spawnGo = new GameObject(SpawnChildName);
                spawnGo.transform.SetParent(transform, false);
                spawnGo.transform.localPosition = new Vector3(0f, 0f, SpawnZ);
                spawnGo.AddComponent<SpawnPoint>();
            }
        }

        void MakeBox(string name, Vector3 localPos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = scale;
        }
    }
}
