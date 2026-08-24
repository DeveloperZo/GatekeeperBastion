using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using Unity.Netcode;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): the local seat's DOWN state. Death
    /// used to end the run outright; now it holds the run open while a respawn can be
    /// honored — immediately when the sim allows one, or at the next break while any
    /// other seat still fights, since their banking can refill the pot and the hatch is
    /// the backstop either way (Docs/Milestones — drop in between waves). Split so
    /// GameController.cs stays under cap; the guest path is GameController.Respawn.cs.</summary>
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
        /// exactly like a hatch loss. Unbanked is wiped either way; already-converted ship
        /// Salvage is kept. Only meaningful while Tethered. The bare overload is a death
        /// with no body to put back — it always ends the run.</summary>
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
            Sim.NotifyPlayerDown();
        }

        /// <summary>Pay the run's next respawn cost and put the local body back at its
        /// spawn. False leaves everything unchanged with the refusal logged sim-side.</summary>
        public bool TryLocalRespawn()
        {
            if (Sim == null || Sim.Phase != RunPhase.Tethered) return false;
            var rig = _downBody;
            if (rig == null || !rig.IsDead) return false;
            if (!Sim.TryRespawn(out long cost)) return false;
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
            string reason = Sim.RespawnRefuse();
            if (reason != null && !AnyRemoteSeatFighting())
            {
                Log.Event("Demo", "no respawn — " + reason);
                return false;
            }
            _downBody = body;
            // Dying wipes the seat's pile whether or not a respawn follows.
            Sim.WipeUnbankedSlot(0);
            Log.Event("Demo", reason == null
                ? "down — respawn offered at " + Sim.NextRespawnCost
                : "down — waiting for the break (" + reason + ")");
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

        /// <summary>Any remote seat alive in the bastion — the seats whose fight keeps a
        /// down host's break coming.</summary>
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
