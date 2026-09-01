using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>Ship hull on the Synty 5 m occupancy grid, SR-2 profile: a long spine
    /// with rooms off it, a wider midship, a bow cockpit and stern engineering, and a
    /// full-length starboard gallery carrying the three hatch doors
    /// (Docs/Demo - Coop/README.md). Rebuilds "Ship - CoOp.unity" — the expanded hull is
    /// authored there while Ship - Demo.unity stays the shipped solo hub untouched; adopting the
    /// new hull as the hub is a deliberate swap, not a side effect of Build All. The
    /// grid is a superset of the old hull, so authored content (Stations, ShipDress,
    /// Player, demo markers) keeps its place and bindings while the hull grows around
    /// it. Hull renderers are visible — the doors and the gallery are Shown, not
    /// implied. The Hatches root is refreshed idempotently and never wiped;
    /// Environment stays the only owned root here.</summary>
    public static partial class SceneBuilder
    {
        const float ShipWallHeight = 3f;
        const float G = 5f;
        const string HatchesRootName = "Hatches";
        const string Ship1ScenePath = "Assets/Scenes/Ship - CoOp.unity";

        sealed class ShipPalette
        {
            public Material Floor, Wall, HatchOpen, HatchSealed;
        }

        /// <summary>
        /// Bow +Z. Spine ix=0 running iz -6..9; bridge iz 8..9; engineering iz -5..-6;
        /// side rooms at ix ±1; wings at iz 0..-1; starboard gallery ix=3 (iz -4..5)
        /// joined to the hull at iz 5, 2 and -4, with the three hatch doors on its
        /// outer wall. The spawn row (iz 2) looks straight down its junction at the
        /// middle hatch.
        /// </summary>
        static readonly int[] OccupiedIx = {
            -1, 0, 1,          // iz  9  bridge
            -1, 0, 1,          // iz  8  bridge
            0,                 // iz  7  spine
            0,                 // iz  6  spine
            -1, 0, 1, 2,       // iz  5  fore rooms + gallery junction
            -1, 0, 1,          // iz  4
            -1, 0, 1,          // iz  3
            -1, 0, 1, 2,       // iz  2  crew row + gallery junction (spawn view line)
            -1, 0, 1,          // iz  1
            -3, -2, -1, 0, 1, 2, // iz 0  wings + gallery junction approach
            -2, -1, 0, 1,      // iz -1  wings aft
            -1, 0, 1,          // iz -2
            -1, 0, 1,          // iz -3
            -1, 0, 1, 2,       // iz -4  aft rooms + gallery junction
            -1, 0, 1,          // iz -5  engineering
            -1, 0, 1,          // iz -6  engineering
            3, 3, 3, 3, 3, 3, 3, 3, 3, 3 // gallery iz -4..5
        };
        static readonly int[] OccupiedIz = {
            9, 9, 9,
            8, 8, 8,
            7,
            6,
            5, 5, 5, 5,
            4, 4, 4,
            3, 3, 3,
            2, 2, 2, 2,
            1, 1, 1,
            0, 0, 0, 0, 0, 0,
            -1, -1, -1, -1,
            -2, -2, -2,
            -3, -3, -3,
            -4, -4, -4, -4,
            -5, -5, -5,
            -6, -6, -6,
            -4, -3, -2, -1, 0, 1, 2, 3, 4, 5
        };

        /// <summary>Hatch ids 1..3 bow to stern, each on the gallery's outer (east)
        /// wall at these iz cells. The middle hatch sits on the spawn row.</summary>
        static readonly int[] HatchIz = { 5, 2, -4 };

        const int GalleryIx = 3;

        /// <summary>RETIRED 2026-08-26 — kept readable, made unable to fire.
        ///
        /// <para>This builds the superseded graybox hull: one deck, a starboard gallery at
        /// ix=3, and the three hatch doors at y=0 on z 27.5 / 12.5 / −17.5. None of those
        /// objects are in the live scene — the hull is authored offline by
        /// Tools/ship-hull/build_hull.py, and the ship's shape lives in that script's
        /// layout tables.</para>
        ///
        /// <para>It was still wired into Build All Scenes, so one click wiped Environment and
        /// relocated every hatch. Docs/Working-Rules.md says "do not run it";
        /// a sentence in a document is not a guard, and after the 2026-08-26 collapse to one
        /// deck this file's single-deck shape would have started to look plausible again —
        /// the trap getting harder to see, not easier. Deleting the file outright is the
        /// owner's call; refusing is the part that could not wait.</para></summary>
        static void RebuildShipGeometry()
        {
            Debug.LogError("[SceneBuilder] RebuildShipGeometry is retired and refuses to run. "
                + "The Ship hull is authored by Tools/ship-hull/build_hull.py; this method "
                + "builds the superseded graybox and would wipe Environment and move the hatch "
                + "doors. Nothing calls it — if you got here, something re-wired it.");
            return;
#pragma warning disable 162 // unreachable: kept so the old hull's shape stays readable
            Scene scene = EditorSceneManager.OpenScene(Ship1ScenePath, OpenSceneMode.Single);
            // ShipDress used to skip this rebuild wholesale; it is a skeleton of empty
            // per-room containers, and the owned-roots wipe never touches it, so the
            // hull can regrow underneath it safely.
            ClearOwnedRoots(scene);
            ShipPalette pal = CreateShipPalette();
            BuildShipLighting();
            BuildShipHull(pal);
            RefreshShipHatches(pal);
            EditorSceneManager.SaveScene(scene, Ship1ScenePath);
            Debug.Log($"[SceneBuilder] Rebuilt {Ship1ScenePath} in place: Environment + Directional Light + {HatchesRootName}.");
#pragma warning restore 162
        }

        static ShipPalette CreateShipPalette()
        {
            return new ShipPalette
            {
                Floor = MakeMaterial("ShipFloor", new Color(0.17f, 0.19f, 0.23f)),
                Wall = MakeMaterial("ShipWall", new Color(0.31f, 0.35f, 0.42f)),
                // Open reuses the Bastion Hatch safety-orange so "your door home" is one
                // colour everywhere; sealed is the wall family, darkened to read shut.
                HatchOpen = MakeMaterial("Hatch", new Color(1f, 0.55f, 0.1f)),
                HatchSealed = MakeMaterial("HatchSealed", new Color(0.19f, 0.21f, 0.26f)),
            };
        }

        static void BuildShipLighting()
        {
            var go = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.8f;
            light.color = new Color(0.88f, 0.92f, 1f);
            go.transform.rotation = Quaternion.Euler(55f, -25f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.20f, 0.22f, 0.27f);
        }

        static void BuildShipHull(ShipPalette pal)
        {
            var env = new GameObject("Environment");
            Transform t = env.transform;
            float y = ShipWallHeight * 0.5f;
            const float th = BastionKit.WallThickness;
            // Occupancy lookup: ix -3..3 → 0..6, iz -6..9 → 0..15.
            var occ = new bool[7, 16];
            for (int i = 0; i < OccupiedIx.Length; i++)
                occ[OccupiedIx[i] + 3, OccupiedIz[i] + 6] = true;

            for (int i = 0; i < OccupiedIx.Length; i++)
            {
                int ix = OccupiedIx[i];
                int iz = OccupiedIz[i];
                Box("Floor_" + ix + "_" + iz,
                    new Vector3((ix + 0.5f) * G, -0.25f, (iz + 0.5f) * G),
                    new Vector3(G, 0.5f, G), pal.Floor, t);
            }

            for (int i = 0; i < OccupiedIx.Length; i++)
            {
                int ix = OccupiedIx[i];
                int iz = OccupiedIz[i];
                int ox = ix + 3;
                int oz = iz + 6;
                if (!At(occ, ox, oz + 1))
                    Box("W_N_" + ix + "_" + iz, new Vector3((ix + 0.5f) * G, y, (iz + 1) * G), new Vector3(G, ShipWallHeight, th), pal.Wall, t);
                if (!At(occ, ox, oz - 1))
                    Box("W_S_" + ix + "_" + iz, new Vector3((ix + 0.5f) * G, y, iz * G), new Vector3(G, ShipWallHeight, th), pal.Wall, t);
                if (!At(occ, ox + 1, oz))
                    Box("W_E_" + ix + "_" + iz, new Vector3((ix + 1) * G, y, (iz + 0.5f) * G), new Vector3(th, ShipWallHeight, G), pal.Wall, t);
                if (!At(occ, ox - 1, oz))
                    Box("W_W_" + ix + "_" + iz, new Vector3(ix * G, y, (iz + 0.5f) * G), new Vector3(th, ShipWallHeight, G), pal.Wall, t);
            }
        }

        /// <summary>Create or refresh the three hatch doors on the gallery's outer wall.
        /// Idempotent by child name under a non-owned root: authored tweaks to the root
        /// survive a rebuild, the door objects themselves are re-emitted.</summary>
        static void RefreshShipHatches(ShipPalette pal)
        {
            GameObject root = GameObject.Find(HatchesRootName);
            if (root == null)
                root = new GameObject(HatchesRootName);

            const float th = BastionKit.WallThickness;
            float wallInnerX = (GalleryIx + 1) * G - th * 0.5f;

            for (int i = 0; i < HatchIz.Length; i++)
            {
                int id = i + 1;
                string name = "Hatch_" + id;
                Transform old = root.transform.Find(name);
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);

                var door = new GameObject(name);
                door.transform.SetParent(root.transform, false);
                float z = (HatchIz[i] + 0.5f) * G;
                door.transform.position = new Vector3(wallInnerX, 0f, z);
                // Forward faces into the gallery (west), the side the player reads it from.
                door.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

                // Frame in wall colour so the slab reads as a doorway, not a poster.
                Box("Frame_Top", new Vector3(0f, 2.85f, 0f), new Vector3(0.45f, 0.3f, 4.0f), pal.Wall, door.transform);
                Box("Frame_L", new Vector3(0f, 1.5f, 1.85f), new Vector3(0.45f, 3f, 0.3f), pal.Wall, door.transform);
                Box("Frame_R", new Vector3(0f, 1.5f, -1.85f), new Vector3(0.45f, 3f, 0.3f), pal.Wall, door.transform);

                GameObject slab = Box("Slab", new Vector3(0.05f, 1.35f, 0f), new Vector3(0.25f, 2.7f, 3.4f), pal.HatchSealed, door.transform);

                var component = door.AddComponent<ShipHatchDoor>();
                var so = new SerializedObject(component);
                so.FindProperty("_hatchId").intValue = id;
                so.FindProperty("_leaf").objectReferenceValue = slab.GetComponent<MeshRenderer>();
                so.FindProperty("_openMaterial").objectReferenceValue = pal.HatchOpen;
                so.FindProperty("_sealedMaterial").objectReferenceValue = pal.HatchSealed;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static bool At(bool[,] occ, int ox, int oz)
        {
            if (ox < 0 || oz < 0 || ox >= occ.GetLength(0) || oz >= occ.GetLength(1)) return false;
            return occ[ox, oz];
        }
    }
}
