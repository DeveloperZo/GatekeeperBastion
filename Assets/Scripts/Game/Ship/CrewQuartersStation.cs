using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    public sealed class CrewQuartersStation : StationTerminal
    {
        const string Tag = "CrewQuartersStation";

        [SerializeField] CrewQuartersPanelView _body;

        protected override string StationName => "CREW QUARTERS";

        protected override string PromptHint => "upgrade your own attributes";

        protected override void Awake()
        {
            base.Awake();
            if (_body == null)
                Log.Error(Tag, StationName + ": missing serialized _body.");
        }

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
            if (_body == null) return; // Awake already logged the missing binding.

            // The ATTRIBUTES heading is authored scene text.
            UpgradeRow(_body.VitalityRow, UpgradeTrack.Vitality, "Vitality",
                l => $"{MetaUpgrades.PlayerMaxHp(l):0} max HP");
            UpgradeRow(_body.StaminaRow, UpgradeTrack.Stamina, "Stamina",
                l => $"{SprintSeconds(l):0.#}s sprint");
            UpgradeRow(_body.ScavengingRow, UpgradeTrack.Scavenging, "Scavenging",
                l => $"{MetaUpgrades.SalvagePerKill(l):0} per kill");
            UpgradeRow(_body.LuckRow, UpgradeTrack.Luck, "Luck",
                l => $"{MetaUpgrades.CritChance(l) * 100f:0.#}% crit");
        }

        static float SprintSeconds(int level)
        {
            return MetaUpgrades.StaminaMax(level) / FirstPersonController.SprintDrainPerSecond;
        }
    }
}
