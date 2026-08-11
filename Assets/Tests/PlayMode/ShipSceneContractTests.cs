using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GatekeeperBastion.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeeperBastion.PlayTests
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
    ///      absent here — the Ship needs its own; GatekeeperBastion.Game.ShipHud),
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
            PlayTestUtil.RequireSceneInBuild(SceneFlow.Ship);
            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.Ship);

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
                "'GatekeeperBastion.Game.ShipHud' (a Ship-scene HUD that draws Interactor.CurrentPrompt) — " +
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
                Assert.IsFalse(string.IsNullOrEmpty(((IInteractable)station).Prompt),
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
            PlayTestUtil.RequireSceneInBuild(SceneFlow.Ship);

            bool lockSupported = false;
            yield return PlayTestUtil.ProbeCursorLockSupport(supported => lockSupported = supported);
            // The probe left the cursor FREE on purpose: whatever locks it now is the scene.

            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.Ship);
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
        // B1b. E opens Armory from anywhere — not the Interactor raycast
        // =====================================================================

        [UnityTest]
        public IEnumerator ShipScene_Interact_OpensArmoryWithoutLookingAtConsole()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.Ship);
            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.Ship);
            yield return PlayTestUtil.WaitUntil(() => PlayerRegistry.Local != null, 5f);

            var ship = UnityEngine.Object.FindFirstObjectByType<ShipController>();
            Assert.IsNotNull(ship, "ShipController missing");
            var armory = UnityEngine.Object.FindFirstObjectByType<ArmoryStation>();
            Assert.IsNotNull(armory, "ArmoryStation missing");

            // Put the player far away, looking at the sky — raycast cannot hit the console.
            var player = PlayerRegistry.Local.transform;
            player.position = armory.transform.position + Vector3.up * 200f;
            player.rotation = Quaternion.LookRotation(Vector3.up);
            var interactor = player.GetComponentInChildren<Interactor>(true);
            Assert.IsNotNull(interactor);
            yield return null;
            Assert.IsTrue(string.IsNullOrEmpty(interactor.CurrentPrompt),
                "Falsifier setup failed: Interactor still has a prompt — player is still targeting something.");

            bool opened = ship.TryOpenArmoryFromInteract();
            Assert.IsTrue(opened, "TryOpenArmoryFromInteract returned false with no panel open");
            yield return null;

            Assert.AreSame(armory, ship.OpenStation,
                "E/Interact must open Armory from anywhere; OpenStation was not Armory after TryOpenArmoryFromInteract.");
            Assert.IsTrue(ship.IsPanelOpen);

            // Console Interact is a no-op — must not be required for open.
            ship.CloseStationPanel(armory);
            yield return null;
            Assert.IsNull(ship.OpenStation);
            ((IInteractable)armory).Interact(0f);
            yield return null;
            Assert.IsNull(ship.OpenStation,
                "StationTerminal.Interact must not open the panel — only ShipController.TryOpenArmoryFromInteract does.");
        }

        // =====================================================================
        // B2. Opening a station panel frees the cursor again
        // =====================================================================

        [UnityTest]
        public IEnumerator ShipScene_OpeningStationPanel_FreesTheCursor_AndClosingRelocksIt()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.Ship);

            bool lockSupported = false;
            yield return PlayTestUtil.ProbeCursorLockSupport(supported => lockSupported = supported);
            if (!lockSupported)
            {
                Assert.Inconclusive("SKIPPED: this session does not honour Cursor.lockState; " +
                                    "cursor free/lock transitions cannot be verified.");
            }

            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.Ship);
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
        public IEnumerator StationLabels_AreReadableFromThePlayerSide_NotMirrored()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.Ship);
            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.Ship);
            // Give any runtime billboard component a few Update/LateUpdate passes to orient itself.
            yield return PlayTestUtil.Frames(5);

            List<TextMesh> labels = PlayTestUtil.FindAll<TextMesh>();
            if (labels.Count == 0)
            {
                Assert.Inconclusive(
                    "SKIPPED: the Ship scene contains no TextMesh labels (the scene builder falls back to " +
                    "blank marker bars when the built-in LegacyRuntime font is unavailable), so text " +
                    "orientation cannot be checked.");
            }

            Vector3 viewer = ViewerEyePosition();

            foreach (TextMesh label in labels)
            {
                Transform t = label.transform;
                string who = DescribePath(t);

                Assert.IsFalse(string.IsNullOrEmpty(label.text), $"Label '{who}' renders empty text.");

                // Mirror check #1: a negative scale axis flips the glyphs even when the object faces
                // the right way.
                Vector3 scale = t.lossyScale;
                Assert.IsTrue(scale.x > 0f && scale.y > 0f && scale.z > 0f,
                    $"Label '{who}' has a negative world scale {scale} — negative scale mirrors the glyphs.");

                // Mirror check #2: TextMesh orientation.
                //
                // CONVENTION BEING ASSERTED (derived, not guessed): a TextMesh builds its glyph quads
                // in the local XY plane with the reading direction along local +X, and the built-in
                // font material renders with Cull Off — so the text is VISIBLE from both sides but
                // reads correctly from only one. A viewer at V looking at the label at L sees the
                // label's local +X on their right (i.e. not mirrored) exactly when
                //     dot(label.forward, L - V) > 0
                // i.e. the label's forward axis points AWAY from the reader. This is the same
                // relationship the canonical Unity billboard snippet produces
                // (transform.forward = camera.forward, which points from the camera into the scene).
                // A console label simply parented to a console that FACES the room inherits the
                // console's forward and therefore points AT the reader: mirrored text. The fix is
                // either a 180-degree flip of the label relative to its console, or a billboard
                // component that re-orients it at runtime; both satisfy this assertion.
                Vector3 viewerToLabel = t.position - viewer;
                if (viewerToLabel.sqrMagnitude < 1e-6f) continue; // degenerate: viewer inside the label
                float facing = Vector3.Dot(t.forward.normalized, viewerToLabel.normalized);
                float angle = Vector3.Angle(t.forward, viewerToLabel);

                Assert.Greater(facing, 0f,
                    $"Station label '{who}' (\"{label.text}\") renders MIRRORED from the player side. " +
                    $"Angle between label.forward and the viewer->label direction is {angle:0.#}° " +
                    "(must be < 90°, i.e. label.forward points AWAY from the reader — that is the " +
                    "orientation Unity's TextMesh reads correctly in, and the one the standard billboard " +
                    "snippet produces). Fix by flipping the label 180° relative to its console, or by " +
                    $"adding a billboard component that faces it away from the camera each frame. Viewer used: {viewer}.");
            }
        }

        // =====================================================================
        // helpers
        // =====================================================================

        /// <summary>Eye position the labels/consoles should read from: the live main camera when
        /// there is one, else the player's head, else the middle of the common room.</summary>
        static Vector3 ViewerEyePosition()
        {
            if (Camera.main != null) return Camera.main.transform.position;
            PlayerHealth player = PlayerRegistry.Local;
            if (player != null) return player.transform.position + Vector3.up * 0.65f;
            return new Vector3(0f, 1.7f, 0f);
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

            Vector3 origin = new Vector3(t.position.x, 0f, t.position.z) + t.forward * standOff;
            origin.y = eyeHeight;
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

        static string DescribePath(Transform t)
        {
            string path = t.name;
            Transform parent = t.parent;
            int guard = 0;
            while (parent != null && guard++ < 8)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}
