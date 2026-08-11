using GatekeeperBastion.Sim;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    public sealed class CrewQuartersStation : StationTerminal
    {
        protected override string StationName => "CREW QUARTERS";

        protected override string PromptHint => "upgrade your own attributes";

        protected override int ContentHash(MetaState m)
        {
            unchecked
            {
                return base.ContentHash(m)
                    ^ m.VitalityLevel * 17
                    ^ m.StaminaLevel * 31
                    ^ m.ScavengingLevel * 41
                    ^ m.LuckLevel * 59;
            }
        }

        protected override void BuildPanel(StationPanelView view, MetaState m)
        {
            // Settings-shell baseline: the four attribute tracks only.
            view.AddLabel("ATTRIBUTES", centered: true);
            view.AddSpacer();

            UpgradeRow(view, UpgradeTrack.Vitality, "Vitality",
                l => $"{MetaUpgrades.PlayerMaxHp(l):0} max HP");
            UpgradeRow(view, UpgradeTrack.Stamina, "Stamina",
                l => $"{SprintSeconds(l):0.#}s sprint");
            UpgradeRow(view, UpgradeTrack.Scavenging, "Scavenging",
                l => $"{MetaUpgrades.SalvagePerKill(l):0} per kill");
            UpgradeRow(view, UpgradeTrack.Luck, "Luck",
                l => $"{MetaUpgrades.CritChance(l) * 100f:0.#}% crit");
        }

        static float SprintSeconds(int level)
        {
            return MetaUpgrades.StaminaMax(level) / FirstPersonController.SprintDrainPerSecond;
        }
    }
}
