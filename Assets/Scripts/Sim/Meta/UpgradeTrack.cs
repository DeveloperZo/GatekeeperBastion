using System;

namespace GatekeeperBastion.Sim
{
    /// <summary>Meta-upgrade tracks purchasable at ship stations. Weapons go through BuyWeapon /
    /// BuyWheelUpgrade / EquipWeapon. The repair rig is equipment (BuyEquipmentSlot / BuyEquipment).
    /// Crew attribute tracks were deleted in save v8 — CREW QUARTERS equips Boarder Protocols.</summary>
    public enum UpgradeTrack
    {
        /// <summary>Hatch max HP (Fabrication).</summary>
        HatchArmor,
    }

    /// <summary>Table over <see cref="UpgradeTrack"/>: labels, station ownership, and level→value.
    /// Doc tools and the mcs harness read this; Game commands write levels on MetaState.</summary>
    public static class UpgradeTracks
    {
        static readonly UpgradeTrack[] AllTracks =
        {
            UpgradeTrack.HatchArmor,
        };

        /// <summary>Enum values in declaration order.</summary>
        public static UpgradeTrack[] All => AllTracks;

        /// <summary>Console / doc display name of a track.</summary>
        public static string Label(UpgradeTrack track)
        {
            switch (track)
            {
                case UpgradeTrack.HatchArmor: return "Hatch Armor";
                default: return track.ToString();
            }
        }

        /// <summary>Caps station name that sells this track (language.md stations_caps).</summary>
        public static string Station(UpgradeTrack track)
        {
            switch (track)
            {
                case UpgradeTrack.HatchArmor: return "FABRICATION";
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
