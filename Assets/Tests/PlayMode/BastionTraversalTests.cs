using System.Collections;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>
    /// THE LOOP IS PHYSICALLY REACHABLE. A wave BEGINS at the Hatch and ENDS at a terminal
    /// (Docs/Design/0-Gameplay.md), and every other decision on the 227 m spine — the
    /// far-weighted spawns, the enemy speeds set under the player's walk, the one-lane
    /// geometry — is priced on the player having somewhere to be. Nothing else in the suite
    /// proves the player can GET there. HatchStartLinePlayModeTests, the chain fixtures and
    /// ZoneTwoLoop all call the host-side verbs from wherever the scene happened to drop
    /// them, so every one of them would keep passing on a build where a wall, a console or a
    /// door leaf sat across the corridor and the walk was impossible. This fixture drives the
    /// real player CharacterController through the real colliders, under gravity, from the
    /// Hatch to T2, and on to T5 and Master Control as each zone earns its door.
    ///
    /// It pins the REFUSAL as hard as the arrival, and that half is what a state-machine test
    /// structurally cannot see: D1 is a physical leaf as well as a graph edge, and a capsule
    /// that strolls up the lane through a shut door is a defect no amount of
    /// IsDoorOpen == false will ever catch.
    ///
    /// WHAT IT CANNOT SEE. It does not fight. The zones are run through BastionRunDrive, which
    /// STRIPS waves rather than killing them, and the lane is emptied before every leg — so
    /// this says nothing about difficulty, spawn pressure, the trickle, enemy pathing, or
    /// whether the walk is survivable. Only that it is walkable. It also walks the lane
    /// directly instead of pathing it: the Bastion's baked NavMesh is the ENEMIES' surface and
    /// a CharacterController never touches it, so a route only a NavMeshAgent could find would
    /// be no evidence about the player at all. And it never presses E at anything — the
    /// shutoffs go through the host verbs — so it says nothing about interact reach or prompts
    /// (HatchStartLinePlayModeTests owns those).
    /// </summary>
    public class BastionTraversalTests
    {
        // ------------------------------------------------------------------ the lane

        /// <summary>The spine's centreline. Every room on it is authored at x=0 and every
        /// doorway is a 3 m aperture at along=0 (Tools/bastion-sketch/bastion-coop-layout.txt,
        /// BastionSegment's opening width), so the walkable lane really is this narrow a line:
        /// a metre and a bit either side of it and the capsule meets wall, not gap.</summary>
        const float LaneX = 0f;

        /// <summary>How far off <see cref="LaneX"/> a landmark may sit before this fixture's
        /// straight-line route stops being the honest one.</summary>
        const float LaneHalfWidth = 0.5f;

        /// <summary>Lateral sidestep used to get past a field console. The Desk is 1.6 m wide
        /// and stands dead centre on the lane at its room's origin, so the 0.35 m capsule needs
        /// roughly 1.25 m of offset to clear it. 3 m clears with margin and is still 5 m inside
        /// a Half room's wall. This is not a workaround: the console is furniture in the
        /// corridor and walking round it is what a player does.</summary>
        const float BypassX = 3f;

        // ------------------------------------------------------------------ the bar

        /// <summary>ARRIVED means this close to the target on the XZ plane. Inside the
        /// Interactor's 3.2 m range, so a capsule that arrived could actually press the button;
        /// and outside the console's own footprint (0.35 m half-depth plus the capsule's
        /// 0.43 m), so a leg ends standing IN FRONT of the desk rather than jammed against it,
        /// and no assertion here depends on how a desk collider resolves.</summary>
        const float ArriveRadius = 2f;

        /// <summary>Intermediate waypoints are open floor, so they are held tight: the sidestep
        /// has to actually put the capsule back on the centreline before it is asked to thread
        /// a 3 m doorway. Comfortably larger than one step (<see cref="WalkSpeed"/> ×
        /// <see cref="MaxStep"/> = 0.25 m), so a waypoint can never be overshot into a hunting
        /// oscillation.</summary>
        const float WaypointRadius = 0.5f;

        /// <summary>How far from the height the traversal STARTED at the capsule may end and
        /// still count as on the floor. The Bastion is single storey and every room floor is
        /// y=0, so this band is generous — and it still catches the only thing it is here for:
        /// a capsule that fell out of the world and "arrived" from underneath.
        /// The band is the assertion rather than CharacterController.isGrounded, which is a
        /// per-Move contact flag and can read false on a frame whose sweep resolved without a
        /// downward touch. Grounded rides along in every failure message instead.</summary>
        const float FloorBand = 1f;

        // ------------------------------------------------------------------ the drive

        /// <summary>The design walk (FirstPersonController's remarks: the 5 m/s walk the
        /// Default's 4.6 m/s chase is deliberately set under). Pinned rather than read off the
        /// component, because this fixture is about whether the geometry lets a body through
        /// and a re-tuned speed must not silently re-tune the leg budgets below.</summary>
        const float WalkSpeed = 5f;

        const float Gravity = -20f;

        /// <summary>Per-frame step clamp. cc.Move sweeps, so a long frame cannot tunnel the
        /// capsule through D1's leaf — but it CAN carry it two metres into a passage door's 5 m
        /// proximity band and out the far side of that door's 0.15 s poll, and the whole point
        /// of the D1 leg is that a door gets its chance to matter. The floor matters too: a
        /// zero deltaTime on the first frame after a scene load would spin a leg without
        /// moving it.</summary>
        const float MinStep = 0.005f;

        const float MaxStep = 0.05f;

        /// <summary>A leg gives up after this long with no net gain on its target. This is what
        /// makes a blocked capsule FAIL FAST instead of burning its whole budget, and it is how
        /// the D1 leg detects the refusal. Well over a proximity leaf's 0.15 s poll plus its
        /// 8 m/s slide, so a door that is working can never be read as a wall.</summary>
        const float StallSeconds = 1.5f;

        /// <summary>Gain that counts as progress. Under this and the stall clock keeps running,
        /// so a capsule grinding sideways along a wall is stalled, not walking.</summary>
        const float ProgressMetres = 0.05f;

        /// <summary>Leg budget: this many times the straight-line walk, plus a fixed floor. It
        /// is the bound that actually ends a runaway leg — every frame adds at least
        /// <see cref="MinStep"/> to the clock, so the budget is always reachable.</summary>
        const float BudgetSlack = 2f;

        const float BudgetFloorSeconds = 3f;

        /// <summary>Absolute frame ceiling, the belt to the budget's braces — PlayTestUtil's
        /// house rule is that every loop has both a time bound and a frame bound. Each leg
        /// DERIVES its own cap from its budget (see Walk) rather than using this directly: a
        /// flat cap silently truncates the longest leg the moment the editor renders frames
        /// shorter than MinStep, which is exactly what an unfocused Game view does.</summary>
        const int MaxLegFrames = 20000;

        /// <summary>Frames of pure gravity after the capsule is placed at the Hatch, so the
        /// traversal starts from a settled ride height rather than mid-drop.</summary>
        const int SettleFrames = 20;

        /// <summary>Arm's length north of the Hatch box — where a player stands to press T.
        /// Added to the Hatch collider's own half-depth, so it is a standoff from the thing in
        /// the scene rather than a number copied out of the emitter.</summary>
        const float HatchStandoff = 1.5f;

        /// <summary>How far short of a locked door's plane a capsule STOPPED BY THE LEAF sits:
        /// the blocker is a metre deep and the capsule is 0.43 m wide, so about 0.9 m. This is
        /// the band that separates "the door stopped it" from "something two rooms back did",
        /// and it is why the D1 assertion cannot be satisfied by a capsule that fell through
        /// the floor at z=0 and never got near the gate.</summary>
        const float DoorReachMetres = 2.5f;

        // ------------------------------------------------------------------ fixture state

        string _campaignJson;

        /// <summary>Ride height the traversal started at, for the floor band.</summary>
        float _rideY;

        /// <summary>Metres of lane actually covered, summed across every leg.</summary>
        float _walked;

        /// <summary>The leg that just ran. Coroutines cannot return and an out-parameter cannot
        /// cross a yield, so the drive files its report here and the caller reads it.</summary>
        Leg _leg;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Snapshot only a session that ALREADY exists; never force one into being. See the
            // remarks on ZoneTwoLoopPlayModeTests — a fixture that calls EnsureLoaded here
            // pulls the developer's mid-demo campaign into every fixture that runs after it.
            _campaignJson = GameSession.Meta == null ? null : GameSession.Meta.ToSnapshotJson();
            _walked = 0f;
            _leg = null;
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

        // ------------------------------------------------------------------ the traversal

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator WalkingTheSpine_HatchToT2_ThenD1Refuses_ThenEachShutoffOpensTheLaneOn()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.BastionCoop);
            yield return EmbarkPlainAtWaveOne();

            GameController gc = Object.FindFirstObjectByType<GameController>();
            Assert.IsNotNull(gc, "the Bastion scene has no GameController.");
            BastionLayout layout = Object.FindFirstObjectByType<BastionLayout>();
            Assert.IsNotNull(layout, "the Bastion scene has no BastionLayout.");
            RunSim sim = gc.Sim;
            Assert.AreEqual(AssaultState.Prep, sim.Assault, "boarding lands in Prep.");

            PlayerHealth player = PlayerRegistry.Local;
            Assert.IsNotNull(player,
                "the Bastion scene registered no local player, so there is no capsule to walk.");
            var cc = player.GetComponent<CharacterController>();
            Assert.IsNotNull(cc,
                "the player rig has no CharacterController — the walk this fixture is about "
                + "cannot be driven through anything else.");
            HatchStation hatch = HatchStation.Instance;
            Assert.IsNotNull(hatch, "the Bastion has no Hatch, so the loop has no start line.");

            string zoneOneId = DemoZones.TerminalIdAt(1, 0);
            string zoneTwoId = DemoZones.TerminalIdAt(2, 0);
            string doorOneId = DemoZones.DoorId(1);
            string doorTwoId = DemoZones.DoorId(2);

            BastionTerminal t2 = layout.FindTerminal(zoneOneId);
            BastionTerminal t5 = layout.FindTerminal(zoneTwoId);
            BastionTerminal master = layout.FindMaster();
            Assert.IsNotNull(t2, "the Bastion has no terminal " + zoneOneId + ".");
            Assert.IsNotNull(t5, "the Bastion has no terminal " + zoneTwoId + ".");
            Assert.IsNotNull(master, "the Control Room has no Master terminal.");

            BastionDoor d1 = FindDoor(doorOneId);
            BastionDoor d3 = FindDoor(doorTwoId);
            Assert.IsNotNull(d1, "the Bastion has no door leaf " + doorOneId + " in the scene.");
            Assert.IsNotNull(d3, "the Bastion has no door leaf " + doorTwoId + " in the scene.");

            // THE ROUTE IS AN ASSUMPTION, SO IT IS AN ASSERTION. This fixture walks a straight
            // line up x=0 rather than pathing, so a re-authored spine has to fail HERE, with a
            // readable message, instead of as a capsule wedged in a wall ninety metres on.
            AssertOnTheLane("the Hatch", hatch.transform.position);
            AssertOnTheLane(zoneOneId, t2.transform.position);
            AssertOnTheLane(zoneTwoId, t5.transform.position);
            AssertOnTheLane("Master Control", master.transform.position);
            AssertOnTheLane(doorOneId, d1.transform.position);
            AssertOnTheLane(doorTwoId, d3.transform.position);
            AssertInOrder("the Hatch", hatch.transform.position, zoneOneId, t2.transform.position);
            AssertInOrder(zoneOneId, t2.transform.position, doorOneId, d1.transform.position);
            AssertInOrder(doorOneId, d1.transform.position, zoneTwoId, t5.transform.position);
            AssertInOrder(zoneTwoId, t5.transform.position, doorTwoId, d3.transform.position);
            AssertInOrder(doorTwoId, d3.transform.position,
                "Master Control", master.transform.position);

            StandAtTheHatch(player, cc, hatch);
            yield return SettleOnTheFloor(player, cc);

            // ---- LEG 1: the walk the whole spine was built for ---------------------------
            // The first thing every player does, on a fresh embark with nothing earned. If this
            // fails the loop cannot be played at all, whatever the state machine says.
            Assert.IsFalse(layout.IsTerminalDone(zoneOneId),
                zoneOneId + " already reads done on a fresh embark, so this is not the walk the "
                + "player meets.");
            ClearTheLane();
            yield return Walk("leg 1 · Hatch → " + zoneOneId, t2.transform.position, ArriveRadius);
            AssertArrived();

            // ---- LEG 2: onto D1's centreline, around zone 1's console --------------------
            // The console is a 1.6 m desk standing on the lane; stepping round it and back is
            // the whole of this leg, and it has to happen before the refusal below means
            // anything — a capsule stopped by a DESK would read exactly like a capsule stopped
            // by a door if it never got onto the aperture's centreline.
            yield return WalkAroundConsole(zoneOneId, t2.transform.position);

            // ---- LEG 3: D1 refuses the lane ---------------------------------------------
            Assert.IsFalse(layout.IsDoorOpen(doorOneId),
                doorOneId + " is open before zone 1 was ever closed at its terminal.");
            Assert.IsFalse(d1.IsOpen,
                doorOneId + "'s leaf reads open while the graph has it shut.");
            yield return Walk("leg 3 · " + doorOneId + " refuses", t5.transform.position,
                ArriveRadius);
            Leg refused = _leg;
            Assert.IsFalse(refused.Arrived,
                "THE CAPSULE WALKED THROUGH A SHUT DOOR. " + doorOneId + " is the gate zone 1 "
                + "has not earned yet, and the player just strolled up the lane past it. "
                + refused.Report());
            float d1PlaneZ = d1.transform.position.z;
            Assert.Less(refused.End.z, d1PlaneZ,
                "the capsule ended NORTH of " + doorOneId + "'s plane (z="
                + d1PlaneZ.ToString("F1") + ") without arriving — it is through the gate and "
                + "merely stuck beyond it. " + refused.Report());
            Assert.Greater(refused.End.z, d1PlaneZ - DoorReachMetres,
                "the capsule stopped " + (d1PlaneZ - refused.End.z).ToString("F1")
                + " m short of " + doorOneId + ", too far back to be the leaf — something else "
                + "in that room is blocking the lane, and the refusal this leg claims to prove "
                + "is not the door's. " + refused.Report());
            Assert.Greater(refused.TravelledXZ, 1.5f,
                "the capsule barely moved before stopping, so this leg proved nothing about "
                + doorOneId + " at all. " + refused.Report());
            AssertOnTheFloor();

            // ---- LEG 4: the zone is closed at its terminal, and the lane runs on ---------
            // The shutoff is the achievement (Docs/Design/0-Gameplay.md) and the ONLY thing
            // that opens D1 now the Convert has left the demo. BastionRunDrive runs the whole
            // composite and presses the off switch through the host verbs — this fixture
            // supplies the walk, not the fight.
            yield return BastionRunDrive.RunZoneComposite(gc, sim, layout, 1);
            Assert.IsTrue(layout.IsTerminalDone(zoneOneId),
                zoneOneId + " did not complete on its shutoff.");
            Assert.IsTrue(layout.IsDoorOpen(doorOneId),
                "closing zone 1 at " + zoneOneId + " must open " + doorOneId + ".");
            Assert.IsTrue(d1.IsOpen,
                "the graph opened " + doorOneId + " but the leaf in the scene never heard — the "
                + "lane is still physically shut, and the walk below would fail for a reason "
                + "every state-machine test reports as fine.");
            Assert.IsFalse(player.IsDead, "the player did not survive zone 1's composite.");
            ClearTheLane();
            yield return Walk("leg 4 · " + doorOneId + " → " + zoneTwoId, t5.transform.position,
                ArriveRadius);
            AssertArrived();
            Assert.Greater(_leg.End.z, d1PlaneZ,
                "the capsule reached " + zoneTwoId + " without ever crossing " + doorOneId
                + "'s plane, which is not possible on a one-lane spine — read the route "
                + "assertions above. " + _leg.Report());

            // ---- LEG 5: zone 2 closes the control door, and the lane runs to the end -----
            yield return BastionRunDrive.RunZoneComposite(gc, sim, layout, 2);
            Assert.IsTrue(layout.IsDoorOpen(doorTwoId),
                "closing zone 2 at " + zoneTwoId + " must open the control door " + doorTwoId
                + ".");
            Assert.IsTrue(d3.IsOpen,
                "the graph opened " + doorTwoId + " but its leaf is still down, so the Control "
                + "Room is unreachable however complete the run reads.");
            Assert.IsFalse(player.IsDead, "the player did not survive zone 2's composite.");
            ClearTheLane();
            yield return WalkAroundConsole(zoneTwoId, t5.transform.position);
            yield return Walk("leg 5 · " + zoneTwoId + " → Master Control",
                master.transform.position, ArriveRadius);
            AssertArrived();
            Assert.Greater(_leg.End.z, d3.transform.position.z,
                "the capsule reached Master Control without crossing " + doorTwoId + "'s plane. "
                + _leg.Report());

            // The headline, in one number: the spine was walked, not skipped down. Measured
            // against the scene's own Hatch-to-Master span rather than the 227 m in the design
            // doc, so re-scaling the kit moves the bar with it. Under 1.0 because every leg
            // stops an ArriveRadius short of its console and the sidesteps are lateral.
            float spine = master.transform.position.z - hatch.transform.position.z;
            Assert.Greater(_walked, spine * 0.8f,
                "the traversal covered only " + _walked.ToString("F0") + " m of a "
                + spine.ToString("F0") + " m spine — the legs passed but the walk did not "
                + "happen, so something is arriving without travelling.");
        }

        // ------------------------------------------------------------------ arrange

        /// <summary>The shape the loop is played in: ship tutorial behind us, embarking plain
        /// at Wave 1 with zone 1 unplayed and the demo's zone rules live. Adopted BEFORE the
        /// scene loads because GameController.Awake hands this exact MetaState instance to the
        /// RunSim, and a swap afterwards leaves the Run writing to an object nothing reads.
        /// The pot stays empty: this fixture never opens a bank window.</summary>
        static IEnumerator EmbarkPlainAtWaveOne()
        {
            MetaState checkpoint = GameController.BuildDemoCheckpoint(1);
            Assert.IsNotNull(checkpoint, "digit 1 no longer builds a checkpoint.");
            GameSession.AdoptDevState(checkpoint);
            GameSession.PendingStartWave = 1;

            yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.BastionCoop);
            yield return PlayTestUtil.WaitUntil(
                () =>
                {
                    GameController g = Object.FindFirstObjectByType<GameController>();
                    return g != null && g.Sim != null;
                }, 10f);
        }

        /// <summary>Put the capsule on the start line: on the lane, an arm's length clear of
        /// the Hatch box, facing up the corridor. The CharacterController is disabled across
        /// the write and Physics.SyncTransforms called before it comes back — the rig's own
        /// teleport idiom (GameController's demo-fight stand), because a live controller caches
        /// its position and quietly undoes a transform write on the next Move.</summary>
        static void StandAtTheHatch(PlayerHealth player, CharacterController cc, HatchStation hatch)
        {
            var box = hatch.GetComponent<Collider>();
            float clear = box != null ? box.bounds.extents.z : 1f;
            var at = new Vector3(
                LaneX,
                player.transform.position.y,
                hatch.transform.position.z + clear + HatchStandoff);

            cc.enabled = false;
            player.transform.SetPositionAndRotation(at, Quaternion.LookRotation(Vector3.forward));
            Physics.SyncTransforms();
            cc.enabled = true;
        }

        /// <summary>Fall onto the Hatch bay floor and record the ride height every later leg is
        /// measured against. Doubles as the first assertion of the traversal: if there is no
        /// floor under the start line there is no walk to test.</summary>
        IEnumerator SettleOnTheFloor(PlayerHealth player, CharacterController cc)
        {
            float yVel = -1f;
            for (int i = 0; i < SettleFrames; i++)
            {
                float dt = Mathf.Clamp(Time.deltaTime, MinStep, MaxStep);
                yVel = cc.isGrounded ? -1f : yVel + Gravity * dt;
                cc.Move(Vector3.up * yVel * dt);
                yield return null;
            }

            _rideY = player.transform.position.y;
            Assert.IsTrue(cc.isGrounded,
                "the capsule found no floor at the Hatch (ended at "
                + player.transform.position.ToString("F2") + ") — the loop's start line is not "
                + "somewhere a player can stand.");
        }

        // ------------------------------------------------------------------ the walk

        /// <summary>One straight leg: drive the CharacterController at <paramref name="target"/>
        /// under gravity until it arrives, stalls, or runs out of budget. Files its report in
        /// <see cref="_leg"/> — the CALLER decides whether arriving was the point, because the
        /// D1 leg wants the opposite answer from every other one.
        /// Shape borrowed from BastionCoopStairWalkTests' WalkFrom, the retired storey-one
        /// climb: constant gravity, cc.Move, bounded iterations.</summary>
        IEnumerator Walk(string label, Vector3 target, float radius)
        {
            PlayerHealth player = PlayerRegistry.Local;
            Assert.IsNotNull(player, label + ": the local player went missing mid-traversal.");
            var cc = player.GetComponent<CharacterController>();
            Assert.IsNotNull(cc, label + ": the player rig lost its CharacterController.");
            Assert.IsFalse(player.IsDead, label + ": the player was already dead at this leg.");

            Vector3 start = player.transform.position;
            var leg = new Leg { Label = label, Start = start, Target = target, Radius = radius };
            _leg = leg;

            float gap = FlatDistance(start, target);
            // A leg that begins inside its own tolerance would pass without a step being taken,
            // which proves nothing about the corridor.
            Assert.Greater(gap, radius + 1f,
                label + ": the capsule was already " + gap.ToString("F1") + " m from its target, "
                + "inside the " + radius.ToString("F1") + " m arrival radius plus a metre. This "
                + "leg cannot show anything.");

            Vector3 heading = Flat(target - start).normalized;
            player.transform.rotation = Quaternion.LookRotation(heading);
            cc.Move(Vector3.zero); // depenetrate before the first real step

            float budget = gap / WalkSpeed * BudgetSlack + BudgetFloorSeconds;
            // Frames enough to spend that whole budget at the smallest step the clamp allows,
            // and a little over. Derived, because a flat cap is a distance limit in disguise.
            int frameCap = Mathf.Min(MaxLegFrames, Mathf.CeilToInt(budget / MinStep) + 100);
            float best = gap;
            float sinceProgress = 0f;
            float yVel = -1f;

            while (leg.Frames < frameCap)
            {
                Vector3 to = Flat(target - player.transform.position);
                float remaining = to.magnitude;
                if (remaining <= radius)
                {
                    leg.Arrived = true;
                    leg.Ending = "arrived";
                    break;
                }

                float dt = Mathf.Clamp(Time.deltaTime, MinStep, MaxStep);
                yVel = cc.isGrounded ? -1f : yVel + Gravity * dt;
                cc.Move((to / remaining * WalkSpeed + Vector3.up * yVel) * dt);
                leg.Frames++;
                leg.Seconds += dt;

                float now = FlatDistance(player.transform.position, target);
                if (now < best - ProgressMetres)
                {
                    best = now;
                    sinceProgress = 0f;
                }
                else
                {
                    sinceProgress += dt;
                }

                if (sinceProgress >= StallSeconds)
                {
                    leg.Ending = "stopped dead for " + StallSeconds.ToString("F1") + " s";
                    break;
                }
                if (leg.Seconds >= budget)
                {
                    leg.Ending = "out of budget after " + budget.ToString("F0") + " s";
                    break;
                }
                yield return null;
            }

            if (leg.Ending == null) leg.Ending = "hit the " + frameCap + " frame cap";
            leg.End = player.transform.position;
            leg.RemainingXZ = FlatDistance(leg.End, target);
            leg.TravelledXZ = FlatDistance(leg.Start, leg.End);
            leg.Grounded = cc.isGrounded;
            _walked += leg.TravelledXZ;
        }

        /// <summary>Step off the lane, past a field console, and back onto the lane clear of it.
        /// Three straight legs rather than one diagonal on purpose: a diagonal from the
        /// console's south face clips the desk corner, and a capsule that ended a bypass on the
        /// wrong side of the centreline meets wall at the next 3 m aperture.</summary>
        IEnumerator WalkAroundConsole(string terminalId, Vector3 console)
        {
            string tag = "bypass · " + terminalId;
            var aside = new Vector3(BypassX, console.y, console.z - ArriveRadius);
            var past = new Vector3(BypassX, console.y, console.z + ArriveRadius + 2f);
            var back = new Vector3(LaneX, console.y, past.z);

            yield return Walk(tag + " · step aside", aside, WaypointRadius);
            AssertArrived();
            yield return Walk(tag + " · step past", past, WaypointRadius);
            AssertArrived();
            yield return Walk(tag + " · back on the lane", back, WaypointRadius);
            AssertArrived();
        }

        // ------------------------------------------------------------------ assertions

        void AssertArrived()
        {
            Leg leg = _leg;
            Assert.IsTrue(leg.Arrived, "THE LANE IS BLOCKED. " + leg.Report());
            // Walk refuses a target that was inside tolerance to begin with, but a leg could
            // still "arrive" by being shoved there by depenetration. Half a metre of honest
            // travel is the floor.
            Assert.Greater(leg.TravelledXZ, 0.5f,
                "the capsule arrived without walking. " + leg.Report());
            AssertOnTheFloor();
        }

        void AssertOnTheFloor()
        {
            Leg leg = _leg;
            Assert.Less(Mathf.Abs(leg.End.y - _rideY), FloorBand,
                "the capsule left the storey: it started the traversal at y="
                + _rideY.ToString("F2") + " and ended at y=" + leg.End.y.ToString("F2")
                + ". The Bastion is single storey, so this is a fall through the world, not a "
                + "walk. " + leg.Report());
        }

        /// <summary>The spine is one straight lane at x=0 and every doorway on it is a 3 m
        /// aperture authored at along=0. This fixture WALKS that line rather than pathing it,
        /// so a landmark that moved off the centreline needs a new route, not a wider
        /// tolerance — and must say so here rather than ninety metres up the corridor.</summary>
        static void AssertOnTheLane(string what, Vector3 at)
        {
            Assert.Less(Mathf.Abs(at.x - LaneX), LaneHalfWidth,
                what + " sits at x=" + at.x.ToString("F2") + ", off the spine's centreline. "
                + "This test walks x=" + LaneX.ToString("F0") + " straight up the corridor; a "
                + "layout that moved off it needs a route, not a tolerance.");
        }

        /// <summary>North is +Z and the loop runs Hatch → T2 → D1 → T5 → D3 → Master. A spine
        /// re-ordered behind this fixture's back would send a leg the wrong way.</summary>
        static void AssertInOrder(string first, Vector3 a, string second, Vector3 b)
        {
            Assert.Less(a.z, b.z,
                second + " (z=" + b.z.ToString("F1") + ") is no longer north of " + first
                + " (z=" + a.z.ToString("F1") + "). The traversal's leg order is the loop's "
                + "order, and the spine has been re-authored under it.");
        }

        // ------------------------------------------------------------------ scene helpers

        /// <summary>Empty the corridor of bodies before a walk. This fixture proves the lane is
        /// WALKABLE, not survivable: a gatekeeper parked in a 3 m aperture would fail a leg for
        /// a reason that belongs to the fight, and the fight is BastionCombatAndDeathTests'.
        /// The composites BastionRunDrive runs already strip their own rosters — this is the
        /// belt for whatever the trickle or a mid-frame emit left standing.</summary>
        static void ClearTheLane()
        {
            WaveSpawner spawner = Object.FindFirstObjectByType<WaveSpawner>();
            // Unity fake-null: a spawner torn down with the scene must read as absent.
            if (spawner != null) spawner.DespawnAllLive();
        }

        /// <summary>The live leaf for a door id. BastionLayout answers for the GRAPH; what this
        /// fixture has to interrogate is the collider standing in the room, and only the
        /// component carries that.</summary>
        static BastionDoor FindDoor(string doorId)
        {
            BastionDoor[] doors = Object.FindObjectsByType<BastionDoor>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < doors.Length; i++)
            {
                BastionDoor door = doors[i];
                if (door != null && door.DoorId == doorId) return door;
            }
            return null;
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        static float FlatDistance(Vector3 a, Vector3 b)
        {
            return Flat(a - b).magnitude;
        }

        /// <summary>What one leg did, in enough detail to diagnose a wall from the message
        /// alone: where it started, where it actually REACHED, why it stopped, and how far
        /// short it was. "Did not arrive" on its own cannot tell a shut door from a hole in the
        /// floor.</summary>
        sealed class Leg
        {
            public string Label;
            public Vector3 Start;
            public Vector3 End;
            public Vector3 Target;
            public float Radius;
            public bool Arrived;
            public string Ending;
            public float TravelledXZ;
            public float RemainingXZ;
            public float Seconds;
            public int Frames;
            public bool Grounded;

            public string Report()
            {
                return "[" + Label + "] started " + Start.ToString("F1") + ", REACHED "
                    + End.ToString("F1") + " — " + Ending + ". Target " + Target.ToString("F1")
                    + " (radius " + Radius.ToString("F1") + " m), still "
                    + RemainingXZ.ToString("F1") + " m short on XZ. Covered "
                    + TravelledXZ.ToString("F1") + " m in " + Seconds.ToString("F1") + " s over "
                    + Frames + " frames, grounded=" + Grounded + ".";
            }
        }
    }
}
