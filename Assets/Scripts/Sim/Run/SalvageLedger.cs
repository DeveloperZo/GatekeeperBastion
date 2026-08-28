namespace GatekeepersProtocol.Sim
{
    /// <summary>
    /// Per-run tally of what this Run has actually put in the realm's bank. Kill awards call
    /// Earn the moment the body falls (Zo, 2026-08-25); a Hatch-bank drip or dump calls it
    /// when a staked pile converts.
    /// </summary>
    public sealed class SalvageLedger
    {
        /// <summary>Whole salvage units this Run has banked, net of anything withdrawn back
        /// out and still at risk.</summary>
        public long RunSalvage { get; private set; }

        /// <summary>Salvage withdrawn that this Run never earned — money the realm was already
        /// holding when the Run began. Held as a debt rather than clamped away, so converting it
        /// back cannot be mistaken for income this Run produced.</summary>
        long _withdrawnBeyondRun;

        /// <summary>Add a floored kill award (or any whole salvage unit) to the run tally.
        /// Units repaying a withdraw of salvage the Run never earned repay that first — only
        /// the surplus above it is this Run's.</summary>
        public void Earn(long amount)
        {
            if (amount <= 0L) return;
            long repay = amount < _withdrawnBeyondRun ? amount : _withdrawnBeyondRun;
            _withdrawnBeyondRun -= repay;
            RunSalvage += amount - repay;
        }

        /// <summary>Take units back out of the tally when they leave the bank on a Withdraw.
        ///
        /// <para>Without this the tally DOUBLE-COUNTS: a kill Earns on landing, and the same
        /// salvage Earns again through <c>CreditSlot</c> if the player withdraws it and
        /// converts it back. A Run that staked and converted everything reported roughly
        /// 2.25x its real inflow, and <c>EndRun</c>'s net carried the inflation with it.
        /// Withdrawing and converting at 1.25x now reads +100, -100, +125 = 125, which is
        /// what the realm actually gained; withdrawing and dying reads +100, -100 = 0.</para>
        ///
        /// <para>What this Run did not earn is REMEMBERED, not clamped away. A player who
        /// withdraws salvage the realm banked on a previous Run has taken nothing out of this
        /// Run's tally, so a bare floor at zero would let the conversion back re-Earn the whole
        /// pile as fresh income — the very double-count this method exists to stop, returning
        /// through the pre-funded-bank door. Withdraw 500 of last Run's money and convert it at
        /// 1.25x and this Run earned the 125 bonus, not 625.</para></summary>
        public void Unearn(long amount)
        {
            if (amount <= 0L) return;
            long fromRun = amount < RunSalvage ? amount : RunSalvage;
            RunSalvage -= fromRun;
            _withdrawnBeyondRun += amount - fromRun;
        }
    }
}
