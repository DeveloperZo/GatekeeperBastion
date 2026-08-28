using System;
using System.Collections;
using GatekeepersProtocol.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>
    /// The co-op ship is ONE deck (Zo, 2026-08-26): deck 2, the open stair and the
    /// double-height void were struck, and everything that stood on deck 2 dropped to y=0.
    /// Following the bastion's precedent when it went single-storey
    /// (<see cref="BastionCoopStairWalkTests"/>), the walk test is replaced by the invariant
    /// that no flight is authored and nothing walkable stands above the deck. If a stair
    /// comes back, this fails and the walk must come back with it.
    ///
    /// COVERAGE LOST — say it plainly, because it is real. The test this replaces was the
    /// ONLY automated proof that a CharacterController can traverse the ship's geometry at
    /// all. It drove a live capsule under gravity across live colliders, and that is what
    /// caught a walk box thinner than the capsule's own skin width, a foot end-cap that was
    /// a wall, and a railing that walled the head of the flight. Nothing left in the suite
    /// moves a capsule through the Ship: ShipSceneContractTests probes with rays, and a ray
    /// cannot see step height, slope, skin width, or a seam a capsule drops through. The
    /// ship is now flat enough that this is an acceptable trade — but the moment it grows
    /// anything a player has to negotiate rather than walk across, the walk test has to be
    /// restored from this file's history.
    /// </summary>
    public class ShipCoopStairWalkTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return PlayTestUtil.ReturnToBlankScene();
        }

        /// <summary>Every name the hull emitter gave a flight or its landing. GStairRamp is the
        /// older name for the walk box and GStairWalk the current one — both are listed so an
        /// emitter rolled back to either still trips this.</summary>
        static readonly string[] StairNames =
        {
            "Stair", "GStairRamp", "GStairWalk", "GStairHead"
        };

        /// <summary>Prefix of the hull's floor colliders, GFloor_&lt;deck&gt;_&lt;ix&gt;_&lt;iz&gt; —
        /// the M101 naming contract Tools/scene-check already reads. Name-keyed rather than
        /// swept geometrically on purpose: a downward probe cannot tell a second deck's floor
        /// from the hull roof (both are wide flat slabs with clear air above), whereas every
        /// floor the hull emits arrives under this name.</summary>
        const string FloorPrefix = "GFloor_";

        /// <summary>Anything walkable topping out above this is a second deck. Deck 1's floor
        /// boxes top out at y=0 and the deck that was struck stood at y=5, so the midpoint
        /// clears any pad or step on the deck while sitting far below any storey.</summary>
        const float SecondDeckY = 2.5f;

        [UnityTest]
        public IEnumerator NoStairsAuthored_TheShipIsSingleDeck()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.ShipCoop);
            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.ShipCoop);

            // FindAll, not GameObject.Find: a flight authored and left inactive is still a
            // flight the hull wrote, and Find cannot see it.
            var all = PlayTestUtil.FindAll<Transform>();
            int floors = 0;
            foreach (Transform t in all)
            {
                foreach (string id in StairNames)
                {
                    Assert.AreNotEqual(id, t.name,
                        id + " exists again — the ship has a flight. Restore the deck-two walk "
                        + "test from this file's history along with it.");
                }

                if (!t.name.StartsWith(FloorPrefix, StringComparison.Ordinal)) continue;
                floors++;
                var col = t.GetComponent<Collider>();
                if (col == null) continue;
                Assert.LessOrEqual(col.bounds.max.y, SecondDeckY,
                    t.name + " is walkable up to y=" + col.bounds.max.y.ToString("F2")
                    + " — that is a second deck standing over the first. The ship is one deck.");
            }

            // Without this the whole test passes vacuously on a scene whose hull failed to
            // load: no floors means no stairs, and no proof of anything.
            Assert.Greater(floors, 0,
                "Ship - CoOp has no " + FloorPrefix + "* colliders at all — the hull is missing, "
                + "so nothing here was actually proved about decks.");
        }
    }
}
