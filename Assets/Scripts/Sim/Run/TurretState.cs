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

        /// <summary>Rounds left in the ordinary magazine. One of the two things that end a
        /// turret; <see cref="LifeRemaining"/> is the other, and whichever runs out first wins.
        /// </summary>
        public int ShotsRemaining { get; private set; }

        /// <summary>Seconds this machine has left to stand. Runs whether or not it is
        /// shooting, so a turret covering a quiet lane still expires.</summary>
        public float LifeRemaining { get; private set; }

        /// <summary>Rounds left on the OVERCHARGE belt. Separate from the magazine on purpose:
        /// redlining used to burn the turret's ordinary life and so charged the player twice
        /// for one spike.</summary>
        public int OverchargeShotsRemaining { get; private set; }

        /// <summary>Rounds fired since it was placed — the counterpart to
        /// <see cref="DamageTaken"/>: what the machine spent rather than what it absorbed.</summary>
        public int ShotsFired { get; private set; }

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
            ShotsRemaining = BoarderProtocol.TurretShotBudget;
            LifeRemaining = BoarderProtocol.TurretLifeSeconds;
            OverchargeShotsRemaining = BoarderProtocol.OverchargeShotBudget;
            ShotsFired = 0;
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

        /// <summary>Spend one round. True when THAT round was the last one and the machine is
        /// now done — the caller reports it as spent, never as destroyed, because running dry
        /// and being killed are different events and a player must be able to tell them apart.
        /// <para>Refuses while not live, so a despawning turret cannot fire a farewell shot.</para>
        /// </summary>
        public bool SpendShot()
        {
            if (!IsLive) return false;
            ShotsFired++;

            // WHILE REDLINED THE ROUNDS COME OFF THE OVERCHARGE BELT, never the magazine. That
            // is the whole point of the belt existing: a spike must not shorten the machine's
            // ordinary life. An overcharge that runs dry drops the turret back to normal fire
            // rather than killing it — it still has whatever the magazine held.
            if (Overcharged && OverchargeShotsRemaining > 0)
            {
                OverchargeShotsRemaining--;
                if (OverchargeShotsRemaining <= 0) Overcharged = false;
                return false;
            }

            if (ShotsRemaining > 0) ShotsRemaining--;
            if (ShotsRemaining > 0) return false;
            return Expire();
        }

        /// <summary>Run the life clock. True on the tick it expires — the machine's time was
        /// up, whatever it had left to shoot.</summary>
        public bool TickLife(float dt)
        {
            if (!IsLive || dt <= 0f) return false;
            LifeRemaining -= dt;
            if (LifeRemaining > 0f) return false;
            LifeRemaining = 0f;
            return Expire();
        }

        /// <summary>Shut the machine down for a reason that is NOT enemy damage. Kept apart
        /// from TakeDamage so "it ran out" and "they killed it" stay different events.</summary>
        bool Expire()
        {
            ShotsRemaining = 0;
            LifeRemaining = 0f;
            OverchargeShotsRemaining = 0;
            IsLive = false;
            Overcharged = false;
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
            IsLive = false;
            Overcharged = false;
            Hp = 0f;
            PosX = PosY = PosZ = 0f;
            DamageTaken = 0f;
            ShotsRemaining = 0;
            LifeRemaining = 0f;
            OverchargeShotsRemaining = 0;
            ShotsFired = 0;
            LifeBurned = 0f;
        }
    }
}
