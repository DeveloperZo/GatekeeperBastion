using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>CLIENT-LOCAL / HOST-ONLY (partial of GameController): the client tick path
    /// and the net repair command. Split so GameController.cs does not grow past cap —
    /// these members share hosted component refs and the RunSim, so they are not a
    /// separate type.</summary>
    public sealed partial class GameController
    {
        #region Public API
        /// <summary>Client-requested hatch repair. Spends the host wallet at the catalog
        /// rate; does not require the host to have the rig equipped. The requester already
        /// gated on their own <see cref="RepairInteractable.Configure"/>.</summary>
        public float RepairHatchFromNet(float requestedHp)
        {
            if (Sim == null || RunIsOver) return 0f;
            var hatch = Sim.Ship.Hatch;
            if (hatch.Hp >= hatch.MaxHp - 0.01f)
            {
                TryWithdrawFromHpRequest(requestedHp);
                return 0f;
            }
            float applied = Sim.RepairHatch(requestedHp);
            if (applied > 0f && hatch.Hp >= hatch.MaxHp - 0.01f)
                Log.Event("Hatch", $"hatch fully repaired — {hatch.Hp:0}/{hatch.MaxHp:0}");
            return applied;
        }

        void TryWithdrawFromHpRequest(float requestedHp)
        {
            if (Sim == null) return;
            float rate = EquipmentCatalog.Def(EquipmentId.RepairRig).repairPerSecond;
            if (rate <= 0f) return;
            Sim.Withdraw(requestedHp / rate);
        }
        #endregion

        #region Private
        /// <summary>Move, fire, abilities, decoy ghost, interact — no Sim.Tick, no spawner.</summary>
        void TickClientLocal(float dt)
        {
            if (_fpc != null) _fpc.Tick(dt);
            if (_gun != null) _gun.Tick(dt);
            if (_ability != null) _ability.Tick(dt);
            if (_decoyPlacement != null) _decoyPlacement.Tick(dt);
            if (_interactor != null) _interactor.Tick(dt);
        }
        #endregion
    }
}
