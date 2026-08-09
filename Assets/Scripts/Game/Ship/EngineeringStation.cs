using GatekeeperBastion.Sim;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    public sealed class EngineeringStation : StationTerminal
    {
        protected override string StationName => "FABRICATION";

        protected override string PromptHint => "fabricate hatch armor, equipment, and payloads";

        protected override void BuildPanel(StationPanelView view, MetaState m)
        {
            view.AddLabel("Hatch defenses — permanent upgrades (cost doubles per level)", centered: true);
            view.AddLabel("Applied to the hatch on your next embark.", centered: true);
            view.AddSpacer(10f);
            UpgradeRow(view, UpgradeTrack.HatchArmor, "Hatch Armor",
                l => $"{MetaUpgrades.HatchHp(l):0} HP");

            view.AddSpacer(12f);
            view.AddLabel("Equipment — bring it or the Hatch stays whatever the fight left it",
                centered: true);
            view.AddSpacer(6f);
            DrawEquipmentSlotRow(view, m);
            if (m.EquipmentSlotPurchased)
                DrawEquipmentRow(view, m, EquipmentId.RepairRig);

            view.AddSpacer(12f);
            view.AddLabel("Payloads — one in the drone bay per Run", centered: true);
            view.AddSpacer(6f);
            DrawPayloadRow(view, m, PayloadId.Decoy);
            string bay = m.EquippedDrone == PayloadId.None
                ? "bay empty"
                : $"bay: {PayloadCatalog.Def(m.EquippedDrone).displayName}";
            view.AddLabel(bay, reason: true);
        }

        static void DrawEquipmentSlotRow(StationPanelView view, MetaState m)
        {
            if (m.EquipmentSlotPurchased)
            {
                view.AddPricedRow("Equipment Slot", "", "owned", "slot open", "BUY", null, false, "OWNED");
                return;
            }

            if (!MetaUpgrades.UpgradesUnlocked(m))
            {
                view.AddPricedRow("Equipment Slot", "", "locked", "slot", "BUY", null, false,
                    "unlocks after first lost Run");
                return;
            }

            long cost = EquipmentCatalog.SlotPrice;
            long banked = m.BankedSalvage;
            bool canBuy = banked >= cost;
            view.AddPricedRow(
                "Equipment Slot",
                $"cost {cost}",
                "",
                $"buy once — {cost} salvage",
                "BUY",
                () => ShipCommands.BuyEquipmentSlot(),
                canBuy,
                canBuy ? null : $"need {cost - banked} more");
        }

        static void DrawEquipmentRow(StationPanelView view, MetaState m, EquipmentId id)
        {
            var def = EquipmentCatalog.Def(id);
            if (m.OwnsEquipment(id))
            {
                string effect = def.repairPerSecond > 0f
                    ? $"{def.repairPerSecond:0} HP/s repair"
                    : "owned";
                view.AddPricedRow(def.displayName, "", "", effect, "BUY", null, false,
                    m.EquippedEquipment == id ? "EQUIPPED" : "OWNED");
                return;
            }

            string buyEffect = def.repairPerSecond > 0f
                ? $"{def.repairPerSecond:0} HP/s when equipped"
                : "equipment";
            long cost = def.cost;
            long banked = m.BankedSalvage;
            bool canBuy = banked >= cost;
            view.AddPricedRow(
                def.displayName,
                $"cost {cost}",
                "",
                buyEffect,
                "BUY",
                () => ShipCommands.BuyEquipment(id),
                canBuy,
                canBuy ? null : $"need {cost - banked} more");
        }

        static void DrawPayloadRow(StationPanelView view, MetaState m, PayloadId id)
        {
            var def = PayloadCatalog.Def(id);
            if (m.OwnsPayload(id))
            {
                string effect = $"{def.charges} charges · {def.hp:0} HP";
                if (m.EquippedDrone == id)
                {
                    view.AddPricedRow(def.displayName, "", "", effect, "EQUIP", null, false, "EQUIPPED");
                }
                else if (m.PayloadLoadoutLocked)
                {
                    view.AddPricedRow(def.displayName, "", "", effect, "EQUIP", null, false,
                        "OWNED — locked mid-Run");
                }
                else
                {
                    view.AddPricedRow(def.displayName, "", "", effect, "EQUIP",
                        () => ShipCommands.EquipPayload(id), true);
                }
                return;
            }

            long cost = def.cost;
            long banked = m.BankedSalvage;
            bool canBuy = banked >= cost;
            view.AddPricedRow(
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
