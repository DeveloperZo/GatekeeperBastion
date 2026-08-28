using System.Collections;
using System.Collections.Generic;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>
    /// A LIVE CAPSULE WALKS THE WHOLE SHIP. This is the coverage
    /// <see cref="ShipCoopStairWalkTests"/> gave up when the stair died: that file says, in as
    /// many words, that it was the only automated proof a CharacterController can traverse the
    /// ship's geometry at all, and that nothing replaced it. This replaces it.
    ///
    /// WHAT A RAY CANNOT SEE, AND THIS CAN. ShipSceneContractTests probes with Physics.Raycast
    /// and Tools/scene-check/reachability.py walks a CELL-INDEX GRAPH — it joins cell (ix,iz) to
    /// (ix,iz+1) by their coordinates and asks whether a graybox box sits between the two
    /// centres. Neither is a body. A ray is a zero-radius line, so it passes a doorframe 5 cm
    /// too narrow for the 0.70 m player capsule and reports the room reachable; the cell graph
    /// does not model width at all. Neither sees step height, skinWidth instability on a thin
    /// walk surface, a lip at a partition, a prop parked 30 cm into a lane, a ceiling the
    /// capsule's 1.8 m head hits, or a seam the capsule drops through. This test drives the
    /// scene's OWN CharacterController under gravity through the OWN colliders along a
    /// 230 m route and asserts it arrives standing at each destination.
    ///
    /// WHAT IT STILL CANNOT SEE, said plainly so nobody over-reads a green run:
    ///   - It ROUTES, it does not pathfind. The ship has no baked NavMesh
    ///     (m_NavMeshData: {fileID: 0}), so NavMesh.CalculatePath is unavailable and the
    ///     waypoints below are derived by hand from the generator's tables. A prop dropped
    ///     half a metre into a lane fails a leg even though the ship is still traversable —
    ///     that is a true report of "the route I was told to walk is blocked", not of
    ///     "the ship is unreachable". Read the failure, then decide which it is.
    ///   - It touches EIGHT destinations, not 56 cells. reachability.py still owns the claim
    ///     that every cell is connected; this owns the claim that a body fits through the
    ///     doors on the way to the things a player actually needs.
    ///   - It does not press E. Whether a station RESPONDS at the arrival point is
    ///     ShipSceneContractTests' contract, not this one. ArriveRadius is set well inside
    ///     Interactor's 3.2 m reach so the two agree, but this test never asks.
    ///   - It drives the controller directly with FirstPersonController DISABLED, so it proves
    ///     the geometry, not the input path: no sprint, no stamina, no mouse-look, no footstep
    ///     audio, and none of the slope handling a player's own inputs would exercise.
    /// </summary>
    public class ShipTraversalTests
    {
        // ------------------------------------------------------------------ layout
        // Transcribed from Tools/ship-hull/build_hull.py, which is the truth for this scene.
        // Everything below is DERIVED from these four numbers rather than measured off the
        // scene by eye, so the route follows the ship when the ship moves.

        /// <summary>Cell pitch: cell (ix,iz) spans x[5*ix, 5*ix+5], z[5*iz, 5*iz+5].</summary>
        const float Cell = 5f;

        /// <summary>build_hull.py CORRIDOR_HALF — the spine runs clear over x[-1.25, 1.25].</summary>
        const float CorridorHalf = 1.25f;

        /// <summary>Thickness of a GSpine_* graybox: gbox(..., (.5, 4, 5)). The wall body sits
        /// OUTBOARD of the corridor line, so the port wall occupies x[-1.75, -1.25].</summary>
        const float SpineThickness = 0.5f;

        /// <summary>Half-beam of the wide rows (ROWS 2..13 are ix -2..1, so x[-10, 10]). The
        /// hull graybox starts at this plane and runs outboard.</summary>
        const float BeamHalf = 10f;

        /// <summary>Spine doors, build_hull.py DOOR_ROWS = {1, 3, 4, 8, 11}. Kept as a field
        /// rather than inlined because it is the whole reason the route is shaped as it is: a
        /// room meets the corridor ONLY at these five rows, and rows 5-7 and 12-15 carry solid
        /// spine wall for their entire length. Tools/scene-check/reachability.py hardcodes the
        /// same five and says it must match the generator; if that set changes, the legs that
        /// cross the spine have to be re-cut, and this array is where you start.</summary>
        static readonly int[] SpineDoorRows = { 1, 3, 4, 8, 11 };

        /// <summary>build_hull.py PARTITIONS[1] — the five transverse bulkhead lines. Each emits
        /// panels over x[-10,-5], x[-5,-1.25], x[1.25,5] and x[5,10], with the graybox spanning
        /// z[L, L+0.5]. NOTHING is emitted over the corridor itself, which is why every
        /// fore-and-aft run on this route is a spine run. The z=55 panel is what stopped run 2.</summary>
        static readonly float[] PartitionZ = { 10f, 25f, 40f, 55f, 65f };

        /// <summary>build_hull.py OPEN_SPINE_FLANK — the two lines whose px -1.25 and px 5.0
        /// panels are deliberately left out, so the compartments either side join through a
        /// 3.75 m opening beside the corridor. These are load-bearing for reachability, not
        /// scenery: z=25 is the ONLY feed to the gallery (rows 5-7 carry no spine door) and z=65
        /// the only feed to the bridge (row 11 is the last door in the ship). Close them and
        /// reachability.py drops to 44/56 and 48/56 respectively.</summary>
        static readonly float[] OpenFlankZ = { 25f, 65f };

        /// <summary>Where the hull steps in from ix -2..1 to ix -1..0 (ROWS 13 -> 14). No
        /// partition is authored here on purpose — the step itself puts N-wall graybox across
        /// x[-10,-5] and x[5,10], so a body crossing z=70 must already be inboard of x=+/-5.</summary>
        const float BowStepZ = 70f;

        // ------------------------------------------------------------------ lanes

        /// <summary>Centre of the clear strip between a hull-side cell edge (x = +/-5) and the
        /// OUTBOARD face of the spine wall (x = +/-1.75): (1.75 + 5) / 2 = 3.375. This is the
        /// fore-and-aft lane a body uses inside the ix = -1 / ix = 0 columns, and it leaves
        /// 1.62 m of clearance on both sides of a 0.35 m capsule.</summary>
        const float SpineLaneX = (Cell + CorridorHalf + SpineThickness) * 0.5f;

        /// <summary>Centre of the spine corridor. The corridor is the one line no partition ever
        /// crosses — PARTITIONS emits panels at px -5, -1.25, 5 and 10 and never over
        /// x[-1.25, 1.25] — so every fore-and-aft run of the route uses it.</summary>
        const float CorridorX = 0f;

        /// <summary>Outboard face of the spine wall body: the corridor line plus the GSpine box.
        /// Between CorridorHalf and this, on any row that is NOT a door row, is solid wall.</summary>
        const float SpineOuterX = CorridorHalf + SpineThickness;

        /// <summary>How far off the hull plane a body stands at a wall-mounted console. It cannot
        /// be much less: every station hangs an "Interact" trigger-less box that juts 0.45 m into
        /// the room, so a 0.35 m capsule bottoms out at 0.80 m from the pivot. One metre leaves
        /// 0.20 m of slack in front of that box and still puts the pivot inside ArriveRadius.</summary>
        const float WallStandoff = 1f;

        /// <summary>Fore-and-aft lane hard against the hull wall, x = +/-9.0.</summary>
        const float HullLaneX = BeamHalf - WallStandoff;

        /// <summary>Standoff from a hatch's leaf plane. Hatch_1..3 carry a 0.30 m slab spanning
        /// x[8.05, 11.25] at their own z — it is a wall across the bay, not a panel in the hull
        /// line — so a body cannot stand ON a hatch's z at all. Every hatch is approached from
        /// 0.70 m to one side of its leaf, which clears the slab by 0.55 m.</summary>
        const float LeafClear = 0.7f;

        /// <summary>Standoff from the Ship Terminal, a FREE-STANDING pedestal (collider
        /// x[-4.2, -3.8], z[42.2, 42.8]) rather than a wall fitting — so it is approached from
        /// its port side and cleared by 0.70 m.</summary>
        const float PedestalStandoff = 0.9f;

        /// <summary>Standoff from the bow bulkhead for the NAV MAP. Bigger than the other consoles'
        /// 1.0 m, and deliberately: the NAV MAP is the only station whose Desk is a free-standing
        /// slab rather than a wall fitting — z[79.30, 80.42], so it projects 0.70 m off the
        /// bulkhead, where every other station's Desk hides inside the 0.35 m the hull wall
        /// already occupies. That Desk is authored INACTIVE (it carries the station's console
        /// colour and nothing at runtime switches it on), so it does not block today — but a
        /// waypoint at 0.90 m would sit 0.15 m INSIDE it the day anything does, and the whole
        /// point of standing off is that it does not depend on that. 1.15 m clears the Desk
        /// footprint by 0.45 m and the Interact box (z[79.55, 80.25]) by 0.70 m, and still puts
        /// the pivot inside ArriveRadius.</summary>
        const float BulkheadStandoff = 1.15f;

        /// <summary>The terminal pedestal stands ON the row-8 door centreline, so the run from
        /// the terminal to the spine door has to step this far aft of it first. Without this
        /// leg the route walks straight through the console.</summary>
        const float PedestalBypass = 1f;

        // ------------------------------------------------------------------ walk rig

        /// <summary>FirstPersonController.moveSpeed. Walking the test at the game's own speed
        /// keeps the iteration caps below honest.</summary>
        const float Speed = 5f;

        /// <summary>FirstPersonController.gravity, and the same constant the retired stair walk
        /// used.</summary>
        const float Gravity = -20f;

        /// <summary>FIXED timestep, deliberately NOT Time.deltaTime. The retired walk used the
        /// frame delta with a 0.02 fallback, which is fine for a 6 m stair and wrong for a 230 m
        /// route: the per-leg iteration cap has to be a real bound, and a cap computed against a
        /// delta that can be 1/200 on one machine and 1/50 on another is a guess. cc.Move is a
        /// kinematic sweep and does not care what the wall clock did, so a fixed step makes the
        /// walk deterministic and the cap exact.</summary>
        const float Dt = 0.02f;

        /// <summary>A leg is done when the capsule is this close to its waypoint on XZ. Every
        /// waypoint on the route has at least 0.45 m of clearance to the nearest collider, so a
        /// capsule that cannot close to 0.20 m has hit something that is not in the layout.</summary>
        const float WaypointTol = 0.2f;

        /// <summary>"Arrived at the landmark" on XZ. Every destination pivot is either ON a wall
        /// plane or inside its own collider, so a body can never stand on it: the worst case on
        /// this route is 1.00 m (the wall consoles, see WallStandoff). 1.5 m is the tightest
        /// circle the capsule can actually enter with margin, it is less than half Interactor's
        /// 3.2 m reach so anything inside it could be used, and the closest two landmarks on the
        /// route (hatch 3 and Crew Quarters) are 5.0 m apart — so no circle here can be satisfied
        /// by standing at the wrong destination.</summary>
        const float ArriveRadius = 1.5f;

        /// <summary>Route sanity: no leg may be shorter than this. A leg that collapses to
        /// nothing proves nothing, and a layout change that folds two waypoints together would
        /// otherwise turn into a silently vacuous pass.</summary>
        const float MinLegLength = 0.75f;

        /// <summary>How far above its rest height the capsule is placed at the start, so Settle
        /// has a fall to resolve and "grounded" is earned rather than inherited from the pose.
        /// Well under stepOffset, so it cannot hop the rig onto anything.</summary>
        const float StartDrop = 0.25f;

        /// <summary>Centre of row iz along z.</summary>
        static float RowMidZ(int iz) { return Cell * iz + Cell * 0.5f; }

        static bool IsDoorRow(int row)
        {
            foreach (int d in SpineDoorRows) if (d == row) return true;
            return false;
        }

        static bool IsOpenFlankLine(float line)
        {
            foreach (float f in OpenFlankZ) if (Mathf.Abs(f - line) < 0.01f) return true;
            return false;
        }

        struct Leg
        {
            public readonly string Why;
            public readonly Vector3 To;
            public readonly string Landmark;

            public Leg(string why, float x, float z, string landmark)
            {
                Why = why;
                To = new Vector3(x, 0f, z);
                Landmark = landmark;
            }
        }

        string _campaignJson;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Snapshot only what already exists; never force a session into being. See the
            // remarks on ZoneTwoLoopPlayModeTests for why a fixture may not decide what the
            // session is for the fixtures that run after it.
            _campaignJson = GameSession.Meta == null ? null : GameSession.Meta.ToSnapshotJson();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            MetaState restored;
            if (_campaignJson == null)
            {
                restored = MetaState.CreateNew();
                restored.DemoActive = false;
            }
            else
            {
                restored = MetaState.FromSnapshotJson(_campaignJson);
            }
            GameSession.AdoptDevState(restored);
            _campaignJson = null;
            yield return PlayTestUtil.ReturnToBlankScene();
        }

        // ==================================================================
        [UnityTest]
        [Timeout(240000)]
        public IEnumerator PlayerCapsule_WalksFromSpawnToEveryStationHatchAndTheBridge()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.ShipCoop);

            // WHERE THE RIG ENDS UP IS DEMO STATE, NOT A SCENE FACT — and a traversal test must
            // not inherit it. DemoShipDirector.Update reads GameSession.Meta and stands the rig
            // somewhere different for each beat. That cost two runs before this comment existed:
            //   RUN 1, ambient session (DemoActive, capsule not yet opened): the director called
            //     ShipTeleporter.BeginSealed -> PlaceOnPad, which stands the rig ON the wake pod
            //     at z 47.5 and brings the pod's own Bed and Lid boxes up around it
            //     (x[-8.60,-6.40], z[46.95,48.05], y 0..0.70 and 0.86..1.04 — both authored
            //     INACTIVE in the scene file, which is exactly why an offline collider survey
            //     cannot see them). Leg 0 moved 0.49 m and stopped: it started inside furniture.
            //   RUN 2, GameController.BuildDemoCheckpoint(1) adopted: under that state the
            //     director takes the PlaceAt(_spawnNav) branch and stands the rig at the BOW
            //     (z 76, in front of the NAV MAP). The route then ran stern-ward from the wrong
            //     end and died on the z=55 transverse partition at x=-3.57 — the ship behaving
            //     correctly against a wrong assumption in the test.
            // So adopt a state with the demo OFF. That is the one branch of the director that
            // touches neither the rig's transform nor the pod (`if (!m.DemoActive)`: hull light
            // up, BeginDormant on the teleporter, return), which leaves the ship as exactly the
            // geometry the hull generator emitted and nothing else. Whether a body fits through
            // the ship is this test's question; every demo beat is somebody else's contract.
            MetaState quiet = MetaState.CreateNew();
            quiet.DemoActive = false;
            GameSession.AdoptDevState(quiet);

            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.ShipCoop);
            yield return PlayTestUtil.WaitUntil(() => PlayerRegistry.Local != null, 5f);

            PlayerHealth player = PlayerRegistry.Local;
            Assert.IsNotNull(player,
                "Ship - CoOp registered no player, so there is no capsule to walk. "
                + "ShipSceneContractTests owns the rig contract — fix that first.");
            var cc = player.GetComponent<CharacterController>();
            Assert.IsNotNull(cc,
                "The ship's player rig has no CharacterController. Nothing here can be proved "
                + "with a ray; this test needs a body.");

            // Exclusive control of the controller. FirstPersonController.Update calls
            // _cc.Move every frame whatever the input says (it drives gravity through the same
            // call), so leaving it enabled means two owners fighting over one capsule and a
            // walk that is not reproducible.
            var fpc = player.GetComponent<FirstPersonController>();
            if (fpc != null) fpc.enabled = false;

            // Same reason, one layer out: DemoShipDirector.Update retries its placement every
            // frame until it succeeds, so a director left live can pick the rig up again in the
            // middle of a leg and teleport it 30 m. With the demo off it will not — but "will
            // not" is a state argument and this is a geometry test, so take the component out of
            // the loop rather than depend on the branch staying the branch it is today.
            foreach (DemoShipDirector director in PlayTestUtil.FindAll<DemoShipDirector>())
                if (director != null) director.enabled = false;

            List<Transform> all = PlayTestUtil.FindAll<Transform>();
            float plane = FloorPlaneY(all);

            // THE START POSE IS OURS, NOT THE DIRECTOR'S. Spawn_Teleporter is the honest choice:
            // it is where PlaceAt stands a player coming home from a run, it is 1.5 m clear of
            // the wake pod rather than inside it, and it is the pose this route was cut around.
            // Y is resolved from the measured deck plus the controller's own rest height rather
            // than from the marker's authored y — markers in this scene are authored at
            // inconsistent heights (pad-pivot vs floor level), which is the same trap
            // DemoShipDirector.PlaceAt raycasts around. The small drop on top gives Settle
            // something to resolve, so "grounded" below is earned rather than assumed.
            Transform spawnMarker = Require(all, "Spawn_Teleporter");
            Vector3 start = new Vector3(
                spawnMarker.position.x,
                plane + cc.height * 0.5f + cc.center.y + StartDrop,
                spawnMarker.position.z);
            // cc.enabled must be false across the teleport or the controller fights the
            // transform write and lands the rig somewhere neither of us chose.
            cc.enabled = false;
            player.transform.position = start;
            cc.enabled = true;
            Physics.SyncTransforms();

            Transform terminal = Require(all, "Ship Terminal");
            Transform navMap = Require(all, "Station_Map");
            Transform hatch1 = Require(all, "Hatch_1");
            Transform hatch2 = Require(all, "Hatch_2");
            Transform hatch3 = Require(all, "Hatch_3");
            Require(all, "Station_Armory");
            Require(all, "Station_Fabrication");
            Require(all, "Station_CrewQuarters");

            // The three spine-door z's the route crosses at, each set by the hatch it has to
            // stand clear of rather than by a number typed in here. Row 3 is approached from
            // AFT of hatch 1's leaf and row 11 from FORWARD of hatch 3's, purely because that
            // is the side of each leaf the rest of the leg comes from.
            float row3Z = hatch1.position.z - LeafClear;
            float row11Z = hatch3.position.z + LeafClear;
            float galleryZ = hatch2.position.z - LeafClear;
            AssertInDoorRow(row3Z, 3);
            AssertInDoorRow(row11Z, 11);

            var route = new List<Leg>
            {
                // --- the muster compartment (rows 8-10), where the player wakes ------------
                new Leg("aft off the teleporter pad into the row-8 door band",
                    spawnMarker.position.x, RowMidZ(8), null),
                new Leg("up to the Ship Terminal, the first thing the player is meant to find",
                    terminal.position.x - PedestalStandoff, terminal.position.z, "Ship Terminal"),
                new Leg("step aft of the terminal pedestal — it stands ON the row-8 centreline",
                    terminal.position.x - PedestalStandoff, RowMidZ(8) - PedestalBypass, null),
                new Leg("through the PORT spine door of row 8 into the corridor",
                    CorridorX, RowMidZ(8) - PedestalBypass, null),

                // --- the shop end (rows 2-4) ---------------------------------------------
                // 19 m of corridor, crossing the z=40 and z=25 bulkhead lines. At z=40 the
                // corridor is the ONLY way through — PARTITIONS closes x[-10,-5], x[-5,-1.25],
                // x[1.25,5] and x[5,10] there — which is exactly why every fore-and-aft run on
                // this route is a spine run.
                new Leg("aft down the spine from the muster to the shop end, through z=40 and z=25",
                    CorridorX, RowMidZ(4), null),
                new Leg("out of the STARBOARD row-4 door to the Armory on the hull wall",
                    HullLaneX, RowMidZ(4), "Station_Armory"),
                // Row 4 is a door row, so BOTH spine flanks are open at this z and the ship can
                // be crossed beam to beam in one straight line. That is only true on five rows
                // in the whole ship.
                new Leg("straight across the beam through both row-4 doors to the port lane",
                    -SpineLaneX, RowMidZ(4), null),
                new Leg("aft along the port lane to the Fabrication row",
                    -SpineLaneX, RowMidZ(2), null),
                new Leg("out to Fabrication on the port hull wall",
                    -HullLaneX, RowMidZ(2), "Station_Fabrication"),
                new Leg("back inboard off the port wall",
                    -SpineLaneX, RowMidZ(2), null),
                new Leg("forward to the row-3 door line, aft of hatch 1's leaf",
                    -SpineLaneX, row3Z, null),
                new Leg("across the beam through both row-3 doors to hatch bay 1",
                    HullLaneX, row3Z, "Hatch_1"),

                // --- the gallery (rows 5-7) ----------------------------------------------
                // Rows 5-7 carry NO spine door, so the gallery is not reachable from the
                // corridor at all. It is fed through the z=25 partition's missing spine-flank
                // panel (OPEN_SPINE_FLANK), which leaves x[1.25,5] open beside the corridor.
                // Walking it is the point: close that flank and reachability.py drops to 44/56.
                new Leg("back to the starboard lane",
                    SpineLaneX, row3Z, null),
                new Leg("forward through the OPEN SPINE FLANK at z=25 into the gallery",
                    SpineLaneX, galleryZ, null),
                new Leg("out to hatch bay 2 amidships",
                    HullLaneX, galleryZ, "Hatch_2"),
                new Leg("back inboard off the starboard wall",
                    SpineLaneX, galleryZ, null),
                new Leg("back out through the z=25 flank — rows 5-7 have no other exit",
                    SpineLaneX, RowMidZ(4), null),
                new Leg("in through the starboard row-4 door to the corridor",
                    CorridorX, RowMidZ(4), null),

                // --- the hatch-3 vestibule (rows 11-12) ----------------------------------
                new Leg("35 m up the spine, through the z=25, z=40 and z=55 bulkheads",
                    CorridorX, row11Z, null),
                new Leg("out of the STARBOARD row-11 door to hatch bay 3",
                    HullLaneX, row11Z, "Hatch_3"),
                new Leg("forward along the starboard wall to Crew Quarters",
                    HullLaneX, RowMidZ(12), "Station_CrewQuarters"),
                new Leg("back inboard off the starboard wall",
                    SpineLaneX, RowMidZ(12), null),
                new Leg("aft to the row-11 door line — row 12 has solid spine wall",
                    SpineLaneX, row11Z, null),
                new Leg("across the beam through both row-11 doors to the port lane",
                    -SpineLaneX, row11Z, null),

                // --- the bridge (rows 13-15) ---------------------------------------------
                // Row 11 is the LAST spine door in the ship, so the whole bow drains back
                // through the z=65 partition's missing port flank panel. 21 m up the port lane,
                // past the hull step at z=70 where rows 14-15 narrow to x[-5,5].
                new Leg("forward through the OPEN SPINE FLANK at z=65, up the bow lane to the NAV MAP",
                    navMap.position.x, navMap.position.z - BulkheadStandoff, "Station_Map"),
            };

            // Let gravity seat the capsule before anything is measured.
            yield return Settle(cc);

            Vector3 seated = cc.transform.position;
            AssertStanding(cc, plane, "the spawn", 0, "settle onto the deck");
            // A slide here means the start pose is not standable — which is a REAL FINDING about
            // Spawn_Teleporter, not a test defect, so it says so. The tightest thing near it is
            // the pod's Console box (x[-7.75,-7.25], y[1.60,1.90], z[46.46,46.58]), 0.46 m from
            // the marker against a 0.35 m capsule: 0.11 m of margin, and the narrowest number
            // anywhere on this route.
            Assert.Less(Flat(seated - start).magnitude, 0.5f,
                "The capsule slid " + Flat(seated - start).magnitude.ToString("F2")
                + " m just standing still at Spawn_Teleporter " + Fmt(start) + ", ending at "
                + Fmt(seated) + ". Something is depenetrating it, so the marker a returning "
                + "player lands on is not a place a body fits. That is a ship defect, not a "
                + "test one — find what is within 0.35 m of " + Fmt(start) + ".");

            float walked = 0f;
            for (int i = 0; i < route.Count; i++)
            {
                Leg leg = route[i];
                Vector3 from = cc.transform.position;
                float legLength = Flat(leg.To - from).magnitude;
                Assert.Greater(legLength, MinLegLength,
                    "Leg " + i + " (" + leg.Why + ") is only " + legLength.ToString("F2")
                    + " m long — the route collapsed, so this leg proves nothing. "
                    + "A layout constant probably moved; re-cut the waypoints.");

                // FRONT-LOADED, before a single frame is spent: a leg that crosses a bulkhead
                // line or the spine wall somewhere the layout says is solid is a bad ROUTE, and
                // it should be reported as one instead of as a mystery stall 600 frames later.
                // This is the check that would have named run 2's failure — "leg 0 crosses the
                // z=55 bulkhead at x=-3.57" — without moving the capsule at all.
                AssertLegClearsBulkheads(i, leg, from, cc.radius);
                AssertLegCrossesSpineOnlyAtDoorRows(i, leg, from, cc.radius);

                if (leg.Landmark != null)
                {
                    Transform mark = Require(all, leg.Landmark);
                    float before = Flat(mark.position - from).magnitude;
                    Assert.Greater(before, ArriveRadius,
                        "Leg " + i + " starts " + before.ToString("F2") + " m from "
                        + leg.Landmark + ", already inside the " + ArriveRadius
                        + " m arrival circle. Arriving would prove nothing but arithmetic.");
                }

                yield return WalkLeg(cc, leg.To, MaxStepsFor(legLength));
                walked += legLength;

                Vector3 at = cc.transform.position;
                float missed = Flat(leg.To - at).magnitude;
                Assert.LessOrEqual(missed, WaypointTol,
                    "LEG " + i + " BLOCKED: " + leg.Why + ".\n"
                    + "  from   " + Fmt(from) + "\n"
                    + "  toward " + Fmt(leg.To) + "\n"
                    + "  got to " + Fmt(at) + " — " + missed.ToString("F2")
                    + " m short, grounded=" + cc.isGrounded + ", after "
                    + walked.ToString("F0") + " m of route.\n"
                    + "  A capsule that stops here has hit something a ray does not see: this "
                    + "lane has at least 0.45 m of clearance to the nearest collider in the "
                    + "layout tables, and the capsule is 0.35 m in radius. Look at what is "
                    + "actually at " + Fmt(at) + ".");
                AssertStanding(cc, plane, leg.Why, i, "arrive");

                if (leg.Landmark == null) continue;
                Transform target = Require(all, leg.Landmark);
                float reached = Flat(target.position - at).magnitude;
                Assert.LessOrEqual(reached, ArriveRadius,
                    "LEG " + i + " reached its waypoint but not " + leg.Landmark + ": the capsule "
                    + "stands at " + Fmt(at) + ", " + reached.ToString("F2") + " m from "
                    + Fmt(target.position) + ". The landmark has moved away from the "
                    + "waypoint this route derives — re-read Tools/ship-hull/build_hull.py's "
                    + "PLACE table.");
            }

            Assert.Greater(walked, 150f,
                "The whole route came to only " + walked.ToString("F0")
                + " m. The ship is 80 m long and the route visits both beams twice; a short "
                + "route means the waypoints collapsed onto each other.");
        }

        // ------------------------------------------------------------------ walk driver

        /// <summary>Straight-line walk toward one waypoint under gravity, bounded. Steers toward
        /// the target every step rather than holding a fixed heading (the retired stair walk
        /// held one, because a stair only goes one way) but never searches: if the straight line
        /// is blocked the leg fails, which is the report we want.</summary>
        static IEnumerator WalkLeg(CharacterController cc, Vector3 target, int maxSteps)
        {
            float yVel = -1f;
            for (int i = 0; i < maxSteps; i++)
            {
                Vector3 to = Flat(target - cc.transform.position);
                if (to.magnitude <= WaypointTol) yield break;

                yVel = cc.isGrounded ? -1f : yVel + Gravity * Dt;
                // Clamp the step to what is left so the capsule settles ON the waypoint instead
                // of oscillating across it once it is inside one frame's travel.
                Vector3 planar = to.normalized * Mathf.Min(Speed * Dt, to.magnitude);
                cc.Move(planar + Vector3.up * yVel * Dt);
                Physics.SyncTransforms();
                yield return null;
            }
        }

        /// <summary>Drop the capsule onto the deck with no horizontal input.</summary>
        static IEnumerator Settle(CharacterController cc)
        {
            float yVel = 0f;
            for (int i = 0; i < 60; i++)
            {
                yVel = cc.isGrounded ? -1f : yVel + Gravity * Dt;
                cc.Move(Vector3.up * yVel * Dt);
                Physics.SyncTransforms();
                if (cc.isGrounded && i > 4) yield break;
                yield return null;
            }
        }

        /// <summary>Frame cap for a leg: the straight-line frames it needs at a fixed step, plus
        /// 60% for the steering the CharacterController does against a wall it slides on, plus a
        /// flat 60 for the settle at each end. A blocked capsule burns this and fails with a
        /// position instead of hanging the suite.</summary>
        static int MaxStepsFor(float distance)
        {
            return Mathf.CeilToInt(distance / (Speed * Dt) * 1.6f) + 60;
        }

        // ------------------------------------------------------------------ assertions

        /// <summary>Arrived means STANDING, not merely "the loop ran out somewhere near". The
        /// controller's rest height above the deck is height/2 + center.y; the tolerance is
        /// stepOffset + skinWidth, read off the controller rather than typed in, which is the
        /// largest legitimate vertical error a Move can leave behind. Anything outside it is the
        /// capsule on top of something, sunk into something, or falling.</summary>
        static void AssertStanding(CharacterController cc, float plane, string why, int leg, string stage)
        {
            float rest = plane + cc.height * 0.5f + cc.center.y;
            float tol = cc.stepOffset + cc.skinWidth;
            float y = cc.transform.position.y;
            Assert.LessOrEqual(Mathf.Abs(y - rest), tol,
                "LEG " + leg + " (" + why + ") — after " + stage + " the capsule is at y="
                + y.ToString("F3") + " but the deck plane is y=" + plane.ToString("F3")
                + ", so it should rest at y=" + rest.ToString("F3") + " (+/-" + tol.ToString("F2")
                + "). It is at " + Fmt(cc.transform.position) + ", grounded=" + cc.isGrounded
                + ". Standing on a prop, sunk in a collider, or through the deck.");
        }

        /// <summary>Guard the two z's the route derives from hatch positions rather than from
        /// row numbers: if a hatch is re-spaced out of its door row, the crossing quietly starts
        /// aiming at solid spine wall and the leg failure would blame the wrong thing.</summary>
        static void AssertInDoorRow(float z, int expectedRow)
        {
            int row = Mathf.FloorToInt(z / Cell);
            Assert.IsTrue(IsDoorRow(row) && row == expectedRow,
                "The route crosses the spine at z=" + z.ToString("F2") + ", which is row " + row
                + " — expected door row " + expectedRow + ". DOOR_ROWS in "
                + "Tools/ship-hull/build_hull.py is {1,3,4,8,11}; a hatch has been re-spaced out "
                + "of its door row, so this crossing now aims at solid spine wall.");
        }

        /// <summary>
        /// FRONT-LOADED ROUTE CHECK, run before the leg is walked: does this straight line cross
        /// a transverse bulkhead somewhere the generator says is closed?
        ///
        /// A leg may cross a PARTITIONS line in exactly two places. Through the CORRIDOR, which
        /// no partition panel ever spans; or, on one of the two OPEN_SPINE_FLANK lines, through
        /// the 3.75 m gap left where the px -1.25 / px 5.0 panel was skipped — that gap is
        /// walkable from the spine wall's outboard face out to the cell edge at x = +/-5.
        /// Anywhere else is a panel. The capsule's radius is included on both sides, because a
        /// centre-line that clears a gap by 0.1 m is a body that does not.
        ///
        /// This exists because a blocked leg reports where the capsule STOPPED, and "stopped at
        /// z 55.93" is a symptom whose cause is three files away. Failing here instead names the
        /// bulkhead, the crossing x and the rule, before a frame is spent.
        /// </summary>
        static void AssertLegClearsBulkheads(int index, Leg leg, Vector3 from, float radius)
        {
            foreach (float line in PartitionZ)
            {
                float x;
                if (!CrossesZ(from, leg.To, line, out x)) continue;
                float ax = Mathf.Abs(x);
                if (ax + radius <= CorridorHalf) continue;                       // down the spine
                bool throughFlank = IsOpenFlankLine(line)
                    && ax - radius >= SpineOuterX && ax + radius <= Cell;
                if (throughFlank) continue;                                      // through the gap
                Assert.Fail(
                    "BAD ROUTE, leg " + index + " (" + leg.Why + "): it crosses the z=" + line
                    + " bulkhead at x=" + x.ToString("F2") + ", where build_hull.py PARTITIONS "
                    + "emits a panel.\n"
                    + "  from " + Fmt(from) + " toward " + Fmt(leg.To) + "\n"
                    + "  A leg may only cross a bulkhead down the corridor (|x| + "
                    + radius.ToString("F2") + " <= " + CorridorHalf + ")"
                    + (IsOpenFlankLine(line)
                        ? ", or through this line's open spine flank (" + SpineOuterX + " < |x| < "
                          + Cell + ") — this crossing does neither."
                        : ". z=" + line + " is NOT in OPEN_SPINE_FLANK, so the corridor is the "
                          + "only way through it at all.")
                    + "\n  Re-cut this leg through the spine, or through a door row.");
            }

            float bx;
            if (CrossesZ(from, leg.To, BowStepZ, out bx) && Mathf.Abs(bx) + radius > Cell)
                Assert.Fail(
                    "BAD ROUTE, leg " + index + " (" + leg.Why + "): it crosses z=" + BowStepZ
                    + " at x=" + bx.ToString("F2") + ", but the hull steps in there (ROWS 13 -> 14) "
                    + "and puts wall across x[-10,-5] and x[5,10]. The bow narrows to x[-5,5]; "
                    + "come inboard before this line.");
        }

        /// <summary>
        /// The other half of the same idea: the spine wall is SOLID for the whole length of every
        /// row that is not in DOOR_ROWS, so a leg may only be inside the wall band on rows
        /// 1, 3, 4, 8 and 11. Sampled rather than solved, because a leg that enters the band and
        /// runs along it (rather than straight across) has to be legal at every z it occupies,
        /// not just where it first touched.
        /// </summary>
        static void AssertLegCrossesSpineOnlyAtDoorRows(int index, Leg leg, Vector3 from, float radius)
        {
            int steps = Mathf.Max(2, Mathf.CeilToInt(Flat(leg.To - from).magnitude / 0.25f));
            for (int s = 0; s <= steps; s++)
            {
                Vector3 p = Vector3.Lerp(from, leg.To, (float)s / steps);
                float ax = Mathf.Abs(p.x);
                if (ax + radius <= CorridorHalf) continue;   // clear inside the corridor
                if (ax - radius >= SpineOuterX) continue;    // clear outboard of the wall body
                int row = Mathf.FloorToInt(p.z / Cell);
                if (IsDoorRow(row)) continue;
                Assert.Fail(
                    "BAD ROUTE, leg " + index + " (" + leg.Why + "): at " + Fmt(p) + " it is inside "
                    + "the spine wall band (" + CorridorHalf + " < |x| < " + SpineOuterX
                    + ") on row " + row + ", which carries no door. DOOR_ROWS is {1,3,4,8,11} and "
                    + "rows 5-7 and 12-15 are solid spine wall for their entire length.\n"
                    + "  from " + Fmt(from) + " toward " + Fmt(leg.To) + "\n"
                    + "  Cross the spine on a door row, or stay outboard of x=" + SpineOuterX + ".");
            }
        }

        /// <summary>x where a straight leg crosses the plane z = line, or false when it does not.
        /// A leg running ALONG the line is not a crossing — it never has to pass through it.</summary>
        static bool CrossesZ(Vector3 from, Vector3 to, float line, out float x)
        {
            x = 0f;
            float span = to.z - from.z;
            if (Mathf.Abs(span) < 0.0001f) return false;
            if (line < Mathf.Min(from.z, to.z) || line > Mathf.Max(from.z, to.z)) return false;
            x = Mathf.Lerp(from.x, to.x, (line - from.z) / span);
            return true;
        }

        // ------------------------------------------------------------------ scene lookups

        /// <summary>Top of the deck, measured off the hull's own GFloor_* graybox rather than
        /// assumed to be zero, so the whole ship can be moved in Y without this test lying. The
        /// M101 naming contract Tools/scene-check already reads.</summary>
        static float FloorPlaneY(List<Transform> all)
        {
            float top = float.NegativeInfinity;
            int tiles = 0;
            foreach (Transform t in all)
            {
                if (t == null || !t.name.StartsWith("GFloor_")) continue;
                var col = t.GetComponent<Collider>();
                if (col == null || !col.enabled) continue;
                tiles++;
                if (col.bounds.max.y > top) top = col.bounds.max.y;
            }
            Assert.Greater(tiles, 0,
                "Ship - CoOp has no GFloor_* colliders — the hull did not load, so there is no "
                + "deck to walk and nothing below would have proved anything.");
            return top;
        }

        /// <summary>Named object from the ACTIVE scene, inactive included. GameObject.Find would
        /// miss a destination somebody disabled, which is a failure worth reporting rather than
        /// a NullReferenceException halfway down a leg.</summary>
        static Transform Require(List<Transform> all, string name)
        {
            foreach (Transform t in all)
                if (t != null && t.name == name) return t;
            Assert.Fail("Ship - CoOp has no '" + name + "'. It is a destination of the traversal "
                + "route (Tools/ship-hull/build_hull.py PLACE), so the walk cannot be run.");
            return null;
        }

        // ------------------------------------------------------------------ small helpers

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        static string Fmt(Vector3 v)
        {
            return "(" + v.x.ToString("F2") + ", " + v.y.ToString("F2") + ", " + v.z.ToString("F2") + ")";
        }
    }
}
