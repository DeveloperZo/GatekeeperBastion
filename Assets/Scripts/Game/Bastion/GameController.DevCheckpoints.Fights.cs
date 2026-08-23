using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;
using UnityEngine.AI;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): apply an F12 Bastion-fight checkpoint
    /// after the run tethers — restore terminals, seed Unbanked, place the player, and for
    /// digit 7 start the elite Hatch window.</summary>
    public sealed partial class GameController
    {
        const float FightStandBack = 2.2f;
        const float FightNavSample = 3.5f;
        const float FloorProbeUp = 0.5f;
        const float FloorProbeDown = 2.5f;

        /// <summary>Consumes <see cref="GameSession.PendingDemoFight"/>. No-op when 0 or the
        /// digit is a Ship checkpoint. Wave-1 embark skips terminal restore in Start, so
        /// fight digits restore CompletedTerminalIds here.</summary>
        void ApplyPendingDemoFight()
        {
            int digit = GameSession.PendingDemoFight;
            GameSession.PendingDemoFight = 0;
            if (!IsDemoFightCheckpoint(digit) || Sim == null) return;

            if (_layout != null && Meta != null)
                _layout.RestoreCompletedTerminals(Meta.CompletedTerminalIds);

            switch (digit)
            {
                case 2:
                    Sim.SeedBankPile(HatchBank.BankCap(2), 2);
                    PlacePlayerAtHatch();
                    break;
                case 4:
                    Sim.SeedBankPile(HatchBank.BankCap(5) - HatchBank.BankCap(2), 3,
                        WaveDirector.MaxWaves);
                    PlacePlayerAtHatch();
                    break;
                case 6:
                    PlacePlayerAtMaster();
                    break;
                case 7:
                    ApplyFinaleHatchFight();
                    break;
            }

            RestoreHatchAndPlayer();
            Log.Event("Dev", "demo fight " + digit + " applied — unbanked=" + Sim.Unbanked
                + " wavesSinceBank=" + Sim.WavesSinceBank
                + " hatchCarry=" + Sim.HatchCarry);
        }

        void ApplyFinaleHatchFight()
        {
            if (_layout != null)
                _masterTerminal = _layout.FindMaster();
            if (!Sim.SkipToMasterHatchHalf())
            {
                Log.Warn(Tag, "finale checkpoint — SkipToMasterHatchHalf refused");
                PlacePlayerAtMaster();
                return;
            }
            PlacePlayerAtHatch();
        }

        void PlacePlayerAtHatch()
        {
            var hatch = HatchStation.Instance;
            if (hatch == null)
            {
                Log.Warn(Tag, "demo fight — no HatchStation to stand at");
                return;
            }
            // Hatch forward is +Z into the rooms. Origin is the face toward the rooms, not the
            // transform centre (1.5 m up / inside the hull) — that was how F12-2/4 dropped the
            // CharacterController past the hull through the floor.
            Vector3 roomSide = hatch.transform.position + hatch.transform.forward * FightStandBack;
            Vector3 face = hatch.ClosestSurfacePoint(roomSide);
            PlacePlayerNear(face, hatch.transform.forward, preferRoomSide: true);
        }

        void PlacePlayerAtMaster()
        {
            BastionTerminal master = _layout != null ? _layout.FindMaster() : null;
            if (master == null)
            {
                Log.Warn(Tag, "demo fight — no Master terminal to stand at");
                return;
            }
            PlacePlayerNear(master.transform.position, master.transform.forward,
                preferRoomSide: false);
        }

        /// <summary>Stand on NavMesh near <paramref name="origin"/>, facing it. XZ from NavMesh;
        /// Y is the Boarder's authored spawn height. Writing NavMesh Y into a CharacterController
        /// whose centre is (0,0,0) put the capsule inside the deck and fell through on the first
        /// physics step — same class as ShipTeleporter / DemoShipDirector (2026-08-18).</summary>
        void PlacePlayerNear(Vector3 origin, Vector3 forward, bool preferRoomSide)
        {
            var player = PlayerRegistry.Local;
            if (player == null) return;

            Vector3 at;
            if (!TryStandOnNavMesh(origin, forward, preferRoomSide, out at))
            {
                Log.Warn(Tag, "demo fight — no walkable stand near origin; leaving player at spawn");
                return;
            }

            // Authored Bastion spawn Y is the only pivot height known to clear the deck for
            // this CharacterController (height 1.8, centre 0 → feet at pivot−0.9).
            at.y = player.transform.position.y;

            var cc = player.GetComponent<CharacterController>();
            if (!HasPhysicsFloorUnder(at, cc))
            {
                Log.Warn(Tag, "demo fight — stand has no physics floor at "
                    + at.ToString("F2") + "; leaving player at spawn");
                return;
            }

            Vector3 look = origin - at;
            look.y = 0f;
            float yaw = look.sqrMagnitude > 0.01f
                ? Quaternion.LookRotation(look).eulerAngles.y
                : player.transform.eulerAngles.y;

            var fpc = player.GetComponent<FirstPersonController>();
            if (cc != null) cc.enabled = false;
            player.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            Physics.SyncTransforms();
            if (cc != null) cc.enabled = true;
            if (fpc != null) fpc.ClearFallVelocity();

            if (!HasPhysicsFloorUnder(player.transform.position, cc))
            {
                Log.Warn(Tag, "demo fight — post-place floor check failed at "
                    + player.transform.position.ToString("F2"));
            }
        }

        static bool TryStandOnNavMesh(Vector3 origin, Vector3 forward, bool preferRoomSide,
            out Vector3 at)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);

            // Room-side first. −forward is past the hatch hull (z &lt; −60 on Bastion) and must
            // never win when a room-side probe exists.
            Vector3[] probes = preferRoomSide
                ? new[]
                {
                    origin + forward * FightStandBack,
                    origin + right * FightStandBack,
                    origin - right * FightStandBack,
                    origin,
                }
                : new[]
                {
                    origin + forward * FightStandBack,
                    origin + right * FightStandBack,
                    origin - right * FightStandBack,
                    origin - forward * FightStandBack,
                    origin,
                };

            for (int i = 0; i < probes.Length; i++)
            {
                if (!NavMesh.SamplePosition(probes[i], out NavMeshHit hit, FightNavSample,
                        NavMesh.AllAreas))
                    continue;
                if (preferRoomSide)
                {
                    Vector3 delta = hit.position - origin;
                    delta.y = 0f;
                    if (Vector3.Dot(delta, forward) < -0.25f)
                        continue;
                }
                at = hit.position;
                return true;
            }
            at = origin;
            return false;
        }

        /// <summary>True when a non-trigger collider sits under the CharacterController feet.</summary>
        static bool HasPhysicsFloorUnder(Vector3 standPivot, CharacterController cc)
        {
            float half = cc != null ? cc.height * 0.5f - cc.center.y : 0.9f;
            Vector3 feet = standPivot + Vector3.down * half;
            Vector3 start = feet + Vector3.up * FloorProbeUp;
            return Physics.Raycast(start, Vector3.down, out _, FloorProbeDown,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }
    }
}
