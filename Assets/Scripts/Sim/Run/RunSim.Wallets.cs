using GatekeepersProtocol.Util;

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

        /// <summary>Burn one seat's unbanked on something that is not banking, and report what
        /// was actually affordable. A sink, not a conversion — nothing is credited and
        /// <see cref="HatchConversions"/> is untouched, so the Hatch-bank wall
        /// (Docs/Design/3-Mechanics.md) still holds. Intensity juice is the same shape.</summary>
        public long SpendUnbanked(int slot, long units)
        {
            if (units <= 0L) return 0L;
            long have = _wallets.Of(slot);
            if (have <= 0L) return 0L;
            long spend = units > have ? have : units;
            _wallets.Set(slot, have - spend);
            return spend;
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
                if (i == 0)
                {
                    _meta.EarnSalvage(share);
                    continue;
                }
                var handler = GuestConverted;
                if (handler != null) handler(i, share);
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
            // Guests count their own on the seat that applies CreditBanked (NetPlayerAvatar).
            if (slot == 0 && _convertedThisBank[0] == 0L)
                _meta.HatchConversions++;
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
