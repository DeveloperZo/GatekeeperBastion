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
            PlacePlayerNear(hatch.transform);
        }

        void PlacePlayerAtMaster()
        {
            BastionTerminal master = _layout != null ? _layout.FindMaster() : null;
            if (master == null)
            {
                Log.Warn(Tag, "demo fight — no Master terminal to stand at");
                return;
            }
            PlacePlayerNear(master.transform);
        }

        /// <summary>Stand on NavMesh in front of <paramref name="target"/>. Hatch forward is
        /// +Z into the rooms; −forward is past the hull at z &lt; −60, which is how F12-2
        /// dropped the CharacterController through the floor.</summary>
        void PlacePlayerNear(Transform target)
        {
            var player = PlayerRegistry.Local;
            if (player == null || target == null) return;

            Vector3 at;
            if (!TryStandOnNavMesh(target.position, target.forward, out at))
            {
                Log.Warn(Tag, "demo fight — no NavMesh near " + target.name
                    + "; leaving player at spawn");
                return;
            }

            var cc = player.GetComponent<CharacterController>();
            if (cc != null)
                at.y += cc.height * 0.5f - cc.center.y + cc.skinWidth;

            Vector3 look = target.position - at;
            look.y = 0f;
            float yaw = look.sqrMagnitude > 0.01f
                ? Quaternion.LookRotation(look).eulerAngles.y
                : player.transform.eulerAngles.y;

            if (cc != null) cc.enabled = false;
            player.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            Physics.SyncTransforms();
            if (cc != null) cc.enabled = true;
        }

        static bool TryStandOnNavMesh(Vector3 origin, Vector3 forward, out Vector3 at)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);

            Vector3[] probes =
            {
                origin + forward * FightStandBack,
                origin + right * FightStandBack,
                origin - right * FightStandBack,
                origin - forward * FightStandBack,
                origin,
            };
            for (int i = 0; i < probes.Length; i++)
            {
                if (NavMesh.SamplePosition(probes[i], out NavMeshHit hit, FightNavSample,
                        NavMesh.AllAreas))
                {
                    at = hit.position;
                    return true;
                }
            }
            at = origin;
            return false;
        }
    }
}
