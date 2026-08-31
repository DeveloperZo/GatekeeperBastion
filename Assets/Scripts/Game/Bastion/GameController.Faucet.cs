using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;
using UnityEngine.AI;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): the faucet-loop graybox surface
    /// (Docs/Brainstorms/coop-bastion-complexity.md, the faucet loop). Sim state lives in
    /// RunSim.Faucet.cs; this partial owns the inputs (shutoff T, draft E/T), the teleport
    /// to the Hatch, the bank start, and the marked-terminal ping. All of it is inert until
    /// the dev toggle turns <see cref="RunSim.FaucetActive"/> on.</summary>
    public sealed partial class GameController
    {
        /// <summary>The terminal whose T began the live faucet cycle. The slice's off switch
        /// is the origin terminal (the authored seat-set pick is the real feature's).</summary>
        BastionTerminal _faucetTerminal;
        int _faucetDraftCursor;
        float _faucetMarkerRefresh;

        /// <summary>Origin terminal id while a cycle (or its draft) is live; null otherwise.</summary>
        public string FaucetTerminalId =>
            _faucetTerminal != null ? _faucetTerminal.TerminalId : null;

        /// <summary>The draft option the next T confirms (index into the axis order).</summary>
        public int FaucetDraftCursor => _faucetDraftCursor;

        /// <summary>Is this terminal the live cycle's origin (the marked off switch)?</summary>
        public bool IsFaucetOrigin(string terminalId) =>
            _faucetTerminal != null && Sim != null
            && (Sim.FaucetCycleLive || Sim.FaucetAwaitingDraft)
            && !string.IsNullOrEmpty(terminalId)
            && terminalId == _faucetTerminal.TerminalId;

        /// <summary>Dev toggle (console binding). Taints the session like every Dev mutation.</summary>
        public void ToggleFaucetLoop()
        {
            GameSession.NotifyDevAction();
            if (Sim == null)
            {
                Log.Warn(Tag, "ToggleFaucetLoop ignored — no Sim.");
                return;
            }
            Sim.SetFaucetLoop(!Sim.FaucetActive);
            Log.Event("Dev", "faucet loop " + (Sim.FaucetActive ? "ON" : "OFF"));
        }

        /// <summary>Per-frame while Tethered: drop the stale origin ref once the cycle is
        /// over (self-healing — every end path funnels through the sim's reset).</summary>
        void TickFaucetHousekeeping()
        {
            if (Sim == null || _faucetTerminal == null) return;
            if (!Sim.FaucetCycleLive && !Sim.FaucetAwaitingDraft)
                _faucetTerminal = null;
        }

        /// <summary>Assault-time faucet input: T at the marked origin arms the shutoff.
        /// Also keeps the worldspace ping on the marked terminal alive.</summary>
        void TickFaucetAssaultCommands()
        {
            if (Sim == null || !Sim.FaucetHatchHunt) return;
            RefreshFaucetMarker();
            if (!Controls.Down(ControlId.PullDraw)) return;
            var looking = _interactor != null ? _interactor.Current as BastionTerminal : null;
            if (looking == null || _faucetTerminal == null
                || looking.TerminalId != _faucetTerminal.TerminalId)
                return;
            Sim.TryArmFaucetShutoff();
        }

        /// <summary>Prep-gap shutoff: T at the origin between chained Waves opens the draft
        /// immediately. Returns true when the T belonged to the faucet (consumed either way;
        /// the sim logs any refusal).</summary>
        bool TryFaucetPrepShutoff(BastionTerminal lookingTerminal)
        {
            if (Sim == null || !Sim.FaucetCycleLive) return false;
            if (lookingTerminal == null || _faucetTerminal == null
                || lookingTerminal.TerminalId != _faucetTerminal.TerminalId)
                return false;
            Sim.TryArmFaucetShutoff();
            return true;
        }

        /// <summary>The open draft: E cycles the highlighted axis, T confirms — teleport to
        /// the Hatch, then the bank window with the axis applied. Runs instead of the normal
        /// Prep commands while the draft is open.</summary>
        void TickFaucetDraft()
        {
            int size = Sim.FaucetDraftSize;
            if (size < 1) return;
            if (_faucetDraftCursor >= size) _faucetDraftCursor = 0;

            if (Controls.Down(ControlId.Interact))
            {
                _faucetDraftCursor = (_faucetDraftCursor + 1) % size;
                Log.Event("Run", "faucet draft — "
                    + FaucetLoop.AxisName((FaucetAxis)_faucetDraftCursor));
                return;
            }
            if (!Controls.Down(ControlId.PullDraw)) return;

            var axis = (FaucetAxis)_faucetDraftCursor;
            if (!Sim.TryPickFaucetAxis(axis)) return;
            _faucetTerminal = null;
            _faucetDraftCursor = 0;
            TeleportLocalToHatch();
            StartFaucetBank();
        }

        void RefreshFaucetMarker()
        {
            if (_faucetTerminal == null || !Sim.FaucetMarked) return;
            if (Time.time < _faucetMarkerRefresh) return;
            _faucetMarkerRefresh = Time.time + 0.75f;
            PingMarker.Attach(_faucetTerminal.transform, 1.5f);
        }

        /// <summary>Place the local Boarder at the Hatch. Shared by the faucet draft confirm
        /// and the recall pads beside field terminals.</summary>
        public void TeleportLocalToHatch()
        {
            var player = PlayerRegistry.Local;
            var hatch = HatchStation.Instance;
            if (player == null || hatch == null)
            {
                Log.Warn(Tag, "hatch teleport skipped — no player or hatch");
                return;
            }

            Vector3 hatchPos = hatch.transform.position;
            Vector3 back = player.transform.position - hatchPos;
            back.y = 0f;
            if (back.sqrMagnitude < 0.01f) back = Vector3.back;
            back.Normalize();
            Vector3 want = hatchPos + back * 3.5f;
            float floorY = want.y;
            if (NavMesh.SamplePosition(want, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
                floorY = navHit.position.y;
            Vector3 at = new Vector3(want.x, floorY + 1.1f, want.z);
            float yaw = Mathf.Atan2(-back.x, -back.z) * Mathf.Rad2Deg;

            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            Physics.SyncTransforms();
            if (cc != null) cc.enabled = true;
            var fpc = player.GetComponent<FirstPersonController>();
            if (fpc != null) fpc.ClearFallVelocity();
            Log.Event("Run", "hatch teleport — at the Hatch");
        }

        /// <summary>Start the post-draft bank window. Mirrors StartHatchBankWave minus the
        /// E-select arm (the draft was the commitment) and minus the demo zone gate, which is
        /// bypassed with a logged line — this is a dev-gated graybox, and the faucet cycle is
        /// its own authorization to bank.</summary>
        bool StartFaucetBank()
        {
            if (Sim == null || RunIsOver) return false;
            if (Sim.TotalUnbanked <= 0L)
            {
                Sim.DropPendingFaucetAxis("unbanked=0");
                Log.Event("Hatch", "faucet bank skipped — unbanked=0");
                return false;
            }
            if (Sim.MasterSequenceLive)
            {
                Sim.DropPendingFaucetAxis("master control live");
                return false;
            }

            ClearPendingTerminal();
            BastionLayout layout = _layout;
            if (layout == null && _repair != null)
                layout = _repair.GetComponentInParent<BastionLayout>();
            if (layout != null)
                layout.ActivateHatchBankSpawns();
            else
                Log.Warn(Tag, "faucet bank — no BastionLayout; spawn registry unchanged");
            _hatchBankLayout = layout;
            if (HatchBankStartBlocked)
                Log.Event("Demo", "faucet bank — demo zone gate bypassed (dev faucet)");

            Sim.TriggerDraw(WaveMode.HatchBank);
            if (Sim.Assault != AssaultState.Assault)
            {
                RestoreHatchBankSpawns();
                Sim.DropPendingFaucetAxis("bank draw did not start");
                Log.Event("Hatch", "faucet bank refused — draw did not start");
                return false;
            }

            Log.Event("Hatch", "faucet bank started — carry=" + Sim.HatchCarry
                + " convert=" + Sim.HatchConvertMultiplier.ToString("0.00")
                + " unbanked=" + Sim.TotalUnbanked);
            return true;
        }
    }
}
