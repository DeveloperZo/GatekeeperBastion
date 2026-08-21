namespace GatekeepersProtocol.Sim
{
    /// <summary>Pure run-time state for the decoy payload: charges, one live prop, and the
    /// gatekeeper-seconds tally. No UnityEngine — multiplayer seam.</summary>
    public sealed class DecoyState
    {
        public int ChargesRemaining { get; private set; }
        public bool IsLive { get; private set; }
        public float Hp { get; private set; }
        public float MaxHp { get; private set; }
        public float DamageTaken { get; private set; }
        public float PosX { get; private set; }
        public float PosY { get; private set; }
        public float PosZ { get; private set; }

        public float PullRadius { get; private set; }
        public float PlaceRange { get; private set; }
        public int MaxLive { get; private set; }
        public DoctrineMode Mode { get; private set; }

        /// <summary>Damage absorbed this Wave, converted at <see cref="PayloadCatalog.DecoyEffectiveDps"/>.</summary>
        public float AbsorbedSeconds => DamageTaken / PayloadCatalog.DecoyEffectiveDps;

        /// <summary>Wave-scoped damage for the per-Wave log line; reset on Wave boundary.</summary>
        public float WaveDamageTaken { get; private set; }

        public float WaveAbsorbedSeconds => WaveDamageTaken / PayloadCatalog.DecoyEffectiveDps;

        /// <summary>Load catalog stats and reset charges for a new Run. Call at Run start only.</summary>
        public void ResetForRun(PayloadId equipped)
        {
            var def = PayloadCatalog.Def(equipped);
            ChargesRemaining = equipped == PayloadId.Decoy ? def.charges : 0;
            MaxHp = def.hp;
            PullRadius = def.pullRadius;
            PlaceRange = def.placeRange;
            MaxLive = def.maxLive;
            ClearLive();
            DamageTaken = 0f;
            WaveDamageTaken = 0f;
        }

        public void BeginWaveTally()
        {
            WaveDamageTaken = 0f;
        }

        /// <summary>Attempt to place at world position. STOCK (0) when callers omit a mode.</summary>
        public bool TryPlace(float x, float y, float z, out string reason)
        {
            return TryPlace(x, y, z, DoctrineMode.Stock, out reason);
        }

        /// <summary>Attempt to place at world position. Writes <paramref name="mode"/> on success.</summary>
        public bool TryPlace(float x, float y, float z, DoctrineMode mode, out string reason)
        {
            if (ChargesRemaining <= 0)
            {
                reason = "no charges remaining";
                return false;
            }
            if (IsLive && MaxLive <= 1)
            {
                reason = "one decoy already live";
                return false;
            }

            PosX = x;
            PosY = y;
            PosZ = z;
            Hp = MaxHp;
            IsLive = true;
            Mode = mode;
            ChargesRemaining--;
            reason = null;
            return true;
        }

        /// <summary>Apply Hatch-damage from an attacker. Returns true when this hit destroys the decoy.</summary>
        public bool TakeDamage(float amount)
        {
            if (!IsLive || amount <= 0f) return false;
            float applied = amount;
            if (applied > Hp) applied = Hp;
            Hp -= applied;
            DamageTaken += applied;
            WaveDamageTaken += applied;
            if (Hp > 0f) return false;
            Hp = 0f;
            IsLive = false;
            return true;
        }

        /// <summary>REDEPLOY: move a live decoy without spending a charge and without
        /// restoring HP. Placing again is not a new placement — the machine keeps the life it
        /// had, which is the whole trade against simply placing a fresh one.</summary>
        public bool TryRelocate(float x, float y, float z, out string reason)
        {
            if (!IsLive)
            {
                reason = "no live decoy to carry";
                return false;
            }
            PosX = x;
            PosY = y;
            PosZ = z;
            reason = null;
            return true;
        }

        /// <summary>FIELD REARM: restore hardware life, capped at <see cref="MaxHp"/>. Returns
        /// the HP actually restored, which is 0 on a dead or already-full machine.</summary>
        public float Repair(float amount)
        {
            if (!IsLive || amount <= 0f) return 0f;
            float room = MaxHp - Hp;
            if (room <= 0f) return 0f;
            float applied = amount > room ? room : amount;
            Hp += applied;
            return applied;
        }

        public void ClearLive()
        {
            IsLive = false;
            Hp = 0f;
            PosX = PosY = PosZ = 0f;
            Mode = DoctrineMode.Stock;
        }

        /// <summary>Whether a Gatekeeper of this kind should path to the live decoy.
        /// Thieves never pull. Defaults already committed to the Hatch never pull.
        /// Distance must be within the pull radius.</summary>
        public static bool ShouldPull(GatekeeperKind kind, bool committedToHatch,
            bool decoyLive, float distanceToDecoy, float pullRadius)
        {
            if (!decoyLive) return false;
            if (kind == GatekeeperKind.Thief) return false;
            if (committedToHatch) return false;
            if (pullRadius <= 0f) return false;
            return distanceToDecoy <= pullRadius;
        }
    }
}

