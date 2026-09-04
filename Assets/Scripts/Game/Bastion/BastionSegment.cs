using System;
using UnityEngine;
using UnityEngine.AI;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY: rebuilds collision boxes and pack-mesh visuals for floor, ceiling and
    /// four sides from block kind and per-edge side state. North = +Z, South = -Z, East = +X,
    /// West = -X.</summary>
    public sealed partial class BastionSegment : MonoBehaviour
    {
        internal enum SegmentEdge : byte
        {
            North = 0,
            South = 1,
            East = 2,
            West = 3,
        }

        const string GeomRootName = "_geom";
        public const string GenPrefix = "Gen_";
        public const string AuthoredParentName = "Authored";
        /// <summary>Clear width of a doorway. 3 -> 6 (Zo, 2026-08-27: "widen the doorways...
        /// that will give more consistent firing angles"). A 3 m gap in a one-lane corridor is
        /// narrower than the fight happening in it: the player could only ever engage what was
        /// squarely in the gap, so holding a line meant standing in the doorway rather than
        /// choosing where to stand.
        /// <para>WIDENED AGAIN 2026-09-04, 6 -> 10 (Zo: "we should also greatly widen the
        /// doorways ... so there's more funnel routes for enemies"). At 6 a passage carried
        /// about one group abreast, so a wave arriving in groups of up to seven queued at the
        /// opening and the fight was decided by whoever held the gap. At 10 the passage is
        /// wider than a group, which spreads the arrival laterally instead of serialising it.
        /// <para>This widens ONE route; it does not add one. The brainstorm's pre-registered
        /// kill for the one-doorway problem says "funnel spacing, not numbers", and spacing is
        /// a second entrance in the sketch rather than a bigger number here.</para>
        /// <para>The clamp had to move with it. <see cref="DoorOpeningMaxShare"/> was 0.4,
        /// which caps a Half room at 6.4 m — so raising this constant alone would have widened
        /// the OpenArea side of a shared passage and left the Half side at 6.4, and the two
        /// rooms build their own walls independently with no knowledge of each other. A
        /// passage whose two ends disagree is the failure this pair exists to prevent, and
        /// <c>BastionApertureAgreementTests</c> is the gate on it.</para></summary>
        public const float DoorOpeningWidth = 10f;

        /// <summary>Largest share of a wall's SHORT side an opening may take, so a doorway
        /// cannot eat the wall it is cut into and leave nothing to build pillars from.
        /// <para>0.65 since 2026-09-04, from 0.4. It is sized by the tightest block the Bastion
        /// authors: a Half room is 16 m, so the clamp allows 10.4 m and
        /// <see cref="DoorOpeningWidth"/> at 10 clears it with 0.4 m to spare and 3 m of wall
        /// each side. That margin is thin ON PURPOSE — it is what makes the agreement test
        /// fail loudly rather than a passage quietly growing two different mouths.</para>
        /// <para>A Quarter block, 10 m, still clamps to 6.5. Nothing in the Bastion authors one;
        /// that is the clamp doing its job rather than the width being ignored.</para></summary>
        public const float DoorOpeningMaxShare = 0.65f;

        /// <summary>Clear height of a doorway: the FULL wall, floor slab to ceiling, so no
        /// lintel is emitted (BuildDoorWall skips it at lintelHeight 0). Was a hardcoded 3.5
        /// against a 4.5 m wall, which hung a metre of geometry across every opening — invisible
        /// to a floor-plan check and directly in the path of a shot. BastionDoorApertureTests
        /// has asserted this equality since before the constant disagreed with it; the test was
        /// right and had been failing for it.</summary>
        public const float DoorOpeningHeight = BastionKit.StoreyHeight - BastionKit.FloorSlabThickness;
        const float WindowSillHeight = 1.1f;
        const float WindowLintelHeight = 0.5f;
        const float WindowWalkBlockHeight = 0.5f;

        [SerializeField] BastionBlockKind blockKind = BastionBlockKind.Quarter;
        [SerializeField] BastionSideKind north = BastionSideKind.Wall;
        [SerializeField] BastionSideKind east = BastionSideKind.Wall;
        [SerializeField] BastionSideKind south = BastionSideKind.Wall;
        [SerializeField] BastionSideKind west = BastionSideKind.Wall;
        [Tooltip("Metres along the edge from centre for Door/Window opening. N/S: +X; E/W: +Z.")]
        [SerializeField] float northDoorAlong;
        [SerializeField] float eastDoorAlong;
        [SerializeField] float southDoorAlong;
        [SerializeField] float westDoorAlong;
        [SerializeField] Material floorMat;
        [SerializeField] Material wallMat;
        [Tooltip("Pack prefab assets, assigned by BastionKitEmitter at kit build time. Serialized so runtime rebuilds need no AssetDatabase.")]
        [SerializeField] GameObject floorPiece;
        [SerializeField] GameObject ceilingPiece;
        [SerializeField] GameObject wallPiece;
        [SerializeField] GameObject doorframePiece;
        [SerializeField] GameObject windowPiece;
        [SerializeField] GameObject cornerPiece;
        [SerializeField] GameObject ceilingLightPiece;

        // Hash of the geometry inputs the last Rebuild used. OnValidate fires on every
        // script reload (compiles, play-mode entry), not just inspector edits — without
        // this stamp each reload rebuilt _geom and destroyed hand-authored content
        // (it wiped Zo's unpacked pieces on 2026-08-14). Zero means pre-stamp instance.
        [SerializeField, HideInInspector] int rebuildStamp;

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

        public float NorthDoorAlong
        {
            get => northDoorAlong;
            set => northDoorAlong = value;
        }

        public float EastDoorAlong
        {
            get => eastDoorAlong;
            set => eastDoorAlong = value;
        }

        public float SouthDoorAlong
        {
            get => southDoorAlong;
            set => southDoorAlong = value;
        }

        public float WestDoorAlong
        {
            get => westDoorAlong;
            set => westDoorAlong = value;
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

        /// <summary>Pack prefab asset for floor tiles. Set by the kit emitter, serialized on the
        /// kit prefab.</summary>
        public GameObject FloorPiece
        {
            get => floorPiece;
            set => floorPiece = value;
        }

        /// <summary>Pack prefab asset for ceiling tiles. Set by the kit emitter, serialized on the
        /// kit prefab.</summary>
        public GameObject CeilingPiece
        {
            get => ceilingPiece;
            set => ceilingPiece = value;
        }

        /// <summary>Pack prefab asset for wall runs. Set by the kit emitter, serialized on the
        /// kit prefab.</summary>
        public GameObject WallPiece
        {
            get => wallPiece;
            set => wallPiece = value;
        }

        /// <summary>Pack prefab asset for the Door side frame. Set by the kit emitter, serialized
        /// on the kit prefab.</summary>
        public GameObject DoorframePiece
        {
            get => doorframePiece;
            set => doorframePiece = value;
        }

        /// <summary>Pack prefab asset for the Window side opening. Set by the kit emitter,
        /// serialized on the kit prefab.</summary>
        public GameObject WindowPiece
        {
            get => windowPiece;
            set => windowPiece = value;
        }

        /// <summary>Pack prefab asset for corner pillars. Set by the kit emitter, serialized on
        /// the kit prefab.</summary>
        public GameObject CornerPiece
        {
            get => cornerPiece;
            set => cornerPiece = value;
        }

        /// <summary>Pack prefab asset for ceiling light fixtures. Set by the kit emitter,
        /// serialized on the kit prefab.</summary>
        public GameObject CeilingLightPiece
        {
            get => ceilingLightPiece;
            set => ceilingLightPiece = value;
        }

        /// <summary>Objects created by <see cref="Rebuild"/> since the last
        /// <see cref="ResetRebuildCounters"/>. Counts collider boxes, walk blocks and visual piece
        /// roots — everything that carries a <see cref="BastionGeneratedGeom"/> marker. The sketch
        /// emitter resets before a batch and logs the totals after (created must equal marked).</summary>
        public static int RebuildObjectsCreated;

        /// <summary>BastionGeneratedGeom markers added by <see cref="Rebuild"/> since the last
        /// <see cref="ResetRebuildCounters"/>.</summary>
        public static int RebuildObjectsMarked;

        /// <summary>Zeroes <see cref="RebuildObjectsCreated"/> / <see cref="RebuildObjectsMarked"/>
        /// and clears the per-piece-metrics log-once set, so the next emit batch logs each piece
        /// type's step and measured bounds again.</summary>
        public static void ResetRebuildCounters()
        {
            RebuildObjectsCreated = 0;
            RebuildObjectsMarked = 0;
            BastionPackTiling.ResetMetricsLog();
        }

        Transform _geomRoot;

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

            // Scene open / NavMesh bake must not wipe Gen_* or force a rebuild over craft.
            // Inspector side changes still rebuild via OnValidate.
            EnsureGeomRoot();
            if (_geomRoot != null && _geomRoot.childCount > 0)
                return;
            if (IsAuthored())
                return;

            Rebuild();
        }

        void OnValidate()
        {
            if (SuppressValidateRebuild)
                return;
            if (!CanMutateHierarchy())
                return;
            if (IsAuthored())
            {
                if (rebuildStamp == 0)
                    rebuildStamp = SettingsHash();
                return;
            }

            // Rebuild only when the geometry inputs changed. Reloads re-fire OnValidate
            // with identical settings — those must never touch _geom.
            int hash = SettingsHash();
            if (rebuildStamp == hash)
                return;
            if (rebuildStamp == 0)
            {
                Transform geom = transform.Find(GeomRootName);
                if (geom != null && geom.childCount > 0)
                {
                    // Pre-stamp scene instance with built geometry: adopt, don't rebuild.
                    rebuildStamp = hash;
                    return;
                }
            }

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
            if (IsAuthored())
                return;
            Rebuild();
        }
#endif

        /// <summary>True when this instance sits under <c>Authored/</c> — unique scene
        /// geometry. Rebuild from kit prefabs is refused so a stamp mismatch or orphan
        /// sweep cannot replace hand edits.</summary>
        public bool IsAuthored()
        {
            Transform t = transform;
            while (t != null)
            {
                if (t.name == AuthoredParentName)
                    return true;
                t = t.parent;
            }
            return false;
        }

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

        /// <summary>Local pose and blocker size for a Door aperture on <paramref name="edge"/>.
        /// False when that side is not Door.</summary>
        public bool TryGetDoorAperture(
            BastionDoorEdge edge,
            out Vector3 localPos,
            out float yawY,
            out Vector3 blockerSize)
        {
            localPos = Vector3.zero;
            yawY = 0f;
            blockerSize = Vector3.zero;

            BastionSideKind side;
            float along;
            switch (edge)
            {
                case BastionDoorEdge.North:
                    side = north;
                    along = northDoorAlong;
                    break;
                case BastionDoorEdge.South:
                    side = south;
                    along = southDoorAlong;
                    break;
                case BastionDoorEdge.East:
                    side = east;
                    along = eastDoorAlong;
                    break;
                case BastionDoorEdge.West:
                    side = west;
                    along = westDoorAlong;
                    break;
                default:
                    return false;
            }

            if (side != BastionSideKind.Door)
                return false;

            float width = BastionKit.FootprintWidth(blockKind);
            float depth = BastionKit.FootprintDepth(blockKind);
            float thickness = BastionKit.WallThickness;
            // ONE copy of the rule, in the Sim: the two ends of a shared passage must agree.
            float openingWidth = BastionKit.ApertureWidth(
                blockKind, DoorOpeningWidth, DoorOpeningMaxShare);
            float halfW = width * 0.5f;
            float halfD = depth * 0.5f;

            switch (edge)
            {
                case BastionDoorEdge.North:
                    localPos = new Vector3(along, 0f, halfD);
                    yawY = 0f;
                    blockerSize = new Vector3(openingWidth, DoorOpeningHeight, thickness * 2f);
                    break;
                case BastionDoorEdge.South:
                    localPos = new Vector3(along, 0f, -halfD);
                    yawY = 180f;
                    blockerSize = new Vector3(openingWidth, DoorOpeningHeight, thickness * 2f);
                    break;
                case BastionDoorEdge.East:
                    localPos = new Vector3(halfW, 0f, along);
                    yawY = 90f;
                    blockerSize = new Vector3(openingWidth, DoorOpeningHeight, thickness * 2f);
                    break;
                case BastionDoorEdge.West:
                    localPos = new Vector3(-halfW, 0f, along);
                    yawY = -90f;
                    blockerSize = new Vector3(openingWidth, DoorOpeningHeight, thickness * 2f);
                    break;
            }

            return true;
        }

        /// <summary>Clear prior generated geometry and emit both layers: collision boxes (floor
        /// plus four sides, the exact box math the primitive kit shipped) and pack-mesh visuals
        /// (floor, ceiling, walls, corners, ceiling lights).</summary>
        public void Rebuild()
        {
            if (this == null)
                return;
            if (!CanMutateHierarchy())
                return;
            if (IsAuthored())
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
            // ONE copy of the rule, in the Sim: the two ends of a shared passage must agree.
            float openingWidth = BastionKit.ApertureWidth(
                blockKind, DoorOpeningWidth, DoorOpeningMaxShare);

            BuildFloor(width, depth);
            BuildSide(SegmentEdge.North, north, northDoorAlong, width, depth, wallHeight, wallCenterY, thickness, openingWidth);
            BuildSide(SegmentEdge.South, south, southDoorAlong, width, depth, wallHeight, wallCenterY, thickness, openingWidth);
            BuildSide(SegmentEdge.East, east, eastDoorAlong, width, depth, wallHeight, wallCenterY, thickness, openingWidth);
            BuildSide(SegmentEdge.West, west, westDoorAlong, width, depth, wallHeight, wallCenterY, thickness, openingWidth);

            BastionSegmentVisuals.Build(this, _geomRoot, width, depth, wallHeight, thickness);
            rebuildStamp = SettingsHash();
        }

        /// <summary>Hash of everything <see cref="Rebuild"/> reads to shape geometry.
        /// Piece and material refs are excluded — the kit emitter swaps those and calls
        /// Rebuild explicitly. Never returns 0 (0 is the pre-stamp sentinel).</summary>
        int SettingsHash()
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + (int)blockKind;
                h = h * 31 + (int)north;
                h = h * 31 + (int)east;
                h = h * 31 + (int)south;
                h = h * 31 + (int)west;
                h = h * 31 + northDoorAlong.GetHashCode();
                h = h * 31 + eastDoorAlong.GetHashCode();
                h = h * 31 + southDoorAlong.GetHashCode();
                h = h * 31 + westDoorAlong.GetHashCode();
                return h == 0 ? 1 : h;
            }
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

        /// <summary>The _geom container, or null before the first rebuild. Editor tooling
        /// entry point — the name is private.</summary>
        public Transform FindGeomRoot()
        {
            return transform.Find(GeomRootName);
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
            // Group nodes (Walls/Floors/Ceiling) carry the marker, not the Gen_ prefix —
            // a rebuild clears them wholesale and re-emits grouped.
            for (int i = _geomRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = _geomRoot.GetChild(i);
                if (!child.name.StartsWith(GenPrefix, StringComparison.Ordinal)
                    && child.GetComponent<BastionGeneratedGeom>() == null)
                    continue;

                // Play-mode Destroy defers to end of frame — detach first, or
                // EnsureGeomGroup finds the doomed group and parents the fresh
                // geometry into it, and the whole room dies with the group.
                child.SetParent(null);
                DestroyObject(child.gameObject);
            }
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
