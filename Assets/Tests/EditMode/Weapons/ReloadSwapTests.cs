using System.Collections.Generic;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.Tests
{
    /// <summary>M25 Path 1 — swap/reload arbitration. Asserts on progress fraction, not
    /// absolute seconds, so M21's reload spoke cannot break these.</summary>
    public class ReloadSwapTests
    {
        GameObject _go;
        PlayerGun _gun;
        MemorySettingsStore _store;

        [SetUp]
        public void SetUp()
        {
            _store = new MemorySettingsStore();
            Settings.Bind(_store);
            Settings.RestoreDefaults();

            _go = new GameObject("ReloadSwapTestGun");
            _gun = _go.AddComponent<PlayerGun>();

            var primary = WeaponCatalog.Def(WeaponId.Br12Shotgun);
            var secondary = WeaponCatalog.Def(WeaponId.Mk1Sidearm);
            _gun.Configure(WeaponSlot.Primary, primary, 0, WeaponId.Br12Shotgun);
            _gun.Configure(WeaponSlot.Secondary, secondary, 0, WeaponId.Mk1Sidearm);
            _gun.SelectSlot(WeaponSlot.Primary);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            _go = null;
            _gun = null;
            Settings.Unbind();
        }

        [Test]
        public void SwapDiscardsWhenSettingOn()
        {
            Assert.IsTrue(Settings.GetBool(SettingId.DiscardReloadOnSwap), "default must be on");

            _gun.BeginReload();
            float half = AdvanceToFraction(0.5f);
            Assert.AreEqual(0.5f, half, 0.05f, "precondition: mid-reload");

            LogAssert.Expect(LogType.Log,
                new System.Text.RegularExpressions.Regex(@"reload on Primary discarded at .* s remaining"));
            LogAssert.Expect(LogType.Log,
                new System.Text.RegularExpressions.Regex(@"switched to Secondary"));

            _gun.SelectSlot(WeaponSlot.Secondary);
            Assert.IsFalse(_gun.SlotIsReloading(WeaponSlot.Primary),
                "outgoing Primary reload must be zeroed");

            LogAssert.Expect(LogType.Log,
                new System.Text.RegularExpressions.Regex(@"switched to Primary"));
            _gun.SelectSlot(WeaponSlot.Primary);
            Assert.AreEqual(0f, _gun.SlotReloadProgress(WeaponSlot.Primary), 0.001f,
                "swap back: discarded progress stays zero");
        }

        [Test]
        public void SwapPreservesWhenSettingOff()
        {
            Settings.SetBool(SettingId.DiscardReloadOnSwap, false);

            _gun.BeginReload();
            float atHalf = AdvanceToFraction(0.5f);

            LogAssert.Expect(LogType.Log,
                new System.Text.RegularExpressions.Regex(@"switched to Secondary"));
            _gun.SelectSlot(WeaponSlot.Secondary);

            Assert.IsTrue(_gun.SlotIsReloading(WeaponSlot.Primary),
                "stowed Primary must keep its timer");
            Assert.AreEqual(atHalf, _gun.SlotReloadProgress(WeaponSlot.Primary), 0.05f);

            LogAssert.Expect(LogType.Log,
                new System.Text.RegularExpressions.Regex(@"switched to Primary"));
            _gun.SelectSlot(WeaponSlot.Primary);
            Assert.AreEqual(atHalf, _gun.SlotReloadProgress(WeaponSlot.Primary), 0.05f,
                "return: fraction unchanged");
        }

        [Test]
        public void StowedSlotNeverAdvances()
        {
            // Preserve mode is the load-bearing case: a kept timer must not tick while stowed.
            Settings.SetBool(SettingId.DiscardReloadOnSwap, false);
            _gun.BeginReload();
            float parked = AdvanceToFraction(0.4f);

            LogAssert.Expect(LogType.Log,
                new System.Text.RegularExpressions.Regex(@"switched to Secondary"));
            _gun.SelectSlot(WeaponSlot.Secondary);
            Assert.AreEqual(parked, _gun.SlotReloadProgress(WeaponSlot.Primary), 0.05f);

            for (int i = 0; i < 30; i++)
                _gun.TickReload(0.1f);

            Assert.AreEqual(parked, _gun.SlotReloadProgress(WeaponSlot.Primary), 0.05f,
                "stowed reload must not advance while Secondary is active");

            // Discard mode: after the zeroing swap there is nothing left to advance.
            Settings.SetBool(SettingId.DiscardReloadOnSwap, true);
            LogAssert.Expect(LogType.Log,
                new System.Text.RegularExpressions.Regex(@"switched to Primary"));
            _gun.SelectSlot(WeaponSlot.Primary);
            _gun.BeginReload();
            AdvanceToFraction(0.4f);
            LogAssert.Expect(LogType.Log,
                new System.Text.RegularExpressions.Regex(@"reload on Primary discarded"));
            LogAssert.Expect(LogType.Log,
                new System.Text.RegularExpressions.Regex(@"switched to Secondary"));
            _gun.SelectSlot(WeaponSlot.Secondary);
            Assert.AreEqual(0f, _gun.SlotReloadProgress(WeaponSlot.Primary), 0.001f);
            for (int i = 0; i < 30; i++)
                _gun.TickReload(0.1f);
            Assert.AreEqual(0f, _gun.SlotReloadProgress(WeaponSlot.Primary), 0.001f);
        }

        [Test]
        public void ResumedReloadIsInterruptible()
        {
            Settings.SetBool(SettingId.DiscardReloadOnSwap, false);

            // Full clip + BeginReload: rounds remain, so fire must cancel rather than lock out.
            int before = _gun.ClipRemaining;
            Assert.Greater(before, 0);
            _gun.BeginReload();
            AdvanceToFraction(0.5f);

            LogAssert.Expect(LogType.Log,
                new System.Text.RegularExpressions.Regex(@"switched to Secondary"));
            _gun.SelectSlot(WeaponSlot.Secondary);
            LogAssert.Expect(LogType.Log,
                new System.Text.RegularExpressions.Regex(@"switched to Primary"));
            _gun.SelectSlot(WeaponSlot.Primary);
            Assert.IsTrue(_gun.IsReloading, "precondition: resumed mid-reload");

            // TryFire cancels before CanFire — EditMode has no locked cursor, so the shot
            // itself may refuse; the reload must still clear and the clip stay.
            _gun.TryFire();
            Assert.IsFalse(_gun.IsReloading, "fire interrupts the resumed reload");
            Assert.AreEqual(before, _gun.ClipRemaining, "interrupt keeps the rounds");
        }

        [Test]
        public void CompletionLineNamesSlot()
        {
            _gun.BeginReload();
            LogAssert.Expect(LogType.Log,
                new System.Text.RegularExpressions.Regex(@"reloaded on Primary — clip \d+"));
            // Overshoot the remaining timer so completion fires once.
            for (int i = 0; i < 40; i++)
                _gun.TickReload(0.2f);
            Assert.IsFalse(_gun.IsReloading);
        }

        float AdvanceToFraction(float target)
        {
            // Drive in small steps until progress is near target. Duration comes from the
            // catalog via EffectiveReload — never hard-code seconds.
            for (int i = 0; i < 200; i++)
            {
                float p = _gun.SlotReloadProgress(WeaponSlot.Primary);
                if (p >= target - 0.02f) return p;
                _gun.TickReload(0.05f);
            }
            return _gun.SlotReloadProgress(WeaponSlot.Primary);
        }

        /// <summary>In-memory <see cref="ISettingsStore"/> — never touches real PlayerPrefs.</summary>
        sealed class MemorySettingsStore : ISettingsStore
        {
            readonly Dictionary<string, int> _ints = new Dictionary<string, int>();
            readonly Dictionary<string, float> _floats = new Dictionary<string, float>();

            public float GetFloat(string key, float defaultValue) =>
                _floats.TryGetValue(key, out float v) ? v : defaultValue;

            public void SetFloat(string key, float value) => _floats[key] = value;

            public int GetInt(string key, int defaultValue) =>
                _ints.TryGetValue(key, out int v) ? v : defaultValue;

            public void SetInt(string key, int value) => _ints[key] = value;

            public void Save() { }
        }
    }
}
