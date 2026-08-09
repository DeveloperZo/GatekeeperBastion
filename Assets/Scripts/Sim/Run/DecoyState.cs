namespace GatekeeperBastion.Sim
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

        /// <summary>Attempt to place at world position. Returns false with a reason when refused.</summary>
        public bool TryPlace(float x, float y, float z, out string reason)
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

        public void ClearLive()
        {
            IsLive = false;
            Hp = 0f;
            PosX = PosY = PosZ = 0f;
        }

        /// <summary>Whether a Gatekeeper of this kind should path to the live decoy.
        /// Rams never pull. Defaults already committed to the Hatch never pull.
        /// Distance must be within the pull radius.</summary>
        public static bool ShouldPull(GatekeeperKind kind, bool committedToHatch,
            bool decoyLive, float distanceToDecoy, float pullRadius)
        {
            if (!decoyLive) return false;
            if (kind == GatekeeperKind.Ram) return false;
            if (committedToHatch) return false;
            if (pullRadius <= 0f) return false;
            return distanceToDecoy <= pullRadius;
        }
    }
}

