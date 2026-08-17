using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: Armory console — catalog first; selecting an owned weapon opens the
    /// upgrade wheel; Back / Esc returns to the catalog.</summary>
    public sealed class ArmoryStation : StationTerminal
    {
        const string Tag = "ArmoryStation";

        [SerializeField] ArmoryPanelView _body;

        // Secondary first: the door prompt is "buy a Secondary", both Secondaries are
        // free, and after a first buy this is the slot that already has a wheel. Primary
        // stays one click away. OnStationOpened also snaps to whichever slot is equipped
        // when the current tab is empty, so a Secondary buy is not hidden behind Primary:
        // empty.
        WeaponSlot _viewSlot = WeaponSlot.Secondary;
        readonly int[] _centreIndexByWeapon = new int[WeaponCatalog.ArraySize];
        bool _wheelDirty;
        bool _showingWheel;

        public override string StationName => "ARMORY";
        public const string PurposeLine = "the weapon in your hands";
        public override string Purpose => PurposeLine;
        public const string TransferLine = "Upgrades stay on this weapon.";

        protected override string PromptHint =>
            GameSession.Meta != null && !GameSession.Meta.OwnsAnyWeapon
                ? "buy a Secondary — both cost nothing; pick the shape you want"
                : "equip and focus-upgrade the weapon in your hands";

        protected override void Awake()
        {
            base.Awake();
            if (_body == null)
                Log.Error(Tag, StationName + ": missing serialized _body.");
        }

        protected override void OnStationOpened(MetaState m)
        {
            _showingWheel = false;
            if (m.Equipped(_viewSlot) != WeaponId.None) return;
            if (m.Equipped(WeaponSlot.Secondary) != WeaponId.None)
                _viewSlot = WeaponSlot.Secondary;
            else if (m.Equipped(WeaponSlot.Primary) != WeaponId.None)
                _viewSlot = WeaponSlot.Primary;
            else
                _viewSlot = WeaponSlot.Secondary;
        }

        public override bool TryBack()
        {
            if (!_showingWheel) return false;
            _showingWheel = false;
            _wheelDirty = true;
            return true;
        }

        protected override void TickWhileOpen(MetaState m)
        {
            if (!_showingWheel) return;
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
                h = h * 397 ^ (_showingWheel ? 1 : 0);
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
            if (_body == null) return;

            view.SetDeal(TransferLine);

            _body.BindSlotButtons(
                _viewSlot == WeaponSlot.Primary ? "[ PRIMARY ]" : "  Primary  ",
                () => { _viewSlot = WeaponSlot.Primary; _showingWheel = false; },
                _viewSlot == WeaponSlot.Secondary ? "[ SECONDARY ]" : "  Secondary  ",
                () => { _viewSlot = WeaponSlot.Secondary; _showingWheel = false; });

            WeaponId equipped = m.Equipped(_viewSlot);
            if (equipped == WeaponId.None)
            {
                _showingWheel = false;
                _body.BindEquipped($"{_viewSlot}: empty — buy below", null);
            }
            else
            {
                var def = WeaponCatalog.Def(equipped);
                int[] pts = m.PointsOf(equipped);
                int tier = m.TierOf(equipped);
                _body.BindEquipped(
                    $"{def.displayName}  ·  T{tier}/{WeaponTier.MaxTier}",
                    $"{WeaponWheel.EffectiveDamage(def, pts[(int)WheelAttr.Damage]):0.#} dmg    " +
                    $"{WeaponWheel.EffectiveFireRate(def, pts[(int)WheelAttr.FireRate]):0.#}/s    " +
                    $"clip {WeaponWheel.EffectiveClip(def, pts[(int)WheelAttr.Clip])}    " +
                    $"reload {WeaponWheel.EffectiveReload(def, pts[(int)WheelAttr.Reload]):0.##}s");
            }

            if (_showingWheel && equipped != WeaponId.None)
                BuildWheelMode(view, m, equipped);
            else
                BuildCatalogMode(view, m, equipped);
        }

        void BuildCatalogMode(StationPanelView view, MetaState m, WeaponId equipped)
        {
            view.SetOnClose(() => Close());
            view.SetCloseLabel("CLOSE  (" + Controls.DisplayOf(ControlId.FreeCursor) + ")");
            _body.BindWheelNotes(null, null);
            _body.BindBack(null);
            view.SetWheelVisible(false);
            _body.SetCatalogVisible(true);
            BindCatalog(m, equipped);
        }

        void BuildWheelMode(StationPanelView view, MetaState m, WeaponId equipped)
        {
            _body.SetCatalogVisible(false);
            view.SetOnClose(() =>
            {
                _showingWheel = false;
                _wheelDirty = true;
            });
            view.SetCloseLabel("BACK");
            _body.BindBack(() =>
            {
                _showingWheel = false;
                _wheelDirty = true;
            });

            string locked = MetaUpgrades.UpgradesUnlocked(m) ? null : "LOCKED — survive a death first";
            BuildWheel(view, m, equipped, locked);
        }

        void OpenWheelFor(WeaponId id)
        {
            if (id == WeaponId.None) return;
            ShipCommands.EquipWeapon(id);
            _showingWheel = true;
            _wheelDirty = true;
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
                _body.BindWheelNotes(null, "Upgrade ring not authored for this weapon yet.");
                view.SetWheelVisible(false);
                return;
            }

            string rotateHint =
                $"[{Controls.DisplayOf(ControlId.WheelRotatePrev)} / " +
                $"{Controls.DisplayOf(ControlId.WheelRotateNext)}] rotate   ·   click a spoke";

            int wi = (int)id;
            int centreIndex = _centreIndexByWeapon[wi] % WeaponWheel.AttrCount;
            var display = BuildWheelDisplay(m, id, ring, centreIndex, lockedLabel);

            var wheel = view.Wheel;
            if (wheel == null)
            {
                _body.BindWheelNotes(rotateHint, "ARMORY wheel missing from StationPanel slot.");
                view.SetWheelVisible(false);
                return;
            }

            _body.BindWheelNotes(rotateHint, null);
            view.SetWheelVisible(true);
            wheel.Configure(
                display,
                idx => _centreIndexByWeapon[wi] = idx,
                () => ShipCommands.BuyWheelUpgrade(display.Weapon, display.BuyAttr));
        }

        void BindCatalog(MetaState m, WeaponId equippedInSlot)
        {
            int i = 0;
            string missing = null;
            foreach (var id in WeaponCatalog.ForSlot(_viewSlot))
            {
                if (i < _body.CatalogRowCount)
                {
                    var row = _body.CatalogRow(i);
                    BindCatalogRow(row, m, id, equippedInSlot);
                    UiBind.Show(row, true);
                }
                else
                {
                    string weapon = WeaponCatalog.Def(id).displayName;
                    missing = missing == null ? weapon : missing + ", " + weapon;
                }
                i++;
            }
            for (int r = i; r < _body.CatalogRowCount; r++)
                UiBind.Show(_body.CatalogRow(r), false);

            bool overflow = missing != null;
            _body.ShowRowsMissing(overflow);
            if (overflow)
            {
                Log.Error(Tag, StationName + " " + _viewSlot + ": catalog roster exceeds " +
                    _body.CatalogRowCount + " authored rows — missing: " + missing);
            }
        }

        static ArmoryWheelDisplay BuildWheelDisplay(MetaState m, WeaponId id, WheelAttr[] ring,
            int centreIndex, string lockedLabel)
        {
            centreIndex %= WeaponWheel.AttrCount;
            WheelAttr centre = ring[centreIndex];
            var def = WeaponCatalog.Def(id);
            int tier = m.TierOf(id);
            bool maxed = tier >= WeaponTier.MaxTier;

            int[] now = m.PointsOf(id);

            var spokeLabels = new string[WeaponWheel.AttrCount];
            var spokeIndices = new int[WeaponWheel.AttrCount];
            var spokeDistances = new int[WeaponWheel.AttrCount];
            for (int i = 0; i < WeaponWheel.AttrCount; i++)
            {
                int ringIndex = (centreIndex + i) % WeaponWheel.AttrCount;
                int dist = WeaponWheel.RingDistance(0, i, WeaponWheel.AttrCount);
                spokeDistances[i] = dist;
                spokeIndices[i] = ringIndex;
                int invested = now[(int)ring[ringIndex]];
                string gain = dist == 0 ? "+5" : dist == 1 ? "+3" : "+0";
                // Points already on this spoke — markers alone do not read at a glance.
                spokeLabels[i] = $"{ShortLabel(ring[ringIndex])} ({invested})\n{gain}";
            }

            var spokeTicks = new int[WeaponWheel.AttrCount];
            // Five markers on a spoke; each mark = FocusAtMax/5 = 10 points (two centre awards).
            // Neighbour max (30) fills three marks; full focus (50) fills all five.
            const int PointsPerTick = WeaponWheel.FocusAtMax / ArmoryWheelView.SpokeCount;
            for (int i = 0; i < WeaponWheel.AttrCount; i++)
            {
                int p = now[(int)ring[(centreIndex + i) % WeaponWheel.AttrCount]];
                spokeTicks[i] = p <= 0
                    ? 0
                    : Mathf.Clamp((p + PointsPerTick - 1) / PointsPerTick, 1, ArmoryWheelView.SpokeCount);
            }
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
                SpokeTicks = spokeTicks,
            };

            long cost = maxed ? 0 : WeaponTier.Cost(def.slot, tier);
            if (lockedLabel != null)
            {
                d.ReasonLine = maxed
                    ? lockedLabel
                    : lockedLabel + " · " + cost + " salvage";
            }
            else if (maxed)
            {
                d.ReasonLine = $"MAX — {WeaponTier.MaxTier} purchases spent";
            }
            else if (!WeaponWheel.SpokePurchasable(id, centre))
            {
                d.ReasonLine =
                    $"{WeaponWheel.AttrLabel(centre).ToUpperInvariant()} cannot be bought on this weapon · {cost} salvage";
            }
            else if (m.BankedSalvage >= cost)
            {
                d.ShowBuyButton = true;
                d.BuyButtonLabel =
                    $"BUY  {WeaponWheel.AttrLabel(centre).ToUpperInvariant()}  ·  {cost} salvage";
            }
            else
            {
                d.ReasonLine = $"need {cost - m.BankedSalvage} more salvage  (cost {cost})";
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

        static string NextUpgradeCostLabel(MetaState m, WeaponId id)
        {
            int tier = m.TierOf(id);
            if (tier >= WeaponTier.MaxTier) return "MAX";
            return "upgrade " + WeaponTier.Cost(WeaponCatalog.Def(id).slot, tier);
        }

        void BindCatalogRow(PanelRowView row, MetaState m, WeaponId id, WeaponId equippedInSlot)
        {
            if (row == null) return;
            var def = WeaponCatalog.Def(id);
            if (m.Owns(id))
            {
                int tier = m.TierOf(id);
                string cost = NextUpgradeCostLabel(m, id);
                if (id == equippedInSlot)
                {
                    row.Bind(def.displayName, cost, $"T{tier}", "equipped", "UPGRADE",
                        () => OpenWheelFor(id), true);
                }
                else
                {
                    row.Bind(def.displayName, cost, $"T{tier}", "owned", "SELECT",
                        () => OpenWheelFor(id), true);
                }
                return;
            }

            string priceLabel = def.cost == 0 ? "BUY (free)" : $"BUY ({def.cost})";
            string headline = def.pellets > 1
                ? $"{def.damage:0}x{def.pellets} @ {def.shotsPerSecond:0.#}/s"
                : $"{def.damage:0} dmg @ {def.shotsPerSecond:0.#}/s";
            bool canBuy = m.BankedSalvage >= def.cost;
            row.Bind(
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
