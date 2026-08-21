namespace GatekeepersProtocol.Sim
{
    /// <summary>Pure run-time state for FORGE's turret: one live machine, its life, and the
    /// overcharge burn. No UnityEngine — multiplayer seam, same shape as
    /// <see cref="DecoyState"/>. Placement is governed by the ability's cooldown rather than
    /// by charges, so there is no charge count here.</summary>
    public sealed class TurretState
    {
        public bool IsLive { get; private set; }
        public float Hp { get; private set; }
        public float MaxHp { get; private set; }
        public bool Overcharged { get; private set; }

        public float PosX { get; private set; }
        public float PosY { get; private set; }
        public float PosZ { get; private set; }

        /// <summary>Damage this machine has absorbed since it was placed. The counterpart to
        /// the decoy's tally: what the turret bought by standing where it stood.</summary>
        public float DamageTaken { get; private set; }

        /// <summary>Life spent by its own operator rather than by the enemy. Kept apart from
        /// <see cref="DamageTaken"/> so "the gatekeepers killed it" and "you redlined it to
        /// death" never read as the same event.</summary>
        public float LifeBurned { get; private set; }

        public TurretState()
        {
            MaxHp = BoarderProtocol.TurretMaxHp;
        }

        public void ResetForRun()
        {
            MaxHp = BoarderProtocol.TurretMaxHp;
            Clear();
        }

        /// <summary>Stand a turret at a world position. Replaces a live one — FIELD DEPLOY is
        /// one machine, and the cooldown is what rations it.</summary>
        public bool TryPlace(float x, float y, float z, out string reason)
        {
            if (MaxHp <= 0f)
            {
                reason = "turret has no life to give";
                return false;
            }
            PosX = x;
            PosY = y;
            PosZ = z;
            Hp = MaxHp;
            IsLive = true;
            Overcharged = false;
            DamageTaken = 0f;
            LifeBurned = 0f;
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
            IsLive = false;
            Overcharged = false;
            return true;
        }

        public void SetOvercharged(bool on)
        {
            Overcharged = IsLive && on;
        }

        /// <summary>Burn life for a span of overcharge. True when the burn is what killed it —
        /// the gatekeepers did not, the operator did.</summary>
        public bool BurnOvercharge(float dt, out float burned)
        {
            burned = 0f;
            if (!IsLive || !Overcharged || dt <= 0f) return false;
            float want = BoarderProtocol.OverchargeBurnForSeconds(dt);
            burned = want > Hp ? Hp : want;
            Hp -= burned;
            LifeBurned += burned;
            if (Hp > 0f) return false;
            Hp = 0f;
            IsLive = false;
            Overcharged = false;
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
            IsLive = false;
            Overcharged = false;
            Hp = 0f;
            PosX = PosY = PosZ = 0f;
            DamageTaken = 0f;
            LifeBurned = 0f;
        }
    }
}
