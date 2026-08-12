using System;

namespace GatekeeperBastion.Sim
{
    /// <summary>Meta-upgrade tracks purchasable at ship stations. Weapons go through BuyWeapon /
    /// BuyWheelUpgrade / EquipWeapon. The repair rig is equipment (BuyEquipmentSlot / BuyEquipment).</summary>
    public enum UpgradeTrack
    {
        /// <summary>Hatch max HP (Fabrication).</summary>
        HatchArmor,

        // ---- PLAYER ATTRIBUTES (Crew Quarters) ----
        // Same Buy/NextCost curve, deliberately NOT gated behind UpgradesUnlocked.

        /// <summary>Player max HP (Crew Quarters).</summary>
        Vitality,
        /// <summary>Sprint stamina pool (Crew Quarters).</summary>
        Stamina,
        /// <summary>Base salvage per kill (Crew Quarters).</summary>
        Scavenging,
        /// <summary>Critical-hit chance (Crew Quarters).</summary>
        Luck
    }

    /// <summary>Table over <see cref="UpgradeTrack"/>: labels, station ownership, and level→value.
    /// Doc tools and the mcs harness read this; Game commands write levels on MetaState.</summary>
    public static class UpgradeTracks
    {
        static readonly UpgradeTrack[] AllTracks =
        {
            UpgradeTrack.HatchArmor,
            UpgradeTrack.Vitality,
            UpgradeTrack.Stamina,
            UpgradeTrack.Scavenging,
            UpgradeTrack.Luck,
        };

        /// <summary>Enum values in declaration order.</summary>
        public static UpgradeTrack[] All => AllTracks;

        /// <summary>Console / doc display name of a track.</summary>
        public static string Label(UpgradeTrack track)
        {
            switch (track)
            {
                case UpgradeTrack.HatchArmor: return "Hatch Armor";
                case UpgradeTrack.Vitality: return "Vitality";
                case UpgradeTrack.Stamina: return "Stamina";
                case UpgradeTrack.Scavenging: return "Scavenging";
                case UpgradeTrack.Luck: return "Luck";
                default: return track.ToString();
            }
        }

        /// <summary>Caps station name that sells this track (language.md stations_caps).</summary>
        public static string Station(UpgradeTrack track)
        {
            switch (track)
            {
                case UpgradeTrack.HatchArmor: return "FABRICATION";
                case UpgradeTrack.Vitality:
                case UpgradeTrack.Stamina:
                case UpgradeTrack.Scavenging:
                case UpgradeTrack.Luck:
                    return "CREW QUARTERS";
                default:
                    return "";
            }
        }

        /// <summary>Effect at <paramref name="level"/> and its unit string for docs.</summary>
        public static ValueAtResult ValueAt(UpgradeTrack track, int level)
        {
            switch (track)
            {
                case UpgradeTrack.HatchArmor:
                    return new ValueAtResult(MetaUpgrades.HatchHp(level), "HP");
                case UpgradeTrack.Vitality:
                    return new ValueAtResult(MetaUpgrades.PlayerMaxHp(level), "HP");
                case UpgradeTrack.Stamina:
                    return new ValueAtResult(MetaUpgrades.StaminaMax(level), "");
                case UpgradeTrack.Scavenging:
                    return new ValueAtResult(MetaUpgrades.SalvagePerKill(level), "salvage/kill");
                case UpgradeTrack.Luck:
                    return new ValueAtResult(MetaUpgrades.CritChance(level) * 100f, "%");
                default:
                    return new ValueAtResult(float.NaN, "");
            }
        }

        /// <summary>Return of <see cref="ValueAt"/> — float + unit without C# 7 ValueTuple on mcs.</summary>
        public struct ValueAtResult
        {
            public readonly float Value;
            public readonly string Unit;

            public ValueAtResult(float value, string unit)
            {
                Value = value;
                Unit = unit ?? "";
            }
        }
    }
}
