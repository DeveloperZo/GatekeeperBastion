using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Sim
{
    /// <summary>The faucet loop's state and transitions (rules in <see cref="FaucetLoop"/>;
    /// spec in Docs/Brainstorms/coop-bastion-complexity.md, the faucet loop). Same class as the
    /// run state machine because every member here mutates it — the Composite/Dock split.
    /// Dev-gated: everything is inert until <see cref="SetFaucetLoop"/> turns it on, and with
    /// it off no shipped path reads any field in this file.</summary>
    public sealed partial class RunSim
    {
        bool _faucetActive;
        bool _faucetCycleLive;
        bool _faucetShutoffArmed;
        bool _faucetAwaitingDraft;
        int _faucetWavesEndured;
        int _faucetDraftSize;
        float _faucetChainTimer;
        bool _faucetAxisPending;
        FaucetAxis _faucetAxis;

        /// <summary>Dev toggle: the faucet loop governs field draws while true.</summary>
        public bool FaucetActive => _faucetActive;

        /// <summary>A faucet cycle is live: from the first faucet field draw until an axis is
        /// picked (or the cycle is reset by a run end, an abort, or the toggle).</summary>
        public bool FaucetCycleLive => _faucetCycleLive;

        /// <summary>Faucet Waves cleared this cycle.</summary>
        public int FaucetWavesEndured => _faucetWavesEndured;

        /// <summary>The origin terminal is marked: the off verb is legal.</summary>
        public bool FaucetMarked =>
            _faucetCycleLive && _faucetWavesEndured >= FaucetLoop.MarkAfterWaves;

        /// <summary>Shutoff taken mid-Assault; the draft opens when the live Wave clears.</summary>
        public bool FaucetShutoffArmed => _faucetShutoffArmed;

        /// <summary>The draft is open: E cycles, T confirms (Game layer drives the pick).</summary>
        public bool FaucetAwaitingDraft => _faucetAwaitingDraft;

        /// <summary>Options in the open draft (0 when no draft is open).</summary>
        public int FaucetDraftSize => _faucetDraftSize;

        /// <summary>A picked axis waiting for the Hatch-bank draw that will consume it.</summary>
        public bool HasPendingFaucetAxis => _faucetAxisPending;

        /// <summary>The picked axis. Meaningful only while <see cref="HasPendingFaucetAxis"/>.</summary>
        public FaucetAxis PendingFaucetAxis => _faucetAxis;

        /// <summary>True while faucet field Waves are the live grammar: the cycle is live and
        /// the mode is Terminal. Targeting reads this to point the Wave at the Hatch.</summary>
        public bool FaucetHatchHunt => _faucetCycleLive && Mode == WaveMode.Terminal;

        /// <summary>Dev toggle. Refused mid-Assault — flipping the wave grammar under a live
        /// Wave would strand targeting state. Turning off resets any live cycle.</summary>
        public void SetFaucetLoop(bool on)
        {
            if (Assault == AssaultState.Assault)
            {
                Log.Info(Tag, "SetFaucetLoop refused — assault live");
                return;
            }
            if (_faucetActive == on) return;
            _faucetActive = on;
            if (!on) ResetFaucetCycle("toggle off");
            Log.Info(Tag, on ? "faucet loop ON" : "faucet loop OFF");
        }

        /// <summary>Called from TriggerDraw's Terminal branch: a single-Wave field draw while
        /// the toggle is on begins a cycle. Deep-start and composite chains do not — the
        /// faucet rides its own chain and mixing the two would double-launch.</summary>
        void BeginFaucetCycleIfEligible()
        {
            if (!_faucetActive || _faucetCycleLive || _chainActive
                || _masterPhase != MasterControlPhase.None) return;
            _faucetCycleLive = true;
            _faucetWavesEndured = 0;
            _faucetShutoffArmed = false;
            _faucetAwaitingDraft = false;
            _faucetDraftSize = 0;
            Log.Info(Tag, "faucet cycle begins — mark after "
                + FaucetLoop.MarkAfterWaves + " Waves");
        }

        /// <summary>The off verb. Legal once marked: mid-Assault it arms (draft opens on the
        /// clear); in a Prep gap it cancels the pending chain and opens the draft now.</summary>
        public bool TryArmFaucetShutoff()
        {
            if (!_faucetCycleLive)
            {
                Log.Info(Tag, "faucet shutoff refused — no cycle live");
                return false;
            }
            if (_faucetShutoffArmed || _faucetAwaitingDraft)
            {
                Log.Info(Tag, "faucet shutoff refused — already armed");
                return false;
            }
            if (!FaucetMarked)
            {
                Log.Info(Tag, "faucet shutoff refused — endure "
                    + FaucetLoop.MarkAfterWaves + " Waves first (at "
                    + _faucetWavesEndured + ")");
                return false;
            }
            if (Assault == AssaultState.Assault)
            {
                _faucetShutoffArmed = true;
                Log.Info(Tag, "faucet shutoff armed — draft opens on the clear");
                return true;
            }
            _faucetChainTimer = 0f;
            OpenFaucetDraft();
            return true;
        }

        void OpenFaucetDraft()
        {
            _faucetShutoffArmed = false;
            _faucetAwaitingDraft = true;
            _faucetDraftSize = FaucetLoop.DraftSize(_faucetWavesEndured);
            if (_faucetDraftSize < 1) _faucetDraftSize = 1;
            Log.Info(Tag, "faucet draft open — endured=" + _faucetWavesEndured
                + " options=" + _faucetDraftSize);
        }

        /// <summary>Confirm the draft. The axis waits on the next Hatch-bank draw, which
        /// applies it to the Wave and adds <see cref="FaucetLoop.ConvertBonus"/> to the
        /// window's convert multiplier. Ends the cycle.</summary>
        public bool TryPickFaucetAxis(FaucetAxis axis)
        {
            if (!_faucetAwaitingDraft)
            {
                Log.Info(Tag, "faucet pick refused — no draft open");
                return false;
            }
            if ((int)axis >= _faucetDraftSize)
            {
                Log.Info(Tag, "faucet pick refused — " + FaucetLoop.AxisName(axis)
                    + " not offered (options=" + _faucetDraftSize + ")");
                return false;
            }
            _faucetAxisPending = true;
            _faucetAxis = axis;
            _faucetAwaitingDraft = false;
            _faucetCycleLive = false;
            _faucetDraftSize = 0;
            Log.Info(Tag, "faucet axis picked — " + FaucetLoop.AxisName(axis));
            return true;
        }

        /// <summary>Drop a picked axis that can no longer be honored (the bank refused).</summary>
        public void DropPendingFaucetAxis(string reason)
        {
            if (!_faucetAxisPending) return;
            _faucetAxisPending = false;
            Log.Info(Tag, "faucet axis dropped — " + reason);
        }

        /// <summary>Back to no-cycle. The toggle itself is untouched.</summary>
        void ResetFaucetCycle(string reason)
        {
            if (!_faucetCycleLive && !_faucetAwaitingDraft && !_faucetAxisPending
                && _faucetChainTimer <= 0f)
                return;
            _faucetCycleLive = false;
            _faucetShutoffArmed = false;
            _faucetAwaitingDraft = false;
            _faucetWavesEndured = 0;
            _faucetDraftSize = 0;
            _faucetChainTimer = 0f;
            if (_faucetAxisPending)
            {
                _faucetAxisPending = false;
                Log.Info(Tag, "faucet axis dropped — " + reason);
            }
            Log.Info(Tag, "faucet cycle reset — " + reason);
        }

        /// <summary>TriggerDraw's Hatch-bank arm: the reward half of a pending drafted axis —
        /// one choice raises both the window's danger (the spec transform) and its convert
        /// rate (one-source stake).</summary>
        void ApplyFaucetConvertBonusIfPending()
        {
            if (!_faucetAxisPending) return;
            _hatchConvertMul += FaucetLoop.ConvertBonus;
            Log.Info(Tag, "faucet convert bonus — mul=" + _hatchConvertMul.ToString("0.00"));
        }

        /// <summary>LaunchWave's arm: apply and consume the drafted axis on a Hatch-bank spec.
        /// A non-bank launch passes through untouched.</summary>
        WaveSpec ConsumeFaucetAxisIfPending(WaveSpec spec)
        {
            if (Mode != WaveMode.HatchBank || !_faucetAxisPending) return spec;
            _faucetAxisPending = false;
            Log.Info(Tag, "faucet axis applied — " + FaucetLoop.AxisName(_faucetAxis));
            return FaucetLoop.ApplyAxis(spec, _faucetAxis);
        }

        /// <summary>NotifyAssaultCleared's faucet arm: a faucet field Wave was endured. Pays
        /// the endure bonus, then either opens the draft (shutoff armed) or re-arms the chain
        /// on <see cref="WaveDirector.InterWaveGap"/>. Returns true when it owned the clear —
        /// the caller then skips the sequential/idle branches so the dial and juice survive
        /// the chain (the whole cycle is one pull).</summary>
        bool FaucetOwnsClear(bool hatchBank)
        {
            if (!_faucetCycleLive || hatchBank || Phase != RunPhase.Tethered) return false;
            _faucetWavesEndured++;
            _wallets.AwardSplit(FaucetLoop.WaveEndureBonus);
            Log.Info(Tag, "faucet wave endured — n=" + _faucetWavesEndured
                + (FaucetMarked ? " (marked)" : "")
                + " +" + FaucetLoop.WaveEndureBonus + " unbanked");
            if (_faucetShutoffArmed)
                OpenFaucetDraft();
            else
                _faucetChainTimer = WaveDirector.InterWaveGap;
            return true;
        }

        /// <summary>Tick's faucet arm: the inter-Wave gap between chained faucet Waves.
        /// Returns true when it consumed the tick.</summary>
        bool FaucetChainTick(float dt)
        {
            if (Assault != AssaultState.Prep || _faucetChainTimer <= 0f) return false;
            _faucetChainTimer -= dt;
            if (_faucetChainTimer <= 0f)
            {
                _faucetChainTimer = 0f;
                TriggerDraw(WaveMode.Terminal);
            }
            return true;
        }
    }
}
