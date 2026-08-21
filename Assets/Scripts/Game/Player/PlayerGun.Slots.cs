using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER (partial of PlayerGun): slot select, including the drone bay.
    /// Fire on the drone slot is placement, not a shot.</summary>
    public sealed partial class PlayerGun
    {
        PlayerProtocolAbilities _protocolAbilities;

        #region Public API
        /// <summary>Mark the drone slot selectable when the bay has a payload. No WeaponDef.</summary>
        public void ConfigureDrone(bool hasPayload)
        {
            int i = (int)WeaponSlot.Drone;
            _slots[i] = default;
            _slots[i].configured = hasPayload;
            if (!hasPayload && _activeSlot == WeaponSlot.Drone)
                SelectSlot(WeaponSlot.Secondary);
        }

        public float EffectiveFireRate
        {
            get
            {
                ref var s = ref _slots[(int)_activeSlot];
                float rate = s.configured
                    ? WeaponWheel.EffectiveFireRate(s.def, Point(s, WheelAttr.FireRate))
                    : shotsPerSecond;
                rate = UsingSalvageCutter ? rate * CutterRateFactor : rate;
                if (_activeSlot != WeaponSlot.Drone)
                {
                    if (_protocolAbilities == null)
                        _protocolAbilities = GetComponentInParent<PlayerProtocolAbilities>();
                    float self = _protocolAbilities != null
                        ? _protocolAbilities.SelfFireMul
                        : 1f;
                    // Zone sources SET rather than stack: the strongest aura in reach wins,
                    // so standing in two of them is worth no more than the better one.
                    float zone = BarrageField.FireMulAt(transform.position);
                    float turret = TurretObject.FireMulAt(transform.position);
                    if (turret > zone) zone = turret;
                    if (_protocolAbilities != null && _protocolAbilities.BarrageWindowOpen
                        && BoarderProtocol.BarrageFireMul > zone)
                        zone = BoarderProtocol.BarrageFireMul;
                    rate *= BoarderProtocol.ComposeFireMul(self, zone);
                }
                return Mathf.Max(0.01f, rate);
            }
        }

        public string DisplayName
        {
            get
            {
                if (_activeSlot == WeaponSlot.Drone) return "Decoy";
                if (UsingSalvageCutter) return "Improvised Cutter";
                ref var s = ref _slots[(int)_activeSlot];
                return s.configured ? s.def.displayName : "Weapon";
            }
        }

        public WeaponCrosshair ActiveCrosshair
        {
            get
            {
                ref var s = ref _slots[(int)_activeSlot];
                return s.configured ? s.def.crosshair : WeaponCrosshair.Cross;
            }
        }

        /// <summary>True when the given slot has a configured weapon.</summary>
        public bool SlotConfigured(WeaponSlot slot) => _slots[(int)slot].configured;

        public WeaponId WeaponInSlot(WeaponSlot slot) =>
            _slots[(int)slot].configured ? _slots[(int)slot].id : WeaponId.None;

        /// <summary>Select a configured slot; public for starting loadout and tests.</summary>
        public void SelectSlot(WeaponSlot slot)
        {
            // Empty slots are no-ops (no Primary owned → 1 does nothing).
            if (!_slots[(int)slot].configured) return;
            if (slot == _activeSlot) return;

            ref var outgoing = ref _slots[(int)_activeSlot];
            ApplyReloadOnSwap(ref outgoing);

            _previousSlot = _activeSlot;
            _activeSlot = slot;
            _cooldown = 0f;
            ref var incoming = ref _slots[(int)_activeSlot];
            if (incoming.configured) ApplyTracerFromDef(incoming.def);
            Log.Event("Combat", $"switched to {SlotLabel(_activeSlot)}");
        }
        #endregion

        #region Private
        void TickSlotInput()
        {
            if (Controls.Down(ControlId.SlotPrimary))
                SelectSlot(WeaponSlot.Primary);
            if (Controls.Down(ControlId.SlotSecondary))
                SelectSlot(WeaponSlot.Secondary);
            if (Controls.Down(ControlId.SlotDrone))
                SelectSlot(WeaponSlot.Drone);

            if (Controls.Down(ControlId.SlotLast))
                SelectSlot(_previousSlot);

            float scroll = Controls.ScrollDelta();
            if (Mathf.Abs(scroll) > 0.01f)
                CycleSlot(scroll > 0f ? 1 : -1);
        }

        void TickDroneFire()
        {
            if (!Controls.Down(ControlId.Fire)) return;
            var place = GetComponent<DecoyPlacement>();
            if (place != null) place.OnFirePressed();
        }

        void CycleSlot(int direction)
        {
            // Cycle non-empty slots only.
            int start = (int)_activeSlot;
            for (int step = 1; step <= SlotCount; step++)
            {
                int idx = (start + direction * step) % SlotCount;
                if (idx < 0) idx += SlotCount;
                var candidate = (WeaponSlot)idx;
                if (!_slots[idx].configured) continue;
                SelectSlot(candidate);
                return;
            }
        }

        static string SlotLabel(WeaponSlot slot)
        {
            switch (slot)
            {
                case WeaponSlot.Primary: return "Primary (1)";
                case WeaponSlot.Secondary: return "Secondary (2)";
                case WeaponSlot.Drone: return "Drone (3)";
                default: return slot.ToString();
            }
        }
        #endregion
    }
}
