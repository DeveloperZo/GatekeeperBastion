using GatekeeperBastion.Sim;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: Armory console — contextual per equipped weapon. Slot toggle picks
    /// which weapon fills Primary/Secondary; the upgrade wheel centres an attribute and buys.</summary>
    public sealed class ArmoryStation : StationTerminal
    {
        WeaponSlot _viewSlot = WeaponSlot.Secondary;
        readonly int[] _centreIndexByWeapon = new int[WeaponCatalog.ArraySize];
        bool _wheelDirty;

        protected override string StationName => "ARMORY";

        protected override string PromptHint =>
            GameSession.Meta != null && !GameSession.Meta.OwnsAnyWeapon
                ? "buy a Secondary — both cost nothing; pick the shape you want"
                : "equip and focus-upgrade the weapon in your hands";

        protected override void TickWhileOpen(MetaState m)
        {
            if (Controls.Down(ControlId.WheelRotatePrev)) { RotateCentre(-1, m); _wheelDirty = true; }
            if (Controls.Down(ControlId.WheelRotateNext)) { RotateCentre(+1, m); _wheelDirty = true; }
        }

        protected override int ContentHash(MetaState m)
        {
            unchecked
            {
                int h = base.ContentHash(m);
                h = h * 397 ^ (int)_viewSlot;
                h = h * 397 ^ (_wheelDirty ? 1 : 0);
                WeaponId eq = m.Equipped(_viewSlot);
                h = h * 397 ^ (int)eq;
                if (eq != WeaponId.None) h = h * 397 ^ _centreIndexByWeapon[(int)eq];
                h = h * 397 ^ (m.OwnsAnyWeapon ? 1 : 0);
                return h;
            }
        }

        protected override void BuildPanel(StationPanelView view, MetaState m)
        {
            _wheelDirty = false;
            // rotate is also handled in TickWhileOpen; keep BuildPanel free of input polls.

            view.BeginRow();
            view.AddButton(_viewSlot == WeaponSlot.Primary ? "[ PRIMARY ]" : "  Primary  ",
                () => _viewSlot = WeaponSlot.Primary);
            view.AddButton(_viewSlot == WeaponSlot.Secondary ? "[ SECONDARY ]" : "  Secondary  ",
                () => _viewSlot = WeaponSlot.Secondary);
            view.EndRow();
            view.AddSpacer();

            WeaponId equipped = m.Equipped(_viewSlot);
            if (equipped == WeaponId.None)
            {
                view.AddLabel($"{_viewSlot}: empty");
                view.AddLabel(
                    _viewSlot == WeaponSlot.Secondary
                        ? "Buy a Secondary below — MK-1 for reach, SEC-D for contact. Both free."
                        : "Buy a Primary below, or equip one you already own.",
                    centered: true);
            }
            else
            {
                var def = WeaponCatalog.Def(equipped);
                int[] pts = m.PointsOf(equipped);
                int tier = m.TierOf(equipped);
                view.AddLabel($"{def.displayName}  ·  T{tier}/{WeaponTier.MaxTier}", centered: true);
                view.AddLabel(
                    $"{WeaponWheel.EffectiveDamage(def, pts[(int)WheelAttr.Damage]):0.#} dmg    " +
                    $"{WeaponWheel.EffectiveFireRate(def, pts[(int)WheelAttr.FireRate]):0.#}/s    " +
                    $"clip {WeaponWheel.EffectiveClip(def, pts[(int)WheelAttr.Clip])}    " +
                    $"reload {WeaponWheel.EffectiveReload(def, pts[(int)WheelAttr.Reload]):0.##}s",
                    centered: true);
                view.AddSpacer();

                string locked = MetaUpgrades.UpgradesUnlocked(m) ? null : "LOCKED — survive a death first";
                BuildWheel(view, m, equipped, locked);
            }

            view.AddSpacer();
            view.AddLabel($"Available {_viewSlot} weapons", centered: true);
            view.AddSpacer();

            foreach (var id in WeaponCatalog.ForSlot(_viewSlot))
                DrawCatalogRow(view, m, id, equipped);
        }

        void RotateCentre(int delta, MetaState m)
        {
            WeaponId id = m.Equipped(_viewSlot);
            var ring = WeaponWheel.RingOf(id);
            if (ring == null) return;
            int i = (int)id;
            int next = _centreIndexByWeapon[i] + delta;
            while (next < 0) next += WeaponWheel.AttrCount;
            _centreIndexByWeapon[i] = next % WeaponWheel.AttrCount;
        }

        void BuildWheel(StationPanelView view, MetaState m, WeaponId id, string lockedLabel)
        {
            var ring = WeaponWheel.RingOf(id);
            if (ring == null)
            {
                view.AddLabel("Upgrade ring not authored for this weapon yet.", reason: true);
                return;
            }

            view.AddLabel(
                $"[{Controls.DisplayOf(ControlId.WheelRotatePrev)} / " +
                $"{Controls.DisplayOf(ControlId.WheelRotateNext)}] rotate   ·   click a spoke",
                centered: true);

            int wi = (int)id;
            int centreIndex = _centreIndexByWeapon[wi] % WeaponWheel.AttrCount;
            var display = BuildWheelDisplay(m, id, ring, centreIndex, lockedLabel);

            var wheel = view.Wheel;
            if (wheel == null)
            {
                view.AddLabel("ARMORY wheel missing from StationPanel slot.", reason: true);
                return;
            }

            view.SetWheelVisible(true);
            wheel.Configure(
                display,
                idx => _centreIndexByWeapon[wi] = idx,
                () => ShipCommands.BuyWheelUpgrade(display.Weapon, display.BuyAttr));
        }

        static ArmoryWheelDisplay BuildWheelDisplay(MetaState m, WeaponId id, WheelAttr[] ring,
            int centreIndex, string lockedLabel)
        {
            centreIndex %= WeaponWheel.AttrCount;
            WheelAttr centre = ring[centreIndex];
            var def = WeaponCatalog.Def(id);
            int tier = m.TierOf(id);
            bool maxed = tier >= WeaponTier.MaxTier;

            var spokeLabels = new string[WeaponWheel.AttrCount];
            var spokeIndices = new int[WeaponWheel.AttrCount];
            var spokeDistances = new int[WeaponWheel.AttrCount];
            for (int i = 0; i < WeaponWheel.AttrCount; i++)
            {
                int ringIndex = (centreIndex + i) % WeaponWheel.AttrCount;
                int dist = WeaponWheel.RingDistance(0, i, WeaponWheel.AttrCount);
                spokeDistances[i] = dist;
                spokeIndices[i] = ringIndex;
                spokeLabels[i] =
                    $"{ShortLabel(ring[ringIndex])}\n{(dist == 0 ? "+5" : dist == 1 ? "+3" : "+0")}";
            }

            int[] now = m.PointsOf(id);
            int[] after = (int[])now.Clone();
            if (!maxed) WeaponWheel.Award(after, ring, centre);

            float burstNow = WeaponWheel.BurstSeconds(def, now[(int)WheelAttr.Clip], now[(int)WheelAttr.FireRate]);
            float burstAfter = WeaponWheel.BurstSeconds(def, after[(int)WheelAttr.Clip], after[(int)WheelAttr.FireRate]);
            float reloadNow = WeaponWheel.EffectiveReload(def, now[(int)WheelAttr.Reload]);
            float reloadAfter = WeaponWheel.EffectiveReload(def, after[(int)WheelAttr.Reload]);

            var d = new ArmoryWheelDisplay
            {
                CentreIndex = centreIndex,
                Centre = centre,
                SpokeLabels = spokeLabels,
                SpokeRingIndices = spokeIndices,
                SpokeDistances = spokeDistances,
                HubTitle = WeaponWheel.AttrLabel(centre).ToUpperInvariant(),
                ProjFireLine = $"Seconds of fire   {burstNow:0.##}  →  {burstAfter:0.##}",
                ProjReloadLine = $"Seconds of reload   {reloadNow:0.##}  →  {reloadAfter:0.##}",
                Weapon = id,
                BuyAttr = centre,
            };

            if (lockedLabel != null)
            {
                d.ReasonLine = lockedLabel;
            }
            else if (maxed)
            {
                d.ReasonLine = $"MAX — {WeaponTier.MaxTier} purchases spent";
            }
            else
            {
                long cost = WeaponTier.Cost(tier);
                if (m.BankedSalvage >= cost)
                {
                    d.ShowBuyButton = true;
                    d.BuyButtonLabel =
                        $"BUY  {WeaponWheel.AttrLabel(centre).ToUpperInvariant()}  ·  {cost} salvage";
                }
                else
                {
                    d.ReasonLine = $"need {cost - m.BankedSalvage} more salvage  (cost {cost})";
                }
            }

            return d;
        }

        static string ShortLabel(WheelAttr a)
        {
            switch (a)
            {
                case WheelAttr.Reload: return "RELOAD";
                case WheelAttr.Clip: return "CLIP";
                case WheelAttr.Damage: return "DMG";
                case WheelAttr.FireRate: return "RATE";
                case WheelAttr.Ability: return "ABILITY";
                default: return a.ToString().ToUpperInvariant();
            }
        }

        static void DrawCatalogRow(StationPanelView view, MetaState m, WeaponId id, WeaponId equippedInSlot)
        {
            var def = WeaponCatalog.Def(id);
            if (m.Owns(id))
            {
                int tier = m.TierOf(id);
                if (id == equippedInSlot)
                {
                    view.AddPricedRow(def.displayName, "", $"T{tier}", "owned", "EQUIP", null, false,
                        "EQUIPPED");
                }
                else
                {
                    view.AddPricedRow(def.displayName, "", $"T{tier}", "owned", "EQUIP",
                        () => ShipCommands.EquipWeapon(id), true);
                }
                return;
            }

            string priceLabel = def.cost == 0 ? "BUY (free)" : $"BUY ({def.cost})";
            string headline = def.pellets > 1
                ? $"{def.damage:0}×{def.pellets} @ {def.shotsPerSecond:0.#}/s"
                : $"{def.damage:0} dmg @ {def.shotsPerSecond:0.#}/s";
            bool canBuy = m.BankedSalvage >= def.cost;
            view.AddPricedRow(
                def.displayName,
                def.cost == 0 ? "free" : $"cost {def.cost}",
                "",
                headline,
                priceLabel,
                () => ShipCommands.BuyWeapon(id),
                canBuy,
                canBuy ? null : $"need {def.cost - m.BankedSalvage} more");
        }
    }
}
