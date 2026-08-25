using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Sim
{
    /// <summary>Hatch-bank convert into per-slot piles. Cap is cumulative across the window
    /// so drip ticks cannot each credit up to BankCap.</summary>
    public sealed partial class RunSim
    {
        /// <summary>How many piles AwardKill splits across, and the n in NextWave. 1 in solo.</summary>
        public void SetPlayerCount(int count)
        {
            _wallets.SetCount(count);
        }

        /// <summary>Guest progress for the banking cap. Slot 0 reads host Meta instead.</summary>
        public void SetSlotDeepest(int slot, int deepest) => _wallets.SetDeepest(slot, deepest);

        /// <summary>A seat went down for good: wipe that slot's unbanked. Call this ONLY on
        /// the path where no respawn was bought — a bought respawn already paid its fee out
        /// of this pile and the remainder survives with the seat
        /// (<see cref="TryRespawn"/>).</summary>
        public void WipeUnbankedSlot(int slot) => _wallets.Wipe(slot);

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

        /// <summary>Returned only: each slot keeps <see cref="HatchBank.ReturnedKeepShare"/>
        /// of its leftover Unbanked before the wipe — surviving pays a little, and only a
        /// little (Docs/Decisions.md); Lost still wipes it all. Runs before WipeUnbanked so
        /// the share reads the pile the player carried home; no bank cap and no carry
        /// multiplier — this is a rebate, not a conversion, and it does not count toward
        /// HatchConversions (the survived window already did).</summary>
        void KeepReturnedShare()
        {
            int n = PlayerCount;
            long kept = 0L;
            for (int i = 0; i < n; i++)
            {
                long share = HatchBank.ReturnedKeep(_wallets.Of(i));
                if (share <= 0L) continue;
                kept += share;
                Salvage.Earn(share);
                // The pot belongs to the realm, and in co-op the realm is the host's, so
                // every seat's rebate lands in the same place. The event still fires for a
                // guest seat because that seat's readout needs to know what it brought in.
                _meta.EarnSalvage(share);
            }
            if (kept > 0L)
                Log.Info(Tag, "returned — kept " + kept + " of unbanked ("
                    + HatchBank.ReturnedKeepShare.ToString("0.00",
                        System.Globalization.CultureInfo.InvariantCulture) + " share)");
        }

        void CreditTaken(long[] taken)
        {
            if (taken == null) return;
            int n = PlayerCount;
            for (int i = 0; i < n; i++)
                CreditSlot(i, taken[i]);
        }

        /// <summary>True until any seat has converted in the current bank window.</summary>
        bool NothingConvertedThisBank()
        {
            for (int i = 0; i < _convertedThisBank.Length; i++)
                if (_convertedThisBank[i] != 0L) return false;
            return true;
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
            // Salvage is coming through the Hatch: the first credit of a window counts it.
            // This is the wheel / equipment-slot unlock event (MetaUpgrades.UpgradesUnlocked).
            // It counts the WINDOW, not the seat — the gate belongs to the realm, so a window
            // that converted for anybody has converted, however many seats fed it.
            if (NothingConvertedThisBank())
                _meta.HatchConversions++;
            _convertedThisBank[slot] += credited;
            Salvage.Earn(credited);
            // Every seat banks into the realm's pot. A guest is playing in somebody else's
            // world and that world's salvage is what fills; what the guest takes home is its
            // Boarder (Docs/Milestones — the realm and the Boarder).
            _meta.EarnSalvage(credited);
        }
    }
}
