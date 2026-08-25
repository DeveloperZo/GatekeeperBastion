using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using Unity.Netcode;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): the local seat's DOWN state. Death
    /// used to end the run outright; now it holds the run open while a respawn can be
    /// honored — immediately when this seat's own carry covers the price, or while any
    /// other seat still fights, since their kills split into every pile including this
    /// one. The frozen pile is the purse; nothing else pays. Split so GameController.cs
    /// stays under cap; the guest path is GameController.Respawn.cs.</summary>
    public sealed partial class GameController
    {
        #region State
        // The body PlayerDied reported. Held so the DOWN tick and the respawn act on the
        // rig that actually died rather than re-resolving through the registry, whose
        // Local requires an FPS rig. Overwritten on the next death; stale outside Tethered.
        PlayerHealth _downBody;
        #endregion

        #region Properties
        /// <summary>True while the local (host or solo) body is dead and the run is still
        /// live — the DOWN overlay is up and Confirm buys the way back.</summary>
        public bool LocalDownActive
        {
            get
            {
                if (Sim == null || Sim.Phase != RunPhase.Tethered) return false;
                return _downBody != null && _downBody.IsDead;
            }
        }
        #endregion

        #region Public API
        /// <summary>Command from PlayerHealth: the player is dead. While a respawn can be
        /// honored the run holds open behind the DOWN overlay; otherwise the run ends
        /// exactly like a hatch loss and the seat's carry dies with it. On the respawn
        /// path the carry is the purse and its remainder survives. Only meaningful while
        /// Tethered. The bare overload is a death with no body to put back — it always
        /// ends the run.</summary>
        public void PlayerDied()
        {
            PlayerDied(null);
        }

        public void PlayerDied(PlayerHealth body)
        {
            if (Sim == null)
            {
                // Guard trip: no sim means no lose screen — free the cursor so the player isn't
                // trapped in a dead body with a captured mouse.
                Log.Error(Tag, "PlayerDied with no Sim; freeing the cursor as a last resort.");
                EnsureCursorFree();
                return;
            }
            if (Sim.Phase != RunPhase.Tethered)
            {
                Log.Warn(Tag, $"PlayerDied ignored in phase {Sim.Phase} (run already over).");
                return;
            }
            if (body != null && TryEnterLocalDown(body))
                return;
            // No respawn can be honored, so this death is final for the seat: the carry it
            // was holding goes with it. On the respawn path the fee is taken out of this
            // same pile instead and the remainder survives, so the wipe belongs here and
            // nowhere else.
            Sim.WipeUnbankedSlot(0);
            Sim.NotifyPlayerDown();
        }

        /// <summary>Pay the run's next respawn cost and put the local body back at its
        /// spawn. False leaves everything unchanged with the refusal logged sim-side.</summary>
        public bool TryLocalRespawn()
        {
            if (Sim == null || Sim.Phase != RunPhase.Tethered) return false;
            var rig = _downBody;
            if (rig == null || !rig.IsDead) return false;
            if (!Sim.TryRespawn(0, out long cost)) return false;
            rig.ResetToSpawn();
            // ResetToSpawn alone leaves MovementLocked set from Die(); HealToFull clears it.
            rig.HealToFull();
            _downBody = null;
            Log.Event("Demo", "respawn seat=0 cost=" + cost);
            return true;
        }
        #endregion

        #region Private
        /// <summary>Called from PlayerDied. True holds the run open behind the DOWN
        /// overlay; false means no respawn can be honored and the caller ends the run,
        /// exactly as every death did before the recut.</summary>
        bool TryEnterLocalDown(PlayerHealth body)
        {
            string reason = Sim.RespawnRefuse(0);
            if (reason != null && !AnyRemoteSeatFighting())
            {
                Log.Event("Demo", "no respawn — " + reason);
                return false;
            }
            _downBody = body;
            // Freeze the pile: it is the purse this seat's respawn buys from, and the
            // crew's next Convert must not drain it while its owner cannot act.
            Sim.SetSeatDown(0, true);
            // No wipe here: the pile is the purse the respawn is bought WITH, so taking it
            // away would price every death at infinity. The fee comes out of it in
            // RunSim.TryRespawn; a death that buys nothing loses it in PlayerDied instead.
            Log.Event("Demo", reason == null
                ? "down — respawn offered at " + Sim.NextRespawnCost
                : "down — cannot buy back (" + reason + "), crew still standing");
            return true;
        }

        /// <summary>Confirm on the DOWN overlay. Ticked from Update while Tethered. ESC is
        /// bound to Confirm for the end screens and to FreeCursor for pause — the sampler
        /// ORs bindings without context, so a press that is also FreeCursor is the pause
        /// on its way open, not a respawn.</summary>
        void TickLocalDown()
        {
            if (!LocalDownActive) return;
            if (OptionsScreen.IsOpen || HudController.IsPauseOpen) return;
            var frame = Controls.Sample();
            if (!frame.confirmDown || frame.freeCursorDown) return;
            TryLocalRespawn();
        }

        /// <summary>A seat just went down or left. If the local seat is down, nobody
        /// remote is still fighting, and no dead seat can afford its own return, the
        /// run can never advance — end it now instead of holding a dead room open
        /// forever. Piles are frozen while down, so affordability cannot change on
        /// its own once the last fighter falls.</summary>
        public void NotifySeatWentDown()
        {
            if (Sim == null || Sim.Phase != RunPhase.Tethered) return;
            if (!LocalDownActive) return;
            if (AnyRemoteSeatFighting()) return;
            if (Sim.RespawnRefuse(0) == null) return;
            if (AnyWaitingSeatCanPay()) return;
            Log.Event("Demo", "no seat can stand — the Boarding ends");
            Sim.WipeUnbankedSlot(0);
            Sim.NotifyPlayerDown();
        }

        bool AnyWaitingSeatCanPay()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsServer || !nm.IsListening) return false;
            var clients = nm.ConnectedClientsList;
            for (int i = 0; i < clients.Count; i++)
            {
                var po = clients[i].PlayerObject;
                if (po == null) continue;
                var avatar = po.GetComponent<NetPlayerAvatar>();
                if (avatar == null || avatar.IsOwner) continue;
                if (!avatar.WaitingOnShip.Value) continue;
                if (Sim.RespawnRefuse(avatar.Slot.Value) == null) return true;
            }
            return false;
        }

        bool AnyRemoteSeatFighting()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsServer || !nm.IsListening) return false;
            var clients = nm.ConnectedClientsList;
            for (int i = 0; i < clients.Count; i++)
            {
                var po = clients[i].PlayerObject;
                if (po == null) continue;
                var avatar = po.GetComponent<NetPlayerAvatar>();
                if (avatar == null || avatar.IsOwner) continue;
                if (!avatar.WaitingOnShip.Value && avatar.Hp.Value > 0f) return true;
            }
            return false;
        }
        #endregion
    }
}
