namespace GatekeepersProtocol.Sim
{
    /// <summary>FORGE's follower. Same phase chain and charge-by-firing rule as
    /// <see cref="TurretState"/>, plus a follow distance. Parallel type, not a subclass —
    /// a shared base with a machine-kind flag would hide that the two are meant to be read
    /// side by side. Reuses <see cref="TurretPhase"/> because the chain is the same chain.
    /// </summary>
    public sealed class OutriderState
    {
        public TurretPhase Phase { get; private set; }
        public bool IsLive =>
            Phase == TurretPhase.Deployed
            || Phase == TurretPhase.Special
            || Phase == TurretPhase.Exhausted;
        public bool MayFire =>
            Phase == TurretPhase.Deployed || Phase == TurretPhase.Special;
        public bool EmpActive => Phase == TurretPhase.Special;

        public float Hp { get; private set; }
        public float MaxHp { get; private set; }
        public float Charge { get; private set; }
        public float ChargeMax => BoarderProtocol.OutriderChargeMax;
        public float ExhaustedRemaining { get; private set; }
        public float FollowDistance => BoarderProtocol.OutriderFollowDistance;

        public float PosX { get; private set; }
        public float PosY { get; private set; }
        public float PosZ { get; private set; }

        public float DamageTaken { get; private set; }
        public int ShotsFired { get; private set; }

        public OutriderState()
        {
            MaxHp = BoarderProtocol.OutriderMaxHp;
            Phase = TurretPhase.Available;
        }

        public void ResetForRun()
        {
            MaxHp = BoarderProtocol.OutriderMaxHp;
            Clear();
        }

        /// <summary>Stand the follower at a world position. Replaces a live Deployed or Special
        /// machine and keeps its charge. Refuses Exhausted — that body is spent.</summary>
        public bool TryPlace(float x, float y, float z, out string reason)
        {
            if (MaxHp <= 0f)
            {
                reason = "outrider has no life to give";
                return false;
            }
            if (Phase == TurretPhase.Exhausted)
            {
                reason = "exhausted outrider cannot be refreshed";
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

        /// <summary>Follow tick writes the body back onto the sim so pairing can be tested
        /// without the scene.</summary>
        public void SetPosition(float x, float y, float z)
        {
            if (!IsLive) return;
            PosX = x;
            PosY = y;
            PosZ = z;
        }

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

        /// <summary>Spend earned charge on EMP. Refuses at zero charge, by that name.
        /// True when Special begins.</summary>
        public bool TryBeginEmp(out string reason)
        {
            if (Phase == TurretPhase.Special)
            {
                reason = "already running EMP";
                return false;
            }
            if (Phase == TurretPhase.Exhausted)
            {
                reason = "outrider is exhausted";
                return false;
            }
            if (Phase != TurretPhase.Deployed)
            {
                reason = "no outrider standing";
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

        /// <summary>Spend one round. Deployed raises charge; Special spends it and the round
        /// that empties the meter ends into Exhausted. True when THAT round spent the machine.
        /// </summary>
        public bool SpendShot()
        {
            if (!MayFire) return false;
            ShotsFired++;

            if (Phase == TurretPhase.Special)
            {
                Charge -= BoarderProtocol.OutriderChargePerShot;
                if (Charge > 0f) return false;
                Charge = 0f;
                EnterExhausted();
                return true;
            }

            Charge += BoarderProtocol.OutriderChargePerShot;
            if (Charge > ChargeMax) Charge = ChargeMax;
            return false;
        }

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

        /// <summary>Seconds between shots. Pairing passes <see cref="BoarderProtocol.OverchargeTurretRateMul"/>
        /// while the turret's Overcharge is lifting this machine; otherwise 1.</summary>
        public float ShotInterval(float rateMul)
        {
            return BoarderProtocol.OutriderShotInterval(rateMul);
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
            ExhaustedRemaining = BoarderProtocol.OutriderExhaustedSeconds;
        }
    }
}
