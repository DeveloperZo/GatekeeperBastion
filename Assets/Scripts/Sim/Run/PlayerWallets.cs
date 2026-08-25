namespace GatekeepersProtocol.Sim
{
    /// <summary>Per-player unbanked piles for one Run. Capacity is
    /// <see cref="WaveDirector.PlayerCapacity"/> so six stays a number, not a rewrite.
    /// Slot 0 is the host / the solo pile. Sim holds ints; Game maps NGO client ids.</summary>
    public sealed class PlayerWallets
    {
        readonly long[] _unbanked = new long[WaveDirector.PlayerCapacity];
        readonly int[] _deepest = new int[WaveDirector.PlayerCapacity];
        readonly long[] _takeBuf = new long[WaveDirector.PlayerCapacity];
        // A down seat's pile is FROZEN: it is the purse that seat's respawn is bought
        // with, so no conversion may drain it while its owner cannot act (the ruling
        // that made \"the remainder survives the death\" true through a Convert).
        readonly bool[] _down = new bool[WaveDirector.PlayerCapacity];
        int _count = 1;

        public PlayerWallets()
        {
            for (int i = 0; i < _deepest.Length; i++)
                _deepest[i] = 1;
        }

        public int Count => _count;

        public long Total
        {
            get
            {
                long sum = 0L;
                for (int i = 0; i < _count; i++)
                    sum += _unbanked[i];
                return sum;
            }
        }

        public long Of(int slot)
        {
            if (slot < 0 || slot >= _count) return 0L;
            return _unbanked[slot];
        }

        public int Deepest(int slot)
        {
            if (slot < 0 || slot >= _count) return 1;
            int d = _deepest[slot];
            return d < 1 ? 1 : d;
        }

        public void SetCount(int count)
        {
            if (count < 1) count = 1;
            if (count > WaveDirector.PlayerCapacity) count = WaveDirector.PlayerCapacity;
            _count = count;
        }

        /// <summary>Mark a seat's pile frozen (its owner is down) or live again.</summary>
        public void SetDown(int slot, bool down)
        {
            if (slot < 0 || slot >= _down.Length) return;
            _down[slot] = down;
        }

        public bool IsDown(int slot) =>
            slot >= 0 && slot < _down.Length && _down[slot];

        /// <summary>All seats live — a fresh run starts with nothing frozen.</summary>
        public void ClearDown()
        {
            for (int i = 0; i < _down.Length; i++)
                _down[i] = false;
        }

        public void Set(int slot, long value)
        {
            if (slot < 0 || slot >= _count) return;
            _unbanked[slot] = value < 0L ? 0L : value;
        }

        public void SetDeepest(int slot, int deepest)
        {
            if (slot < 0 || slot >= WaveDirector.PlayerCapacity) return;
            if (deepest < 1) deepest = 1;
            if (deepest > WaveDirector.MaxWaves) deepest = WaveDirector.MaxWaves;
            if (deepest > _deepest[slot])
                _deepest[slot] = deepest;
        }

        /// <summary>Equal split. Remainder goes to slot 0 so no salvage is destroyed.
        /// Count 1 is <c>slot[0] += award</c>.</summary>
        public void AwardSplit(long award)
        {
            if (award <= 0L) return;
            int n = _count;
            long share = award / n;
            long rem = award % n;
            for (int i = 0; i < n; i++)
                _unbanked[i] += share;
            _unbanked[0] += rem;
        }

        public void Add(int slot, long amount)
        {
            if (slot < 0 || slot >= _count || amount <= 0L) return;
            _unbanked[slot] += amount;
        }

        public void Wipe(int slot)
        {
            if (slot < 0 || slot >= _count) return;
            _unbanked[slot] = 0L;
        }

        public void WipeAll()
        {
            for (int i = 0; i < _unbanked.Length; i++)
                _unbanked[i] = 0L;
        }

        /// <summary>Pull <paramref name="units"/> from every slot in proportion to pile size.
        /// Remainder from slot 0. Writes into a reused buffer; do not store the reference.</summary>
        public long[] TakeTotal(long units)
        {
            for (int i = 0; i < _takeBuf.Length; i++)
                _takeBuf[i] = 0L;
            if (units <= 0L) return _takeBuf;
            // Down seats do not participate: their pile is the purse their respawn is
            // bought with, and a Convert that drained it would strand them for good.
            long total = 0L;
            for (int i = 0; i < _count; i++)
                if (!_down[i]) total += _unbanked[i];
            if (total <= 0L) return _takeBuf;
            if (units > total) units = total;

            long assigned = 0L;
            for (int i = 0; i < _count; i++)
            {
                if (_down[i]) continue;
                long share = units * _unbanked[i] / total;
                if (share > _unbanked[i]) share = _unbanked[i];
                _unbanked[i] -= share;
                _takeBuf[i] = share;
                assigned += share;
            }
            long leftover = units - assigned;
            if (leftover > 0L)
            {
                for (int i = 0; i < _count && leftover > 0L; i++)
                {
                    if (_down[i]) continue;
                    long take = leftover > _unbanked[i] ? _unbanked[i] : leftover;
                    _unbanked[i] -= take;
                    _takeBuf[i] += take;
                    leftover -= take;
                }
            }
            return _takeBuf;
        }
    }
}
