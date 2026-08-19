using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GatekeepersProtocol.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>
    /// SHIP ISOMETRIC PROBE CONTRACT — the isometric camera probe lives in its own scene,
    /// Ship_Isometric (the first-person Ship scene is untouched by it), and this fixture
    /// guards that scene's hard gate: hiding a camera-facing wall's full-height renderers
    /// (Half mode draws the hand rail instead) must NOT remove the wall's full-height
    /// collision. Render state and collision are different objects by construction —
    /// WallRenderState touches Renderer.enabled only; the full-height colliders live on the
    /// separate invisible graybox. The obvious wrong implementation (SetActive(false) on the
    /// wall object, taking its collider with it) fails here.
    /// </summary>
    public class ShipIsometricSceneTests
    {
        /// <summary>The isometric probe scene. Not part of SceneFlow — game flow never routes
        /// here; only these tests (and the editor) load it, by name from Build Settings.</summary>
        const string IsometricShipScene = "Ship_Isometric";

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // PlayTestUtil.ReturnToBlankScene only recognises the SceneFlow scene names, and
            // Ship_Isometric is deliberately not one of them — so this fixture unloads its own
            // scene the same way: leave a blank active scene so the next fixture (including
            // CoreLoopPlayModeTests, which builds its objects into whatever scene is active)
            // does not inherit a fully populated game scene.
            Scene active = SceneManager.GetActiveScene();
            if (!active.IsValid() || active.name != IsometricShipScene)
            {
                yield return PlayTestUtil.ReturnToBlankScene();
                yield break;
            }

            Scene blank = SceneManager.CreateScene(
                "PlayTests_Blank_" + Guid.NewGuid().ToString("N").Substring(0, 6));
            SceneManager.SetActiveScene(blank);
            AsyncOperation op = SceneManager.UnloadSceneAsync(active);
            if (op != null) yield return op;
            yield return null;
        }

        // =====================================================================
        // Wall render state vs collision — the probe scene's hard gate
        // =====================================================================

        /// <summary>
        /// THE WALL RAYCAST TEST (the criterion that "will be implemented wrong"): hiding a
        /// camera-facing wall's full-height renderers (Half mode draws the hand rail instead)
        /// must NOT open the wall to gunfire. Render state and collision are different objects
        /// by construction — WallRenderState touches Renderer.enabled only; the full-height
        /// colliders live on the separate invisible graybox under Environment. The obvious
        /// wrong implementation (SetActive(false) on the wall object, taking its collider
        /// with it) fails here. A scene WITHOUT the feature also fails here: no south/west
        /// WallRenderState groups, no pass.
        /// </summary>
        [UnityTest]
        public IEnumerator IsometricShip_CameraFacingWall_HalfMode_KeepsFullHeightCollision()
        {
            PlayTestUtil.RequireSceneInBuild(IsometricShipScene);
            yield return PlayTestUtil.LoadSceneAndSettle(IsometricShipScene);

            List<WallRenderState> groups = PlayTestUtil.FindAll<WallRenderState>();
            Assert.Greater(groups.Count, 0,
                "The Ship_Isometric scene contains NO WallRenderState instances. The isometric " +
                "probe requires camera-facing wall groups (south + west — at 45° yaw the camera " +
                "looks toward +X/+Z, see Sim.ShipCamera) whose render mode can be driven without " +
                "touching collision. A scene without the feature must fail this test.");

            string found = DescribeGroups(groups);
            WallRenderState south = FindWallGroup(groups, "South");
            Assert.IsNotNull(south,
                "No WallRenderState group named for the SOUTH side (expected e.g. 'WallStateSouth'). " +
                "South is a camera-facing side at the locked 45° yaw and must have a toggleable " +
                "render state. WallRenderState instances found: " + found);
            WallRenderState west = FindWallGroup(groups, "West");
            Assert.IsNotNull(west,
                "No WallRenderState group named for the WEST side (expected e.g. 'WallStateWest'). " +
                "West is a camera-facing side at the locked 45° yaw and must have a toggleable " +
                "render state. WallRenderState instances found: " + found);

            AssertHiddenWallStillBlocksAtFullHeight(south);
            AssertHiddenWallStillBlocksAtFullHeight(west);

            yield return null;
        }

        // =====================================================================
        // helpers
        // =====================================================================

        /// <summary>Probe height for the wall raycast: the full-height band — above any ~1 m
        /// hand rail, below the 3 m wall top — where a "hidden" wall must still be solid.</summary>
        const float WallProbeHeight = 2.5f;

        /// <summary>Horizontal stand-off from the wall face for the raycast origin.</summary>
        const float WallProbeStandOff = 2f;

        static WallRenderState FindWallGroup(List<WallRenderState> groups, string sideName)
        {
            foreach (WallRenderState group in groups)
            {
                if (group == null) continue;
                if (group.gameObject.name.IndexOf(sideName, StringComparison.OrdinalIgnoreCase) >= 0)
                    return group;
            }
            return null;
        }

        static string DescribeGroups(List<WallRenderState> groups)
        {
            var names = new List<string>();
            foreach (WallRenderState group in groups)
                if (group != null) names.Add(group.gameObject.name);
            return names.Count == 0 ? "(none)" : string.Join(", ", names);
        }

        /// <summary>Reflect one of WallRenderState's private renderer arrays. Reflection on
        /// purpose: the arrays are serialized implementation detail, but the test must prove
        /// the VISUAL state actually changed, not just that SetMode was callable.</summary>
        static Renderer[] ReadWallPieces(WallRenderState group, string fieldName)
        {
            FieldInfo field = typeof(WallRenderState).GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field,
                $"WallRenderState no longer has a private '{fieldName}' field; this test reads " +
                "it by reflection to prove the renderers really toggled. If the field was " +
                "renamed, point this test at the new name.");
            return field.GetValue(group) as Renderer[];
        }

        static int CountLive(Renderer[] pieces)
        {
            if (pieces == null) return 0;
            int live = 0;
            foreach (Renderer r in pieces)
                if (r != null) live++;
            return live;
        }

        /// <summary>
        /// The invariant for one camera-facing wall group: drive the mode EXPLICITLY (never
        /// trust the authored default) to Full then Half, prove every full-height renderer
        /// went dark and every half piece (the rail) draws, then raycast horizontally through
        /// the hidden wall at full height — from the ship interior, the direction a player
        /// would shoot over the rail — and demand a hit on a collider that is NOT any renderer
        /// this test just hid.
        /// </summary>
        static void AssertHiddenWallStillBlocksAtFullHeight(WallRenderState group)
        {
            string who = group.gameObject.name;
            Renderer[] full = ReadWallPieces(group, "_fullPieces");
            Renderer[] half = ReadWallPieces(group, "_halfPieces");
            Assert.Greater(CountLive(full), 0,
                $"{who}: _fullPieces holds no renderers — a camera-facing wall group with " +
                "nothing to hide toggles nothing, so this group cannot satisfy the probe.");
            Assert.Greater(CountLive(half), 0,
                $"{who}: _halfPieces holds no renderers — Half mode must draw a real piece " +
                "(the hand rail), otherwise Half is just None with a different name.");

            // Full first: proves SetMode enables these exact renderers, and lets the probe
            // piece's world bounds be captured while it is enabled (no reliance on what a
            // disabled renderer reports). Pick the largest-footprint piece — the wall slab,
            // not a trim strip or doorframe.
            group.SetMode(WallRenderMode.Full);
            Renderer probe = null;
            float probeFootprint = 0f;
            foreach (Renderer r in full)
            {
                if (r == null) continue;
                Assert.IsTrue(r.enabled,
                    $"{who}: SetMode(Full) left full piece '{r.name}' disabled — SetMode does " +
                    "not actually drive this renderer.");
                Bounds b = r.bounds;
                float footprint = b.size.x * b.size.z;
                if (probe == null || footprint > probeFootprint)
                {
                    probe = r;
                    probeFootprint = footprint;
                }
            }
            Bounds wall = probe.bounds;

            group.SetMode(WallRenderMode.Half);
            Assert.AreEqual(WallRenderMode.Half, group.Mode, $"{who}: SetMode(Half) did not take.");
            foreach (Renderer r in full)
            {
                if (r == null) continue;
                Assert.IsFalse(r.enabled,
                    $"{who}: full-height piece '{r.name}' is still ENABLED in Half mode — the " +
                    "visual state provably did not change, so this test would otherwise be " +
                    "raycasting a wall that was never hidden.");
            }
            foreach (Renderer r in half)
            {
                if (r == null) continue;
                Assert.IsTrue(r.enabled,
                    $"{who}: half piece '{r.name}' is DISABLED in Half mode — Half must draw the rail.");
            }

            // The raycast. The wall slab is axis-aligned, so its outward normal is the thin
            // horizontal axis of its world bounds. At the locked 45° yaw the camera looks
            // toward +X/+Z, meaning the ship interior sits on the +normal side of the south
            // (+Z) and west (+X) walls — cast from inside, through the hidden slab's centre
            // line at full height, exactly the shot that must NOT leave the ship.
            Vector3 normal = wall.size.x < wall.size.z ? Vector3.right : Vector3.forward;
            float thickness = Mathf.Min(wall.size.x, wall.size.z);
            Vector3 target = new Vector3(wall.center.x, WallProbeHeight, wall.center.z);
            Vector3 origin = target + normal * (WallProbeStandOff + thickness * 0.5f);
            float castDistance = WallProbeStandOff + thickness + 1f;

            RaycastHit hit;
            bool didHit = Physics.Raycast(origin, -normal, out hit, castDistance, ~0,
                QueryTriggerInteraction.Ignore);
            Assert.IsTrue(didHit,
                $"{who}: a horizontal ray at {WallProbeHeight} m (full-height band, above the " +
                $"rail) aimed through hidden wall piece '{probe.name}' (origin {origin}, toward " +
                $"{target}) hit NOTHING. The wall renders Half but its full-height collision is " +
                "GONE — players can shoot (and sight) straight over a wall that must stay solid. " +
                "Collision must live on the invisible graybox, untouched by render state.");

            // The hit must be collision near the hidden wall's own plane — a console or prop
            // the ray clipped on the way must not be allowed to close this gate.
            float planeDistance = Mathf.Abs(Vector3.Dot(hit.point - wall.center, normal));
            Assert.LessOrEqual(planeDistance, thickness * 0.5f + 0.5f,
                $"{who}: the ray hit '{hit.collider.name}' {planeDistance:0.##} m from the hidden " +
                "wall's plane — that is some other object, not the wall's collision, so the wall " +
                "itself may still be open at full height.");

            // And the hit collider must NOT belong to any renderer this test just hid (nor to
            // the rail): collision genuinely lives elsewhere — the separate invisible graybox —
            // by construction, not by the accident of a collider riding a disabled renderer
            // (which the "obvious" SetActive implementation would one day take down with it).
            foreach (Renderer r in full) AssertHitIsNotOnPiece(who, hit, r);
            foreach (Renderer r in half) AssertHitIsNotOnPiece(who, hit, r);
        }

        static void AssertHitIsNotOnPiece(string who, RaycastHit hit, Renderer piece)
        {
            if (piece == null) return;
            Assert.IsFalse(hit.collider.transform.IsChildOf(piece.transform),
                $"{who}: the full-height ray hit collider '{hit.collider.name}', which lives ON " +
                $"render piece '{piece.name}'. Collision must be owned by the separate invisible " +
                "graybox, never by the toggled renderers — a collider that rides the render piece " +
                "is one refactor away from vanishing with it.");
        }
    }
}
