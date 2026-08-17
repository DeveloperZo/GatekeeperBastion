namespace GatekeepersProtocol.Sim
{
    /// <summary>Hatch-bank convert into per-slot piles. Cap is cumulative across the window
    /// so drip ticks cannot each credit up to BankCap.</summary>
    public sealed partial class RunSim
    {
        void ResetConvertedThisBank()
        {
            for (int i = 0; i < _convertedThisBank.Length; i++)
                _convertedThisBank[i] = 0L;
        }

        void DripTick(float dt)
        {
            long units = HatchBank.DripUnits(dt, ref _dripAcc);
            CreditTaken(_wallets.TakeTotal(units));
        }

        void DumpUnbanked()
        {
            CreditTaken(_wallets.TakeTotal(TotalUnbanked));
        }

        void AttritUnbanked(long units)
        {
            if (units <= 0L) return;
            _wallets.TakeTotal(units);
        }

        void WipeUnbanked()
        {
            _wallets.WipeAll();
            _dripAcc = 0f;
            _withdrawAcc = 0f;
        }

        void CreditTaken(long[] taken)
        {
            if (taken == null) return;
            int n = PlayerCount;
            for (int i = 0; i < n; i++)
                CreditSlot(i, taken[i]);
        }

        void CreditSlot(int slot, long unbankedTaken)
        {
            if (unbankedTaken <= 0L) return;
            long credited = HatchBank.BankedFrom(unbankedTaken, _hatchConvertMul);
            if (credited <= 0L) return;
            if (PlayerCount > 1)
            {
                int deepest = slot == 0 ? _meta.DeepestWaveReached : _wallets.Deepest(slot);
                long cap = HatchBank.BankCap(deepest);
                long already = _convertedThisBank[slot];
                long room = cap - already;
                if (room <= 0L) return;
                if (credited > room) credited = room;
            }
            if (credited <= 0L) return;
            _convertedThisBank[slot] += credited;
            Salvage.Earn(credited);
            if (slot == 0)
            {
                _meta.EarnSalvage(credited);
                return;
            }
            var handler = GuestConverted;
            if (handler != null) handler(slot, credited);
        }
    }
}
