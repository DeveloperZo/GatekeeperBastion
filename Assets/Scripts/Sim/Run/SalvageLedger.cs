namespace GatekeeperBastion.Sim
{
    /// <summary>
    /// Per-run salvage tally, for display only. Salvage is real currency the moment an enemy
    /// dies: RunSim.AwardKill floors the kill value once, credits that integer to
    /// MetaState.BankedSalvage, and mirrors the same integer here so the HUD can show what this
    /// run banked. Nothing in this class moves money.
    ///
    /// There is no banking API any more — BankCleanExit/BankPodded were deleted along with the
    /// loss-stake mechanic. A run's tally resets simply by starting a new run: each run builds
    /// a fresh RunSim, and therefore a fresh ledger.
    /// </summary>
    public sealed class SalvageLedger
    {
        /// <summary>Whole salvage units banked this run. Never decreases during a run. Agrees
        /// exactly with the sum of floored kill awards credited to the bank.</summary>
        public long RunSalvage { get; private set; }

        /// <summary>Add a floored kill award (or any whole salvage unit) to the run tally.</summary>
        public void Earn(long amount)
        {
            if (amount <= 0L) return;
            RunSalvage += amount;
        }
    }
}
