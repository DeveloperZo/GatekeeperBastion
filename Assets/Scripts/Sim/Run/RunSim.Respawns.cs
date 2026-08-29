using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Sim
{
    /// <summary>Respawn counter for the live run (partial of RunSim, same split as
    /// Wallets/Dock: one concern per file). The rule itself is <see cref="Respawns"/>; this
    /// is the state it adjudicates over and the one mutation path. The purse debited is the
    /// REALM'S BANK (Zo, 2026-08-25) — never a seat's staked pile — so the affordability test
    /// and the spend cannot drift apart.
    /// <para>It was the dying seat's own carry until kills began banking on landing. Charging
    /// the carry now would tax the seat that chose to gamble, spare the one that did not, and
    /// price a never-gambling seat's return at infinity — which is the no-progress death the
    /// ruling forbids.</para></summary>
    public sealed partial class RunSim
    {
        /// <summary>Respawns taken this run, every seat together. Prices the next one.</summary>
        public int RespawnsTaken { get; private set; }

        /// <summary>What the next respawn costs, before asking whether it may be taken.</summary>
        public long NextRespawnCost => Respawns.CostFor(RespawnsTaken);

        /// <summary>Null when <paramref name="slot"/> may buy its way back now. Named refuse
        /// otherwise.</summary>
        public string RespawnRefuse(int slot)
        {
            // The fee comes out of the BANK now that kills bank on landing — the carried
            // pile a seat used to buy its way back with no longer fills on its own.
            return Respawns.Refuse(Phase, NextRespawnCost, BankedSalvage, RespawnsTaken);
        }

        /// <summary>Take a respawn for <paramref name="slot"/>: adjudicate, debit that seat's
        /// own Unbanked, and count it. <b>The remainder of the carry survives the death</b> —
        /// the fee is the whole price, so nothing on this path may wipe the pile. False
        /// leaves every number unchanged and the refusal in the log.
        /// <para>The caller owns putting the body back — this is the rule, not the rig.</para></summary>
        public bool TryRespawn(int slot, out long cost)
        {
            cost = NextRespawnCost;
            string reason = RespawnRefuse(slot);
            if (reason != null)
            {
                Log.Event(Tag, "respawn refused — " + reason);
                return false;
            }
            long bank = BankedSalvage;
            if (!SpendBanked(cost))
            {
                Log.Event(Tag, "respawn refused — bank=" + bank + " cost=" + cost);
                return false;
            }
            _wallets.SetDown(slot, false);
            BleedOutRemaining = 0f;
            BleedOutExpired = false;
            RespawnsTaken++;
            Log.Event(Tag, "respawn taken — slot=" + slot + " cost=" + cost
                + " bank " + bank + "->" + BankedSalvage
                + " taken=" + RespawnsTaken);
            return true;
        }

        /// <summary>A seat went down or stood up. While down its pile is frozen — the
        /// purse the respawn is bought with may not be converted out from under it.
        /// The Game layer mirrors this from the death/wait surfaces; TryRespawn clears
        /// it itself on success.</summary>
        public void SetSeatDown(int slot, bool down)
        {
            _wallets.SetDown(slot, down);
            if (down)
            {
                // Not restarted for a second seat: the clock belongs to the RUN, so a party
                // dropping one at a time cannot refill it by taking turns.
                if (BleedOutRemaining <= 0f) BleedOutRemaining = Respawns.BleedOutSeconds;
            }
            else BleedOutRemaining = 0f;
        }

        /// <summary>Seconds left for a downed seat to buy back, 0 when nobody is bleeding.
        /// Counts down in <see cref="Tick"/>. THE SIM DOES NOT ACT ON EXPIRY — reaching 0 is
        /// reported, not enforced, because whether the Boarding ends depends on a crewmate
        /// still fighting and that is not a fact this layer holds.</summary>
        public float BleedOutRemaining { get; private set; }

        /// <summary>True while a seat is bleeding out. The HUD's cue to draw the countdown.</summary>
        public bool BleedOutLive => BleedOutRemaining > 0f;

        /// <summary>Advance the bleed clock. True on the frame it reaches zero, once.</summary>
        bool TickBleedOut(float dt)
        {
            if (BleedOutRemaining <= 0f) return false;
            BleedOutRemaining -= dt;
            if (BleedOutRemaining > 0f) return false;
            BleedOutRemaining = 0f;
            Log.Event(Tag, "bled out — nobody stood up inside "
                + Respawns.BleedOutSeconds + "s");
            return true;
        }

        /// <summary>True once a downed seat has run out of time. Latched at 0 by the clock, so
        /// the Game layer may read it on any later frame rather than catching one edge.</summary>
        public bool BleedOutExpired { get; private set; }

        /// <summary>Zero the counter and thaw every pile for a fresh run. Called from
        /// EngageTether — the one gate into Tethered.</summary>
        void SeedRespawns()
        {
            RespawnsTaken = 0;
            BleedOutRemaining = 0f;
            BleedOutExpired = false;
            _wallets.ClearDown();
        }
    }
}
