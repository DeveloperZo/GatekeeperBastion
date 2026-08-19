using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GatekeepersProtocol.Sim
{
    /// <summary>MetaState's hand-rolled JSON face: ToJson/FromJson plus the append and
    /// extract helpers. Split by responsibility (the serializer, not the rules) to hold
    /// the Sim line cap - the version gates themselves stay in FromSaveData.</summary>
    public sealed partial class MetaState
    {
        public string ToJson()
        {
            SaveData d = ToSaveData();
            var sb = new StringBuilder(256);
            sb.Append('{');
            AppendField(sb, "SaveVersion", d.SaveVersion.ToString(CultureInfo.InvariantCulture), false);
            AppendField(sb, "BankedSalvage", d.BankedSalvage.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "LifetimeSalvageEarned", d.LifetimeSalvageEarned.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "LifetimeSalvageSpent", d.LifetimeSalvageSpent.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "TotalRuns", d.TotalRuns.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "CleanExits", d.CleanExits.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "PodEjections", d.PodEjections.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "DeepestWaveReached",
                d.DeepestWaveReached.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "HatchArmorLevel", d.HatchArmorLevel.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "EquippedProtocol", d.EquippedProtocol.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "EquippedPassive", d.EquippedPassive.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "EquipmentSlotPurchased", d.EquipmentSlotPurchased ? "true" : "false", true);
            AppendField(sb, "OwnedEquipmentMask", d.OwnedEquipmentMask.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "EquippedEquipment", d.EquippedEquipment.ToString(CultureInfo.InvariantCulture), true);
            AppendBoolArray(sb, "WeaponsOwned", d.WeaponsOwned);
            AppendIntArray(sb, "WeaponWheelPoints", d.WeaponWheelPoints);
            AppendField(sb, "EquippedPrimary", d.EquippedPrimary.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "EquippedSecondary", d.EquippedSecondary.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "EquippedDrone", d.EquippedDrone.ToString(CultureInfo.InvariantCulture), true);
            AppendField(sb, "OwnedPayloadMask", d.OwnedPayloadMask.ToString(CultureInfo.InvariantCulture), true);
            AppendStringArray(sb, "CompletedTerminalIds", d.CompletedTerminalIds);
            AppendStringArray(sb, "OpenDoorIds", d.OpenDoorIds);
            AppendField(sb, "DemoActive", d.DemoActive ? "true" : "false", true);
            AppendField(sb, "DemoCapsuleOpened", d.DemoCapsuleOpened ? "true" : "false", true);
            AppendField(sb, "DemoNavPowered", d.DemoNavPowered ? "true" : "false", true);
            AppendField(sb, "DemoArmoryPowered", d.DemoArmoryPowered ? "true" : "false", true);
            AppendField(sb, "DemoWeaponGranted", d.DemoWeaponGranted ? "true" : "false", true);
            AppendField(sb, "DemoShipFightDone", d.DemoShipFightDone ? "true" : "false", true);
            AppendField(sb, "DemoCrewMenuUnlocked", d.DemoCrewMenuUnlocked ? "true" : "false", true);
            AppendField(sb, "DemoFabPowered", d.DemoFabPowered ? "true" : "false", true);
            AppendField(sb, "DemoWheelBuysLeft",
                d.DemoWheelBuysLeft.ToString(CultureInfo.InvariantCulture), true);
            AppendIntArray(sb, "DemoZoneDeepestWave", d.DemoZoneDeepestWave);
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>Parse keys into <see cref="SaveData"/> then migrate via
        /// <see cref="FromSaveData"/>. Version gates live only in FromSaveData.</summary>
        public static MetaState FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new ArgumentException("json is null or empty", "json");
            var d = new SaveData();
            // Missing key must not impersonate v9 — ApplyDemoFlags would skip pre-v9
            // migration and leave DemoActive at the DTO default (false).
            d.SaveVersion = (int)ExtractLong(json, "SaveVersion", 0);
            d.BankedSalvage = ExtractLong(json, "BankedSalvage", 0);
            d.TotalRuns = (int)ExtractLong(json, "TotalRuns", 0);
            d.CleanExits = (int)ExtractLong(json, "CleanExits", 0);
            d.PodEjections = (int)ExtractLong(json, "PodEjections", 0);
            int deepest = (int)ExtractLong(json, "DeepestWaveReached", 1);
            d.DeepestWaveReached = deepest < 1 ? 1 : deepest;
            d.HatchArmorLevel = (int)ExtractLong(json, "HatchArmorLevel", 0);
            d.EquippedProtocol = (int)ExtractLong(json, "EquippedProtocol", (int)BoarderProtocolId.None);
            d.EquippedPassive = (int)ExtractLong(json, "EquippedPassive", (int)BoarderAbilityId.None);
            d.VitalityLevel = (int)ExtractLong(json, "VitalityLevel", 0);
            d.StaminaLevel = (int)ExtractLong(json, "StaminaLevel", 0);
            d.ScavengingLevel = (int)ExtractLong(json, "ScavengingLevel", 0);
            d.LuckLevel = (int)ExtractLong(json, "LuckLevel", 0);

            // Missing ledger keys → -1 at any version (JSON contract; FromSaveData must not gate).
            d.LifetimeSalvageEarned = ExtractLong(json, "LifetimeSalvageEarned", -1L);
            d.LifetimeSalvageSpent = ExtractLong(json, "LifetimeSalvageSpent", -1L);

            d.RepairRigLevel = (int)ExtractLong(json, "RepairRigLevel", 0);
            d.EngineeringLevel = (int)ExtractLong(json, "EngineeringLevel", 0);

            d.EquipmentSlotPurchased = ExtractBool(json, "EquipmentSlotPurchased", false);
            d.OwnedEquipmentMask = (int)ExtractLong(json, "OwnedEquipmentMask", 0);
            d.EquippedEquipment = (int)ExtractLong(json, "EquippedEquipment", (int)EquipmentId.None);
            d.OwnedPayloadMask = (int)ExtractLong(json, "OwnedPayloadMask", 0);
            d.CompletedTerminalIds = ExtractStringArray(json, "CompletedTerminalIds");
            d.OpenDoorIds = ExtractStringArray(json, "OpenDoorIds");

            // v9 demo flags. Version gates live in FromSaveData; these defaults are only
            // reachable on a hand-edited v9 file with keys removed.
            d.DemoActive = ExtractBool(json, "DemoActive", false);
            d.DemoCapsuleOpened = ExtractBool(json, "DemoCapsuleOpened", false);
            d.DemoNavPowered = ExtractBool(json, "DemoNavPowered", false);
            d.DemoArmoryPowered = ExtractBool(json, "DemoArmoryPowered", false);
            d.DemoWeaponGranted = ExtractBool(json, "DemoWeaponGranted", false);
            d.DemoShipFightDone = ExtractBool(json, "DemoShipFightDone", false);
            d.DemoCrewMenuUnlocked = ExtractBool(json, "DemoCrewMenuUnlocked", false);
            d.DemoFabPowered = ExtractBool(json, "DemoFabPowered", false);
            d.DemoWheelBuysLeft = (int)ExtractLong(json, "DemoWheelBuysLeft", 0);
            d.DemoZoneDeepestWave = new int[DemoZoneCount];
            ExtractIntArray(json, "DemoZoneDeepestWave", d.DemoZoneDeepestWave);

            bool hasV3Weapons = json.IndexOf("\"WeaponsOwned\"", StringComparison.Ordinal) >= 0;
            if (hasV3Weapons)
            {
                d.WeaponsOwned = new bool[WeaponCatalog.ArraySize];
                ExtractBoolArray(json, "WeaponsOwned", d.WeaponsOwned);
                d.EquippedPrimary = (int)ExtractLong(json, "EquippedPrimary", (int)WeaponId.None);
                d.EquippedSecondary = (int)ExtractLong(json, "EquippedSecondary", (int)WeaponId.None);
                d.EquippedDrone = (int)ExtractLong(json, "EquippedDrone", (int)PayloadId.None);

                bool hasWheel = json.IndexOf("\"WeaponWheelPoints\"", StringComparison.Ordinal) >= 0;
                if (hasWheel)
                {
                    d.WeaponWheelPoints = new int[WeaponCatalog.ArraySize * WeaponWheel.AttrCount];
                    ExtractIntArray(json, "WeaponWheelPoints", d.WeaponWheelPoints);
                }
                else
                {
                    d.WeaponTiers = new int[WeaponCatalog.ArraySize];
                    ExtractIntArray(json, "WeaponTiers", d.WeaponTiers);
                }
            }
            else
            {
                d.WeaponOwned = ExtractBool(json, "WeaponOwned", false);
                d.GunDamageLevel = (int)ExtractLong(json, "GunDamageLevel", 0);
                d.FireRateLevel = (int)ExtractLong(json, "FireRateLevel", 0);
                d.ClipSizeLevel = (int)ExtractLong(json, "ClipSizeLevel", 0);
            }

            return FromSaveData(d);
        }

        static void AppendField(StringBuilder sb, string name, string value, bool leadingComma)
        {
            if (leadingComma) sb.Append(',');
            sb.Append('"').Append(name).Append("\":").Append(value);
        }

        static void AppendStringArray(StringBuilder sb, string name, string[] values)
        {
            sb.Append(",\"").Append(name).Append("\":[");
            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append('"').Append(values[i] ?? "").Append('"');
                }
            }
            sb.Append(']');
        }

        static string[] ExtractStringArray(string json, string key)
        {
            var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\\[([^\\]]*)\\]");
            if (!match.Success) return Array.Empty<string>();
            string inner = match.Groups[1].Value.Trim();
            if (inner.Length == 0) return Array.Empty<string>();
            string[] parts = inner.Split(',');
            int n = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i].Trim();
                if (p.Length >= 2 && p[0] == '"' && p[p.Length - 1] == '"')
                    p = p.Substring(1, p.Length - 2);
                if (p.Length == 0) continue;
                parts[n++] = p;
            }
            if (n == parts.Length) return parts;
            var trimmed = new string[n];
            Array.Copy(parts, trimmed, n);
            return trimmed;
        }

        static void AppendBoolArray(StringBuilder sb, string name, bool[] values)
        {
            sb.Append(",\"").Append(name).Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(values[i] ? "true" : "false");
            }
            sb.Append(']');
        }

        static void AppendIntArray(StringBuilder sb, string name, int[] values)
        {
            sb.Append(",\"").Append(name).Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(values[i].ToString(CultureInfo.InvariantCulture));
            }
            sb.Append(']');
        }

        static void ExtractBoolArray(string json, string key, bool[] dest)
        {
            var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\\[([^\\]]*)\\]");
            if (!match.Success) return;
            string[] parts = match.Groups[1].Value.Split(',');
            for (int i = 0; i < dest.Length && i < parts.Length; i++)
            {
                string p = parts[i].Trim();
                dest[i] = p == "true" || p == "1";
            }
        }

        static void ExtractIntArray(string json, string key, int[] dest)
        {
            var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\\[([^\\]]*)\\]");
            if (!match.Success) return;
            string[] parts = match.Groups[1].Value.Split(',');
            for (int i = 0; i < dest.Length && i < parts.Length; i++)
            {
                string p = parts[i].Trim();
                if (p.Length == 0) continue;
                long v;
                if (long.TryParse(p, NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                    dest[i] = (int)v;
            }
        }

        static bool ExtractBool(string json, string key, bool fallback)
        {
            var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*(true|false|1|0)");
            if (!match.Success) return fallback;
            return match.Groups[1].Value == "true" || match.Groups[1].Value == "1";
        }

        static long ExtractLong(string json, string key, long fallback)
        {
            var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*(-?\\d+)");
            return match.Success
                ? long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)
                : fallback;
        }
    }
}
