using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Sim
{
    /// <summary>Respawn stock and counter for the live run (partial of RunSim, same split
    /// as Wallets/Dock: one concern per file). The rule itself is <see cref="Respawns"/>;
    /// this is the state it adjudicates over and the one mutation path. The pot debited is
    /// the realm's bank — the same purse Withdraw spends — so the test and the spend
    /// cannot drift apart.</summary>
    public sealed partial class RunSim
    {
        /// <summary>In-wave respawn charges left. Seeded per seat as the run becomes
        /// Tethered; burns one per mid-Assault respawn; break respawns burn none.</summary>
        public int RespawnStock { get; private set; }

        /// <summary>Respawns taken this run, every seat together. Prices the next one.</summary>
        public int RespawnsTaken { get; private set; }

        /// <summary>What the next respawn costs, before asking whether it may be taken.</summary>
        public long NextRespawnCost => Respawns.CostFor(RespawnsTaken);

        /// <summary>Null when a respawn may be taken now. Named refuse otherwise.</summary>
        public string RespawnRefuse()
        {
            long pot = _meta != null ? _meta.BankedSalvage : 0L;
            return Respawns.Refuse(Phase, Assault, RespawnStock, NextRespawnCost, pot);
        }

        /// <summary>Take a respawn: adjudicate, debit the realm's pot, burn a charge when a
        /// wave is on the floor, and count it. False leaves every number unchanged and the
        /// refusal in the log. The caller owns putting the body back — this is the rule,
        /// not the rig.</summary>
        public bool TryRespawn(out long cost)
        {
            cost = NextRespawnCost;
            string reason = RespawnRefuse();
            if (reason != null)
            {
                Log.Event(Tag, "respawn refused — " + reason);
                return false;
            }
            if (_meta == null || !_meta.SpendSalvage(cost))
            {
                Log.Event(Tag, "respawn refused — cannot afford");
                return false;
            }
            if (Assault == AssaultState.Assault) RespawnStock--;
            RespawnsTaken++;
            Log.Event(Tag, "respawn taken — cost=" + cost + " stock=" + RespawnStock
                + " taken=" + RespawnsTaken);
            return true;
        }

        /// <summary>Open the pool for this run's seats. Called from EngageTether.</summary>
        void SeedRespawns()
        {
            RespawnStock = Respawns.PoolPerSeat * PlayerCount;
            RespawnsTaken = 0;
            Log.Event(Tag, "respawn pool — " + RespawnStock + " charges for "
                + PlayerCount + " seats");
        }

        /// <summary>A seat followed the party in mid-run: the pool grows with it. Increase
        /// only — a leaving seat refunds nothing, or leaving and refollowing farms
        /// charges.</summary>
        void GrowRespawnPool(int oldCount, int newCount)
        {
            if (Phase != RunPhase.Tethered || newCount <= oldCount) return;
            RespawnStock += Respawns.PoolPerSeat * (newCount - oldCount);
            Log.Event(Tag, "respawn pool — grew to " + RespawnStock + " charges for "
                + newCount + " seats");
        }
    }
}
