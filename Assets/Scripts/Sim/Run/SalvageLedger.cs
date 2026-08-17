namespace GatekeepersProtocol.Sim
{
    /// <summary>
    /// Per-run converted tally. Hatch-bank drip and dump call Earn; kill awards do not.
    /// </summary>
    public sealed class SalvageLedger
    {
        /// <summary>Whole salvage units converted this run. Never decreases during a run.</summary>
        public long RunSalvage { get; private set; }

        /// <summary>Add a floored kill award (or any whole salvage unit) to the run tally.</summary>
        public void Earn(long amount)
        {
            if (amount <= 0L) return;
            RunSalvage += amount;
        }
    }
}
