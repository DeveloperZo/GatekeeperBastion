using GatekeeperBastion.Game;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace GatekeeperBastion.EditorTools
{
    /// <summary>Ship half of the SceneBuilder: the player's HOME hull between runs.
    /// Same gray-box family as the bastion but reads as a different place — cleaner
    /// steel blues/greys, brighter light, orthogonal compact layout. A central common
    /// room with FOUR alcoves, one meta station per alcove:
    ///   West  = ArmoryStation       (weapon-orange console)
    ///   East  = EngineeringStation  (repair-green console, labelled FABRICATION)
    ///   North = MapStation          (nav-cyan console)
    ///   South = CrewQuartersStation (amber/gold console — player attributes)
    /// Player rig has no gun aboard the ship. No NavMesh, no enemies, no GameController —
    /// and therefore no HudController either (it is gated on GameController.Instance and
    /// would draw nothing here), so this scene gets ShipHud instead: crosshair, interaction
    /// prompt, controls legend, salvage header and objective line.</summary>
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
        /// Every station root is set back far enough that the player (capsule radius 0.35)
        /// can stand within ~0.8 m of its screen — well inside the Interactor's 3.2 m range.
        /// Player spawns at (0, 1.1, -4) with identity rotation, i.e. facing +Z / north,
        /// looking straight down the common room at the NAV MAP alcove; CREW QUARTERS is the
        /// console directly behind him.
        /// </summary>
        static void BuildShipScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ShipPalette pal = CreateShipPalette();

            BuildShipLighting();
            BuildShipHull(pal);
            BuildShipStations(pal);
            BuildShipPlayerRig();

            // ShipController owns cursor/walk state; ShipHud is the ONLY HUD in this scene
            // (HudController is Bastion-only and draws nothing without a GameController).
            // StationPanel (with nested ArmoryWheel) is the shared station host — stations
            // resolve it via StationPanelView.EnsureInstance(). DevConsole is editor/dev-build
            // only — F8 grant salvage reaches shop states here.
            var control = new GameObject("ShipController");
            control.AddComponent<ShipController>();
            var shipHud = InstantiateUiPrefab("ShipHud");
            shipHud.name = "ShipHud";
            shipHud.transform.SetParent(control.transform, false);
            if (shipHud.GetComponent<ShipHud>() == null)
                shipHud.AddComponent<ShipHud>();
            if (shipHud.GetComponentInChildren<ShipHudView>(true) == null)
                shipHud.AddComponent<ShipHudView>();

            var stationPanel = InstantiateUiPrefab("StationPanel");
            stationPanel.name = "StationPanel";
            stationPanel.transform.SetParent(control.transform, false);
            if (stationPanel.GetComponentInChildren<StationPanelView>(true) == null)
                stationPanel.AddComponent<StationPanelView>();
            // ArmoryWheel is nested under StationPanel by BastionUiAssembler — not a scene root.
            if (stationPanel.GetComponentInChildren<ArmoryWheelView>(true) == null)
                Debug.LogWarning(
                    "[SceneBuilder] StationPanel missing ArmoryWheelView — re-run Assemble Bastion UI.");
            else
                EnsureNestedNamed(stationPanel, "ArmoryWheel", "StationPanel ArmoryWheel");

            control.AddComponent<DevConsole>();

            EditorSceneManager.SaveScene(scene, ShipScenePath);
            Debug.Log(
                $"[SceneBuilder] Built {ShipScenePath}: common room + 4 alcoves, " +
                "ARMORY/FABRICATION/NAV MAP/CREW QUARTERS stations (eye-height screens, " +
                "front-facing labels), gunless player rig, ShipController + ShipHud + " +
                "StationPanel (ArmoryWheel nested) + DevConsole.");
        }

        /// <summary>Home palette: clean steel blues/greys vs the bastion's dark violet/teal.
        /// Console colors code their function (weapon-orange / repair-green / nav-cyan /
        /// crew amber-gold). The four hues are picked to stay distinguishable at a glance from
        /// the middle of the common room — amber is warm and pale where the armory's orange is
        /// saturated and red-shifted, so the two never read as the same console.</summary>
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

        static void BuildShipStations(ShipPalette pal)
        {
            var root = new GameObject("Stations");
            Transform t = root.transform;

            MakeStationConsole(t, "Station_Armory", new Vector3(-11.5f, 0f, 0f), 90f,
                pal.ConsoleArmory, "ARMORY").AddComponent<ArmoryStation>();
            MakeStationConsole(t, "Station_Fabrication", new Vector3(11.5f, 0f, 0f), -90f,
                pal.ConsoleFabrication, "FABRICATION").AddComponent<EngineeringStation>();
            MakeStationConsole(t, "Station_Map", new Vector3(0f, 0f, 11.5f), 180f,
                pal.ConsoleMap, "MAP").AddComponent<MapStation>();
            // Yaw 0: the south alcove's console faces +Z, i.e. back up the common room at the
            // player's spawn — the same "root's +Z points at the player" rule the other three use.
            MakeStationConsole(t, "Station_CrewQuarters", new Vector3(0f, 0f, -11.5f), 0f,
                pal.ConsoleCrew, "CREW QUARTERS").AddComponent<CrewQuartersStation>();
        }

        /// <summary>A station: an unscaled root (carrying the StationTerminal component) with a
        /// waist-high DESK and an upright SCREEN, both keeping their primitive BoxColliders, plus
        /// a floating TextMesh label (thin marker bar if the built-in font is unavailable).
        /// Yaw turns the root's +Z toward the common room, i.e. toward the player.
        ///
        /// Two things here are load-bearing and must not be "tidied" away:
        ///
        /// 1. THE SCREEN EXISTS SO THE RAYCAST CAN LAND. The player camera sits at y = 1.75
        ///    (spawn y 1.1 + camera local y 0.65). A desk alone tops out at 1.2, entirely below
        ///    the eyeline, so a level look-forward ray from Interactor sails over it and the
        ///    console is effectively un-interactable without aiming at the floor. The screen
        ///    spans y 1.25..2.15, straddling eye height. Colliders are NOT stripped here (unlike
        ///    the bastion's floor decals); the Interactor resolves the station by walking up
        ///    from whichever child collider it hits (GetComponentInParent).
        ///
        /// 2. THE LABEL IS FLIPPED 180°. A TextMesh reads correctly only from its local -Z side,
        ///    so its forward must point AWAY from the reader. The root already faces the player,
        ///    so an unrotated child label renders MIRRORED — that is the "text is flipped
        ///    backwards for fabrication/map/etc." bug. ShipHud re-billboards these at runtime,
        ///    which makes the bug impossible even if this rotation is ever wrong again.
        ///
        /// ALL FOUR stations go through this one function, unchanged, for exactly that reason:
        /// screen height and collider survival are both shipped-bug classes, and a station built
        /// "just slightly differently" is how they come back.</summary>
        static GameObject MakeStationConsole(Transform parent, string name, Vector3 pos, float yaw,
            Material mat, string label)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            var desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            desk.name = "Desk";
            desk.transform.SetParent(root.transform, false);
            desk.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            desk.transform.localScale = new Vector3(1.6f, 1.2f, 0.7f);
            desk.GetComponent<Renderer>().sharedMaterial = mat;

            var screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "Screen";
            screen.transform.SetParent(root.transform, false);
            screen.transform.localPosition = new Vector3(0f, 1.7f, -0.15f);
            screen.transform.localScale = new Vector3(1.6f, 0.9f, 0.12f);
            screen.GetComponent<Renderer>().sharedMaterial = mat;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(root.transform, false);
                labelGo.transform.localPosition = new Vector3(0f, 2.45f, 0f);
                labelGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // see (2) above
                var text = labelGo.AddComponent<TextMesh>();
                text.text = label;
                text.font = font;
                text.fontSize = 48;
                text.characterSize = 0.035f;
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                text.color = mat.color;
                labelGo.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }
            else
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(marker.GetComponent<Collider>());
                marker.name = "LabelMarker";
                marker.transform.SetParent(root.transform, false);
                marker.transform.localPosition = new Vector3(0f, 2.45f, 0f);
                marker.transform.localScale = new Vector3(0.9f, 0.12f, 0.15f);
                marker.GetComponent<Renderer>().sharedMaterial = mat;
            }
            return root;
        }

        /// <summary>Ship player rig: same as the bastion rig but with NO PlayerGun —
        /// the ship is a safe space. Spawns in the common room facing the map alcove.</summary>
        static void BuildShipPlayerRig()
        {
            Material playerMat = MakeMaterial("Player", new Color(0.85f, 0.85f, 0.9f));

            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            Object.DestroyImmediate(player.GetComponent<Collider>());
            player.transform.position = new Vector3(0f, 1.1f, -4f);
            player.GetComponent<Renderer>().sharedMaterial = playerMat;

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;

            var camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<Interactor>();

            var fpc = player.AddComponent<FirstPersonController>();
            fpc.cameraTransform = camGo.transform;
            player.AddComponent<PlayerHealth>();
        }
    }
}
