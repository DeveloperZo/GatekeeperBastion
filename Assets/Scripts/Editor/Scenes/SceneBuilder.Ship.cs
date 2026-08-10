using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GatekeeperBastion.EditorTools
{
    /// <summary>Ship half of the SceneBuilder: the player's HOME hull between runs.
    /// Same gray-box family as the bastion but reads as a different place — cleaner
    /// steel blues/greys, brighter light, orthogonal compact layout. A central common
    /// room with FOUR alcoves. The station consoles in those alcoves (ARMORY /
    /// FABRICATION / NAV MAP / CREW QUARTERS), the player rig, ShipController and the
    /// UI canvases are authored, persistent scene content (M35 criterion 6) — this
    /// class rebuilds only the hull geometry and the light, in place.</summary>
    public static partial class SceneBuilder
    {
        const float ShipWallHeight = 3f;

        sealed class ShipPalette
        {
            public Material Floor, Wall, ConsoleArmory, ConsoleFabrication, ConsoleMap, ConsoleCrew;
        }

        /// <summary>
        /// Ship layout (top-down, all right angles):
        ///   Common room: x -7..7, z -7..7 — player spawns here, ShipController lives here.
        ///   North alcove: x -3..3, z 7..13   — MapStation          (root at z  11.5, yaw 180).
        ///   West alcove:  x -13..-7, z -3..3 — ArmoryStation       (root at x -11.5, yaw  90).
        ///   East alcove:  x 7..13, z -3..3   — EngineeringStation  (root at x 11.5, yaw -90).
        ///   South alcove: x -3..3, z -13..-7 — CrewQuartersStation (root at z -11.5, yaw   0).
        /// The south alcove is the exact mirror of the north one across z = 0, including the
        /// doorway cut in the common room's south wall (x -3..3, the same full-width opening the
        /// north wall has). It is NOT a variation on the theme: reachability is a shipped-bug
        /// class here, and mirroring geometry that is already proven reachable is the cheapest
        /// way not to reopen it.
        /// The authored station roots are set back far enough that the player (capsule radius
        /// 0.35) can stand within ~0.8 m of each screen — well inside the Interactor's 3.2 m
        /// range. The geometry rebuilt here must keep those alcoves where the stations sit.
        /// </summary>
        static void RebuildShipGeometry()
        {
            Scene scene = OpenAndClearOwnedRoots(ShipScenePath);
            ShipPalette pal = CreateShipPalette();

            BuildShipLighting();
            BuildShipHull(pal);

            EditorSceneManager.SaveScene(scene, ShipScenePath);
            Debug.Log($"[SceneBuilder] Rebuilt {ShipScenePath} in place: Environment + Directional Light. Authored stations, rig, controllers and UI untouched.");
        }

        /// <summary>Home palette: clean steel blues/greys vs the bastion's dark violet/teal.
        /// Console colors code their function (weapon-orange / repair-green / nav-cyan /
        /// crew amber-gold). The console materials stay in the refresh so the authored
        /// station meshes keep their look when materials are regenerated.</summary>
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

            // Flat ambient, noticeably brighter and cooler than the bastion's murk.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.20f, 0.22f, 0.27f);
        }

        static void BuildShipHull(ShipPalette pal)
        {
            var env = new GameObject("Environment");
            Transform t = env.transform;
            float y = ShipWallHeight * 0.5f;
            const float th = WallThickness;

            // Floors (one per room so nothing pokes outside the hull footprint).
            Box("Floor_Common", new Vector3(0f, -0.25f, 0f), new Vector3(14.5f, 0.5f, 14.5f), pal.Floor, t);
            Box("Floor_North", new Vector3(0f, -0.25f, 10.25f), new Vector3(6.5f, 0.5f, 6.5f), pal.Floor, t);
            Box("Floor_South", new Vector3(0f, -0.25f, -10.25f), new Vector3(6.5f, 0.5f, 6.5f), pal.Floor, t);
            Box("Floor_West", new Vector3(-10.25f, -0.25f, 0f), new Vector3(6.5f, 0.5f, 6.5f), pal.Floor, t);
            Box("Floor_East", new Vector3(10.25f, -0.25f, 0f), new Vector3(6.5f, 0.5f, 6.5f), pal.Floor, t);

            // Common room perimeter; each alcove-facing wall leaves a full-width opening.
            // The south wall used to be one solid 15 m slab; it is now split around a doorway
            // identical to the north one (opening x -3..3) so the CREW QUARTERS alcove is
            // WALKABLE and not a sealed room with a console the player can see and never reach.
            Box("Wall_S_WestOfDoor", new Vector3(-5.25f, y, -7.25f), new Vector3(4.5f, ShipWallHeight, th), pal.Wall, t);
            Box("Wall_S_EastOfDoor", new Vector3(5.25f, y, -7.25f), new Vector3(4.5f, ShipWallHeight, th), pal.Wall, t);
            Box("Wall_N_WestOfDoor", new Vector3(-5.25f, y, 7.25f), new Vector3(4.5f, ShipWallHeight, th), pal.Wall, t);
            Box("Wall_N_EastOfDoor", new Vector3(5.25f, y, 7.25f), new Vector3(4.5f, ShipWallHeight, th), pal.Wall, t);
            Box("Wall_W_SouthOfDoor", new Vector3(-7.25f, y, -5.25f), new Vector3(th, ShipWallHeight, 4.5f), pal.Wall, t);
            Box("Wall_W_NorthOfDoor", new Vector3(-7.25f, y, 5.25f), new Vector3(th, ShipWallHeight, 4.5f), pal.Wall, t);
            Box("Wall_E_SouthOfDoor", new Vector3(7.25f, y, -5.25f), new Vector3(th, ShipWallHeight, 4.5f), pal.Wall, t);
            Box("Wall_E_NorthOfDoor", new Vector3(7.25f, y, 5.25f), new Vector3(th, ShipWallHeight, 4.5f), pal.Wall, t);

            // North alcove (MapStation).
            Box("Alcove_N_W", new Vector3(-3.25f, y, 10.25f), new Vector3(th, ShipWallHeight, 6.5f), pal.Wall, t);
            Box("Alcove_N_E", new Vector3(3.25f, y, 10.25f), new Vector3(th, ShipWallHeight, 6.5f), pal.Wall, t);
            Box("Alcove_N_Back", new Vector3(0f, y, 13.25f), new Vector3(7f, ShipWallHeight, th), pal.Wall, t);

            // South alcove (CrewQuartersStation) — mirror of the north alcove across z = 0.
            Box("Alcove_S_W", new Vector3(-3.25f, y, -10.25f), new Vector3(th, ShipWallHeight, 6.5f), pal.Wall, t);
            Box("Alcove_S_E", new Vector3(3.25f, y, -10.25f), new Vector3(th, ShipWallHeight, 6.5f), pal.Wall, t);
            Box("Alcove_S_Back", new Vector3(0f, y, -13.25f), new Vector3(7f, ShipWallHeight, th), pal.Wall, t);

            // West alcove (ArmoryStation).
            Box("Alcove_W_S", new Vector3(-10.25f, y, -3.25f), new Vector3(6.5f, ShipWallHeight, th), pal.Wall, t);
            Box("Alcove_W_N", new Vector3(-10.25f, y, 3.25f), new Vector3(6.5f, ShipWallHeight, th), pal.Wall, t);
            Box("Alcove_W_Back", new Vector3(-13.25f, y, 0f), new Vector3(th, ShipWallHeight, 7f), pal.Wall, t);

            // East alcove (EngineeringStation, labelled FABRICATION).
            Box("Alcove_E_S", new Vector3(10.25f, y, -3.25f), new Vector3(6.5f, ShipWallHeight, th), pal.Wall, t);
            Box("Alcove_E_N", new Vector3(10.25f, y, 3.25f), new Vector3(6.5f, ShipWallHeight, th), pal.Wall, t);
            Box("Alcove_E_Back", new Vector3(13.25f, y, 0f), new Vector3(th, ShipWallHeight, 7f), pal.Wall, t);
        }
    }
}
