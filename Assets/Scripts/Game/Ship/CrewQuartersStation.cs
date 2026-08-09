using GatekeeperBastion.Sim;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    public sealed class CrewQuartersStation : StationTerminal
    {
        protected override string StationName => "CREW QUARTERS";

        protected override string PromptHint => "upgrade your own attributes";

        protected override void BuildPanel(StationPanelView view, MetaState m)
        {
            view.AddLabel("Player attributes — permanent upgrades (cost doubles per level)", centered: true);
            view.AddLabel("These are about YOU, not the ship. No death required to buy any of them.",
                centered: true);
            view.AddSpacer(10f);

            UpgradeRow(view, UpgradeTrack.Vitality, "Vitality",
                l => $"{MetaUpgrades.PlayerMaxHp(l):0} max HP");
            UpgradeRow(view, UpgradeTrack.Stamina, "Stamina",
                l => $"{SprintSeconds(l):0.#}s sprint");
            UpgradeRow(view, UpgradeTrack.Scavenging, "Scavenging",
                l => $"{MetaUpgrades.SalvagePerKill(l):0} per kill");
            UpgradeRow(view, UpgradeTrack.Luck, "Luck",
                l => $"{MetaUpgrades.CritChance(l) * 100f:0.#}% crit");

            view.AddSpacer(10f);
            view.AddLabel(
                $"Sprint: hold SHIFT while moving. {MetaUpgrades.StaminaMax(m.StaminaLevel):0} stamina, " +
                $"{FirstPersonController.SprintDrainPerSecond:0}/s to run, " +
                $"{FirstPersonController.StaminaRegenPerSecond:0}/s back after {FirstPersonController.StaminaRegenDelaySeconds:0.#}s.",
                centered: true);
            view.AddLabel(
                $"Crits deal x{PlayerGun.CritDamageMultiplier:0.#} damage — Luck buys the chance, never the size.",
                centered: true);
            view.AddLabel(
                "Scavenging raises every kill. Hatch repair is the Repair Rig at FABRICATION.",
                centered: true);
        }

        static float SprintSeconds(int level)
        {
            return MetaUpgrades.StaminaMax(level) / FirstPersonController.SprintDrainPerSecond;
        }
    }
}
