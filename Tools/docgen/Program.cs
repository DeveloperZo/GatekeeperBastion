// Gatekeeper's Protocol — doc generator.
//
// Writes Docs/Play/Field-Manual.md (whole file) and splices countable blocks into
// Docs/Design/4-Data.md. Every number comes from a Sim/ call; prose outside GKB markers
// is copied through unchanged.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;

public static class Docgen
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    static readonly string[] KnownBlockIds =
    {
        "mk1-base", "wheel", "rings", "tier-cost", "abilities",
        "gatekeepers", "waves", "dial", "player-count", "intensity-juice", "hatch", "hatch-bank", "respawns", "master-control", "tracks",
        "bastion-mesh", "ship-camera"
    };

    public static int Main(string[] args)
    {
        string fieldManual = null;
        string data = null;
        bool check = false;

        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            if (a == "--check") check = true;
            else if (a == "--field-manual" && i + 1 < args.Length) fieldManual = args[++i];
            else if (a == "--data" && i + 1 < args.Length) data = args[++i];
            else if (!a.StartsWith("-") && fieldManual == null && data == null)
                fieldManual = a; // legacy single-path write
            else
            {
                Console.Error.WriteLine("docgen: unknown arg " + a);
                return 2;
            }
        }

        if (fieldManual == null && data == null)
        {
            Console.Error.WriteLine("docgen: pass --field-manual <path> and/or --data <path>");
            return 2;
        }

        int worst = 0;

        if (fieldManual != null)
        {
            int rc = RunFieldManual(fieldManual, check);
            if (rc > worst) worst = rc;
        }

        if (data != null)
        {
            int rc = RunData(data, check);
            if (rc > worst) worst = rc;
        }

        return worst;
    }

    static int RunFieldManual(string path, bool check)
    {
        string generated = BuildFieldManual();
        if (check)
        {
            if (!File.Exists(path))
            {
                Console.Error.WriteLine("docgen --check: " + Rel(path) + " does not exist.");
                return 1;
            }
            string committed = File.ReadAllText(path);
            if (Normalize(committed) != Normalize(generated))
            {
                Console.Error.WriteLine("docgen --check: " + Rel(path) + " is stale.");
                return 1;
            }
            Console.WriteLine("docgen --check: " + Rel(path) + " matches the build.");
            return 0;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
        File.WriteAllText(path, generated);
        Console.WriteLine("wrote " + Rel(path) + "  (" + generated.Length + " bytes)");
        return 0;
    }

    static int RunData(string path, bool check)
    {
        if (!File.Exists(path))
        {
            Console.Error.WriteLine("docgen: " + Rel(path) + " does not exist.");
            return 2;
        }

        string committed = File.ReadAllText(path);
        Dictionary<string, string> blocks;
        try
        {
            blocks = BuildDataBlocks();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("docgen: emitter failed: " + ex.Message);
            return 2;
        }

        string spliced;
        string fault;
        int parseRc = Splice(committed, blocks, out spliced, out fault);
        if (parseRc != 0)
        {
            Console.Error.WriteLine("docgen: " + Rel(path) + ": " + fault);
            return 2;
        }

        if (check)
        {
            // Compare per block so the failure names the id.
            Dictionary<string, string> committedBodies;
            string bodyFault;
            int bodyRc = ExtractBodies(committed, out committedBodies, out bodyFault);
            if (bodyRc != 0)
            {
                Console.Error.WriteLine("docgen: " + Rel(path) + ": " + bodyFault);
                return 2;
            }

            foreach (var id in KnownBlockIds)
            {
                string expected = NormalizeBlock(blocks[id]);
                string actual = NormalizeBlock(committedBodies[id]);
                if (expected != actual)
                {
                    Console.Error.WriteLine(
                        "docgen --check: " + Rel(path) + " block `" + id + "` is stale.");
                    return 1;
                }
            }

            // Also catch drift in the non-block remainder (e.g. header notice).
            if (Normalize(committed) != Normalize(spliced))
            {
                Console.Error.WriteLine("docgen --check: " + Rel(path) + " is stale (outside blocks).");
                return 1;
            }

            Console.WriteLine("docgen --check: " + Rel(path) + " matches the build.");
            return 0;
        }

        File.WriteAllText(path, spliced);
        Console.WriteLine("wrote " + Rel(path) + "  (" + spliced.Length + " bytes)");
        return 0;
    }

    // ---------- splice ----------

    /// <summary>Replace bodies between GKB:GEN/END markers. Returns 0 ok, 2 on structural fault.</summary>
    public static int Splice(
        string committed, IDictionary<string, string> blocks,
        out string result, out string fault)
    {
        result = null;
        fault = null;
        Dictionary<string, string> existing;
        int rc = ExtractBodies(committed, out existing, out fault);
        if (rc != 0) return rc;

        foreach (var id in existing.Keys)
        {
            if (!blocks.ContainsKey(id))
            {
                fault = "block id `" + id + "` has no emitter";
                return 2;
            }
        }
        foreach (var id in blocks.Keys)
        {
            if (!existing.ContainsKey(id))
            {
                fault = "emitter `" + id + "` has no block in the file";
                return 2;
            }
        }

        var genRe = new Regex(
            @"<!--\s*GKB:GEN\s+(\S+)\s*—\s*[^>]*-->\r?\n?",
            RegexOptions.CultureInvariant);
        var endRe = new Regex(
            @"<!--\s*GKB:END\s+(\S+)\s*-->\r?\n?",
            RegexOptions.CultureInvariant);

        var sb = new StringBuilder();
        int pos = 0;
        while (pos < committed.Length)
        {
            var gm = genRe.Match(committed, pos);
            if (!gm.Success)
            {
                sb.Append(committed, pos, committed.Length - pos);
                break;
            }

            sb.Append(committed, pos, gm.Index - pos);
            sb.Append(gm.Value);
            if (!gm.Value.EndsWith("\n")) sb.Append('\n');

            string id = gm.Groups[1].Value;
            var em = endRe.Match(committed, gm.Index + gm.Length);
            // ExtractBodies already validated pairing; find matching END for this id.
            while (em.Success && em.Groups[1].Value != id)
                em = endRe.Match(committed, em.Index + em.Length);
            if (!em.Success)
            {
                fault = "unclosed GKB:GEN `" + id + "`";
                return 2;
            }

            string body = blocks[id];
            if (body.Length > 0)
            {
                sb.Append(body);
                if (!body.EndsWith("\n")) sb.Append('\n');
            }
            sb.Append(em.Value);
            if (!em.Value.EndsWith("\n")) sb.Append('\n');
            pos = em.Index + em.Length;
        }

        result = sb.ToString();
        fault = null;
        return 0;
    }

    static int ExtractBodies(
        string committed, out Dictionary<string, string> bodies, out string fault)
    {
        bodies = new Dictionary<string, string>(StringComparer.Ordinal);
        fault = null;

        var genRe = new Regex(
            @"<!--\s*GKB:GEN\s+(\S+)\s*—\s*[^>]*-->\r?\n?",
            RegexOptions.CultureInvariant);
        var endRe = new Regex(
            @"<!--\s*GKB:END\s+(\S+)\s*-->\r?\n?",
            RegexOptions.CultureInvariant);

        var matches = genRe.Matches(committed);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        int cursor = 0;

        foreach (Match gm in matches)
        {
            if (gm.Index < cursor)
            {
                fault = "overlapping GKB:GEN markers";
                return 2;
            }

            string id = gm.Groups[1].Value;
            if (!seen.Add(id))
            {
                fault = "duplicate block id `" + id + "`";
                return 2;
            }

            var em = endRe.Match(committed, gm.Index + gm.Length);
            if (!em.Success)
            {
                fault = "unclosed GKB:GEN `" + id + "`";
                return 2;
            }
            if (em.Groups[1].Value != id)
            {
                // Look for the matching end; a wrong id first means nested/mismatched.
                Match correct = em;
                while (correct.Success && correct.Groups[1].Value != id)
                    correct = endRe.Match(committed, correct.Index + correct.Length);
                if (!correct.Success)
                {
                    fault = "unclosed GKB:GEN `" + id + "`";
                    return 2;
                }
                em = correct;
            }

            int bodyStart = gm.Index + gm.Length;
            int bodyLen = em.Index - bodyStart;
            bodies[id] = committed.Substring(bodyStart, bodyLen);
            cursor = em.Index + em.Length;
        }

        // Trailing END without GEN.
        foreach (Match em in endRe.Matches(committed))
        {
            string id = em.Groups[1].Value;
            if (!bodies.ContainsKey(id))
            {
                fault = "GKB:END `" + id + "` without GKB:GEN";
                return 2;
            }
        }

        return 0;
    }

    static Dictionary<string, string> BuildDataBlocks()
    {
        var d = new Dictionary<string, string>(StringComparer.Ordinal);
        d["mk1-base"] = EmitMk1Base();
        d["wheel"] = EmitWheel();
        d["rings"] = EmitRings();
        d["tier-cost"] = EmitTierCost();
        d["abilities"] = EmitAbilities();
        d["gatekeepers"] = EmitGatekeepers();
        d["waves"] = EmitWaves();
        d["dial"] = EmitDial();
        d["player-count"] = EmitPlayerCount();
        d["intensity-juice"] = EmitIntensityJuice();
        d["hatch"] = EmitHatch();
        d["hatch-bank"] = EmitHatchBank();
        d["respawns"] = EmitRespawns();
        d["master-control"] = EmitMasterControl();
        d["tracks"] = EmitTracks();
        d["bastion-mesh"] = EmitBastionMesh();
        d["ship-camera"] = EmitShipCamera();
        return d;
    }

    // ---------- data emitters ----------

    static string EmitMk1Base()
    {
        var d = WeaponCatalog.Def(WeaponId.Mk1Sidearm);
        var sb = new StringBuilder();
        sb.AppendLine("| | base | | base |");
        sb.AppendLine("|---|---|---|---|");
        sb.AppendLine("| damage/shot | " + N(d.damage) + " | reload | " + N(d.reloadSeconds) + "s |");
        sb.AppendLine("| shots/second | " + N(d.shotsPerSecond) + " | range | " + N(d.range) + " m |");
        sb.AppendLine("| clip | " + N(d.clipSize) + " | | |");
        return sb.ToString().TrimEnd('\r', '\n');
    }

    static string EmitWheel()
    {
        int focus = WeaponWheel.FocusAtMax;
        int neigh = WeaponWheel.NeighbourAtMax;
        var sb = new StringBuilder();
        sb.AppendLine("| attribute | per point | focus (" + focus + ") | neighbour (" + neigh + ") |");
        sb.AppendLine("|---|---|---|---|");
        sb.AppendLine("| damage | ×" + N(WeaponWheel.DamagePerPoint) + " | ×" +
                      N(WeaponWheel.DamageMul(focus)) + " | ×" + N(WeaponWheel.DamageMul(neigh)) + " |");
        sb.AppendLine("| clip | ×" + N(WeaponWheel.ClipPerPoint) + " | ×" +
                      N(WeaponWheel.ClipMul(focus)) + " | ×" + N(WeaponWheel.ClipMul(neigh)) + " |");
        sb.AppendLine("| reload | ×" + N(WeaponWheel.ReloadPerPoint) + " | ×" +
                      N(WeaponWheel.ReloadMul(focus)) + " | ×" + N(WeaponWheel.ReloadMul(neigh)) + " |");
        sb.AppendLine("| fire rate | ×" + N(WeaponWheel.FireRatePerPoint) + " | ×" +
                      N(WeaponWheel.FireRateMul(focus)) + " | ×" + N(WeaponWheel.FireRateMul(neigh)) + " |");
        sb.AppendLine("| ability | dial points → `WeaponAbility.Magnitude` (`t = p/" + focus + "`) | | |");
        return sb.ToString().TrimEnd('\r', '\n');
    }

    static string EmitRings()
    {
        var sb = new StringBuilder();
        sb.AppendLine("| Weapon | Ring | Strength | Weakness |");
        sb.AppendLine("|---|---|---|---|");
        foreach (var id in RingTableWeapons())
        {
            string name = ShortWeaponName(id);
            var ring = WeaponWheel.RingOf(id);
            if (ring == null)
            {
                sb.AppendLine("| " + name + " | *unset* | — | — |");
                continue;
            }
            string ringStr = string.Join(" ", ring.Select(AttrAbbrev));
            WheelAttr strength, weakness;
            WeaponWheel.TryIdentity(id, out strength, out weakness);
            sb.AppendLine("| " + name + " | " + ringStr + " | " +
                          WeaponWheel.AttrLabel(strength) + " | " +
                          WeaponWheel.AttrLabel(weakness) + " |");
        }
        return sb.ToString().TrimEnd('\r', '\n');
    }

    static IEnumerable<WeaponId> RingTableWeapons()
    {
        yield return WeaponId.Br12Shotgun;
        yield return WeaponId.Mr35Rifle;
        yield return WeaponId.PriC;
        yield return WeaponId.Mk1Sidearm;
        yield return WeaponId.SecD;
    }

    static string EmitTierCost()
    {
        var primary = new List<string>();
        var secondary = new List<string>();
        for (int t = 0; t < WeaponTier.MaxTier; t++)
        {
            primary.Add(WeaponTier.Cost(t).ToString("N0", Inv));
            secondary.Add(WeaponTier.Cost(WeaponSlot.Secondary, t).ToString("N0", Inv));
        }
        return "Cost of the next purchase on one centred spoke, Primary slot `400 + 200 × centredCount` = " +
               string.Join("/", primary) + ",\n**" +
               WeaponTier.SpentToReach(WeaponTier.MaxTier).ToString("N0", Inv) +
               "** cumulative to stack ten on that spoke. Secondary slot `150 + 150 × centredCount` = " +
               string.Join("/", secondary) + ",\n**" +
               WeaponTier.SpentToReach(WeaponSlot.Secondary, WeaponTier.MaxTier).ToString("N0", Inv) +
               "** cumulative. Neighbour spill does not raise a neighbour's next price. Ten purchases still cap the weapon.";
    }

    static string EmitAbilities()
    {
        int max = WeaponAbility.MaxDialPoints;
        var sb = new StringBuilder();
        sb.AppendLine("| weapon | ability | dial | at 0 | at " + max + " | cooldown | fixed |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        AppendAbilityRow(sb, "BR-12", AbilityId.Br12Concussive, "metres of shove",
            N(WeaponAbility.Magnitude(AbilityId.Br12Concussive, 0)) + " m",
            N(WeaponAbility.Magnitude(AbilityId.Br12Concussive, max)) + " m",
            "cone half-angle " + N(WeaponAbility.Br12ConeHalfAngleDegrees) + "°");
        AppendAbilityRow(sb, "MR-35", AbilityId.Mr35Pin, "seconds held",
            N(WeaponAbility.Magnitude(AbilityId.Mr35Pin, 0)) + " s",
            N(WeaponAbility.Magnitude(AbilityId.Mr35Pin, max)) + " s",
            WeaponAbility.PinHitThreshold + " hits to pin; " +
            N(WeaponAbility.PinBuildDecaySeconds) + " s decay");
        AppendAbilityRow(sb, "MK-1", AbilityId.Mk1RegenSurge, "**surge duration**",
            N(WeaponAbility.Magnitude(AbilityId.Mk1RegenSurge, 0)) + " s",
            N(WeaponAbility.Magnitude(AbilityId.Mk1RegenSurge, max)) + " s",
            "rate fixed at " + N(WeaponAbility.Mk1SurgeMultiplier) + "×");
        AppendAbilityRow(sb, "PRI-C", AbilityId.PriCRailshot, "bodies at full damage",
            N(WeaponAbility.Magnitude(AbilityId.PriCRailshot, 0)),
            N(WeaponAbility.Magnitude(AbilityId.PriCRailshot, max)),
            "arms " + N(WeaponAbility.PriCRailshotArmedSeconds) +
            " s; ignores `pierceDamageMul`");
        return sb.ToString().TrimEnd('\r', '\n');
    }

    static void AppendAbilityRow(
        StringBuilder sb, string weapon, AbilityId id, string dial,
        string at0, string atMax, string fixedCol)
    {
        sb.AppendLine("| " + weapon + " | " + WeaponAbility.DisplayName(id) + " | " + dial +
                      " | " + at0 + " | " + atMax + " | " +
                      N(WeaponAbility.Cooldown(id)) + " s | " + fixedCol + " |");
    }

    static string EmitGatekeepers()
    {
        var sb = new StringBuilder();
        sb.AppendLine("| kind | HP | player dmg | Hatch dmg | patrol | chase | refuses |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        AppendGkRow(sb, "Default", GatekeeperKind.Default);
        AppendGkRow(sb, "Hunter", GatekeeperKind.Hunter);
        AppendGkRow(sb, "Thief", GatekeeperKind.Thief);
        AppendGkRow(sb, "Cloaker", GatekeeperKind.Cloaker);
        return sb.ToString().TrimEnd('\r', '\n');
    }

    static void AppendGkRow(StringBuilder sb, string name, GatekeeperKind kind)
    {
        var s = WaveDirector.StatsFor(kind);
        string refuses = "—";
        if (s.ignoresHatch) refuses = "the Hatch (`ignoresHatch`)";
        else if (s.ignoresPlayer) refuses = "the player (`ignoresPlayer`; never provoked)";
        // The Cloaker's refusal is a per-frame STATE (watched / unobserved) in the Game
        // layer's CloakerGatekeeper — neither pushed flag is set, so it is stated here or
        // the table prints "—" and the kind reads as a Default in a costume.
        if (kind == GatekeeperKind.Cloaker)
            refuses = "being seen (watch cone; a state in `CloakerGatekeeper`, not a flag)";

        string pd = s.playerDamage <= 0f ? "**0**" : N(s.playerDamage);
        string hd = s.hatchDamage <= 0f ? "**0**" : N(s.hatchDamage);
        if (kind == GatekeeperKind.Hunter) hd = "**0**";
        if (kind == GatekeeperKind.Thief) pd = "**0**";

        sb.AppendLine("| " + name + " | " + N(s.maxHp) + " | " + pd + " | " + hd + " | " +
                      N(s.patrolSpeed) + " | " + N(s.chaseSpeed) + " | " + refuses + " |");
    }

    static string EmitWaves()
    {
        var wd = new WaveDirector();
        var sb = new StringBuilder();
        sb.AppendLine("| Wave | Bodies | Default | Hunter | Thief | Cloaker | HP × | groups of | Every |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
        for (int i = 0; i < WaveDirector.MaxWaves; i++)
        {
            var s = wd.NextWave(i, 0);
            sb.AppendLine("| " + (i + 1) + " | " + s.enemyCount + " | " + s.defaultCount + " | " +
                          s.hunterCount + " | " + s.thiefCount + " | " + s.cloakerCount + " | " +
                          N(s.enemyHealthMultiplier) + " | " + s.groupSize + " | " +
                          N(s.spawnInterval) + " s |");
        }
        return sb.ToString().TrimEnd('\r', '\n');
    }

    static string EmitDial()
    {
        var wd = new WaveDirector();
        var sb = new StringBuilder();
        sb.AppendLine("| Dial | You earn | Wave 1 sends | Wave " + WaveDirector.MaxWaves + " sends |");
        sb.AppendLine("|---|---|---|---|");
        for (int lvl = TetherSiphon.MinLevel; lvl <= TetherSiphon.MaxLevel; lvl++)
        {
            float mul = TetherSiphon.RewardMultiplierFor(lvl);
            sb.AppendLine("| " + lvl + " | ×" + mul.ToString("0.00", Inv) + " | " +
                          wd.NextWave(0, lvl).enemyCount + " | " +
                          wd.NextWave(WaveDirector.MaxWaves - 1, lvl).enemyCount + " |");
        }
        return sb.ToString().TrimEnd('\r', '\n');
    }

    static string EmitPlayerCount()
    {
        var wd = new WaveDirector();
        var sb = new StringBuilder();
        sb.AppendLine("| | |");
        sb.AppendLine("|---|---|");
        sb.AppendLine("| supported | " + WaveDirector.SupportedPlayerCount + " (host plus two) |");
        sb.AppendLine("| capacity | " + WaveDirector.PlayerCapacity + " |");
        sb.AppendLine("| wave 1 bodies ×1 / ×3 | " + wd.NextWave(0, 0, 1).enemyCount +
                      " / " + wd.NextWave(0, 0, 3).enemyCount + " |");
        sb.AppendLine("| bank cap deepest 1 | " + HatchBank.BankCap(1) + " |");
        sb.AppendLine("| bank cap deepest 2 | " + HatchBank.BankCap(2) + " |");
        return sb.ToString().TrimEnd('\r', '\n');
    }

    static string EmitIntensityJuice()
    {
        var sb = new StringBuilder();
        sb.AppendLine("| | |");
        sb.AppendLine("|---|---|");
        sb.AppendLine("| juice per step | " + IntensityJuice.CostPerStep + " banked |");
        return sb.ToString().TrimEnd('\r', '\n');
    }

    static string EmitHatch()
    {
        var rig = EquipmentCatalog.Def(EquipmentId.RepairRig);
        var sb = new StringBuilder();
        sb.AppendLine("| cost | value |");
        sb.AppendLine("|---|---|");
        sb.AppendLine("| Hatch HP (Armor 0) | " + N(ShipSystems.HatchMaxHp) + " |");
        sb.AppendLine("| Hatch HP (Armor " + MetaUpgrades.MaxLevel + ") | " +
                      N(MetaUpgrades.HatchHp(MetaUpgrades.MaxLevel)) + " |");
        sb.AppendLine("| per HP healed | " + N(ShipSystems.RepairSalvagePerHp) +
                      " Salvage (`ShipSystems.RepairSalvagePerHp`) |");
        sb.AppendLine("| equipment slot | **" + EquipmentCatalog.SlotPrice.ToString("N0", Inv) +
                      "** Salvage, once, after the first bank |");
        sb.AppendLine("| repair rig | **" + rig.cost.ToString("N0", Inv) +
                      "** Salvage, flat " + N(rig.repairPerSecond) + " HP/s |");
        return sb.ToString().TrimEnd('\r', '\n');
    }

    static string EmitHatchBank()
    {
        var wd = new WaveDirector();
        var sb = new StringBuilder();
        sb.AppendLine("| | |");
        sb.AppendLine("|---|---|");
        sb.AppendLine("| drip | " + N(HatchBank.DripPerSecond) + " staked/s |");
        sb.AppendLine("| duration | " + N(HatchBank.DurationSeconds) + " s |");
        sb.AppendLine("| withdraw | " + N(HatchBank.WithdrawPerSecond) + " salvage/s |");
        sb.AppendLine("| returned keeps | " + N(HatchBank.ReturnedKeepShare * 100f) +
                      "% of a leftover stake (Lost wipes the stake, never the bank) |");
        sb.AppendLine();
        sb.AppendLine("| Waves carried | convert | Hatch Wave | bodies at intensity 0 |");
        sb.AppendLine("|---|---|---|---|");
        for (int w = 1; w <= WaveDirector.MaxWaves; w++)
        {
            int idx = HatchBank.CarryWaveIndex(w);
            var spec = HatchBank.ApplyHatchComposition(wd.NextWave(idx, 0), w);
            sb.AppendLine("| " + w + " | ×" +
                          HatchBank.CarryConvert(w).ToString("0.00", Inv) +
                          " | " + (idx + 1) + " | " + spec.enemyCount + " |");
        }
        return sb.ToString().TrimEnd('\r', '\n');
    }

    static string EmitRespawns()
    {
        var sb = new StringBuilder();
        sb.AppendLine("| | |");
        sb.AppendLine("|---|---|");
        sb.AppendLine("| paid from | the realm's bank; a staked pile is never touched |");
        // Read the cap off Respawns rather than restating the shape: the climb was uncapped
        // until 2026-08-27 and this line said so in hardcoded text, which is exactly how a
        // generated block starts lying about the code it is generated from.
        sb.AppendLine("| cost | " + Respawns.BaseCost + " × min(taken + 1, " + Respawns.MaxSteps + ") |");
        sb.AppendLine("| first three | " + Respawns.CostFor(0) + " / " + Respawns.CostFor(1) +
                      " / " + Respawns.CostFor(2) + " |");
        sb.AppendLine("| climbs to | " + Respawns.CostFor(Respawns.MaxSteps - 1) +
                      " and stops — every later death is the same price |");
        sb.AppendLine("| cap | " + Respawns.MaxRespawns + " per run, all seats together |");
        sb.AppendLine("| bleed-out | " + Respawns.BleedOutSeconds + " s to buy back, or the Boarding ends |");
        sb.AppendLine("| refused when | the cap is spent, or the bank is short of the price |");
        return sb.ToString().TrimEnd('\r', '\n');
    }

    static string EmitMasterControl()
    {
        var sb = new StringBuilder();
        sb.AppendLine("| | |");
        sb.AppendLine("|---|---|");
        var shapes = new List<string>();
        for (int i = 1; i <= MasterControl.TerminalWaves; i++)
            shapes.Add(WaveShapes.Name(MasterControl.ShapeForTerminalWave(i)));
        sb.AppendLine("| terminal Waves | " + MasterControl.TerminalWaves + " — "
                      + string.Join(", then ", shapes) + ", at the Wave-" + WaveDirector.MaxWaves + " size |");
        sb.AppendLine("| gap | " + N(WaveDirector.InterWaveGap) + " s |");
        sb.AppendLine("| beat before the elite | " + N(MasterControl.FinaleBeatSeconds) + " s |");
        sb.AppendLine("| escort | " + MasterControl.EscortHunters + " Hunters, "
                      + MasterControl.EscortCloakers + " Cloakers — the elite is shielded until they are down |");
        // The finale has no Hatch-bank half since 2026-08-28 — it ends at its own terminal
        // with the elite as the gate. Emitting MasterControl.HatchWaves here documented a
        // beat the game no longer has.
        sb.AppendLine("| elite waves | 1, and killing it arms the terminal |");
        sb.AppendLine("| finale elite HP | " + N(MasterControl.FinaleEliteHp) + " |");
        return sb.ToString().TrimEnd('\r', '\n');
    }

    static string EmitTracks()
    {
        var costs = new List<string>();
        long cum = 0;
        for (int l = 0; l < MetaUpgrades.MaxLevel; l++)
        {
            long c = MetaUpgrades.NextCost(l);
            cum += c;
            costs.Add(c.ToString("N0", Inv));
        }

        var sb = new StringBuilder();
        sb.AppendLine("Cost `" + MetaUpgrades.BaseCost + " * 2^currentLevel` = " +
                      string.Join("/", costs) + ", **" + cum.ToString("N0", Inv) +
                      "** to max a track.");
        sb.AppendLine();
        sb.AppendLine("| Track | Station | Level 0 | Level " + MetaUpgrades.MaxLevel + " |");
        sb.AppendLine("|---|---|---|---|");
        foreach (var track in UpgradeTracks.All)
        {
            var a0 = UpgradeTracks.ValueAt(track, 0);
            var aM = UpgradeTracks.ValueAt(track, MetaUpgrades.MaxLevel);
            sb.AppendLine("| " + UpgradeTracks.Label(track) + " | " +
                          UpgradeTracks.Station(track) + " | " +
                          FormatTrackValue(track, a0) + " | " +
                          FormatTrackValue(track, aM) + " |");
        }
        return sb.ToString().TrimEnd('\r', '\n');
    }

    static string FormatTrackValue(UpgradeTrack track, UpgradeTracks.ValueAtResult v)
    {
        if (string.IsNullOrEmpty(v.Unit))
            return N(v.Value);
        return N(v.Value) + " " + v.Unit;
    }

    static string EmitBastionMesh()
    {
        var sb = new StringBuilder();
        sb.AppendLine("| Cell | Prefab |");
        sb.AppendLine("|---|---|");
        sb.AppendLine("| Floor | `" + BastionMeshMap.Floor + "` |");
        sb.AppendLine("| Ceiling | `" + BastionMeshMap.Ceiling + "` |");
        foreach (BastionSideKind side in Enum.GetValues(typeof(BastionSideKind)))
        {
            string prefab = BastionMeshMap.ForSide(side); // throws on an unmapped kind
            sb.AppendLine("| " + side + " | " +
                          (prefab == null ? "nothing emitted" : "`" + prefab + "`") + " |");
        }
        sb.AppendLine("| Wall corner | `" + BastionMeshMap.CornerPillar + "` |");
        sb.AppendLine("| Ceiling fixture | `" + BastionMeshMap.CeilingLight + "` |");
        return sb.ToString().TrimEnd('\r', '\n');
    }

    static string EmitShipCamera()
    {
        var sb = new StringBuilder();
        sb.AppendLine("| Rig | Value |");
        sb.AppendLine("|---|---|");
        sb.AppendLine("| yaw (locked) | " + N(ShipCamera.YawDegrees) + "° |");
        sb.AppendLine("| pitch | " + N(ShipCamera.PitchDegrees) + "° |");
        sb.AppendLine("| field of view | " + N(ShipCamera.FovDegrees) + "° |");
        sb.AppendLine("| follow distance | " + N(ShipCamera.FollowDistance) + " m |");
        sb.AppendLine("| dead zone half X / half Z | " + N(ShipCamera.DeadZoneHalfX) + " / " +
                      N(ShipCamera.DeadZoneHalfZ) + " m |");
        sb.AppendLine("| follow lerp | " + N(ShipCamera.FollowLerpPerSecond) + "/s |");
        return sb.ToString().TrimEnd('\r', '\n');
    }

    // ---------- field manual ----------

    static string BuildFieldManual()
    {
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
        return sb.ToString();
    }

    static void Header(StringBuilder sb)
    {
        sb.AppendLine("# Field Manual");
        sb.AppendLine();
        sb.AppendLine("<!-- GENERATED FILE — DO NOT EDIT BY HAND.");
        sb.AppendLine("     Produced by Tools/docgen/run.sh from the live code: Controls.cs,");
        sb.AppendLine("     WeaponCatalog, WeaponWheel, WeaponTier, WaveDirector, TetherSiphon,");
        sb.AppendLine("     MetaUpgrades, UpgradeTracks, EquipmentCatalog.");
        sb.AppendLine("     Re-run it after any change to those files; a hand edit here is lost.");
        sb.AppendLine("     Prose about what the game IS lives in The-Game.md and is hand-written. -->");
        sb.AppendLine();
        sb.AppendLine("Everything you can press, carry, fight and buy — read straight out of the build.");
        sb.AppendLine("If this file and the game disagree, the file was not regenerated; the game is right.");
        sb.AppendLine();
    }

    static void Controls(StringBuilder sb)
    {
        sb.AppendLine("## Controls");
        sb.AppendLine();
        sb.AppendLine("The game shows these to you in play — a panel in the bottom-left lists whatever is");
        sb.AppendLine("live right now, so this table is a reference, not something you have to memorise.");
        sb.AppendLine();

        var rows = GatekeepersProtocol.Game.Controls.All;
        foreach (var section in rows.Select(r => r.Section).Distinct())
        {
            sb.AppendLine("**" + section + "**");
            sb.AppendLine();
            sb.AppendLine("| Key | Does | Live when |");
            sb.AppendLine("|---|---|---|");
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

    static void Weapons(StringBuilder sb)
    {
        var shop = WeaponCatalog.ShopWeapons().ToList();

        sb.AppendLine("## Weapons");
        sb.AppendLine();
        sb.AppendLine("You carry one **Primary** and one **Secondary** at once. The Drone bay holds a payload — today the **Decoy**, sold at FABRICATION.");
        sb.AppendLine("The Primary is chosen at the ARMORY before you embark and is fixed for the Boarding;");
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

        sb.AppendLine("### Upgrade wheel — what a purchase buys");
        sb.AppendLine();
        sb.AppendLine("Upgrades are **per weapon**, not global. One purchase awards **" +
                      WeaponWheel.FocusAward + " / " + WeaponWheel.NeighbourAward + " / " +
                      WeaponWheel.NeighbourAward + " / 0 / 0** by ring distance (" +
                      WeaponWheel.PointsPerPurchase + " points). Tier is derived as" +
                      " total / " + WeaponWheel.PointsPerPurchase + ". Cadence still reads as the");
        sb.AppendLine("weapon: fire-rate points exist, but a purchase chooses a spoke. Purchases unlock");
        sb.AppendLine("once this realm has earned any Salvage at all. Full focus on one spoke is **" +
                      WeaponWheel.FocusAtMax + "** points; each neighbour reaches **" +
                      WeaponWheel.NeighbourAtMax + "**.");
        sb.AppendLine();
        sb.AppendLine("| attribute | per point | at full focus (" + WeaponWheel.FocusAtMax +
                      ") | at neighbour (" + WeaponWheel.NeighbourAtMax + ") |");
        sb.AppendLine("|---|---|---|---|");
        sb.AppendLine("| damage | ×" + N(WeaponWheel.DamagePerPoint) + " | ×" +
                      N(WeaponWheel.DamageMul(WeaponWheel.FocusAtMax)) + " | ×" +
                      N(WeaponWheel.DamageMul(WeaponWheel.NeighbourAtMax)) + " |");
        sb.AppendLine("| clip | ×" + N(WeaponWheel.ClipPerPoint) + " | ×" +
                      N(WeaponWheel.ClipMul(WeaponWheel.FocusAtMax)) + " | ×" +
                      N(WeaponWheel.ClipMul(WeaponWheel.NeighbourAtMax)) + " |");
        sb.AppendLine("| reload | ×" + N(WeaponWheel.ReloadPerPoint) + " | ×" +
                      N(WeaponWheel.ReloadMul(WeaponWheel.FocusAtMax)) + " | ×" +
                      N(WeaponWheel.ReloadMul(WeaponWheel.NeighbourAtMax)) + " |");
        sb.AppendLine("| fire rate | ×" + N(WeaponWheel.FireRatePerPoint) + " | ×" +
                      N(WeaponWheel.FireRateMul(WeaponWheel.FocusAtMax)) + " | ×" +
                      N(WeaponWheel.FireRateMul(WeaponWheel.NeighbourAtMax)) + " |");
        sb.AppendLine();

        sb.Append("Each Primary purchase on one spoke costs ");
        var costs = new List<string>();
        for (int t = 0; t < WeaponTier.MaxTier; t++)
            costs.Add(WeaponTier.Cost(t).ToString("N0", Inv));
        sb.AppendLine(string.Join(" / ", costs) + " salvage — **" +
                      WeaponTier.SpentToReach(WeaponTier.MaxTier).ToString("N0", Inv) +
                      "** to take one weapon all the way (" + WeaponTier.MaxTier + " purchases).");
        sb.Append("Each Secondary purchase on one spoke costs ");
        var secCosts = new List<string>();
        for (int t = 0; t < WeaponTier.MaxTier; t++)
            secCosts.Add(WeaponTier.Cost(WeaponSlot.Secondary, t).ToString("N0", Inv));
        sb.AppendLine(string.Join(" / ", secCosts) + " salvage — **" +
                      WeaponTier.SpentToReach(WeaponSlot.Secondary, WeaponTier.MaxTier).ToString("N0", Inv) +
                      "** cumulative.");
        sb.AppendLine();
    }

    static void Gatekeepers(StringBuilder sb)
    {
        sb.AppendLine("## What comes aboard");
        sb.AppendLine();
        sb.AppendLine("Four kinds, and each is defined by what it **refuses** to do. Every kind swings on");
        sb.AppendLine("the same " + N(WaveDirector.GatekeeperAttackInterval) +
                      " s clock — they differ in what they hit, never in how often.");
        sb.AppendLine();
        sb.AppendLine("| Kind | HP | Hits you for | Hits the hatch for | Speed | Refuses |");
        sb.AppendLine("|---|---|---|---|---|---|");

        AppendManualGk(sb, GatekeeperKind.Default, "Default",
            "nothing — it will march, and it will turn on you if you hurt it");
        AppendManualGk(sb, GatekeeperKind.Hunter, "Hunter",
            "to touch the hatch; it only ever wants you");
        AppendManualGk(sb, GatekeeperKind.Thief, "Thief",
            "to turn when shot; it marches the Hatch on every Wave and never hurts you");
        AppendManualGk(sb, GatekeeperKind.Cloaker, "Cloaker",
            "to advance while watched — any Boarder's gaze stops it dead, and it fades and closes the moment every eye is elsewhere");
        sb.AppendLine();
        sb.AppendLine("HP above is the base line. Later waves multiply it (see the wave table).");
        sb.AppendLine();
    }

    static void AppendManualGk(StringBuilder sb, GatekeeperKind kind, string name, string refuses)
    {
        var s = WaveDirector.StatsFor(kind);
        string speed = Math.Abs(s.patrolSpeed - s.chaseSpeed) < 0.01f
            ? N(s.patrolSpeed) + " m/s"
            : N(s.patrolSpeed) + " → " + N(s.chaseSpeed) + " m/s when provoked";
        sb.AppendLine("| **" + name + "** | " + N(s.maxHp) + " | " +
                      (s.playerDamage <= 0f ? "—" : N(s.playerDamage)) + " | " +
                      (s.hatchDamage <= 0f ? "—" : N(s.hatchDamage)) + " | " +
                      speed + " | " + refuses + " |");
    }

    /// <summary>"1 wave" / "2 waves" — the counts are read from DemoZones, so a table edit
    /// must not be able to print "2 wave".</summary>
    static string Plural(int n, string noun)
    {
        return n + " " + noun + (n == 1 ? "" : "s");
    }

    static void Waves(StringBuilder sb)
    {
        var wd = new WaveDirector();
        sb.AppendLine("## The waves");
        sb.AppendLine();
        sb.AppendLine("**" + WaveDirector.MaxWaves + " waves to a Boarding**, and none of them start until you press");
        sb.AppendLine("`T` at the Hatch. A wave BEGINS at the Hatch and ENDS at a terminal: press `T` at the zone's");
        sb.AppendLine("terminal while the wave runs and it shuts off, completing the zone and banking everything you");
        sb.AppendLine("are carrying. Each zone runs its whole span back to back (zone 1 = " +
                      Plural(DemoZones.WavesPerTrigger(1), "wave") + ", the deeper zone = " +
                      Plural(DemoZones.WavesPerTrigger(2), "wave") +
                      "). Prep after a clear lasts as long as you want — starting a Convert is legal there.");
        sb.AppendLine("Gatekeepers arrive in groups, not a stream.");
        sb.AppendLine();
        sb.AppendLine("At Ante 0:");
        sb.AppendLine();
        sb.AppendLine("| Wave | Bodies | Default | Hunter | Thief | Cloaker | HP × | Arrive in groups of | Every |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
        for (int i = 0; i < WaveDirector.MaxWaves; i++)
        {
            var s = wd.NextWave(i, 0);
            sb.AppendLine("| " + (i + 1) + " | " + s.enemyCount + " | " + s.defaultCount + " | " +
                          s.hunterCount + " | " + s.thiefCount + " | " + s.cloakerCount + " | " +
                          N(s.enemyHealthMultiplier) + " | " + s.groupSize + " | " +
                          N(s.spawnInterval) + " s |");
        }
        sb.AppendLine();
        sb.AppendLine("Hunters, Thieves and Cloakers are on a fixed schedule — the Ante never adds more of them.");
        sb.AppendLine();
    }

    static void Dial(StringBuilder sb)
    {
        sb.AppendLine("## The Ante — the wager");
        sb.AppendLine();
        sb.AppendLine("Ante up at a field terminal, Master Control, or the Hatch (`+/-`), then `E` to select and `T` to start. Locked once a");
        sb.AppendLine("wave is live. It pays more and sends more, and the extra bodies are **all Defaults** —");
        sb.AppendLine("it cannot conjure a sixth Thief. It changes nothing else: no extra health, no tighter");
        sb.AppendLine("spacing, no larger groups. Anteing up costs `" + IntensityJuice.CostPerStep +
                      "` banked Salvage per step — money you would otherwise take home.");
        sb.AppendLine();
        sb.AppendLine("| Ante | You earn | Wave 1 sends | Wave " + WaveDirector.MaxWaves + " sends |");
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

    static void Economy(StringBuilder sb)
    {
        sb.AppendLine("## Salvage");
        sb.AppendLine();
        sb.AppendLine("**Gatekeepers are worth Salvage, and it is yours the moment the body falls.**");
        sb.AppendLine("A kill banks straight to the realm at face value — no Convert stands between you and it,");
        sb.AppendLine("and no ending can take it back. Nothing you have banked is ever at risk.");
        sb.AppendLine();
        sb.AppendLine("If you want more than face value you have to put some of it back on the table: hold `E`");
        sb.AppendLine("at a full Hatch to **Withdraw**, then run a Convert Wave to bring it home multiplied.");
        sb.AppendLine("That pile — and only that pile — is what a death or a Hatch breach can take.");
        sb.AppendLine();
        sb.AppendLine("A kill pays `" + N(MetaUpgrades.SalvagePerKill()) + "`, then multiplied by the Ante.");
        sb.AppendLine("Nothing pays per second except the Convert drip (`" + N(HatchBank.DripPerSecond) +
                      "`/s), and the quiet between waves earns nothing at all.");
        sb.AppendLine();
        sb.AppendLine("The hatch is what you are defending: **" + N(ShipSystems.HatchMaxHp) +
                      " HP** at Hatch Armor 0, up to **" + N(MetaUpgrades.HatchHp(MetaUpgrades.MaxLevel)) +
                      "**. Hold `E` near it to repair.");
        sb.AppendLine("If it falls, the Boarding ends — whatever is banked stays banked. If you fall, you buy");
        sb.AppendLine("yourself back out of the realm's bank: `" + N(Respawns.BaseCost) +
                      " × min(respawns taken + 1, " + Respawns.MaxSteps + ")` — it climbs to " +
                      N(Respawns.CostFor(Respawns.MaxSteps - 1)) + " and stops. Anything you");
        sb.AppendLine("staked on a Withdraw stands up with you untouched. There is one pot, and repair, the Ante");
        sb.AppendLine("and your own returns all come out of it — which is what makes each of them a decision.");
        sb.AppendLine();
    }

    static void Stations(StringBuilder sb)
    {
        sb.AppendLine("## The ship — where salvage goes");
        sb.AppendLine();
        sb.AppendLine("Between runs you walk your own ship. Four consoles, each owning one subject.");
        sb.AppendLine();
        sb.AppendLine("| Console | Owns | Sells |");
        sb.AppendLine("|---|---|---|");
        sb.AppendLine("| **ARMORY** | the weapon in your hands | weapons, and wheel purchases for whichever you carry |");

        var byStation = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var track in UpgradeTracks.All)
        {
            string st = UpgradeTracks.Station(track);
            if (!byStation.ContainsKey(st)) byStation[st] = new List<string>();
            byStation[st].Add(UpgradeTracks.Label(track));
        }

        // Enumerated from the shop catalog so a new protocol lands here without an edit —
        // the hand-written form of this row went stale the day the second protocol shipped.
        var crewParts = new List<string>();
        foreach (var pid in BoarderProtocol.ShopProtocols())
        {
            // The doctrine word alone — the sheet's full sentence drowns a table cell.
            string doctrine = BoarderProtocol.DoctrineText(pid);
            int dash = doctrine.IndexOf(" — ", StringComparison.Ordinal);
            if (dash > 0) doctrine = doctrine.Substring(0, dash);
            var part = new StringBuilder();
            part.Append(BoarderProtocol.ProtocolName(pid));
            part.Append(" (").Append(doctrine);
            part.Append("; G ").Append(BoarderProtocol.DisplayName(BoarderProtocol.CrewAbilityOf(pid)));
            part.Append("; H ").Append(BoarderProtocol.DisplayName(BoarderProtocol.PersonalAbilityOf(pid)));
            for (int p = 0; p < BoarderProtocol.PassiveCount(pid); p++)
                part.Append("; ").Append(BoarderProtocol.DisplayName(BoarderProtocol.PassiveAt(pid, p)));
            part.Append(")");
            crewParts.Add(part.ToString());
        }
        string crew = string.Join(", ", crewParts);
        string fabTracks = byStation.ContainsKey("FABRICATION")
            ? string.Join(", ", byStation["FABRICATION"]) : "";
        string fabSells = fabTracks;
        if (fabSells.Length > 0) fabSells += ", ";
        fabSells += "Equipment slot, Repair Rig, Decoy";

        sb.AppendLine("| **CREW QUARTERS** | you | " + crew + " |");
        sb.AppendLine("| **FABRICATION** | the ship | " + fabSells + " |");
        sb.AppendLine("| **NAV MAP** | where you go | the destination, and embarking |");
        sb.AppendLine();
        sb.AppendLine("Hatch Armor runs 0–" + MetaUpgrades.MaxLevel + " at +5% a level, costing " +
                      MetaUpgrades.NextCost(0).ToString("N0", Inv) + " × 2^level — " +
                      Enumerable.Range(0, MetaUpgrades.MaxLevel)
                                .Select(l => MetaUpgrades.NextCost(l).ToString("N0", Inv))
                                .Aggregate((a, b) => a + " / " + b) + ".");
        sb.AppendLine();
        sb.AppendLine("| Track | Level 0 | Level " + MetaUpgrades.MaxLevel + " |");
        sb.AppendLine("|---|---|---|");
        foreach (var track in UpgradeTracks.All)
        {
            var a0 = UpgradeTracks.ValueAt(track, 0);
            var aM = UpgradeTracks.ValueAt(track, MetaUpgrades.MaxLevel);
            string label = UpgradeTracks.Label(track);
            string gloss = TrackGloss(track);
            sb.AppendLine("| " + label + (gloss.Length > 0 ? " — " + gloss : "") + " | " +
                          FormatTrackValue(track, a0) + " | " +
                          FormatTrackValue(track, aM) + " |");
        }
        var rig = EquipmentCatalog.Def(EquipmentId.RepairRig);
        sb.AppendLine("| Equipment slot (once, after this realm earns anything) | — | " +
                      EquipmentCatalog.SlotPrice.ToString("N0", Inv) + " salvage |");
        sb.AppendLine("| Repair Rig — flat repair rate | " +
                      N(rig.repairPerSecond) + " HP/s | " +
                      rig.cost.ToString("N0", Inv) + " salvage |");
        var decoy = PayloadCatalog.Def(PayloadId.Decoy);
        sb.AppendLine("| Decoy — Drone payload | " + decoy.charges + " charges | " +
                      decoy.cost.ToString("N0", Inv) + " salvage |");
        sb.AppendLine();
        var shopNames = BoarderProtocol.ShopProtocols()
            .Select(BoarderProtocol.ProtocolName).ToArray();
        sb.AppendLine("Crit chance is frozen at 0. There is no Luck shop. Protocols are CREW claims:");
        sb.AppendLine(string.Join(", ", shopNames) +
                      " share one panel, DEPOT at 0 salvage. AEGIS and LEDGER are not sold.");
        sb.AppendLine();
        sb.AppendLine("Weapon wheel purchases and the equipment slot unlock once this realm has earned any Salvage.");
        sb.AppendLine();
    }

    static string TrackGloss(UpgradeTrack track)
    {
        switch (track)
        {
            case UpgradeTrack.HatchArmor: return "hatch HP";
            default: return "";
        }
    }

    static void Footer(StringBuilder sb)
    {
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("*Generated from the build by `Tools/docgen/run.sh`. What the game is, rather than");
        sb.AppendLine("what it contains, is in [The Game](The-Game.md).*");
    }

    // ---------- helpers ----------

    static string N(float v)
    {
        if (float.IsNaN(v) || float.IsInfinity(v)) return v.ToString(Inv);
        if (Math.Abs(v - Math.Round(v)) < 0.005f) return ((int)Math.Round(v)).ToString(Inv);
        return v.ToString("0.##", Inv);
    }

    static string AttrAbbrev(WheelAttr a)
    {
        switch (a)
        {
            case WheelAttr.Reload: return "R";
            case WheelAttr.Clip: return "C";
            case WheelAttr.Damage: return "D";
            case WheelAttr.FireRate: return "F";
            case WheelAttr.Ability: return "A";
            default: return "?";
        }
    }

    static string ShortWeaponName(WeaponId id)
    {
        switch (id)
        {
            case WeaponId.Br12Shotgun: return "BR-12";
            case WeaponId.Mr35Rifle: return "MR-35";
            case WeaponId.PriC: return "PRI-C";
            case WeaponId.Mk1Sidearm: return "MK-1";
            case WeaponId.SecD: return "SEC-D";
            default: return WeaponCatalog.Def(id).displayName;
        }
    }

    static string Rel(string path)
    {
        string full = Path.GetFullPath(path).Replace('\\', '/');
        string marker = "/Docs/";
        int i = full.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (i >= 0) return "Docs" + full.Substring(i + "/Docs".Length);
        marker = "/Play/";
        // fall through
        string name = Path.GetFileName(path);
        if (name.Equals("Field-Manual.md", StringComparison.OrdinalIgnoreCase))
            return "Docs/Play/Field-Manual.md";
        if (name.Equals("4-Data.md", StringComparison.OrdinalIgnoreCase))
            return "Docs/Design/4-Data.md";
        return path.Replace('\\', '/');
    }

    static string Normalize(string s) =>
        s.Replace("\r\n", "\n").Replace('\r', '\n');

    static string NormalizeBlock(string s) =>
        Normalize(s).TrimEnd('\n');
}
