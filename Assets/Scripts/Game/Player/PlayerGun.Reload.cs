using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>Per-slot reload timers, swap discard/preserve, and the interrupt path — partial
    /// of <see cref="PlayerGun"/>. Stowed slots never tick.</summary>
    public sealed partial class PlayerGun
    {
        public bool IsReloading => SlotIsReloading(_activeSlot);

        public float ReloadProgress => SlotReloadProgress(_activeSlot);

        /// <summary>True when the given slot's reload timer is running (active or stowed).</summary>
        public bool SlotIsReloading(WeaponSlot slot) => _slots[(int)slot].reloadTimer > 0f;

        /// <summary>0..1 progress on the given slot's reload, or 0 when idle. Uses the same
        /// duration helper as <see cref="StartReload"/> so the wheel's reload spoke stays one place.</summary>
        public float SlotReloadProgress(WeaponSlot slot)
        {
            ref var s = ref _slots[(int)slot];
            if (s.reloadTimer <= 0f) return 0f;
            float reload = EffectiveReloadSeconds(slot);
            return 1f - Mathf.Clamp01(s.reloadTimer / Mathf.Max(0.01f, reload));
        }

        /// <summary>True when a non-active, non-Drone slot holds a configured weapon — the HUD
        /// warning only matters when a swap target exists.</summary>
        public bool HasAlternateWeapon
        {
            get
            {
                for (int i = 0; i < SlotCount; i++)
                {
                    if ((WeaponSlot)i == WeaponSlot.Drone) continue;
                    if ((WeaponSlot)i == _activeSlot) continue;
                    if (_slots[i].configured) return true;
                }
                return false;
            }
        }

        /// <summary>Short slot name for Combat log lines (no key hint).</summary>
        static string SlotName(WeaponSlot slot)
        {
            switch (slot)
            {
                case WeaponSlot.Primary: return "Primary";
                case WeaponSlot.Secondary: return "Secondary";
                default: return slot.ToString();
            }
        }

        /// <summary>Advance the active slot's reload by <paramref name="dt"/>. Stowed slots
        /// never tick — that is the whole distinction between preserve-on-swap and a free
        /// reload. EditMode tests call this with an injected step because Update is not run.</summary>
        public void TickReload(float dt)
        {
            ref var s = ref _slots[(int)_activeSlot];
            if (s.reloadTimer <= 0f) return;
            s.reloadTimer -= dt;
            if (s.reloadTimer > 0f) return;
            s.reloadTimer = 0f;
            s.clipRemaining = MagazineCapacity;
            if (s.reloadLogged)
                Log.Event("Combat", $"reloaded on {SlotName(_activeSlot)} — clip {MagazineCapacity}");
            s.reloadLogged = false;
        }

        /// <summary>Zero the active reload timer without changing the clip. Used when fire
        /// interrupts a resumed reload that still had rounds.</summary>
        void CancelActiveReload()
        {
            ref var s = ref _slots[(int)_activeSlot];
            s.reloadTimer = 0f;
            s.reloadLogged = false;
        }

        /// <summary>Begin a reload on the active slot. Public for EditMode tests that drive
        /// the reload path without going through Controls.</summary>
        public void BeginReload() => StartReload();

        void StartReload()
        {
            ref var s = ref _slots[(int)_activeSlot];
            s.reloadTimer = Mathf.Max(0.01f, EffectiveReloadSeconds(_activeSlot));
            s.reloadLogged = true;
            if (_view != null) _view.NotifyReload();
        }

        float EffectiveReloadSeconds(WeaponSlot slot)
        {
            ref var s = ref _slots[(int)slot];
            if (!s.configured) return reloadSeconds;
            return WeaponWheel.EffectiveReload(s.def, Point(s, WheelAttr.Reload));
        }

        /// <summary>On an outgoing mid-reload: discard (default) or keep progress per setting.</summary>
        void ApplyReloadOnSwap(ref SlotState outgoing)
        {
            if (outgoing.reloadTimer <= 0f) return;
            if (!Settings.GetBool(SettingId.DiscardReloadOnSwap)) return;

            float remaining = outgoing.reloadTimer;
            outgoing.reloadTimer = 0f;
            outgoing.reloadLogged = false;
            Log.Event("Combat",
                $"reload on {SlotName(_activeSlot)} discarded at {remaining:0.#} s remaining");
        }
    }
}
