// Gatekeeper Bastion — Field Manual generator.
//
// Emits Docs/Play/Field-Manual.md by CALLING the same code the game runs: Controls.All,
// WeaponCatalog.Def, WeaponTier, WaveDirector.NextWave, TetherSiphon, MetaUpgrades. Every
// number and every key in the manual is therefore the number and key the player meets.
//
// This exists because the hand-maintained versions drifted three ways at once: the root
// README listed a dial on the number row that had moved to [ and ], the builder-side game doc
// sold a weapon the catalog no longer contains, and Docs/Decisions.md recorded a five-wave
// retune as already shipped while WaveDirector still said three. A table nobody can forget
// to update is the fix; prose about the game stays hand-written in The-Game.md.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using GatekeeperBastion.Game;
using GatekeeperBastion.Sim;

public static class FieldManual
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static int Main(string[] args)
    {
        string outPath = args.Length > 0 ? args[0] : "Docs/Play/Field-Manual.md";

        var sb = new StringBuilder();
        Header(sb);
        Controls(sb);
        Weapons(sb);
        Gatekeepers(sb);
        Waves(sb);
        Dial(sb);
        Economy(sb);
        Stations(sb);
        Footer(sb);

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath)));
        File.WriteAllText(outPath, sb.ToString());
        Console.WriteLine("wrote " + outPath + "  (" + sb.Length + " bytes)");
        return 0;
    }

    static string N(float v)
    {
        if (Math.Abs(v - Math.Round(v)) < 0.005f) return ((int)Math.Round(v)).ToString(Inv);
        return v.ToString("0.##", Inv);
    }

    static void Header(StringBuilder sb)
    {
        sb.AppendLine("# Field Manual");
        sb.AppendLine();
        sb.AppendLine("<!-- GENERATED FILE — DO NOT EDIT BY HAND.");
        sb.AppendLine("     Produced by Tools/docgen/run.sh from the live code: Controls.cs,");
        sb.AppendLine("     WeaponCatalog, WeaponTier, WaveDirector, TetherSiphon, MetaUpgrades.");
        sb.AppendLine("     Re-run it after any change to those files; a hand edit here is lost.");
        sb.AppendLine("     Prose about what the game IS lives in The-Game.md and is hand-written. -->");
        sb.AppendLine();
        sb.AppendLine("Everything you can press, carry, fight and buy — read straight out of the build.");
        sb.AppendLine("If this file and the game disagree, the file was not regenerated; the game is right.");
        sb.AppendLine();
    }

    // ---------- controls ----------

    static void Controls(StringBuilder sb)
    {
        sb.AppendLine("## Controls");
        sb.AppendLine();
        sb.AppendLine("The game shows these to you in play — a panel in the bottom-left lists whatever is");
        sb.AppendLine("live right now, so this table is a reference, not something you have to memorise.");
        sb.AppendLine();

        var rows = GatekeeperBastion.Game.Controls.All;
        foreach (var section in rows.Select(r => r.Section).Distinct())
        {
            sb.AppendLine("**" + section + "**");
            sb.AppendLine();
            sb.AppendLine("| Key | Does | Live when |");
            sb.AppendLine("|---|---|---|");
            // Display string only. The alt key is a convenience (keypad, right shift) and
            // printing its enum name — "SHIFT / RightShift" — reads like a leaked identifier.
            foreach (var b in rows.Where(r => r.Section == section))
                sb.AppendLine("| `" + b.Display + "` | " + b.Label + " | " + Contexts(b.Context) + " |");
            sb.AppendLine();
        }
    }

    static string Contexts(ControlContext c)
    {
        if (c == ControlContext.None) return "—";
        var parts = new List<string>();
        if ((c & ControlContext.Fight) != 0 && (c & ControlContext.Prep) != 0
                                            && (c & ControlContext.Assault) != 0)
            parts.Add("in the bastion");
        else
        {
            if ((c & ControlContext.Prep) != 0) parts.Add("between waves");
            if ((c & ControlContext.Assault) != 0) parts.Add("during a wave");
        }
        if ((c & ControlContext.NearInteract) != 0) parts.Add("near the hatch");
        if ((c & ControlContext.Ship) != 0) parts.Add("on the ship");
        if ((c & ControlContext.EndScreen) != 0) parts.Add("end screen");
        return parts.Count == 0 ? "—" : string.Join(", ", parts);
    }

    // ---------- weapons ----------

    static void Weapons(StringBuilder sb)
    {
        var shop = WeaponCatalog.ShopWeapons().ToList();

        sb.AppendLine("## Weapons");
        sb.AppendLine();
        sb.AppendLine("You carry one **Primary** and one **Secondary** at once, plus an empty Drone slot.");
        sb.AppendLine("The Primary is chosen at the ARMORY before you embark and is fixed for the run;");
        sb.AppendLine("switching in the bastion swaps between the slots you already carry, never between");
        sb.AppendLine("owned weapons. Each slot keeps its own clip, so switching is not a free reload.");
        sb.AppendLine();
        sb.AppendLine("| Weapon | Slot | Cost | Damage | Shots/s | Clip | Reload | Range | Notes |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
        foreach (var id in shop)
        {
            var d = WeaponCatalog.Def(id);
            var notes = new List<string>();
            if (d.pellets > 1) notes.Add(d.pellets + " pellets, " + N(d.spreadDegrees) + "° spread");
            if (d.pierceCount > 0) notes.Add("pierces " + d.pierceCount);
            if (d.falloffStart > 0f) notes.Add("falls off past " + N(d.falloffStart) + " m");
            if (d.cost == 0L) notes.Add("free — always affordable");
            sb.AppendLine("| **" + d.displayName + "** | " + d.slot + " | " +
                          (d.cost == 0 ? "0" : d.cost.ToString("N0", Inv)) + " | " +
                          N(d.damage) + (d.pellets > 1 ? " ×" + d.pellets : "") + " | " +
                          N(d.shotsPerSecond) + " | " + N(d.clipSize) + " | " +
                          N(d.reloadSeconds) + " s | " + N(d.range) + " m | " +
                          (notes.Count == 0 ? "—" : string.Join("; ", notes)) + " |");
        }
        sb.AppendLine();

        sb.AppendLine("### Tiers — what upgrading a weapon buys");
        sb.AppendLine();
        sb.AppendLine("Upgrades are **per weapon**, not global. Damage rises ×" +
                      N(WeaponTier.DamageMul(1)) + " a Tier and clip ×" + N(WeaponTier.ClipMul(1)) +
                      "; fire rate never changes, because cadence is how a weapon reads. Tiers unlock");
        sb.AppendLine("after your first death. Effective damage per second, every pellet connecting:");
        sb.AppendLine();

        sb.Append("| Weapon |");
        for (int t = 0; t <= WeaponTier.MaxTier; t++) sb.Append(" T" + t + " |");
        sb.AppendLine();
        sb.Append("|---|");
        for (int t = 0; t <= WeaponTier.MaxTier; t++) sb.Append("---|");
        sb.AppendLine();
        foreach (var id in shop)
        {
            var d = WeaponCatalog.Def(id);
            sb.Append("| " + d.displayName + " |");
            for (int t = 0; t <= WeaponTier.MaxTier; t++)
                sb.Append(" " + (int)Math.Round(WeaponTier.EffectiveDps(d, t)) + " |");
            sb.AppendLine();
        }
        sb.AppendLine();

        sb.Append("Each Tier costs ");
        var costs = new List<string>();
        for (int t = 0; t < WeaponTier.MaxTier; t++) costs.Add(WeaponTier.Cost(t).ToString("N0", Inv));
        sb.AppendLine(string.Join(" / ", costs) + " salvage — **" +
                      WeaponTier.SpentToReach(WeaponTier.MaxTier).ToString("N0", Inv) +
                      "** to take one weapon all the way.");
        sb.AppendLine();
    }

    // ---------- gatekeepers ----------

    static void Gatekeepers(StringBuilder sb)
    {
        sb.AppendLine("## What comes aboard");
        sb.AppendLine();
        sb.AppendLine("Three kinds, and each is defined by what it **refuses** to do. Every kind swings on");
        sb.AppendLine("the same " + N(WaveDirector.GatekeeperAttackInterval) +
                      " s clock — they differ in what they hit, never in how often.");
        sb.AppendLine();
        sb.AppendLine("| Kind | HP | Hits you for | Hits the hatch for | Speed | Refuses |");
        sb.AppendLine("|---|---|---|---|---|---|");

        var kinds = new object[][]
        {
            new object[] { GatekeeperKind.Default, "Default", "nothing — it will march, and it will turn on you if you hurt it" },
            new object[] { GatekeeperKind.Hunter,  "Hunter",  "to touch the hatch; it only ever wants you" },
            new object[] { GatekeeperKind.Ram,     "Ram",     "to notice you at all, even while you shoot it" },
        };
        foreach (var row in kinds)
        {
            var kind = (GatekeeperKind)row[0];
            var name = (string)row[1];
            var refuses = (string)row[2];
            var s = WaveDirector.StatsFor(kind);
            string speed = Math.Abs(s.patrolSpeed - s.chaseSpeed) < 0.01f
                ? N(s.patrolSpeed) + " m/s"
                : N(s.patrolSpeed) + " → " + N(s.chaseSpeed) + " m/s when provoked";
            sb.AppendLine("| **" + name + "** | " + N(s.maxHp) + " | " +
                          (s.playerDamage <= 0f ? "—" : N(s.playerDamage)) + " | " +
                          (s.hatchDamage <= 0f ? "—" : N(s.hatchDamage)) + " | " +
                          speed + " | " + refuses + " |");
        }
        sb.AppendLine();
        sb.AppendLine("HP above is the base line. Later waves multiply it (see the wave table).");
        sb.AppendLine();
    }

    // ---------- waves ----------

    static void Waves(StringBuilder sb)
    {
        var wd = new WaveDirector();

        sb.AppendLine("## The waves");
        sb.AppendLine();
        sb.AppendLine("**" + WaveDirector.MaxWaves + " waves to a run**, and none of them start until you pull the");
        sb.AppendLine("siphon draw. There is no timer: the quiet between waves lasts exactly as long as you");
        sb.AppendLine("want it to. Gatekeepers arrive in groups, not a stream.");
        sb.AppendLine();
        sb.AppendLine("At the dial's lowest setting:");
        sb.AppendLine();
        sb.AppendLine("| Wave | Bodies | Default | Hunter | Ram | HP × | Arrive in groups of | Every |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|");
        for (int i = 0; i < WaveDirector.MaxWaves; i++)
        {
            var s = wd.NextWave(i, 0);
            sb.AppendLine("| " + (i + 1) + " | " + s.enemyCount + " | " + s.defaultCount + " | " +
                          s.hunterCount + " | " + s.ramCount + " | " +
                          N(s.enemyHealthMultiplier) + " | " + s.groupSize + " | " +
                          N(s.spawnInterval) + " s |");
        }
        sb.AppendLine();
        sb.AppendLine("Hunters and Rams are on a fixed schedule — the dial never adds more of them.");
        sb.AppendLine();
    }

    // ---------- dial ----------

    static void Dial(StringBuilder sb)
    {
        sb.AppendLine("## The siphon dial — the wager");
        sb.AppendLine();
        sb.AppendLine("Set between waves, locked once a wave is live. It pays more and sends more, and the");
        sb.AppendLine("extra bodies are **all Defaults** — it cannot conjure a sixth Ram. It changes nothing");
        sb.AppendLine("else: no extra health, no tighter spacing, no larger groups.");
        sb.AppendLine();
        sb.AppendLine("| Dial | You earn | Wave 1 sends | Wave " + WaveDirector.MaxWaves + " sends |");
        sb.AppendLine("|---|---|---|---|");
        var wd = new WaveDirector();
        for (int lvl = TetherSiphon.MinLevel; lvl <= TetherSiphon.MaxLevel; lvl++)
        {
            float mul = TetherSiphon.RewardMultiplierFor(lvl);
            sb.AppendLine("| " + lvl + " | ×" + mul.ToString("0.00", Inv) + " | " +
                          wd.NextWave(0, lvl).enemyCount + " | " +
                          wd.NextWave(WaveDirector.MaxWaves - 1, lvl).enemyCount + " |");
        }
        sb.AppendLine();
    }

    // ---------- economy ----------

    static void Economy(StringBuilder sb)
    {
        sb.AppendLine("## Salvage");
        sb.AppendLine();
        sb.AppendLine("**Gatekeepers are worth money and the money is yours the instant they die.** Not at the");
        sb.AppendLine("end of the wave, not if you survive — the moment they drop. Dying takes none of it");
        sb.AppendLine("back, and clearing the bastion adds no bonus. A run you lose still paid you.");
        sb.AppendLine();
        sb.AppendLine("A kill pays `" + N(MetaUpgrades.SalvagePerKill(0)) + "` at Scavenging 0, up to `" +
                      N(MetaUpgrades.SalvagePerKill(MetaUpgrades.MaxLevel)) + "` at " +
                      MetaUpgrades.MaxLevel + ", then multiplied by the dial.");
        sb.AppendLine("Nothing pays per second, and the quiet between waves earns nothing at all.");
        sb.AppendLine();
        sb.AppendLine("The hatch is what you are defending: **" + N(ShipSystems.HatchMaxHp) +
                      " HP** at Hatch Armor 0, up to **" + N(MetaUpgrades.HatchHp(MetaUpgrades.MaxLevel)) +
                      "**. Hold `E` near it to repair.");
        sb.AppendLine("If it falls, the run ends. If you fall, the run ends. There is no other way out —");
        sb.AppendLine("no bail-out, no escape lever, no respawn.");
        sb.AppendLine();
    }

    // ---------- stations ----------

    static void Stations(StringBuilder sb)
    {
        sb.AppendLine("## The ship — where salvage goes");
        sb.AppendLine();
        sb.AppendLine("Between runs you walk your own ship. Four consoles, each owning one subject.");
        sb.AppendLine();
        sb.AppendLine("| Console | Owns | Sells |");
        sb.AppendLine("|---|---|---|");
        sb.AppendLine("| **ARMORY** | the weapon in your hands | weapons, and Tiers for whichever you carry |");
        sb.AppendLine("| **CREW QUARTERS** | you | Vitality, Stamina, Scavenging, Luck |");
        sb.AppendLine("| **FABRICATION** | the ship | Hatch Armor, Equipment slot, Repair Rig |");
        sb.AppendLine("| **NAV MAP** | where you go | the destination, and embarking |");
        sb.AppendLine();
        sb.AppendLine("Attribute and ship tracks run 0–" + MetaUpgrades.MaxLevel + " at +5% a level, costing " +
                      MetaUpgrades.NextCost(0).ToString("N0", Inv) + " × 2^level — " +
                      Enumerable.Range(0, MetaUpgrades.MaxLevel)
                                .Select(l => MetaUpgrades.NextCost(l).ToString("N0", Inv))
                                .Aggregate((a, b) => a + " / " + b) + ".");
        sb.AppendLine();
        sb.AppendLine("| Track | Level 0 | Level " + MetaUpgrades.MaxLevel + " |");
        sb.AppendLine("|---|---|---|");
        sb.AppendLine("| Vitality — your HP | " + N(MetaUpgrades.PlayerMaxHp(0)) + " | " +
                      N(MetaUpgrades.PlayerMaxHp(MetaUpgrades.MaxLevel)) + " |");
        sb.AppendLine("| Stamina — sprint pool | " + N(MetaUpgrades.StaminaMax(0)) + " | " +
                      N(MetaUpgrades.StaminaMax(MetaUpgrades.MaxLevel)) + " |");
        sb.AppendLine("| Scavenging — salvage per kill | " + N(MetaUpgrades.SalvagePerKill(0)) + " | " +
                      N(MetaUpgrades.SalvagePerKill(MetaUpgrades.MaxLevel)) + " |");
        sb.AppendLine("| Luck — crit chance (double damage) | " +
                      N(MetaUpgrades.CritChance(0) * 100f) + "% | " +
                      N(MetaUpgrades.CritChance(MetaUpgrades.MaxLevel) * 100f) + "% |");
        sb.AppendLine("| Hatch Armor — hatch HP | " + N(MetaUpgrades.HatchHp(0)) + " | " +
                      N(MetaUpgrades.HatchHp(MetaUpgrades.MaxLevel)) + " |");
        sb.AppendLine("| Equipment slot (once, after first loss) | — | " +
                      EquipmentCatalog.SlotPrice.ToString("N0", Inv) + " salvage |");
        sb.AppendLine("| Repair Rig — flat repair rate | " +
                      N(EquipmentCatalog.Def(EquipmentId.RepairRig).repairPerSecond) + " HP/s | " +
                      EquipmentCatalog.Def(EquipmentId.RepairRig).cost.ToString("N0", Inv) + " salvage |");
        sb.AppendLine();
        sb.AppendLine("Luck is the only source of critical hits in the game, on purpose: two sources would");
        sb.AppendLine("multiply against each other and the numbers on the panel would stop meaning anything.");
        sb.AppendLine();
        sb.AppendLine("**Attributes are never locked.** They are the answer to dying, so you can buy them");
        sb.AppendLine("before you have died. Weapon Tiers and the equipment slot unlock after your first death.");
        sb.AppendLine();
    }

    static void Footer(StringBuilder sb)
    {
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("*Generated from the build by `Tools/docgen/run.sh`. What the game is, rather than");
        sb.AppendLine("what it contains, is in [The Game](The-Game.md).*");
    }
}
