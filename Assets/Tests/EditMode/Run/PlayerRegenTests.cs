using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>EditMode tests for <see cref="PlayerRegen"/> — the passive health regen rule
    /// added after the 2026-08-25 composite death (cleared Wave 1 at 22 HP, dead 4.1 s into
    /// Wave 2 with no route back to fighting strength).</summary>
    /// <remarks>Every tick here injects dt; the rule reads no clock, so these run with no scene.</remarks>
    public class PlayerRegenTests
    {
        const float MaxHp = 100f;

        /// <summary>Advance <paramref name="seconds"/> at a fixed step and return the HP reached.</summary>
        static float Run(ref PlayerRegen regen, float hp, float seconds, float dt = 0.05f)
        {
            int steps = (int)(seconds / dt);
            for (int i = 0; i < steps; i++) hp = regen.Tick(dt, hp, MaxHp, isDead: false);
            return hp;
        }

        /// <summary>Advance <paramref name="seconds"/> while killing one gatekeeper every
        /// <paramref name="killEvery"/> seconds. Returns the HP reached.</summary>
        static float RunKilling(ref PlayerRegen regen, float hp, float seconds, float killEvery,
                                float dt = 0.05f)
        {
            int steps = (int)(seconds / dt);
            float sinceKill = 0f;
            for (int i = 0; i < steps; i++)
            {
                sinceKill += dt;
                if (sinceKill >= killEvery)
                {
                    sinceKill -= killEvery;
                    regen.NotifyKill();
                }
                hp = regen.Tick(dt, hp, MaxHp, isDead: false);
            }
            return hp;
        }

        // ---- death ----------------------------------------------------------------

        [Test]
        public void Dead_DoesNotRegen()
        {
            var regen = new PlayerRegen();
            float hp = 0f;
            for (int i = 0; i < 400; i++) hp = regen.Tick(0.05f, hp, MaxHp, isDead: true);
            Assert.AreEqual(0f, hp, 0.0001f, "a corpse regenerated");
        }

        [Test]
        public void Dead_DoesNotDecayTheBoost_NorServeTheDelay()
        {
            var regen = new PlayerRegen();
            regen.NotifyKill();
            regen.NotifyDamaged();
            float boost = regen.KillBoostPerSecond;
            for (int i = 0; i < 400; i++) regen.Tick(0.05f, 50f, MaxHp, isDead: true);
            Assert.AreEqual(boost, regen.KillBoostPerSecond, 0.0001f, "boost decayed while dead");
            Assert.AreEqual(PlayerRegen.DamageDelaySeconds, regen.DelayRemaining, 0.0001f,
                "damage hold ran down while dead");
        }

        // ---- the ceiling ----------------------------------------------------------

        [Test]
        public void StopsAtMax_AndNeverOvershoots()
        {
            var regen = new PlayerRegen();
            // 60 s of maximum-boost regen from 99 HP: far more headroom than 1 HP of room.
            for (int i = 0; i < 20; i++) regen.NotifyKill();
            float hp = RunKilling(ref regen, 99f, 60f, killEvery: 0.5f);
            Assert.AreEqual(MaxHp, hp, 0.0001f, "regen did not settle exactly at max");
        }

        [Test]
        public void HidingReachesFull_ButSlowly()
        {
            var regen = new PlayerRegen();
            // The log's player: 22/100. Base rate is 0.8 HP/s, so 78 HP wants ~97.5 s.
            float hp = Run(ref regen, 22f, 90f);
            Assert.Less(hp, MaxHp, "hiding reached full inside 90 s — the base rate is too fast");
            hp = Run(ref regen, hp, 20f);
            Assert.AreEqual(MaxHp, hp, 0.0001f, "hiding never reached full — Zo asked for full");
        }

        // ---- kills raise the rate -------------------------------------------------

        [Test]
        public void Kill_RaisesTheRate()
        {
            var regen = new PlayerRegen();
            float cold = regen.RatePerSecond;
            regen.NotifyKill();
            Assert.Greater(regen.RatePerSecond, cold, "a kill did not raise the rate");
            Assert.AreEqual(PlayerRegen.BaseRegenPerSecond + PlayerRegen.RegenPerKill,
                regen.RatePerSecond, 0.0001f);
        }

        [Test]
        public void FightingHealsStrictlyFasterThanHiding()
        {
            var hiding = new PlayerRegen();
            var slow = new PlayerRegen();
            var fast = new PlayerRegen();

            float hidHp = Run(ref hiding, 22f, 30f);
            float slowHp = RunKilling(ref slow, 22f, 30f, killEvery: 3f);
            float fastHp = RunKilling(ref fast, 22f, 30f, killEvery: 1.5f);

            Assert.Greater(slowHp, hidHp, "killing every 3 s healed no better than hiding");
            Assert.Greater(fastHp, slowHp, "killing twice as often healed no better");
        }

        [Test]
        public void KillCadenceRecoversTheCompositeDeath()
        {
            // The justification in PlayerRegen's remarks: 22 HP back to full at one kill per
            // 3 s should land near 35 s, and never anywhere near the 97.5 s of hiding.
            var regen = new PlayerRegen();
            float hp = RunKilling(ref regen, 22f, 45f, killEvery: 3f);
            Assert.AreEqual(MaxHp, hp, 0.0001f,
                "a kill every 3 s did not restore a 22 HP player inside 45 s");
        }

        // ---- the boost is bounded and decays --------------------------------------

        [Test]
        public void Boost_IsCapped()
        {
            var regen = new PlayerRegen();
            for (int i = 0; i < 200; i++) regen.NotifyKill();
            Assert.AreEqual(PlayerRegen.MaxKillBoostPerSecond, regen.KillBoostPerSecond, 0.0001f,
                "200 kills stacked past the cap");
            Assert.AreEqual(PlayerRegen.BaseRegenPerSecond + PlayerRegen.MaxKillBoostPerSecond,
                regen.RatePerSecond, 0.0001f);
        }

        [Test]
        public void CappedRate_StillLosesToOneGatekeeper()
        {
            // 6 damage per 0.7 s swing = 8.57 dps. The cap exists so that no amount of killing
            // makes standing in melee survivable; if this ever flips, regen is a tank build.
            var regen = new PlayerRegen();
            for (int i = 0; i < 200; i++) regen.NotifyKill();
            const float DefaultGatekeeperDps = 6f / 0.7f;
            Assert.Less(regen.RatePerSecond, DefaultGatekeeperDps,
                "max regen outheals a single Default gatekeeper");
        }

        [Test]
        public void Boost_DecaysByHalfEachHalfLife()
        {
            var regen = new PlayerRegen();
            regen.NotifyKill();
            float start = regen.KillBoostPerSecond;
            Run(ref regen, 50f, PlayerRegen.KillBoostHalfLifeSeconds, dt: 0.01f);
            Assert.AreEqual(start * 0.5f, regen.KillBoostPerSecond, 0.01f,
                "boost did not halve over one half-life");
        }

        [Test]
        public void Boost_DecaysToNothingWhenTheKillingStops()
        {
            var regen = new PlayerRegen();
            for (int i = 0; i < 20; i++) regen.NotifyKill();
            Run(ref regen, 50f, 90f);
            Assert.AreEqual(0f, regen.KillBoostPerSecond, 0.0001f,
                "boost survived 90 s without a kill");
            Assert.AreEqual(PlayerRegen.BaseRegenPerSecond, regen.RatePerSecond, 0.0001f);
        }

        // ---- the post-damage hold -------------------------------------------------

        [Test]
        public void Damage_HoldsRegenForTheDelay()
        {
            var regen = new PlayerRegen();
            regen.NotifyDamaged();
            float hp = Run(ref regen, 50f, PlayerRegen.DamageDelaySeconds - 0.1f);
            Assert.AreEqual(50f, hp, 0.0001f, "regen ran during the post-damage hold");

            hp = Run(ref regen, hp, 5f);
            Assert.Greater(hp, 50f, "regen never resumed after the hold expired");
        }

        [Test]
        public void SustainedDamage_KeepsRegenAtZero()
        {
            // A Default swings every 0.7 s and the hold is 1 s, so a body actually on you
            // pins regen at zero for as long as it stays on you.
            var regen = new PlayerRegen();
            float hp = 50f;
            float sinceSwing = 0f;
            regen.NotifyDamaged(); // the first swing lands before the window opens
            for (int i = 0; i < 400; i++) // 20 s at dt 0.05
            {
                sinceSwing += 0.05f;
                if (sinceSwing >= 0.7f) { sinceSwing -= 0.7f; regen.NotifyDamaged(); }
                hp = regen.Tick(0.05f, hp, MaxHp, isDead: false);
            }
            Assert.AreEqual(50f, hp, 0.0001f, "regen leaked through sustained melee");
        }

        [Test]
        public void Damage_DoesNotBankTheBoost()
        {
            // Tanking must not preserve a boost: it decays on wall clock through the hold.
            var regen = new PlayerRegen();
            regen.NotifyKill();
            float start = regen.KillBoostPerSecond;
            float hp = 50f;
            for (int i = 0; i < 120; i++) // 6 s, re-held every 0.7 s
            {
                if (i % 14 == 0) regen.NotifyDamaged();
                hp = regen.Tick(0.05f, hp, MaxHp, isDead: false);
            }
            Assert.Less(regen.KillBoostPerSecond, start * 0.6f,
                "boost was banked through the damage hold");
        }

        // ---- housekeeping ---------------------------------------------------------

        [Test]
        public void Reset_ClearsBoostAndDelay()
        {
            var regen = new PlayerRegen();
            regen.NotifyKill();
            regen.NotifyDamaged();
            regen.Reset();
            Assert.AreEqual(0f, regen.KillBoostPerSecond, 0.0001f);
            Assert.AreEqual(0f, regen.DelayRemaining, 0.0001f);
            Assert.AreEqual(PlayerRegen.BaseRegenPerSecond, regen.RatePerSecond, 0.0001f);
        }

        [Test]
        public void NonPositiveDt_IsANoOp()
        {
            var regen = new PlayerRegen();
            regen.NotifyKill();
            float boost = regen.KillBoostPerSecond;
            Assert.AreEqual(50f, regen.Tick(0f, 50f, MaxHp, isDead: false), 0.0001f);
            Assert.AreEqual(50f, regen.Tick(-1f, 50f, MaxHp, isDead: false), 0.0001f);
            Assert.AreEqual(boost, regen.KillBoostPerSecond, 0.0001f);
        }

        [Test]
        public void RateIsIndependentOfStepSize()
        {
            var coarse = new PlayerRegen();
            var fine = new PlayerRegen();
            float a = RunKilling(ref coarse, 22f, 20f, killEvery: 2f, dt: 0.1f);
            float b = RunKilling(ref fine, 22f, 20f, killEvery: 2f, dt: 0.01f);
            Assert.AreEqual(a, b, 0.5f, "regen drifts with frame rate");
        }
    }
}
