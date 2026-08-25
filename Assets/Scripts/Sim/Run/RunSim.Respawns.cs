using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Sim
{
    /// <summary>Respawn counter for the live run (partial of RunSim, same split as
    /// Wallets/Dock: one concern per file). The rule itself is <see cref="Respawns"/>; this
    /// is the state it adjudicates over and the one mutation path. The purse debited is the
    /// DYING SEAT'S OWN Unbanked — never the realm's bank and never another seat's pile — so
    /// the affordability test and the spend cannot drift apart.</summary>
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
            return Respawns.Refuse(Phase, NextRespawnCost, UnbankedOf(slot));
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
            long carry = UnbankedOf(slot);
            _wallets.Set(slot, carry - cost);
            _wallets.SetDown(slot, false);
            RespawnsTaken++;
            Log.Event(Tag, "respawn taken — slot=" + slot + " cost=" + cost
                + " carry " + carry + "->" + UnbankedOf(slot)
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
        }

        /// <summary>Zero the counter and thaw every pile for a fresh run. Called from
        /// EngageTether — the one gate into Tethered.</summary>
        void SeedRespawns()
        {
            RespawnsTaken = 0;
            _wallets.ClearDown();
        }
    }
}
