using GatekeeperBastion.Game.Ui;
using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    public sealed class EngineeringStation : StationTerminal
    {
        const string Tag = "EngineeringStation";

        [SerializeField] FabricationPanelView _body;

        protected override string StationName => "FABRICATION";

        protected override string PromptHint => "fabricate hatch armor, equipment, and payloads";

        protected override void Awake()
        {
            base.Awake();
            if (_body == null)
                Log.Error(Tag, StationName + ": missing serialized _body.");
        }

        protected override void BuildPanel(StationPanelView view, MetaState m)
        {
            if (_body == null) return; // Awake already logged the missing binding.

            // HATCH / EQUIPMENT / PAYLOAD headings are authored scene text.
            UpgradeRow(_body.HatchRow, UpgradeTrack.HatchArmor, "Hatch Armor",
                l => $"{MetaUpgrades.HatchHp(l):0} HP");

            BindEquipmentSlotRow(_body.EquipmentSlotRow, m);
            // A row that cannot apply yet is deactivated, not absent.
            UiBind.Show(_body.RepairRigRow, m.EquipmentSlotPurchased);
            if (m.EquipmentSlotPurchased)
                BindEquipmentRow(_body.RepairRigRow, m, EquipmentId.RepairRig);

            BindPayloadRow(_body.DecoyRow, m, PayloadId.Decoy);
            string bay = m.EquippedDrone == PayloadId.None
                ? "bay empty"
                : $"bay: {PayloadCatalog.Def(m.EquippedDrone).displayName}";
            _body.SetBayLine(bay);
        }

        static void BindEquipmentSlotRow(PanelRowView row, MetaState m)
        {
            if (row == null) return;
            if (m.EquipmentSlotPurchased)
            {
                row.Bind("Equipment Slot", "", "owned", "slot open", "BUY", null, false, "OWNED");
                return;
            }

            if (!MetaUpgrades.UpgradesUnlocked(m))
            {
                row.Bind("Equipment Slot", "", "locked", "slot", "BUY", null, false,
                    "unlocks after first lost Run");
                return;
            }

            long cost = EquipmentCatalog.SlotPrice;
            long banked = m.BankedSalvage;
            bool canBuy = banked >= cost;
            row.Bind(
                "Equipment Slot",
                $"cost {cost}",
                "",
                $"buy once — {cost} salvage",
                "BUY",
                () => ShipCommands.BuyEquipmentSlot(),
                canBuy,
                canBuy ? null : $"need {cost - banked} more");
        }

        static void BindEquipmentRow(PanelRowView row, MetaState m, EquipmentId id)
        {
            if (row == null) return;
            var def = EquipmentCatalog.Def(id);
            if (m.OwnsEquipment(id))
            {
                string effect = def.repairPerSecond > 0f
                    ? $"{def.repairPerSecond:0} HP/s repair"
                    : "owned";
                row.Bind(def.displayName, "", "", effect, "BUY", null, false,
                    m.EquippedEquipment == id ? "EQUIPPED" : "OWNED");
                return;
            }

            string buyEffect = def.repairPerSecond > 0f
                ? $"{def.repairPerSecond:0} HP/s when equipped"
                : "equipment";
            long cost = def.cost;
            long banked = m.BankedSalvage;
            bool canBuy = banked >= cost;
            row.Bind(
                def.displayName,
                $"cost {cost}",
                "",
                buyEffect,
                "BUY",
                () => ShipCommands.BuyEquipment(id),
                canBuy,
                canBuy ? null : $"need {cost - banked} more");
        }

        static void BindPayloadRow(PanelRowView row, MetaState m, PayloadId id)
        {
            if (row == null) return;
            var def = PayloadCatalog.Def(id);
            if (m.OwnsPayload(id))
            {
                string effect = $"{def.charges} charges · {def.hp:0} HP";
                if (m.EquippedDrone == id)
                {
                    row.Bind(def.displayName, "", "", effect, "EQUIP", null, false, "EQUIPPED");
                }
                else if (m.PayloadLoadoutLocked)
                {
                    row.Bind(def.displayName, "", "", effect, "EQUIP", null, false,
                        "OWNED — locked mid-Run");
                }
                else
                {
                    row.Bind(def.displayName, "", "", effect, "EQUIP",
                        () => ShipCommands.EquipPayload(id), true);
                }
                return;
            }

            long cost = def.cost;
            long banked = m.BankedSalvage;
            bool canBuy = banked >= cost;
            row.Bind(
                def.displayName,
                $"cost {cost}",
                "",
                $"{def.charges} ch · {def.hp:0} HP pull",
                "BUY",
                () => ShipCommands.BuyPayload(id),
                canBuy,
                canBuy ? null : $"need {cost - banked} more");
        }
    }
}
