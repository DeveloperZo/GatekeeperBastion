using System;
using UnityEngine;
using UnityEngine.AI;

namespace GatekeeperBastion.Game
{
    /// <summary>Rebuilds floor and four sides from block kind and per-edge side state.
    /// North = +Z, South = -Z, East = +X, West = -X.</summary>
    public sealed class BastionSegment : MonoBehaviour
    {
        enum SegmentEdge : byte
        {
            North = 0,
            South = 1,
            East = 2,
            West = 3,
        }

        const string GeomRootName = "_geom";
        const string GenPrefix = "Gen_";
        const float DoorOpeningWidth = 3f;
        const float DoorOpeningHeight = 3.5f;
        const float WindowSillHeight = 1.1f;
        const float WindowLintelHeight = 0.5f;
        const float WindowWalkBlockHeight = 0.5f;

        [SerializeField] BastionBlockKind blockKind = BastionBlockKind.Quarter;
        [SerializeField] BastionSideKind north = BastionSideKind.Wall;
        [SerializeField] BastionSideKind east = BastionSideKind.Wall;
        [SerializeField] BastionSideKind south = BastionSideKind.Wall;
        [SerializeField] BastionSideKind west = BastionSideKind.Wall;
        [SerializeField] Material floorMat;
        [SerializeField] Material wallMat;

        public BastionBlockKind BlockKind
        {
            get => blockKind;
            set => blockKind = value;
        }

        public BastionSideKind North
        {
            get => north;
            set => north = value;
        }

        public BastionSideKind East
        {
            get => east;
            set => east = value;
        }

        public BastionSideKind South
        {
            get => south;
            set => south = value;
        }

        public BastionSideKind West
        {
            get => west;
            set => west = value;
        }

        public Material FloorMaterial
        {
            get => floorMat;
            set => floorMat = value;
        }

        public Material WallMaterial
        {
            get => wallMat;
            set => wallMat = value;
        }

        Transform _geomRoot;
        Material _runtimeFloorMat;
        Material _runtimeWallMat;

        /// <summary>When true, inspector changes do not rebuild (emitter / batch use explicit Rebuild).</summary>
        public static bool SuppressValidateRebuild;

#if UNITY_EDITOR
        bool _rebuildQueued;
#endif

        void OnEnable()
        {
            if (SuppressValidateRebuild)
                return;
            if (!CanMutateHierarchy())
                return;
            Rebuild();
        }

        void OnValidate()
        {
            if (SuppressValidateRebuild)
                return;
            if (!CanMutateHierarchy())
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (_rebuildQueued)
                    return;
                _rebuildQueued = true;
                UnityEditor.EditorApplication.delayCall += DeferredEditorRebuild;
                return;
            }
#endif
            Rebuild();
        }

#if UNITY_EDITOR
        void DeferredEditorRebuild()
        {
            _rebuildQueued = false;
            if (SuppressValidateRebuild)
                return;
            if (this == null)
                return;
            if (!CanMutateHierarchy())
                return;
            Rebuild();
        }
#endif

        /// <summary>True when parenting new meshes under this object is allowed (scene instance or
        /// prefab stage). False for the Prefab Asset on disk — SetParent there orphans Gen_* into
        /// the open scene.</summary>
        public bool CanMutateHierarchy()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorUtility.IsPersistent(gameObject))
                return false;
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
                return false;
