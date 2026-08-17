using System.Collections.Generic;
using System.Text;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER (partial of HudController): the weapon readout — the weapon card,
    /// slot strip, clip/reload row, ability and decoy lines, and the crosshair shape.</summary>
    public sealed partial class HudController
    {
        readonly List<string> _weaponLines = new List<string>(8);
        readonly StringBuilder _weaponBlock = new StringBuilder(256);

        string _cacheWeaponName = "\0";
        string _cacheWeaponAmmo = "\0";
        string _cacheWeaponDetail = "\0";

        void PushWeaponReadout(GameController gc)
        {
            var bay = gc != null ? gc.Decoys : null;
            _view.SetAbilityBlockVisible(false);
            _weaponLines.Clear();

            if (_gun == null)
            {
                ResolveRefs();
                if (_gun == null)
                {
                    PushWeaponCard("NO WEAPON", "—", "Buy the MK-1 at the ship's ARMORY.");
                    _view.SetReloadLabel("—");
                    _view.SetReloadBar(0f);
                    _view.SetReloadBlockVisible(false);
                    return;
                }
            }

            if (_gun.ActiveSlot == WeaponSlot.Drone)
            {
                PushDecoyReadout(_weaponLines, bay);
                _weaponLines.Add(SlotStrip(_gun, bay));
                PushWeaponCard("DECOY", SlotStrip(_gun, bay), JoinWeaponLines());
                // The decoy has no clip. Dim the reserved row; keep a label so the slot is not blank.
                _view.SetReloadLabel("—");
                _view.SetReloadBar(0f);
                _view.SetReloadBlockVisible(false);
                return;
            }

            string name = _gun.UsingSalvageCutter ? "Improvised cutter" : _gun.DisplayName;
            string ammo;
            // Clip bar: depletes left→right as you fire; refill left→right while reloading.
            _view.SetReloadBlockVisible(true);
            if (_gun.IsReloading)
            {
                _view.SetReloadLabel("RELOADING");
                _view.SetReloadBar(_gun.ReloadProgress);
                ammo = "RELOADING";
            }
            else
            {
                // Always-on clip row: keep the label filled so the reserved third row is never blank.
                _view.SetReloadLabel("CLIP");
                float cap = _gun.MagazineCapacity;
                float fill = cap > 0f ? _gun.ClipRemaining / cap : 0f;
                _view.SetReloadBar(fill);
                _lineBuf.Clear();
                _lineBuf.Append(_gun.ClipRemaining).Append('/').Append(_gun.MagazineCapacity);
                ammo = _lineBuf.ToString();
            }

            if (_gun.UsingSalvageCutter)
            {
                _lineBuf.Clear();
                _lineBuf.Append(_gun.EffectiveDamage.ToString("0")).Append(" dmg  ·  buy MK-1 at ARMORY");
                PushWeaponCard(name, ammo, _lineBuf.ToString());
                return;
            }

            string blocked = _gun.WeaponBlockedReason;
            if (!string.IsNullOrEmpty(blocked))
                _weaponLines.Add("HOLSTERED: " + blocked);

            _weaponLines.Add(SlotStrip(_gun, bay));
            _lineBuf.Clear();
            _lineBuf.Append(_gun.EffectiveDamage.ToString("0")).Append(" dmg | ")
                .Append(_gun.EffectiveFireRate.ToString("0.#")).Append("/s | clip ")
                .Append(_gun.MagazineCapacity);
            if (_gun.CritChance > 0f)
                _lineBuf.Append(" | ").Append((_gun.CritChance * 100f).ToString("0.#")).Append("% crit");
            _weaponLines.Add(_lineBuf.ToString());

            PushAbilityReadout(_weaponLines);
            PushDecoyReadout(_weaponLines, bay);
            PushWeaponCard(name, ammo, JoinWeaponLines());
        }

        void PushWeaponCard(string name, string ammo, string detail)
        {
            if (name != _cacheWeaponName || ammo != _cacheWeaponAmmo)
            {
                _cacheWeaponName = name;
                _cacheWeaponAmmo = ammo;
                _view.SetWeapon(name, ammo);
            }
            if (detail != _cacheWeaponDetail)
            {
                _cacheWeaponDetail = detail;
                _view.SetDrawerWeaponDetail(detail);
            }
        }

        string JoinWeaponLines()
        {
            _weaponBlock.Clear();
            for (int i = 0; i < _weaponLines.Count; i++)
            {
                if (i > 0) _weaponBlock.Append('\n');
                _weaponBlock.Append(_weaponLines[i]);
            }
            return _weaponBlock.ToString();
        }

        void PushDecoyReadout(List<string> lines, DecoyBay bay)
        {
            if (bay == null || !bay.HasPayload) return;
            _lineBuf.Clear();
            _lineBuf.Append("DECOY  ").Append(bay.State.ChargesRemaining).Append(" left  ·  ");
            if (bay.State.IsLive)
                _lineBuf.Append("LIVE ").Append(bay.State.Hp.ToString("0")).Append('/')
                    .Append(bay.State.MaxHp.ToString("0"));
            else
                _lineBuf.Append("ready");
            lines.Add(_lineBuf.ToString());
        }

        void PushAbilityReadout(List<string> lines)
        {
            if (_ability == null && _gun != null) _ability = _gun.GetComponent<PlayerAbility>();
            // Ability chrome lives in the right-drawer weapon detail string. A separate
            // Ability host was dual-wired to Weapon Detail and blanked / overwrote it.
            _view.SetAbilityBlockVisible(false);
            if (_ability == null || !_ability.HasAbility) return;

            string name = _ability.AbilityName;
            _lineBuf.Clear();
            if (_ability.RegenSurgeActive)
            {
                _lineBuf.Append("<b>ABILITY ").Append(name).Append(" — SURGING</b>");
            }
            else if (_ability.CooldownRemaining > 0.05f)
            {
                _lineBuf.Append("ABILITY ").Append(name).Append("  ")
                    .Append(_ability.CooldownRemaining.ToString("0.0")).Append('s');
            }
            else
            {
                _lineBuf.Append("ABILITY ").Append(name).Append("  [")
                    .Append(Controls.DisplayOf(ControlId.WeaponAbility)).Append(']');
                if (_ability.PinBuildHits > 0)
                    _lineBuf.Append("  [").Append(_ability.PinBuildHits).Append('/')
                        .Append(WeaponAbility.PinHitThreshold).Append(']');
            }
            lines.Add(_lineBuf.ToString());
        }

        static string SlotStrip(PlayerGun gun, DecoyBay bay)
        {
            return Mark(gun, bay, WeaponSlot.Primary, 1) + "   " +
                   Mark(gun, bay, WeaponSlot.Secondary, 2) + "   " +
                   Mark(gun, bay, WeaponSlot.Drone, 3);
        }

        static string Mark(PlayerGun gun, DecoyBay bay, WeaponSlot slot, int key)
        {
            bool live = gun.ActiveSlot == slot;
            string mark = live ? "▸" : "·";
            if (slot == WeaponSlot.Drone)
            {
                if (bay == null || !bay.HasPayload)
                    return key + (live ? " ▸ —" : " — —");
                return key + " " + mark + " Decoy" + (bay.State.IsLive ? " live" : "");
            }
            if (!gun.SlotConfigured(slot))
                return key + (live ? " ▸ —" : " — —");
            string name = WeaponCatalog.Def(gun.WeaponInSlot(slot)).displayName;
            int sp = name.IndexOf(' ');
            string shortName = sp > 0 && sp < 6 ? name.Substring(0, sp)
                : (name.Length > 8 ? name.Substring(0, 8) : name);
            string paused = !live
                && !Settings.GetBool(SettingId.DiscardReloadOnSwap)
                && gun.SlotIsReloading(slot)
                ? " reload paused"
                : "";
            return key + " " + mark + " " + shortName + paused;
        }

        void PushCrosshair()
        {
            WeaponCrosshair shape = _gun != null ? _gun.ActiveCrosshair : WeaponCrosshair.Cross;
            _view.SetCrosshair(shape switch
            {
                WeaponCrosshair.Dot => CrosshairShape.Dot,
                WeaponCrosshair.Brackets => CrosshairShape.Brackets,
                _ => CrosshairShape.Cross
            });
        }
    }
}
