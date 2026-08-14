using GatekeeperBastion.Sim;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GatekeeperBastion.EditorTools
{
    /// <summary>Ship hull on the Synty 5 m occupancy grid. Visual kit is generated
    /// into ShipDress; this rebuilds Environment colliders + light only.</summary>
    public static partial class SceneBuilder
    {
        const float ShipWallHeight = 3f;
        const float G = 5f;

        sealed class ShipPalette
        {
            public Material Floor, Wall, ConsoleArmory, ConsoleFabrication, ConsoleMap, ConsoleCrew;
        }

        /// <summary>
        /// Bow +Z. Corridor ix=0 (x 0..5). Bridge iz 4..5, engineering iz=-3.
        /// Map / Armory / Crew / Fabrication sit in Bridge / starboard util / port cabin / aft.
        /// </summary>
        static void RebuildShipGeometry()
        {
            Scene scene = OpenAndClearOwnedRoots(ShipScenePath);
            ShipPalette pal = CreateShipPalette();
            BuildShipLighting();
            BuildShipHull(pal);
            EditorSceneManager.SaveScene(scene, ShipScenePath);
            Debug.Log($"[SceneBuilder] Rebuilt {ShipScenePath} in place: Environment + Directional Light.");
        }

        static ShipPalette CreateShipPalette()
        {
            return new ShipPalette
            {
                Floor = MakeMaterial("ShipFloor", new Color(0.17f, 0.19f, 0.23f)),
                Wall = MakeMaterial("ShipWall", new Color(0.31f, 0.35f, 0.42f)),
                ConsoleArmory = MakeMaterial("ConsoleArmory", new Color(0.95f, 0.50f, 0.10f)),
                ConsoleFabrication = MakeMaterial("ConsoleFabrication", new Color(0.22f, 0.75f, 0.32f)),
                ConsoleMap = MakeMaterial("ConsoleMap", new Color(0.12f, 0.72f, 0.85f)),
                ConsoleCrew = MakeMaterial("ConsoleCrew", new Color(0.96f, 0.78f, 0.26f)),
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

        static readonly int[] OccupiedIx = {
            -1,0,1, -1,0,1,
            0, -1, 1,
            0,
            0, -1, 1,
            0, -1, 1, -2, 2,
            0,
            0, -1, 1,
            -1,0,1
        };
        static readonly int[] OccupiedIz = {
            5,5,5, 4,4,4,
            3, 3, 3,
            2,
            1, 1, 1,
            0, 0, 0, 0, 0,
            -1,
            -2, -2, -2,
            -3,-3,-3
        };

        static void BuildShipHull(ShipPalette pal)
        {
            var env = new GameObject("Environment");
            Transform t = env.transform;
            float y = ShipWallHeight * 0.5f;
            const float th = BastionKit.WallThickness;
            var occ = new bool[8, 12];
            for (int i = 0; i < OccupiedIx.Length; i++)
                occ[OccupiedIx[i] + 3, OccupiedIz[i] + 4] = true;

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
                int oz = iz + 4;
                if (!At(occ, ox, oz + 1))
                    Box("W_N_" + ix + "_" + iz, new Vector3((ix + 0.5f) * G, y, (iz + 1) * G), new Vector3(G, ShipWallHeight, th), pal.Wall, t);
                if (!At(occ, ox, oz - 1))
                    Box("W_S_" + ix + "_" + iz, new Vector3((ix + 0.5f) * G, y, iz * G), new Vector3(G, ShipWallHeight, th), pal.Wall, t);
                if (!At(occ, ox + 1, oz))
                    Box("W_E_" + ix + "_" + iz, new Vector3((ix + 1) * G, y, (iz + 0.5f) * G), new Vector3(th, ShipWallHeight, G), pal.Wall, t);
                if (!At(occ, ox - 1, oz))
                    Box("W_W_" + ix + "_" + iz, new Vector3(ix * G, y, (iz + 0.5f) * G), new Vector3(th, ShipWallHeight, G), pal.Wall, t);
            }

            foreach (MeshRenderer r in env.GetComponentsInChildren<MeshRenderer>(true))
                r.enabled = false;
        }

        static bool At(bool[,] occ, int ox, int oz)
        {
            if (ox < 0 || oz < 0 || ox >= occ.GetLength(0) || oz >= occ.GetLength(1)) return false;
            return occ[ox, oz];
        }
    }
}