#endif
            return true;
        }

        /// <summary>Clear prior generated geometry and emit floor plus four sides.</summary>
        public void Rebuild()
        {
            if (this == null)
                return;
            if (!CanMutateHierarchy())
                return;

            EnsureGeomRoot();
            if (_geomRoot == null)
                return;

            ClearGeneratedChildren();

            Vector2 footprint = BastionKit.Footprint(blockKind);
            float width = footprint.x;
            float depth = footprint.y;
            float wallHeight = BastionKit.StoreyHeight - BastionKit.FloorSlabThickness;
            float wallCenterY = wallHeight * 0.5f;
            float thickness = BastionKit.WallThickness;
            float shortSide = Mathf.Min(width, depth);
            float openingWidth = Mathf.Min(DoorOpeningWidth, shortSide * 0.4f);

            Material floor = ResolveFloorMaterial();
            Material wall = ResolveWallMaterial();

            BuildFloor(width, depth, floor);
            BuildSide(SegmentEdge.North, north, width, depth, wallHeight, wallCenterY, thickness, openingWidth, wall);
            BuildSide(SegmentEdge.South, south, width, depth, wallHeight, wallCenterY, thickness, openingWidth, wall);
            BuildSide(SegmentEdge.East, east, width, depth, wallHeight, wallCenterY, thickness, openingWidth, wall);
            BuildSide(SegmentEdge.West, west, width, depth, wallHeight, wallCenterY, thickness, openingWidth, wall);
        }

        /// <summary>Strip generated meshes so a prefab asset stays component-only.</summary>
        public void ClearGeneratedHierarchy()
        {
            _geomRoot = null;
            Transform existing = transform.Find(GeomRootName);
            if (existing != null)
                DestroyObject(existing.gameObject);

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name.StartsWith(GenPrefix, StringComparison.Ordinal)
                    || child.GetComponent<BastionGeneratedGeom>() != null)
                    DestroyObject(child.gameObject);
            }
        }

        void EnsureGeomRoot()
        {
            if (_geomRoot != null) return;

            Transform existing = transform.Find(GeomRootName);
            if (existing != null)
            {
                _geomRoot = existing;
                return;
            }

            var go = new GameObject(GeomRootName);
            go.transform.SetParent(transform, false);
            _geomRoot = go.transform;
        }

        void ClearGeneratedChildren()
        {
            for (int i = _geomRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = _geomRoot.GetChild(i);
                if (!child.name.StartsWith(GenPrefix))
                    continue;

                DestroyObject(child.gameObject);
            }
        }

        void BuildFloor(float width, float depth, Material mat)
        {
            CreateBox(
                GenPrefix + "Floor",
                new Vector3(0f, -BastionKit.FloorSlabThickness * 0.5f, 0f),
                new Vector3(width, BastionKit.FloorSlabThickness, depth),
                mat,
                true);
        }

        void BuildSide(
            SegmentEdge orientation,
            BastionSideKind kind,
            float width,
            float depth,
            float wallHeight,
            float wallCenterY,
            float thickness,
            float openingWidth,
            Material mat)
        {
            if (kind == BastionSideKind.None)
                return;

            bool alongX = orientation == SegmentEdge.North || orientation == SegmentEdge.South;
            float span = alongX ? width : depth;
            float edgeOffset = alongX ? depth * 0.5f + thickness * 0.5f : width * 0.5f + thickness * 0.5f;

            switch (kind)
            {
                case BastionSideKind.Wall:
                    BuildSolidWall(orientation, span, wallHeight, wallCenterY, thickness, edgeOffset, mat);
                    break;
                case BastionSideKind.Door:
                    BuildDoorWall(orientation, span, wallHeight, thickness, edgeOffset, openingWidth, mat);
                    break;
                case BastionSideKind.Window:
                    BuildWindowWall(orientation, span, wallHeight, thickness, edgeOffset, openingWidth, mat);
                    break;
            }
        }

        void BuildSolidWall(
            SegmentEdge orientation,
            float span,
            float wallHeight,
            float wallCenterY,
            float thickness,
            float edgeOffset,
            Material mat)
        {
            Vector3 size = SideSize(orientation, span, wallHeight, thickness);
            Vector3 center = SideCenter(orientation, 0f, wallCenterY, edgeOffset);
            CreateBox(GenPrefix + orientation + "_Wall", center, size, mat, true);
        }

        void BuildDoorWall(
            SegmentEdge orientation,
            float span,
            float wallHeight,
            float thickness,
            float edgeOffset,
            float openingWidth,
            Material mat)
        {
            float pillarWidth = (span - openingWidth) * 0.5f;
            if (pillarWidth <= 0f)
            {
                BuildSolidWall(orientation, span, wallHeight, wallHeight * 0.5f, thickness, edgeOffset, mat);
                return;
            }

            float pillarCenterX = openingWidth * 0.5f + pillarWidth * 0.5f;
            float lintelHeight = wallHeight - DoorOpeningHeight;

            CreateBox(
                GenPrefix + orientation + "_Door_West",
                SideCenter(orientation, -pillarCenterX, wallHeight * 0.5f, edgeOffset),
                SideSize(orientation, pillarWidth, wallHeight, thickness),
                mat,
                true);

            CreateBox(
                GenPrefix + orientation + "_Door_East",
                SideCenter(orientation, pillarCenterX, wallHeight * 0.5f, edgeOffset),
                SideSize(orientation, pillarWidth, wallHeight, thickness),
                mat,
                true);

            if (lintelHeight > 0f)
            {
                float lintelCenterY = DoorOpeningHeight + lintelHeight * 0.5f;
                CreateBox(
                    GenPrefix + orientation + "_Door_Lintel",
                    SideCenter(orientation, 0f, lintelCenterY, edgeOffset),
                    SideSize(orientation, openingWidth, lintelHeight, thickness),
                    mat,
                    true);
            }
        }

        void BuildWindowWall(
            SegmentEdge orientation,
            float span,
            float wallHeight,
            float thickness,
            float edgeOffset,
            float openingWidth,
            Material mat)
        {
            float bandHeight = wallHeight - WindowSillHeight - WindowLintelHeight;
            if (bandHeight <= 0f)
            {
                BuildSolidWall(orientation, span, wallHeight, wallHeight * 0.5f, thickness, edgeOffset, mat);
                return;
            }

            float sillCenterY = WindowSillHeight * 0.5f;
            CreateBox(
                GenPrefix + orientation + "_Window_Sill",
                SideCenter(orientation, 0f, sillCenterY, edgeOffset),
                SideSize(orientation, span, WindowSillHeight, thickness),
                mat,
                true);

            float lintelCenterY = wallHeight - WindowLintelHeight * 0.5f;
            CreateBox(
                GenPrefix + orientation + "_Window_Lintel",
                SideCenter(orientation, 0f, lintelCenterY, edgeOffset),
                SideSize(orientation, span, WindowLintelHeight, thickness),
                mat,
                true);

            float pillarWidth = (span - openingWidth) * 0.5f;
            float bandCenterY = WindowSillHeight + bandHeight * 0.5f;

            if (pillarWidth > 0f)
            {
                float pillarCenterX = openingWidth * 0.5f + pillarWidth * 0.5f;
                CreateBox(
                    GenPrefix + orientation + "_Window_West",
                    SideCenter(orientation, -pillarCenterX, bandCenterY, edgeOffset),
                    SideSize(orientation, pillarWidth, bandHeight, thickness),
                    mat,
                    true);
                CreateBox(
                    GenPrefix + orientation + "_Window_East",
                    SideCenter(orientation, pillarCenterX, bandCenterY, edgeOffset),
                    SideSize(orientation, pillarWidth, bandHeight, thickness),
                    mat,
                    true);
            }

            Vector3 blockCenter = SideCenter(orientation, 0f, WindowWalkBlockHeight * 0.5f, edgeOffset);
            Vector3 blockSize = SideSize(orientation, openingWidth, WindowWalkBlockHeight, thickness);
            CreateWalkBlock(GenPrefix + orientation + "_Window_WalkBlock", blockCenter, blockSize);
        }

        static Vector3 SideSize(SegmentEdge orientation, float span, float height, float thickness)
        {
            if (orientation == SegmentEdge.North || orientation == SegmentEdge.South)
                return new Vector3(span, height, thickness);
            return new Vector3(thickness, height, span);
        }

        static Vector3 SideCenter(SegmentEdge orientation, float along, float y, float edgeOffset)
        {
            switch (orientation)
            {
                case SegmentEdge.North:
                    return new Vector3(along, y, edgeOffset);
                case SegmentEdge.South:
                    return new Vector3(along, y, -edgeOffset);
                case SegmentEdge.East:
                    return new Vector3(edgeOffset, y, along);
                default:
                    return new Vector3(-edgeOffset, y, along);
            }
        }

        void CreateBox(string name, Vector3 localCenter, Vector3 size, Material mat, bool collider)
        {
            if (_geomRoot == null)
                return;

            // Avoid CreatePrimitive — during OnValidate / prefab save it orphans into the open scene.
            var go = new GameObject(name);
            go.transform.SetParent(_geomRoot, false);
            go.transform.localPosition = localCenter;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = size;
            go.layer = 0;

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = CubeMesh();

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = mat;

            if (collider)
            {
                var meshCollider = go.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = filter.sharedMesh;
            }

            go.AddComponent<BastionGeneratedGeom>();
        }

        void CreateWalkBlock(string name, Vector3 localCenter, Vector3 size)
        {
            if (_geomRoot == null)
                return;

            var go = new GameObject(name);
            go.transform.SetParent(_geomRoot, false);
            go.transform.localPosition = localCenter;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = size;
            go.layer = 0;

            var obstacle = go.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.center = Vector3.zero;
            obstacle.size = Vector3.one;

            go.AddComponent<BastionGeneratedGeom>();
        }

        static Mesh _cubeMesh;

        static Mesh CubeMesh()
        {
            if (_cubeMesh != null)
                return _cubeMesh;

            _cubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            if (_cubeMesh != null)
                return _cubeMesh;

            // Fallback unit cube (no CreatePrimitive — that orphans in the open scene).
            _cubeMesh = new Mesh { name = "BastionKitCube" };
            _cubeMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f),
            };
            _cubeMesh.triangles = new[]
            {
                0, 2, 1, 0, 3, 2, 1, 6, 5, 1, 2, 6, 5, 7, 4, 5, 6, 7,
                4, 3, 0, 4, 7, 3, 3, 6, 2, 3, 7, 6, 4, 1, 5, 4, 0, 1,
            };
            _cubeMesh.RecalculateNormals();
            _cubeMesh.RecalculateBounds();
            return _cubeMesh;
        }

        Material ResolveFloorMaterial()
        {
            if (floorMat != null)
                return floorMat;
            if (_runtimeFloorMat == null)
                _runtimeFloorMat = CreateRuntimeMaterial(new Color(0.55f, 0.55f, 0.55f));
            return _runtimeFloorMat;
        }

        Material ResolveWallMaterial()
        {
            if (wallMat != null)
                return wallMat;
            if (_runtimeWallMat == null)
                _runtimeWallMat = CreateRuntimeMaterial(Color.magenta);
            return _runtimeWallMat;
        }

        static Material CreateRuntimeMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                return null;

            var mat = new Material(shader);
            mat.color = color;
            return mat;
        }

        static void DestroyObject(UnityEngine.Object obj)
        {
            if (obj == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(obj);
                return;
            }
#endif
            UnityEngine.Object.Destroy(obj);
        }
    }
}
