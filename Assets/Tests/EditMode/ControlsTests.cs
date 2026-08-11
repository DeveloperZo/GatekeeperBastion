using System;
using System.Collections.Generic;
using GatekeeperBastion.Game;
using NUnit.Framework;
using UnityEngine;

namespace GatekeeperBastion.Tests
{
    /// <summary>M15: the binding list is the single source — no live overlap on a KeyCode,
    /// every id resolves, and a throwaway row appears on the panel with no UI edit.
    /// EditMode on purpose: these pins need no scene.</summary>
    public class ControlsTests
    {
        [TearDown]
        public void TearDown()
        {
            Controls.ClearTestBindings();
            Controls.SetCollapsed(false);
            Controls.SetMissionDrawerOpen(false);
        }

        [Test]
        public void ToggleMissionDrawer_IsTab_FightOnly_HiddenFromPanel()
        {
            var rows = Controls.All;
            int idx = -1;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Id == ControlId.ToggleMissionDrawer) { idx = i; break; }
            }
            Assert.GreaterOrEqual(idx, 0, "ToggleMissionDrawer binding missing");
            var row = rows[idx];
            Assert.AreEqual(KeyCode.Tab, row.Key);
            Assert.IsFalse(row.ShowOnPanel);
            Assert.AreEqual(Controls.FightAlways, row.Context & Controls.FightAlways);
            Assert.AreEqual(ControlContext.None, row.Context & ControlContext.Ship,
                "mission drawer must not be live on Ship");
        }

        [Test]
        public void NoOverlappingContext_SharesAKey()
        {
            var rows = Controls.All;
            for (int i = 0; i < rows.Count; i++)
            {
                var a = rows[i];
                if (a.DisplayOnly || !IsPlayerFacing(a)) continue;
                for (int j = i + 1; j < rows.Count; j++)
                {
                    var b = rows[j];
                    if (b.DisplayOnly || !IsPlayerFacing(b)) continue;
                    if ((a.Context & b.Context) == 0) continue;

                    AssertNoSharedKey(a, b);
                }
            }
        }

        [Test]
        public void EveryId_HasAtLeastOneRow()
        {
            foreach (ControlId id in Enum.GetValues(typeof(ControlId)))
            {
                bool found = false;
                var rows = Controls.All;
                for (int i = 0; i < rows.Count; i++)
                {
                    if (rows[i].Id == id) { found = true; break; }
                }
                Assert.IsTrue(found, $"ControlId.{id} has no binding row");
            }
        }

        [Test]
        public void EveryPollableRow_HasARealKey()
        {
            var rows = Controls.All;
            for (int i = 0; i < rows.Count; i++)
            {
                var b = rows[i];
                if (b.DisplayOnly) continue;
                Assert.AreNotEqual(KeyCode.None, b.Key,
                    $"{b.Id}/{b.Display} is pollable but Key is None");
            }
        }

        [Test]
        public void ThrowawayBinding_AppearsInPanelRows()
        {
            var probe = new ControlBinding(
                ControlId.Move, KeyCode.F15, KeyCode.None, "F15", "probe", "TEST",
                ControlContext.Prep);

            int before;
            var buf = new List<ControlBinding>(32);
            Controls.CollectPanelRows(ControlContext.Prep, buf);
            before = CountLabel(buf, "probe");

            using (Push(probe))
            {
                Controls.CollectPanelRows(ControlContext.Prep, buf);
                Assert.AreEqual(before + 1, CountLabel(buf, "probe"),
                    "Adding a binding must surface it on the panel row set with no UI edit");
            }

            Controls.CollectPanelRows(ControlContext.Prep, buf);
            Assert.AreEqual(before, CountLabel(buf, "probe"), "dispose must remove the probe");
        }

        [Test]
        public void DialAndSlots_DoNotShareNumberRow()
        {
            // Criterion 3 generalised: Prep dial must not use Alpha1/2/3 (weapon slots).
            var rows = Controls.All;
            for (int i = 0; i < rows.Count; i++)
            {
                var b = rows[i];
                if ((b.Context & ControlContext.Prep) == 0) continue;
                if (b.Id != ControlId.DialUp && b.Id != ControlId.DialDown) continue;
                Assert.AreNotEqual(KeyCode.Alpha1, b.Key);
                Assert.AreNotEqual(KeyCode.Alpha2, b.Key);
                Assert.AreNotEqual(KeyCode.Alpha3, b.Key);
                Assert.AreNotEqual(KeyCode.Alpha0, b.Key);
                Assert.AreNotEqual(KeyCode.Keypad1, b.Key);
                Assert.AreNotEqual(KeyCode.Keypad2, b.Key);
                Assert.AreNotEqual(KeyCode.Keypad3, b.Key);
                Assert.AreNotEqual(KeyCode.Keypad0, b.Key);
            }
        }

        [Test]
        public void PrepNumberRow_OnlyBelongsToWeaponSlots()
        {
            // The embark collision: anything live in Prep on Alpha0–9 / keypad must be a slot.
            // Dial used to be Alpha0+level; this pins that never returns.
            var rows = Controls.All;
            for (int i = 0; i < rows.Count; i++)
            {
                var b = rows[i];
                if (b.DisplayOnly) continue;
                if ((b.Context & ControlContext.Prep) == 0) continue;
                if (!IsNumberRow(b.Key) && !IsNumberRow(b.AltKey)) continue;

                Assert.IsTrue(
                    b.Id == ControlId.SlotPrimary
                    || b.Id == ControlId.SlotSecondary
                    || b.Id == ControlId.SlotDrone,
                    $"Prep-live number-row key {b.Key}/{b.AltKey} on {b.Id} — only weapon slots may own it");
            }
        }

        static bool IsNumberRow(KeyCode key) =>
            (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
            || (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9);

        static bool IsPlayerFacing(ControlBinding b) => b.ShowOnPanel || b.Id == ControlId.TogglePanel;

        static void AssertNoSharedKey(ControlBinding a, ControlBinding b)
        {
            if (a.Key != KeyCode.None)
            {
                Assert.AreNotEqual(a.Key, b.Key,
                    $"Key {a.Key} shared by {a.Id} ({a.Context}) and {b.Id} ({b.Context})");
                if (b.AltKey != KeyCode.None)
                    Assert.AreNotEqual(a.Key, b.AltKey,
                        $"Key {a.Key} on {a.Id} collides with alt of {b.Id}");
            }
            if (a.AltKey != KeyCode.None)
            {
                Assert.AreNotEqual(a.AltKey, b.Key,
                    $"Alt {a.AltKey} on {a.Id} collides with {b.Id}");
                if (b.AltKey != KeyCode.None)
                    Assert.AreNotEqual(a.AltKey, b.AltKey,
                        $"Alt {a.AltKey} shared by {a.Id} and {b.Id}");
            }
        }

        static int CountLabel(List<ControlBinding> rows, string label)
        {
            int n = 0;
            for (int i = 0; i < rows.Count; i++)
                if (rows[i].Label == label) n++;
            return n;
        }

        static IDisposable Push(ControlBinding b)
        {
            var remove = Controls.PushForTest(b);
            return new DisposableAction(remove);
        }

        sealed class DisposableAction : IDisposable
        {
            readonly Action _action;
            public DisposableAction(Action action) => _action = action;
            public void Dispose() => _action?.Invoke();
        }
    }
}
