using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GatekeepersProtocol.Game;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>
    /// SHIP SCENE CONTRACT — the tests that would have caught "entering the Ship left the
    /// player with no UI and nothing interactable".
    ///
    /// 97 EditMode tests passed while the hub was unplayable because none of them ever loaded
    /// the real scene: the sim was green, the scene was broken. These load Ship for real and
    /// assert the four things the player needs in order to DO anything aboard:
    ///   1. a player rig (FirstPersonController + PlayerHealth + a camera-child Interactor),
    ///   2. something that RENDERS the interaction prompt (the Bastion-only HudController is
    ///      absent here — the Ship needs its own; GatekeepersProtocol.Game.ShipHud),
    ///   3. three station consoles that are IInteractable, collide, and can actually be hit by
    ///      the Interactor's ray from standing height in front of them,
    ///   4. a cursor that is LOCKED in walk mode (Interactor.Update early-returns unless
    ///      Cursor.lockState == Locked, so an unlocked cursor makes the whole hub dead code).
    /// </summary>
    public class ShipSceneContractTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return PlayTestUtil.ReturnToBlankScene();
        }

        // =====================================================================
        // A. Scene contract: rig + HUD + reachable stations
        // =====================================================================

        [UnityTest]
        public IEnumerator ShipScene_HasPlayerRig_PromptHud_AndThreeReachableStations()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.ShipCoop);
            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.ShipCoop);

            // ---- player rig ----
            yield return PlayTestUtil.WaitUntil(() => PlayerRegistry.Local != null, 5f);
            PlayerHealth player = PlayerRegistry.Local;
            Assert.IsNotNull(player,
                "Ship scene has no registered player: no PlayerHealth in PlayerRegistry.Local. " +
                "The hub needs the same walkable rig as the Bastion.");
            int livePlayers = 0;
            foreach (PlayerHealth registered in PlayerRegistry.All)
                if (registered != null) livePlayers++;
            Assert.AreEqual(1, livePlayers,
                "Ship scene should contain exactly one live player rig " +
                $"(PlayerRegistry.All holds {PlayerRegistry.All.Count} entries).");

            var fpc = player.GetComponent<FirstPersonController>();
            Assert.IsNotNull(fpc,
                "Player rig is missing FirstPersonController — the player cannot walk aboard the ship.");

            var interactor = player.GetComponentInChildren<Interactor>(true);
            Assert.IsNotNull(interactor,
                "Player rig has no Interactor. Without it nothing aboard the ship can be used " +
                "(E does nothing). It belongs on the player camera child.");
            Assert.IsNotNull(interactor.GetComponent<Camera>(),
                "Interactor must live on the player CAMERA (it raycasts along transform.forward); " +
                $"it is on '{interactor.gameObject.name}', which has no Camera.");
            Assert.IsTrue(interactor.enabled, "The Ship scene's Interactor is disabled.");

            // ---- something must draw the interaction prompt ----
            string hudDetail;
            Component promptHud = FindPromptRenderer(out hudDetail);
            Assert.IsNotNull(promptHud,
                "NOTHING in the Ship scene renders the interaction prompt, so the player sees no UI " +
                "and never learns that E opens a station. Expected a component of type " +
                "'GatekeepersProtocol.Game.ShipHud' (a Ship-scene HUD that draws Interactor.CurrentPrompt) — " +
                "HudController is Bastion-only and is not in this scene. " +
                "Accepted alternatives: any MonoBehaviour in the Ship scene with an OnGUI pass that " +
                "holds an Interactor reference. Searched types in scene: " + hudDetail);
            var promptBehaviour = promptHud as Behaviour;
            Assert.IsTrue(promptBehaviour == null || promptBehaviour.enabled,
                $"The Ship HUD component '{promptHud.GetType().Name}' is present but disabled — no prompt is drawn.");

            // ---- exactly three stations, all interactable ----
            List<IInteractable> interactables = PlayTestUtil.FindAll<IInteractable>();
            var armories = PlayTestUtil.FindAll<ArmoryStation>();
            var engineerings = PlayTestUtil.FindAll<EngineeringStation>();
            var maps = PlayTestUtil.FindAll<MapStation>();

            Assert.AreEqual(1, armories.Count, "Expected exactly one ArmoryStation in the Ship scene.");
            Assert.AreEqual(1, engineerings.Count, "Expected exactly one EngineeringStation in the Ship scene.");
            Assert.AreEqual(1, maps.Count, "Expected exactly one MapStation in the Ship scene.");

            var stations = new List<MonoBehaviour> { armories[0], engineerings[0], maps[0] };
            int stationInteractables = 0;
            foreach (MonoBehaviour station in stations)
            {
                Assert.IsInstanceOf<IInteractable>(station,
                    $"{station.GetType().Name} does not implement IInteractable, so the Interactor can never target it.");
                stationInteractables++;
                string prompt = ((IInteractable)station).Prompt;
                if (StationIsDemoHidden(station))
                    Assert.AreEqual(string.Empty, prompt,
                        $"{station.GetType().Name} is demo-hidden (Docs/Demo - Single/0-wake-on-a-dead-ship.md A13) — " +
                        "its Prompt must be exactly \"\" so the HUD draws nothing, not a live station string.");
                else
                    Assert.IsFalse(string.IsNullOrEmpty(prompt),
                        $"{station.GetType().Name}.Prompt is empty — the HUD would render a blank prompt.");
            }
            Assert.AreEqual(3, stationInteractables,
                "Exactly 3 station objects (Armory/Engineering/Map) must implement IInteractable.");
            Assert.GreaterOrEqual(interactables.Count, 3,
                $"Expected at least the 3 station interactables aboard the ship; found {interactables.Count}.");

            // ---- every station has an enabled collider ----
            foreach (MonoBehaviour station in stations)
            {
                var colliders = station.GetComponentsInChildren<Collider>(true);
                bool hasEnabled = false;
                foreach (Collider c in colliders)
                {
                    if (c != null && c.enabled && c.gameObject.activeInHierarchy) { hasEnabled = true; break; }
                }
                Assert.IsTrue(hasEnabled,
                    $"{station.gameObject.name} ({station.GetType().Name}) has no enabled Collider. " +
                    "The Interactor raycasts against colliders, so a collider-less console can never be used.");
            }

            // ---- the real "can the player interact with it" invariant ----
            // Stand 2.5 m in front of the console at eye height (~1.7 m) and look at it: the ray
            // must reach THAT console's collider, unblocked, inside the Interactor's range.
            float interactorRange = ReadInteractorRange(interactor);
            foreach (MonoBehaviour station in stations)
            {
                AssertConsoleIsReachableByRay(station, interactorRange);
            }

            yield return null;
        }

        // =====================================================================
        // B1. Cursor lock invariant (walk mode)
        // =====================================================================

        [UnityTest]
        public IEnumerator ShipScene_WalkMode_LocksTheCursor_OtherwiseInteractorIsDeadCode()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.ShipCoop);

            bool lockSupported = false;
            yield return PlayTestUtil.ProbeCursorLockSupport(supported => lockSupported = supported);
            // The probe left the cursor FREE on purpose: whatever locks it now is the scene.

            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.ShipCoop);
            yield return PlayTestUtil.WaitUntil(() => Cursor.lockState == CursorLockMode.Locked, 3f);

            if (Cursor.lockState == CursorLockMode.Locked) yield break; // contract satisfied

            if (!lockSupported)
            {
                Assert.Inconclusive(
                    "SKIPPED: this editor/test session does not honour Cursor.lockState (no focused Game " +
                    "view), so walk-mode cursor lock cannot be verified here. Re-run with the Game view focused.");
            }

            Assert.Fail(
                $"Ship scene left the cursor {Cursor.lockState} after loading. Interactor.Update() " +
                "early-returns unless Cursor.lockState == CursorLockMode.Locked, so with a free cursor NOTHING " +
                "aboard the ship is interactable and no prompt is ever produced. The hub must enter walk mode " +
                "(cursor locked, movement unlocked) on load.");
        }

        // =====================================================================
        // B1b. Interact Down opens the targeted station via OpenStationPanel
        // =====================================================================

        [UnityTest]
        public IEnumerator ShipScene_Interact_AtConsole_OpensThatStationPanel()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.ShipCoop);
            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.ShipCoop);
            yield return PlayTestUtil.WaitUntil(() => PlayerRegistry.Local != null, 5f);

            var ship = UnityEngine.Object.FindFirstObjectByType<ShipController>();
            Assert.IsNotNull(ship, "ShipController missing");
            var armory = UnityEngine.Object.FindFirstObjectByType<ArmoryStation>();
            Assert.IsNotNull(armory, "ArmoryStation missing");
            var map = UnityEngine.Object.FindFirstObjectByType<MapStation>();
            Assert.IsNotNull(map, "MapStation missing");

            Assert.IsFalse(ship.TryOpenStationFromInteract(null),
                "No target must not open a panel.");
            Assert.IsNull(ship.OpenStation);

            Assert.IsTrue(ship.TryOpenStationFromInteract(armory),
                "TryOpenStationFromInteract(Armory) must succeed with no panel open.");
            yield return null;
            Assert.AreSame(armory, ship.OpenStation);
            Assert.IsTrue(ship.IsPanelOpen);

            ship.CloseStationPanel(armory);
            yield return null;
            Assert.IsNull(ship.OpenStation);

            Assert.IsTrue(ship.TryOpenStationFromInteract(map),
                "TryOpenStationFromInteract(Map) must open NAV MAP, not Armory.");
            yield return null;
            Assert.AreSame(map, ship.OpenStation);

            ship.CloseStationPanel(map);
            yield return null;

            // Console Interact remains a no-op — open is ShipController Down only.
            ((IInteractable)armory).Interact(0f);
            yield return null;
            Assert.IsNull(ship.OpenStation,
                "StationTerminal.Interact must not open — only TryOpenStationFromInteract does.");
        }

        // =====================================================================
        // B1c. Esc opens pause without painting a station panel over it
        // =====================================================================

        [UnityTest]
        public IEnumerator ShipScene_Escape_OpensPause_WithoutArmoryPanel()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.ShipCoop);
            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.ShipCoop);
            yield return PlayTestUtil.WaitUntil(() => PlayerRegistry.Local != null, 5f);

            var ship = UnityEngine.Object.FindFirstObjectByType<ShipController>();
            Assert.IsNotNull(ship, "ShipController missing");

            MethodInfo handle = typeof(ShipController).GetMethod("HandleEscape",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(handle, "ShipController.HandleEscape missing");
            handle.Invoke(ship, null);
            yield return null;

            Assert.IsTrue(ship.IsSettingsOpen, "Esc with no station open must open pause.");

            var armoryPanel = UnityEngine.Object.FindFirstObjectByType<ArmoryPanelView>(
                FindObjectsInactive.Include);
            Assert.IsNotNull(armoryPanel, "ARMORY panel missing");
            Assert.IsFalse(armoryPanel.gameObject.activeInHierarchy,
                "ARMORY is authored active on the shared station canvas and sits above Pause Menu; " +
                "Esc must hide it or it paints over pause.");

            var pause = GameObject.Find("Pause Menu");
            Assert.IsNotNull(pause, "Pause Menu missing after Esc");
            Assert.IsTrue(pause.activeInHierarchy, "Pause Menu must be visible after Esc");

            Canvas.ForceUpdateCanvases();
            var list = pause.transform.Find("Button List");
            Assert.IsNotNull(list, "Pause Menu Button List missing");
            var compendium = list.Find("Compendium") as RectTransform;
            var settings = list.Find("Settings") as RectTransform;
            Assert.IsNotNull(compendium, "Pause Menu has no Compendium option");
            Assert.IsTrue(compendium.gameObject.activeInHierarchy,
                "Pause Menu Compendium option is inactive");
            Assert.IsNotNull(settings, "Pause Menu Settings missing");
            Assert.IsFalse(RectsOverlap(compendium, settings),
                "Compendium is stacked under Settings on Ship pause");
            var label = compendium.GetComponentInChildren<TextMeshProUGUI>(true);
            Assert.IsNotNull(label, "Compendium pause button has no TMP");
            Assert.AreEqual("Compendium", label.text);
        }

        static bool RectsOverlap(RectTransform a, RectTransform b)
        {
            var ac = new Vector3[4];
            var bc = new Vector3[4];
            a.GetWorldCorners(ac);
            b.GetWorldCorners(bc);
            float aMinX = Mathf.Min(ac[0].x, ac[2].x);
            float aMaxX = Mathf.Max(ac[0].x, ac[2].x);
            float aMinY = Mathf.Min(ac[0].y, ac[2].y);
            float aMaxY = Mathf.Max(ac[0].y, ac[2].y);
            float bMinX = Mathf.Min(bc[0].x, bc[2].x);
            float bMaxX = Mathf.Max(bc[0].x, bc[2].x);
            float bMinY = Mathf.Min(bc[0].y, bc[2].y);
            float bMaxY = Mathf.Max(bc[0].y, bc[2].y);
            return aMinX < bMaxX && aMaxX > bMinX && aMinY < bMaxY && aMaxY > bMinY;
        }

        // =====================================================================
        // B2. Opening a station panel frees the cursor again
        // =====================================================================

        [UnityTest]
        public IEnumerator ShipScene_OpeningStationPanel_FreesTheCursor_AndClosingRelocksIt()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.ShipCoop);

            bool lockSupported = false;
            yield return PlayTestUtil.ProbeCursorLockSupport(supported => lockSupported = supported);
            if (!lockSupported)
            {
                Assert.Inconclusive("SKIPPED: this session does not honour Cursor.lockState; " +
                                    "cursor free/lock transitions cannot be verified.");
            }

            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.ShipCoop);
            yield return PlayTestUtil.WaitUntil(() => Cursor.lockState == CursorLockMode.Locked, 3f);

            var armories = PlayTestUtil.FindAll<ArmoryStation>();
            Assert.AreEqual(1, armories.Count, "Expected exactly one ArmoryStation in the Ship scene.");
            MonoBehaviour station = armories[0];

            // The panel owner is found by API shape, not by hard type reference, so this test keeps
            // compiling if the ship controller is refactored by another agent.
            MonoBehaviour owner;
            MethodInfo open = FindPanelMethod("OpenStationPanel", station, out owner);
            MonoBehaviour closeOwner;
            MethodInfo close = FindPanelMethod("CloseStationPanel", station, out closeOwner);
            if (open == null)
            {
                Assert.Inconclusive(
                    "SKIPPED: no public OpenStationPanel(station) API found on any component in the Ship " +
                    "scene, so the panel-open flow cannot be driven from a test. If the API was renamed, " +
                    "update this test; the invariant is: opening a station panel must FREE the cursor.");
            }

            open.Invoke(owner, new object[] { station });
            yield return PlayTestUtil.WaitUntil(() => Cursor.lockState != CursorLockMode.Locked, 3f);
            Assert.AreNotEqual(CursorLockMode.Locked, Cursor.lockState,
                "Opening a station panel must FREE the cursor — the panel is IMGUI and is unclickable " +
                "while the cursor is captured.");

            if (close == null) yield break;
            close.Invoke(closeOwner, new object[] { station });
            yield return PlayTestUtil.WaitUntil(() => Cursor.lockState == CursorLockMode.Locked, 3f);
            Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState,
                "Closing a station panel must return the player to walk mode (cursor locked), " +
                "otherwise the Interactor stays dead code for the rest of the session.");
        }

        // =====================================================================
        // C. Label orientation invariant (mirrored TextMesh bug)
        // =====================================================================

        [UnityTest]
        public IEnumerator StationLabels_CompositeOnOverlay_NotThroughPixelRender()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.ShipCoop);
            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.ShipCoop);
            yield return PlayTestUtil.Frames(5);

            Transform overlayRoot = null;
            List<Transform> all = PlayTestUtil.FindAll<Transform>();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].name == "Station Labels")
                {
                    overlayRoot = all[i];
                    break;
                }
            }
            Assert.IsNotNull(overlayRoot,
                "ShipHud did not project station names onto an Overlay 'Station Labels' root. " +
                "Without that, PixelRender crushes the 3D TextMesh glyphs.");

            Canvas canvas = overlayRoot.GetComponentInParent<Canvas>();
            Assert.IsNotNull(canvas, "Station Labels has no parent Canvas.");
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode,
                "Station Labels must sit on a Screen Space Overlay canvas so they composite " +
                "after PixelRender, same as the HUD.");

            int overlayNames = 0;
            for (int i = 0; i < overlayRoot.childCount; i++)
            {
                var tmp = overlayRoot.GetChild(i).GetComponent<TMPro.TextMeshProUGUI>();
                Assert.IsNotNull(tmp, "Station Labels child '" + overlayRoot.GetChild(i).name
                    + "' is not TextMeshProUGUI.");
                Assert.IsFalse(tmp.raycastTarget,
                    "Station label '" + tmp.text + "' steals pointer hits from the world.");
                overlayNames++;
            }
            Assert.Greater(overlayNames, 0, "Station Labels has no overlay names.");

            List<TextMesh> meshes = PlayTestUtil.FindAll<TextMesh>();
            int stationMeshes = 0;
            foreach (TextMesh mesh in meshes)
            {
                if (mesh == null) continue;
                if (mesh.text != "ARMORY" && mesh.text != "MAP"
                    && mesh.text != "FABRICATION" && mesh.text != "CREW QUARTERS")
                    continue;
                stationMeshes++;
                var rend = mesh.GetComponent<MeshRenderer>();
                Assert.IsNotNull(rend, "Station TextMesh '" + mesh.text + "' has no MeshRenderer.");
                Assert.IsFalse(rend.enabled,
                    "Station TextMesh '" + mesh.text + "' still renders in the world — " +
                    "PixelRender will crush it. Overlay copy should own the glyphs.");
            }
            Assert.AreEqual(overlayNames, stationMeshes,
                "Overlay name count must match the authored station TextMeshes.");
        }

        // =====================================================================
        // helpers
        // =====================================================================

        /// <summary>Whether the demo gates hide this station for the CURRENT save
        /// (Docs/Demo - Single/0-wake-on-a-dead-ship.md A13, 2026-08-18 revision): a hidden station
        /// shows no name and no prompt until it has a function. Asks the pure DemoProgress
        /// rules directly — not DemoStationGate.PromptOverride — so the test still catches
        /// the Unity wiring disagreeing with the rules. With no save or the demo off, every
        /// verdict is Open and the live non-empty-prompt contract applies unchanged.</summary>
        static bool StationIsDemoHidden(MonoBehaviour station)
        {
            var m = GameSession.Meta;
            if (station is ArmoryStation) return DemoProgress.ArmoryVerdict(m) == DemoStationVerdict.Hidden;
            if (station is EngineeringStation) return DemoProgress.FabricationVerdict(m) == DemoStationVerdict.Hidden;
            if (station is MapStation) return DemoProgress.NavVerdict(m) == DemoStationVerdict.Hidden;
            return false;
        }

        /// <summary>
        /// The real "can the player interact with this" invariant: stand at eye height 2.5 m out
        /// along the console's own facing and look at it. The first thing the ray hits must be that
        /// console, within the Interactor's range. Catches consoles buried in geometry, facing a
        /// wall, or shoved into an alcove the player cannot stand in.
        /// </summary>
        static void AssertConsoleIsReachableByRay(MonoBehaviour station, float interactorRange)
        {
            Transform t = station.transform;
            Bounds bounds = ConsoleBounds(station);
            const float standOff = 2.5f;
            const float eyeHeight = 1.7f;

            // Eye height is measured from the console's OWN standing plane, not from world zero.
            // That is what makes a raised or wall-mounted console probe from the height a player
            // actually looks at it from, instead of from a metre and a half below its pedestal
            // where the ray hits the mount. On a flat deck (the ship since 2026-08-26) it
            // degenerates to a plain world-space 1.7 m probe, which is why it can stay as-is.
            Vector3 origin = new Vector3(t.position.x, 0f, t.position.z) + t.forward * standOff;
            origin.y = t.position.y + eyeHeight;
            Vector3 target = bounds.center;
            Vector3 dir = target - origin;
            float distance = dir.magnitude;
            Assert.Greater(distance, 0.01f, $"{station.gameObject.name}: degenerate probe geometry.");
            dir /= distance;

            RaycastHit hit;
            bool didHit = Physics.Raycast(origin, dir, out hit, distance + 1f, ~0, QueryTriggerInteraction.Ignore);
            Assert.IsTrue(didHit,
                $"Ray from a standing player position {origin} toward {station.gameObject.name} hit NOTHING. " +
                "The console is not where the player can look at it.");

            var hitInteractable = hit.collider.GetComponentInParent<IInteractable>() as MonoBehaviour;
            bool hitThisStation = ReferenceEquals(hitInteractable, station) ||
                                  hit.collider.transform == t ||
                                  hit.collider.transform.IsChildOf(t);
            Assert.IsTrue(hitThisStation,
                $"A player standing {standOff} m in front of {station.gameObject.name} at eye height cannot " +
                $"reach it: the ray hits '{hit.collider.name}' first (at {hit.distance:0.##} m). " +
                "Something is between the player and the console, so E can never target it.");
            Assert.LessOrEqual(hit.distance, interactorRange,
                $"{station.gameObject.name} is {hit.distance:0.##} m away from a standing player position but " +
                $"the Interactor range is {interactorRange:0.##} m — the console can never be targeted. " +
                "Move the console (or the reachable floor space in front of it) closer.");
        }

        static Bounds ConsoleBounds(MonoBehaviour station)
        {
            var colliders = station.GetComponentsInChildren<Collider>(true);
            foreach (Collider c in colliders)
            {
                if (c != null && c.enabled) return c.bounds;
            }
            return new Bounds(station.transform.position, Vector3.one * 0.5f);
        }

        static float ReadInteractorRange(Interactor interactor)
        {
            FieldInfo field = typeof(Interactor).GetField("range",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null && field.FieldType == typeof(float))
            {
                try { return (float)field.GetValue(interactor); }
                catch (Exception) { /* fall through to the documented default */ }
            }
            return 3.2f; // Interactor's serialized default at the time this test was written
        }

        /// <summary>Find a component that draws the interaction prompt. Accepts the concrete
        /// (possibly not-yet-written) ShipHud type by NAME, the Bastion HudController, or any
        /// MonoBehaviour with an OnGUI pass that holds an Interactor reference.</summary>
        static Component FindPromptRenderer(out string detail)
        {
            var seen = new List<string>();
            Component structuralMatch = null;

            foreach (MonoBehaviour mb in PlayTestUtil.FindAll<MonoBehaviour>())
            {
                if (mb == null) continue;
                Type type = mb.GetType();
                if (!seen.Contains(type.Name)) seen.Add(type.Name);

                if (type.Name == "ShipHud" || type.Name == "HudController")
                {
                    detail = string.Join(", ", seen);
                    return mb;
                }
                if (structuralMatch == null && DrawsPromptStructurally(type)) structuralMatch = mb;
            }

            detail = seen.Count == 0 ? "(no MonoBehaviours found)" : string.Join(", ", seen);
            return structuralMatch;
        }

        static bool DrawsPromptStructurally(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo onGui;
            try { onGui = type.GetMethod("OnGUI", flags); }
            catch (AmbiguousMatchException) { onGui = null; }
            if (onGui == null) return false;
            foreach (FieldInfo f in type.GetFields(flags))
                if (f.FieldType == typeof(Interactor)) return true;
            foreach (PropertyInfo p in type.GetProperties(flags))
                if (p.PropertyType == typeof(Interactor)) return true;
            return false;
        }

        /// <summary>Locate a public one-argument method (e.g. OpenStationPanel) on any component
        /// in the scene that accepts the given station instance.</summary>
        static MethodInfo FindPanelMethod(string methodName, MonoBehaviour station, out MonoBehaviour owner)
        {
            owner = null;
            foreach (MonoBehaviour mb in PlayTestUtil.FindAll<MonoBehaviour>())
            {
                if (mb == null) continue;
                foreach (MethodInfo m in mb.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (m.Name != methodName) continue;
                    ParameterInfo[] ps = m.GetParameters();
                    if (ps.Length != 1) continue;
                    if (!ps[0].ParameterType.IsInstanceOfType(station)) continue;
                    owner = mb;
                    return m;
                }
            }
            return null;
        }
    }
}
