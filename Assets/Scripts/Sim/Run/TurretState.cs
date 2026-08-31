namespace GatekeepersProtocol.Sim
{
    /// <summary>FORGE turret lifecycle. Charge is earned by firing and spent on Overcharge;
    /// the machine ends by being spent (Exhausted), by enemy damage, or by the operator
    /// despawning an Exhausted body. No UnityEngine — same seam as <see cref="DecoyState"/>.</summary>
    public enum TurretPhase
    {
        Available = 0,
        Deployed = 1,
        Special = 2,
        Exhausted = 3,
        Despawned = 4,
    }

    public sealed class TurretState
    {
        public TurretPhase Phase { get; private set; }
        public bool IsLive =>
            Phase == TurretPhase.Deployed
            || Phase == TurretPhase.Special
            || Phase == TurretPhase.Exhausted;
        public bool MayFire =>
            Phase == TurretPhase.Deployed || Phase == TurretPhase.Special;
        public bool Overcharged => Phase == TurretPhase.Special;

        public float Hp { get; private set; }
        public float MaxHp { get; private set; }
        public float Charge { get; private set; }
        public float ChargeMax => BoarderProtocol.TurretChargeMax;
        public float ExhaustedRemaining { get; private set; }

        public float PosX { get; private set; }
        public float PosY { get; private set; }
        public float PosZ { get; private set; }

        /// <summary>Damage this machine has absorbed since it was placed. The counterpart to
        /// the decoy's tally: what the turret bought by standing where it stood.</summary>
        public float DamageTaken { get; private set; }

        /// <summary>Rounds fired since it was placed — the counterpart to
        /// <see cref="DamageTaken"/>: what the machine spent rather than what it absorbed.</summary>
        public int ShotsFired { get; private set; }

        public TurretState()
        {
            MaxHp = BoarderProtocol.TurretMaxHp;
            Phase = TurretPhase.Available;
        }

        public void ResetForRun()
        {
            MaxHp = BoarderProtocol.TurretMaxHp;
            Clear();
        }

        /// <summary>Stand a turret at a world position. Replaces a live Deployed or Special
        /// machine and keeps its charge. Refuses Exhausted — that body is spent, and a press
        /// despawns it rather than refreshing it.</summary>
        public bool TryPlace(float x, float y, float z, out string reason)
        {
            if (MaxHp <= 0f)
            {
                reason = "turret has no life to give";
                return false;
            }
            if (Phase == TurretPhase.Exhausted)
            {
                reason = "exhausted turret cannot be refreshed";
                return false;
            }

            float savedCharge = (Phase == TurretPhase.Deployed || Phase == TurretPhase.Special)
                ? Charge
                : 0f;

            PosX = x;
            PosY = y;
            PosZ = z;
            Hp = MaxHp;
            DamageTaken = 0f;
            ShotsFired = 0;
            Charge = savedCharge;
            ExhaustedRemaining = 0f;
            Phase = TurretPhase.Deployed;
            reason = null;
            return true;
        }

        /// <summary>Enemy damage intake. True when this hit destroys the turret.</summary>
        public bool TakeDamage(float amount)
        {
            if (!IsLive || amount <= 0f) return false;
            float applied = amount > Hp ? Hp : amount;
            Hp -= applied;
            DamageTaken += applied;
            if (Hp > 0f) return false;
            Hp = 0f;
            Phase = TurretPhase.Despawned;
            ExhaustedRemaining = 0f;
            return true;
        }

        /// <summary>Replication intake for a scenery copy: the server's Hp, so a client's life
        /// bar reads the truth instead of full-forever. Never flips <see cref="IsLive"/> —
        /// removal stays with the despawn message, so a zero arriving first just shows an
        /// empty bar on a machine about to fall.</summary>
        public void MirrorHp(float hp)
        {
            if (!IsLive) return;
            if (hp < 0f) hp = 0f;
            if (hp > MaxHp) hp = MaxHp;
            Hp = hp;
        }

        public void MirrorCharge(float charge)
        {
            if (!IsLive) return;
            if (charge < 0f) charge = 0f;
            if (charge > ChargeMax) charge = ChargeMax;
            Charge = charge;
        }

        public void MirrorPhase(TurretPhase phase)
        {
            if (!IsLive) return;
            if (phase != TurretPhase.Deployed
                && phase != TurretPhase.Special
                && phase != TurretPhase.Exhausted)
                return;
            Phase = phase;
            if (phase != TurretPhase.Exhausted) ExhaustedRemaining = 0f;
        }

        /// <summary>Spend the earned charge on Overcharge. Refuses at zero charge, by that
        /// name. True when Special begins.</summary>
        public bool TryBeginOvercharge(out string reason)
        {
            if (Phase == TurretPhase.Special)
            {
                reason = "already overcharged";
                return false;
            }
            if (Phase == TurretPhase.Exhausted)
            {
                reason = "turret is exhausted";
                return false;
            }
            if (Phase != TurretPhase.Deployed)
            {
                reason = "no turret standing";
                return false;
            }
            if (Charge <= 0f)
            {
                reason = "no charge";
                return false;
            }
            Phase = TurretPhase.Special;
            reason = null;
            return true;
        }

        /// <summary>Spend one round. While Deployed, that round raises charge. While Special,
        /// it spends charge, and the round that empties the meter ends into Exhausted.
        /// True when THAT round is what spent the machine into Exhausted.
        /// <para>Refuses while it may not fire, so an Exhausted or despawning turret cannot
        /// fire a farewell shot.</para></summary>
        public bool SpendShot()
        {
            if (!MayFire) return false;
            ShotsFired++;

            if (Phase == TurretPhase.Special)
            {
                Charge -= BoarderProtocol.TurretChargePerShot;
                if (Charge > 0f) return false;
                Charge = 0f;
                EnterExhausted();
                return true;
            }

            Charge += BoarderProtocol.TurretChargePerShot;
            if (Charge > ChargeMax) Charge = ChargeMax;
            return false;
        }

        /// <summary>Run the Exhausted window. True on the tick the body should despawn.</summary>
        public bool TickExhausted(float dt)
        {
            if (Phase != TurretPhase.Exhausted || dt <= 0f) return false;
            ExhaustedRemaining -= dt;
            if (ExhaustedRemaining > 0f) return false;
            ExhaustedRemaining = 0f;
            Phase = TurretPhase.Despawned;
            Hp = 0f;
            return true;
        }

        /// <summary>Seconds between shots right now — the overcharge multiplier folded in.</summary>
        public float ShotInterval()
        {
            return BoarderProtocol.TurretShotInterval(
                Overcharged ? BoarderProtocol.OverchargeTurretRateMul : 1f);
        }

        public void Clear()
        {
            Phase = TurretPhase.Available;
            Hp = 0f;
            Charge = 0f;
            ExhaustedRemaining = 0f;
            PosX = PosY = PosZ = 0f;
            DamageTaken = 0f;
            ShotsFired = 0;
        }

        void EnterExhausted()
        {
            Phase = TurretPhase.Exhausted;
            ExhaustedRemaining = BoarderProtocol.TurretExhaustedSeconds;
        }
    }
}
