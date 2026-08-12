using System;
using UnityEngine;
using UnityEngine.AI;
using GatekeeperBastion.Sim;

namespace GatekeeperBastion.Game
{
    /// <summary>Rebuilds floor and four sides from block kind and per-edge side state.
    /// North = +Z, South = -Z, East = +X, West = -X.</summary>
    public sealed partial class BastionSegment : MonoBehaviour
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
        /// prefab stage). False for the Prefab Asset on disk â€” SetParent there orphans Gen_* into
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

            Vector2 footprint = new Vector2(
                BastionKit.FootprintWidth(blockKind),
                BastionKit.FootprintDepth(blockKind));
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

        static void DestroyObject(GameObject go)
        {
            if (go == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(go);
                return;
            }
#endif
            Destroy(go);
        }
    }
}
