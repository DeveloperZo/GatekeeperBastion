#!/usr/bin/env python3
"""Gatekeeper's Protocol -- offline loop cost model.

WHAT THIS IS. A cheap, readable account of what one zone COSTS the player: time,
bodies, and hit points. It is not a combat simulator and must never become one. It
answers "roughly how long, roughly how bloody, and does the walk cost anything" so
a tuning question knows where to look. The repo's design rule is measure to find
where to look, decide by playing (Docs/Design) — this is the first half only.

WHAT IT REFUSES TO DO. It does not compute a dps and call that a kill rate. The
kill rate is an INPUT, calibrated from a real playthrough log, because the measured
gap between a weapon's ceiling and what a player actually achieves is the single
largest term in the whole model and no amount of arithmetic recovers it.

EVERY CONSTANT IS READ, NOT RESTATED. WaveDirector, FieldTrickle, PlayerRegen,
WeaponCatalog, FirstPersonController, EnemyGatekeeper, DemoZones and the Bastion
scene are parsed at run time. A value this file cannot find is exit 2 — never a
default. A hardcoded constant would make this tool a second source of truth and it
would drift the first time somebody tuned the real one.
"""

import argparse
import math
import os
import re
import sys

sys.dont_write_bytecode = True   # no __pycache__ beside a tool that runs once a session

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))


class SourceError(Exception):
    """A constant could not be read from the tree. Always fatal -- never a default."""


# --------------------------------------------------------------------------------
# Reading the tree
# --------------------------------------------------------------------------------

_CACHE = {}


def _text(rel):
    if rel not in _CACHE:
        path = os.path.join(ROOT, rel)
        if not os.path.isfile(path):
            raise SourceError("expected %s — is the repo laid out as this tool assumes?" % rel)
        with open(path, encoding="utf-8", errors="replace") as handle:
            _CACHE[rel] = handle.read()
    return _CACHE[rel]


def _line_of(rel, needle_index):
    return _text(rel).count("\n", 0, needle_index) + 1


class Value:
    """A number plus where it came from, so the report can show its provenance."""

    __slots__ = ("value", "rel", "line")

    def __init__(self, value, rel, line):
        self.value = value
        self.rel = rel
        self.line = line

    def __float__(self):
        return float(self.value)

    def __repr__(self):
        return repr(self.value)

    @property
    def cite(self):
        return self.rel if not self.line else "%s:%d" % (self.rel, self.line)


def const(rel, name, kind=float):
    """Read `public const <type> <name> = <literal>;` out of a C# file."""
    body = _text(rel)
    match = re.search(
        r"const\s+(?:float|int|double)\s+" + re.escape(name) + r"\s*=\s*(-?[\d.]+)f?\s*;", body)
    if not match:
        raise SourceError("could not read const %s from %s" % (name, rel))
    return Value(kind(match.group(1)), rel, _line_of(rel, match.start()))


def field(rel, name, kind=float):
    """Read a serialized/plain field initialiser: `... <type> <name> = <literal>;`."""
    body = _text(rel)
    match = re.search(
        r"(?:float|int)\s+" + re.escape(name) + r"\s*=\s*(-?[\d.]+)f?\s*;", body)
    if not match:
        raise SourceError("could not read field %s from %s" % (name, rel))
    return Value(kind(match.group(1)), rel, _line_of(rel, match.start()))


def int_array(rel, name):
    body = _text(rel)
    match = re.search(
        r"int\[\]\s+" + re.escape(name) + r"\s*=\s*\{([^}]*)\}", body)
    if not match:
        raise SourceError("could not read int[] %s from %s" % (name, rel))
    nums = [int(n) for n in re.findall(r"-?\d+", match.group(1))]
    return Value(nums, rel, _line_of(rel, match.start()))


def stats_for(kind):
    """Pull one gatekeeper kind's stat line out of WaveDirector.StatsFor's switch."""
    rel = "Assets/Scripts/Sim/Waves/WaveDirector.cs"
    body = _text(rel)
    if kind == "Default":
        # Default reads the shared constants rather than carrying its own literals.
        return {
            "maxHp": const(rel, "BaseEnemyHp"),
            "playerDamage": const(rel, "GatekeeperPlayerDamage"),
            "hatchDamage": const(rel, "GatekeeperHatchDamage"),
            "patrolSpeed": const(rel, "GatekeeperPatrolSpeed"),
            "chaseSpeed": const(rel, "GatekeeperChaseSpeed"),
        }
    match = re.search(
        r"case\s+GatekeeperKind\." + kind + r":(.*?)break;", body, re.S)
    if not match:
        raise SourceError("could not find StatsFor case for %s in %s" % (kind, rel))
    block, base = match.group(1), match.start(1)
    out = {}
    for name in ("maxHp", "playerDamage", "hatchDamage", "patrolSpeed", "chaseSpeed"):
        inner = re.search(r"stats\." + name + r"\s*=\s*(-?[\d.]+)f?\s*;", block)
        if inner:
            out[name] = Value(float(inner.group(1)), rel,
                              _line_of(rel, base + inner.start()))
        else:
            # The kind defers to a shared WaveDirector constant.
            ref = re.search(r"stats\." + name + r"\s*=\s*(\w+)\s*;", block)
            if not ref:
                raise SourceError("StatsFor(%s) has no %s" % (kind, name))
            out[name] = const(rel, ref.group(1))
    return out


def weapon(name):
    """Read one WeaponCatalog row by its display name."""
    rel = "Assets/Scripts/Sim/Meta/WeaponCatalog.cs"
    body = _text(rel)
    match = re.search(
        r'"' + re.escape(name) + r'"[^;]*?damage:\s*([\d.]+)f?,\s*shotsPerSecond:\s*([\d.]+)f?,'
        r'\s*clipSize:\s*([\d.]+)f?,\s*reloadSeconds:\s*([\d.]+)f?', body, re.S)
    if not match:
        raise SourceError("could not read weapon %s from %s" % (name, rel))
    line = _line_of(rel, match.start())
    return {
        "name": name,
        "damage": Value(float(match.group(1)), rel, line),
        "shotsPerSecond": Value(float(match.group(2)), rel, line),
        "clipSize": Value(float(match.group(3)), rel, line),
        "reloadSeconds": Value(float(match.group(4)), rel, line),
    }


def demo_zones():
    rel = "Assets/Scripts/Sim/Bastion/DemoZones.cs"
    body = _text(rel)
    terms = re.search(r"TerminalIdsByZone\s*=\s*\{(.*?)\n\s*\};", body, re.S)
    if not terms:
        raise SourceError("could not read TerminalIdsByZone from %s" % rel)
    ids = re.findall(r'"([^"]+)"', terms.group(1))
    return {
        "terminals": Value(ids, rel, _line_of(rel, terms.start())),
        "firstWave": int_array(rel, "FirstWaveByZone"),
        "endWave": int_array(rel, "EndWaveByZone"),
        "doorId": Value(re.findall(r'"([^"]+)"',
                                   re.search(r"DoorIdByZone\s*=\s*\{([^}]*)\}", body).group(1)),
                        rel, 0),
    }


# --------------------------------------------------------------------------------
# The scene: hatch, terminals, spawn closets. Geometry is authored, so read it.
# --------------------------------------------------------------------------------

SCENE_REL = "Assets/Scenes/Bastion_Coop.unity"


def _scene_docs():
    if "__scene__" in _CACHE:
        return _CACHE["__scene__"]
    body = _text(SCENE_REL)
    objs = {}
    for doc in body.split("\n--- "):
        head = re.match(r"!u!(\d+) &(\d+)", doc)
        if head:
            objs[head.group(2)] = (head.group(1), doc)
    transform_of = {}
    for fid, (cls, doc) in objs.items():
        if cls in ("4", "224"):
            owner = re.search(r"m_GameObject: \{fileID: (\d+)\}", doc)
            if owner:
                transform_of[owner.group(1)] = fid
    _CACHE["__scene__"] = (objs, transform_of)
    return _CACHE["__scene__"]


def _world_z(go_fid):
    objs, transform_of = _scene_docs()
    z, cur, hops = 0.0, transform_of.get(go_fid), 0
    while cur and cur in objs and hops < 32:
        doc = objs[cur][1]
        pos = re.search(
            r"m_LocalPosition: \{x: (-?[\d.eE+-]+), y: (-?[\d.eE+-]+), z: (-?[\d.eE+-]+)\}", doc)
        if pos:
            z += float(pos.group(3))
        parent = re.search(r"m_Father: \{fileID: (\d+)\}", doc)
        cur = parent.group(1) if parent and parent.group(1) != "0" else None
        hops += 1
    return z


def _go_name(go_fid):
    objs, _ = _scene_docs()
    match = re.search(r"\n  m_Name: (.*)", objs[go_fid][1])
    return match.group(1).strip() if match else "?"


def scene_geometry():
    objs, _ = _scene_docs()
    hatch_z, terminals, spawns = None, {}, {}
    for fid, (cls, doc) in objs.items():
        if cls != "114":
            continue
        cid = re.search(r"m_EditorClassIdentifier: (.*)", doc)
        if not cid:
            continue
        cid = cid.group(1).strip()
        owner = re.search(r"m_GameObject: \{fileID: (\d+)\}", doc)
        if not owner:
            continue
        go = owner.group(1)
        if cid.endswith("HatchStation"):
            hatch_z = _world_z(go)
        elif cid.endswith("BastionTerminal"):
            terminals[_go_name(go).replace("T_", "")] = _world_z(go)
        elif cid.endswith("Game.SpawnPoint"):
            wt = re.search(r"\n  weight: (-?\d+)", doc)
            spawns[_go_name(go).replace("S_", "")] = {
                "z": _world_z(go),
                "weight": max(1, int(wt.group(1))) if wt else 1,
            }
    if hatch_z is None:
        raise SourceError("no HatchStation in %s" % SCENE_REL)
    if not terminals or not spawns:
        raise SourceError("no terminals or spawn closets in %s" % SCENE_REL)
    return hatch_z, terminals, spawns


# Which closets are live during which zone. The rule lives in the emitter
# (BastionSketchEmitter.IsZoneOneAlwaysLive + WireSealedSpawns): zone 1's named
# closets stay live from the start, every other non-control closet is sealed behind
# the lowest-id door and comes live when that door opens.
def zone_one_live_ids():
    rel = "Assets/Scripts/Editor/Bastion/BastionSketchEmitter.cs"
    body = _text(rel)
    match = re.search(r"IsZoneOneAlwaysLive\(string spawnId\)\s*\{\s*return([^;]*);", body, re.S)
    if not match:
        raise SourceError("could not read IsZoneOneAlwaysLive from %s" % rel)
    return set(re.findall(r'"([^"]+)"', match.group(1))), _line_of(rel, match.start())


# --------------------------------------------------------------------------------
# Wave composition — a faithful port of WaveDirector.NextWave for dial 0, solo.
# --------------------------------------------------------------------------------

class Director:
    def __init__(self):
        rel = "Assets/Scripts/Sim/Waves/WaveDirector.cs"
        self.rel = rel
        self.base_count = const(rel, "BaseEnemyCount", int)
        self.per_wave = const(rel, "CountPerWave", int)
        self.max_count = const(rel, "MaxEnemyCount", int)
        self.min_interval = const(rel, "MinSpawnInterval")
        self.base_group = const(rel, "BaseGroupSize", int)
        self.max_group = const(rel, "MaxGroupSize", int)
        self.hp_per_wave = const(rel, "EnemyHealthPerWave")
        self.max_waves = const(rel, "MaxWaves", int)
        self.attack_interval = const(rel, "GatekeeperAttackInterval")
        self.hunters = int_array(rel, "HunterPerWave")
        self.thieves = int_array(rel, "ThiefPerWave")
        self.cloakers = int_array(rel, "CloakerPerWave")
        # spawnInterval = max(MinSpawnInterval, A (+|-) B * waveIndex), where waveIndex may
        # be clamped to MaxWaves-1. Read A, B and the SIGN rather than restating them, since
        # they are the wave's whole delivery cadence.
        #
        # The sign is captured, not assumed. It was '-' until 2026-08-27 (the gap NARROWED as
        # the wave grew, which is what stacked 103 bodies on the player in the 03:18 log) and
        # is '+' now. A parser that hard-codes one direction silently reports the wrong
        # delivery table the next time it flips, which is worse than the exit 2 this raises.
        # The Math.Min clamp is optional in the pattern so this still reads the unclamped
        # shape; when present, wave() holds the index the same way the C# does.
        m = re.search(r"spec\.spawnInterval\s*=\s*Math\.Max\(\s*MinSpawnInterval\s*,\s*"
                      r"([\d.]+)f\s*([+-])\s*([\d.]+)f\s*\*\s*"
                      r"(?:Math\.Min\(\s*waveIndex\s*,\s*MaxWaves\s*-\s*1\s*\)|waveIndex)\s*\)",
                      _text(rel))
        if not m:
            raise SourceError("could not read the spawnInterval formula from %s" % rel)
        signed = float(m.group(3)) * (-1.0 if m.group(2) == "-" else 1.0)
        self.interval_base = Value(float(m.group(1)), rel, _line_of(rel, m.start()))
        self.interval_slope = Value(signed, rel, _line_of(rel, m.start()))
        self.interval_clamped = "Math.Min" in m.group(0)

    @staticmethod
    def _row(table, index):
        return table[-1] if index >= len(table) else table[index]

    def wave(self, index):
        """dial 0 (RewardMultiplierFor(0) == 1), playerCount 1 -- the demo's table."""
        total = self.base_count.value + self.per_wave.value * index
        total = min(self.max_count.value, total)
        hunter = min(self._row(self.hunters.value, index), total)
        thief = min(self._row(self.thieves.value, index), total - hunter)
        cloaker = min(self._row(self.cloakers.value, index), total - hunter - thief)
        return {
            "index": index,
            "wave": index + 1,
            "count": total,
            "hunter": hunter,
            "thief": thief,
            "cloaker": cloaker,
            "default": total - hunter - thief - cloaker,
            "hpMul": 1.0 + self.hp_per_wave.value * index,
            "groupSize": max(1, min(self.base_group.value + index, self.max_group.value, total)),
            "spawnInterval": max(self.min_interval.value,
                                 self.interval_base.value
                                 + self.interval_slope.value * self._interval_index(index)),
        }

    def _interval_index(self, index):
        """The waveIndex the C# feeds the gap formula: clamped to the last authored wave when
        the source clamps it, so a probe index holds the last gap instead of extrapolating."""
        if self.interval_clamped:
            return min(index, self.max_waves.value - 1)
        return index

    @staticmethod
    def delivery(spec):
        groups = math.ceil(spec["count"] / spec["groupSize"])
        seconds = (groups - 1) * spec["spawnInterval"]
        return groups, seconds


# --------------------------------------------------------------------------------
# Calibration from a real playthrough log
# --------------------------------------------------------------------------------

KILL_RE = re.compile(
    r"\[(\d\d):(\d\d):(\d\d)\.(\d\d\d)\] \[Log\] \[Combat\] killed a (\w+).*?-- (\d+) left"
    r"(?:.*?hatchDist=([\d.]+))?")
WAVE_RE = re.compile(r"\[(\d\d):(\d\d):(\d\d)\.(\d\d\d)\] \[Log\] \[Run\] wave (\d+)/\d+ — (\d+) gatekeepers")
CLEAR_RE = re.compile(r"\[(\d\d):(\d\d):(\d\d)\.(\d\d\d)\] \[Log\] \[Run\] wave (\d+)/\d+ cleared")
HURT_RE = re.compile(r"\[(\d\d):(\d\d):(\d\d)\.(\d\d\d)\] \[Log\] \[Player\] took ([\d.]+) damage — HP ([\d.]+)/([\d.]+)")
SUMMARY_RE = re.compile(r"\[Run\] summary schema=\d+ result=(\w+) .*?duration=(\d+) .*?banked=(\d+) respawns=(\d+)")


def _clock(match, base=1):
    h, m, s, ms = (int(match.group(base + i)) for i in range(4))
    return h * 3600 + m * 60 + s + ms / 1000.0


def read_log(path):
    with open(path, encoding="utf-8", errors="replace") as handle:
        lines = handle.readlines()
    waves, kills, hurts, summary = [], [], [], None
    for raw in lines:
        m = WAVE_RE.search(raw)
        if m:
            waves.append({"t": _clock(m), "wave": int(m.group(5)),
                          "count": int(m.group(6)), "cleared": None, "kills": []})
            continue
        m = CLEAR_RE.search(raw)
        if m:
            for w in reversed(waves):
                if w["wave"] == int(m.group(5)) and w["cleared"] is None:
                    w["cleared"] = _clock(m)
                    break
            continue
        m = KILL_RE.search(raw)
        if m:
            kills.append({"t": _clock(m), "kind": m.group(5), "left": int(m.group(6)),
                          "hatchDist": float(m.group(7)) if m.group(7) else None})
            continue
        m = HURT_RE.search(raw)
        if m:
            hurts.append({"t": _clock(m), "dmg": float(m.group(5)), "hp": float(m.group(6))})
            continue
        m = SUMMARY_RE.search(raw)
        if m:
            summary = {"result": m.group(1), "duration": int(m.group(2)),
                       "banked": int(m.group(3)), "respawns": int(m.group(4))}
    # A draw's window closes at the FIRST of: its own "cleared" line, or the next draw
    # starting. A composite has no gap between its waves and only logs "cleared" for the
    # last one, so leaning on the clear line alone hands every wave-1 kill to wave 2.
    for i, w in enumerate(waves):
        nxt = waves[i + 1]["t"] if i + 1 < len(waves) else float("inf")
        own = w["cleared"] if w["cleared"] is not None else float("inf")
        w["end"] = min(nxt, own)
        w["draw"] = i + 1
    for k in kills:
        for w in waves:
            if w["t"] <= k["t"] <= w["end"] + 0.01:
                w["kills"].append(k)
                break
    for h in hurts:
        for w in waves:
            if w["t"] <= h["t"] <= w["end"] + 0.01:
                w.setdefault("hurts", []).append(h)
                break
    return {"waves": waves, "kills": kills, "hurts": hurts, "summary": summary}


def bursts(hurts, gap=2.0):
    """Group hits into bursts: consecutive hits no more than `gap` seconds apart."""
    out = []
    for h in hurts:
        if out and h["t"] - out[-1]["end"] <= gap:
            out[-1]["end"] = h["t"]
            out[-1]["hits"] += 1
            out[-1]["dmg"] += h["dmg"]
            out[-1]["low"] = h["hp"]
        else:
            out.append({"start": h["t"], "end": h["t"], "hits": 1, "dmg": h["dmg"],
                        "high": h["hp"] + h["dmg"], "low": h["hp"]})
    return out


# --------------------------------------------------------------------------------
# Report
# --------------------------------------------------------------------------------

def rule(title=""):
    if title:
        print("\n" + title)
        print("-" * max(len(title), 8))
    else:
        print("-" * 78)


def emit_sources(bag):
    rule("SOURCES -- every number below was read from the tree, not restated here")
    width = max(len(k) for k in bag)
    for name in sorted(bag):
        val, cite = bag[name]
        print("  %-*s = %-10s   %s" % (width, name, val, cite))


def shots_per_body(spec, gun, default, hunter, thief, cloaker):
    """Mean MK-1-equivalent shots one body of this wave costs, at its health multiplier.

    This is the honest measure of how much harder a deep wave is: wave 1's Default dies
    to one shot, wave 5's takes three, and the Thief takes eleven. It is NOT converted
    into a kill rate -- it only sizes how optimistic a flat kill rate is.
    """
    def shots(hp):
        return math.ceil(hp * spec["hpMul"] / gun["damage"].value)
    return (spec["default"] * shots(default["maxHp"].value)
            + spec["hunter"] * shots(hunter["maxHp"].value)
            + spec["thief"] * shots(thief["maxHp"].value)
            + spec["cloaker"] * shots(cloaker["maxHp"].value)) / spec["count"]


def average_boost(kill_rate, regen):
    """Time-average kill boost, HP/s, for a steady kill cadence.

    A kill adds RegenPerKill and the standing boost decays with a half-life, so
    the mean of the sawtooth is RegenPerKill * rate / lambda, lambda = ln2/halfLife.
    That is PlayerRegen's own arithmetic (see its <remarks>); this reproduces it
    from the same constants rather than copying the answer.
    """
    lam = math.log(2.0) / regen["halfLife"].value
    return min(regen["maxBoost"].value, regen["perKill"].value * kill_rate / lam)


def sprint_profile(distance, walk, sprint, stamina_max, drain, regen_rate, delay):
    """Seconds to cover `distance` walking, and sprinting as hard as stamina allows.

    Opening burst: a full tank buys stamina_max/drain seconds of sprint. After that
    the pool is the limit — a sustainable cycle is `delay` seconds of walking, then
    walking while the pool refills at regen_rate, then spending it at drain. Solving
    18*r = 25*s for the refill/spend balance gives the steady-state speed below.
    """
    burst_s = stamina_max / drain
    burst_m = burst_s * sprint
    if distance <= burst_m:
        return distance / sprint, distance / walk
    s = burst_s                      # spend the whole tank each cycle
    r = drain * s / regen_rate       # seconds of walking to refill it
    cycle_t = delay + r + s
    cycle_m = walk * (delay + r) + sprint * s
    steady = cycle_m / cycle_t
    rest = distance - burst_m
    return burst_s + rest / steady, distance / walk


def run_report(args):
    src = {}

    wd_rel = "Assets/Scripts/Sim/Waves/WaveDirector.cs"
    ft_rel = "Assets/Scripts/Sim/Waves/FieldTrickle.cs"
    pr_rel = "Assets/Scripts/Sim/Player/PlayerRegen.cs"
    fpc_rel = "Assets/Scripts/Game/Player/FirstPersonController.cs"
    eg_rel = "Assets/Scripts/Game/Enemies/EnemyGatekeeper.cs"

    director = Director()
    default = stats_for("Default")
    hunter = stats_for("Hunter")
    thief = stats_for("Thief")
    cloaker = stats_for("Cloaker")

    trickle = {
        "gap": const(ft_rel, "SecondsBetweenBodies"),
        "maxLive": const(ft_rel, "MaxLive", int),
    }
    # WHAT-IF OVERRIDES. The DEFAULT is always the value read from the tree; these only
    # let a tuning candidate be priced without editing Assets/Scripts first. Every
    # override is announced in the SOURCES block so a number can never look authored
    # when it was typed on the command line.
    overrides = []
    if args.trickle_gap is not None:
        overrides.append("FieldTrickle.SecondsBetweenBodies %.2f -> %.2f"
                         % (trickle["gap"].value, args.trickle_gap))
        trickle["gap"] = Value(args.trickle_gap, "--trickle-gap OVERRIDE", 0)
    regen = {
        "base": const(pr_rel, "BaseRegenPerSecond"),
        "perKill": const(pr_rel, "RegenPerKill"),
        "maxBoost": const(pr_rel, "MaxKillBoostPerSecond"),
        "halfLife": const(pr_rel, "KillBoostHalfLifeSeconds"),
        "damageDelay": const(pr_rel, "DamageDelaySeconds"),
    }
    move = {
        "walk": field(fpc_rel, "moveSpeed"),
        "sprint": field(fpc_rel, "sprintSpeed"),
        "staminaMax": field(fpc_rel, "staminaMax"),
        "drain": const(fpc_rel, "SprintDrainPerSecond"),
        "staminaRegen": const(fpc_rel, "StaminaRegenPerSecond"),
        "staminaDelay": const(fpc_rel, "StaminaRegenDelaySeconds"),
    }
    enemy = {
        "notice": field(eg_rel, "noticeRange"),
        "alert": field(eg_rel, "alertRadius"),
        "attackRange": field(eg_rel, "attackRange"),
        "hatchCommit": field(eg_rel, "hatchCommitRadius"),
    }
    gun = weapon(args.weapon)
    zones = demo_zones()
    hatch_z, terminals, spawns = scene_geometry()
    live_ids, live_line = zone_one_live_ids()

    for key, val in (
            ("BaseEnemyCount", director.base_count), ("CountPerWave", director.per_wave),
            ("EnemyHealthPerWave", director.hp_per_wave), ("BaseGroupSize", director.base_group),
            ("MaxGroupSize", director.max_group), ("MinSpawnInterval", director.min_interval),
            ("spawnInterval base", director.interval_base),
            ("spawnInterval slope", director.interval_slope),
            ("HunterPerWave", director.hunters), ("ThiefPerWave", director.thieves),
            ("CloakerPerWave", director.cloakers),
            ("GatekeeperAttackInterval", director.attack_interval),
            ("Default maxHp", default["maxHp"]), ("Default playerDamage", default["playerDamage"]),
            ("Default patrolSpeed", default["patrolSpeed"]),
            ("Default chaseSpeed", default["chaseSpeed"]),
            ("Hunter maxHp", hunter["maxHp"]), ("Hunter chaseSpeed", hunter["chaseSpeed"]),
            ("Thief maxHp", thief["maxHp"]), ("Cloaker maxHp", cloaker["maxHp"]),
            ("FieldTrickle.SecondsBetweenBodies", trickle["gap"]),
            ("FieldTrickle.MaxLive", trickle["maxLive"]),
            ("PlayerRegen.Base", regen["base"]), ("PlayerRegen.PerKill", regen["perKill"]),
            ("PlayerRegen.MaxBoost", regen["maxBoost"]),
            ("PlayerRegen.HalfLife", regen["halfLife"]),
            ("PlayerRegen.DamageDelay", regen["damageDelay"]),
            ("player walk", move["walk"]), ("player sprint", move["sprint"]),
            ("stamina max", move["staminaMax"]), ("stamina drain", move["drain"]),
            ("stamina regen", move["staminaRegen"]),
            ("enemy noticeRange", enemy["notice"]), ("enemy alertRadius", enemy["alert"]),
            ("enemy attackRange", enemy["attackRange"]),
            ("%s damage" % gun["name"], gun["damage"]),
            ("%s shots/s" % gun["name"], gun["shotsPerSecond"]),
            ("%s clip" % gun["name"], gun["clipSize"]),
            ("%s reload" % gun["name"], gun["reloadSeconds"]),
            ("DemoZones.FirstWaveByZone", zones["firstWave"]),
            ("DemoZones.EndWaveByZone", zones["endWave"]),
    ):
        src[key] = (val.value, val.cite)
    src["hatch z"] = (hatch_z, SCENE_REL)
    for tid, tz in sorted(terminals.items()):
        src["terminal %s z" % tid] = (tz, SCENE_REL)
    for sid in sorted(spawns):
        src["closet %s (z, weight)" % sid] = (
            (spawns[sid]["z"], spawns[sid]["weight"]), SCENE_REL)
    src["zone-1 live closets"] = (sorted(live_ids),
                                  "Assets/Scripts/Editor/Bastion/BastionSketchEmitter.cs:%d" % live_line)

    print("=" * 78)
    print("GATEKEEPER'S PROTOCOL -- LOOP COST MODEL")
    print("=" * 78)
    print("This is an APPROXIMATION of the experience, calibrated from playthrough logs.")
    print("It is not a combat simulator and its numbers are not predictions of a fight.")

    emit_sources(src)
    if overrides:
        print()
        print("  WHAT-IF OVERRIDES IN EFFECT (typed, not authored):")
        for o in overrides:
            print("    " + o)
    if args.no_trickle_regen:
        print("    trickle kills do NOT feed the regen boost (candidate change)")

    # ---- calibration ----------------------------------------------------------
    rule("CALIBRATION -- the kill rate is measured, never computed")
    calib = {}
    default_damage = default["playerDamage"].value
    if args.calibrate:
        log = read_log(args.calibrate)
        print("  log: %s" % os.path.relpath(args.calibrate, ROOT).replace("\\", "/"))
        if log["summary"]:
            s = log["summary"]
            print("  run summary: result=%s duration=%ss banked=%s respawns=%s"
                  % (s["result"], s["duration"], s["banked"], s["respawns"]))
        print()
        print("  A DRAW's window runs from its start line to the next draw's start (a")
        print("  composite logs no 'cleared' between its waves, so leaning on the clear")
        print("  line alone hands every wave-1 kill to wave 2).")
        print("  ENGAGED rate is kills / (first kill -> last kill) in the window: the rate")
        print("  while actually fighting. WALL rate divides by the whole window, so it also")
        print("  carries the LEAD-IN -- the seconds the first bodies spend walking down the")
        print("  lane before anything is in range. The model uses engaged rate + a lead-in,")
        print("  because a composite's second wave inherits a lane that is already stocked.")
        print()
        print("  draw wave  auth  killed  window  engaged  eng/s  wall/s  spawn/s  peak  med   hits")
        counted_hits = counted_kills = 0
        for w in log["waves"]:
            if not w["kills"]:
                continue
            span = w["end"] - w["t"]
            eng = w["kills"][-1]["t"] - w["kills"][0]["t"]
            eng_rate = len(w["kills"]) / eng if eng > 0 else float("nan")
            wall_rate = len(w["kills"]) / span if span > 0 else 0.0
            spec = director.wave(w["wave"] - 1)
            _, deliver = Director.delivery(spec)
            spawn_rate = spec["count"] / deliver if deliver > 0 else float("inf")
            peak = max(k["left"] for k in w["kills"]) + 1
            dists = sorted(k["hatchDist"] for k in w["kills"] if k["hatchDist"] is not None)
            med = dists[len(dists) // 2] if dists else float("nan")
            hits = len(w.get("hurts", []))
            print("   %2d   %2d  %4d   %5d  %6.1f   %6.1f  %5.2f  %6.2f  %6.2f  %4d  %5.1f  %4d"
                  % (w["draw"], w["wave"], w["count"], len(w["kills"]), span, eng,
                     eng_rate, wall_rate, spawn_rate, peak, med, hits))
            # First draw of a wave wins the calibration: a retrigger is a different
            # fight (different starting HP, different position, no salvage).
            calib.setdefault(w["wave"], eng_rate)
            if args.calibrate_draws and w["draw"] > args.calibrate_draws:
                continue
            counted_hits += hits
            counted_kills += len(w["kills"])
        print("  NOTE a composite's draws OVERLAP: wave 2's start line fires an InterWaveGap")
        print("  after wave 1 finishes DELIVERING, not after it is cleared, so wave 1's")
        print("  stragglers die inside wave 2's window. 'killed' is per window, not per wave.")
        drawn = [w for w in log["waves"] if w["kills"]]
        if len(drawn) >= 2 and drawn[1]["t"] - drawn[0]["end"] < 1.0:
            span = drawn[1]["end"] - drawn[0]["t"]
            bodies = len(drawn[0]["kills"]) + len(drawn[1]["kills"])
            print("  composite (draws 1+2): %d bodies in %.1f s wall = %.2f kills/s"
                  % (bodies, span, bodies / span))
        lead = drawn[0]["kills"][0]["t"] - drawn[0]["t"] if drawn else float("nan")
        calib["leadIn"] = lead
        print("  LEAD-IN measured: %.1f s from the wave's start line to the first kill."
              % lead)
        hb = bursts(log["hurts"])
        print()
        print("  DAMAGE ARRIVES IN BURSTS, NOT EVENLY -- %d burst(s) in this run" % len(hb))
        print("  (a burst = hits no more than 2 s apart). Between them, nothing.")
        for b in hb:
            print("    %5.1f s window, %2d hit(s), %3.0f HP: %3.0f -> %3.0f    at t+%.0f s"
                  % (b["end"] - b["start"], b["hits"], b["dmg"], b["high"], b["low"],
                     b["start"] - log["waves"][0]["t"] if log["waves"] else 0))
        if counted_kills:
            calib["hitsPerBody"] = counted_hits / counted_kills
            print()
            print("  hits taken per body killed, over the first %d draw(s) = %d / %d = %.3f"
                  % (args.calibrate_draws or len(log["waves"]), counted_hits,
                     counted_kills, calib["hitsPerBody"]))
            print("  one Default swing is %.0f HP, so that is %.2f HP per body killed."
                  % (default_damage, calib["hitsPerBody"] * default_damage))
        # Regen shows up in the log as HP that came back between bursts: the HP the
        # next hit landed on, minus what the previous burst left. This is the only
        # in-play evidence PlayerRegen's arithmetic has, so print it beside the model.
        if len(hb) > 1:
            print()
            print("  REGEN, AS THE LOG SAW IT (HP recovered between bursts)")
            print("    gap s   HP %5s   HP/s observed   kills in the gap   HP/s modelled"
                  % "back")
            for prev, nxt in zip(hb, hb[1:]):
                gap = nxt["start"] - prev["end"]
                back = nxt["high"] - prev["low"]
                if gap <= 0 or back <= 0:
                    continue
                n = sum(1 for k in log["kills"] if prev["end"] <= k["t"] <= nxt["start"])
                obs = back / gap
                modelled = regen["base"].value + average_boost(n / gap, regen)
                note = "  (capped at full for part of the gap)" if nxt["high"] >= 99.5 else ""
                print("    %5.1f   %8.0f   %13.2f   %16d   %13.2f%s"
                      % (gap, back, obs, n, modelled, note))
    else:
        print("  (no --calibrate log given; using the --kill-rate default)")

    default_rate = args.kill_rate
    print()
    print("  kill rate used for unmeasured waves: %.2f /s  (--kill-rate)" % default_rate)
    ceiling = (gun["clipSize"].value
               / (gun["clipSize"].value / gun["shotsPerSecond"].value + gun["reloadSeconds"].value))
    print("  %s sustained SHOT rate = clip / (clip/rate + reload)" % gun["name"])
    print("    = %.0f / (%.0f/%.1f + %.1f) = %.2f shots/s"
          % (gun["clipSize"].value, gun["clipSize"].value, gun["shotsPerSecond"].value,
             gun["reloadSeconds"].value, ceiling))
    print("  a wave-1 Default is %.0f HP / %.0f damage = 1 shot, so the CEILING kill rate is"
          % (default["maxHp"].value, gun["damage"].value))
    print("    %.2f /s. The measured rate is the number this model uses; the ceiling is here"
          % ceiling)
    print("    only to show the size of the gap (%.0f%% of ceiling)."
          % (100.0 * default_rate / ceiling))

    # ---- the zone -------------------------------------------------------------
    zone = args.zone
    first = zones["firstWave"].value[zone - 1]
    last = zones["endWave"].value[zone - 1]
    term_id = zones["terminals"].value[zone - 1]
    if term_id not in terminals:
        raise SourceError("zone %d's terminal %s is not in %s" % (zone, term_id, SCENE_REL))
    walk_m = abs(terminals[term_id] - hatch_z)

    rule("ZONE %d -- %s, waves %d..%d" % (zone, term_id, first, last))

    def shots_for(hp):
        return math.ceil(hp / gun["damage"].value)

    fight_total = 0.0
    bodies_total = 0
    print("  BODIES AUTHORED AND DELIVERED")
    print("  wave  bodies   D/H/T/C      hpMul  group  gap    groups  delivery  spawn/s")
    specs = []
    for wave in range(first, last + 1):
        spec = director.wave(wave - 1)
        specs.append(spec)
        groups, deliver = Director.delivery(spec)
        print("   %2d    %4d   %2d/%2d/%2d/%2d   %.2f    %2d   %.1fs    %3d    %6.1fs   %5.2f"
              % (wave, spec["count"], spec["default"], spec["hunter"], spec["thief"],
                 spec["cloaker"], spec["hpMul"], spec["groupSize"], spec["spawnInterval"],
                 groups, deliver, spec["count"] / deliver if deliver else float("inf")))
        bodies_total += spec["count"]

    # Lead-in: the first bodies of a zone have to walk the lane before anything is in
    # range. Only the FIRST wave of a composite pays it — the next inherits a stocked
    # lane. Derived from geometry, or taken from the log when one is given.
    far_id = max((s for sid, s in spawns.items()
                  if sid in live_ids or zone != 1), key=lambda s: s["weight"] * 1000 - abs(s["z"]))
    far_dist = abs(far_id["z"] - hatch_z) - args.start_from
    geo_lead = max(0.0, (far_dist - args.weapon_range) / default["patrolSpeed"].value)
    lead = calib.get("leadIn", geo_lead)

    print()
    print("  TIME TO CLEAR THE ROSTER, at the calibrated kill rate")
    print("  lead-in: the far closet is %.1f m from the player; the gun reaches %.0f m; a"
          % (far_dist, args.weapon_range))
    print("    Default walks in at its %.1f m/s patrol -> (%.1f - %.0f) / %.1f = %.1f s"
          % (default["patrolSpeed"].value, far_dist, args.weapon_range,
             default["patrolSpeed"].value, geo_lead))
    print("    using %.1f s (%s). Only the zone's FIRST wave pays it."
          % (lead, "measured from the log" if "leadIn" in calib else "geometric"))
    print()
    print("  wave  rate/s   source        shots/body   lead   fight   total   spawn/s  verdict")
    for i, spec in enumerate(specs):
        wave = spec["wave"]
        rate = calib.get(wave, default_rate)
        source = "measured" if wave in calib else "ASSUMED"
        per_body = shots_per_body(spec, gun, default, hunter, thief, cloaker)
        this_lead = lead if i == 0 else 0.0
        fight = spec["count"] / rate
        _, deliver = Director.delivery(spec)
        spawn_rate = spec["count"] / deliver if deliver else float("inf")
        verdict = "holds" if rate >= spawn_rate else "backlog +%.2f/s" % (spawn_rate - rate)
        print("   %2d   %5.2f   %-11s   %8.2f   %4.1f   %5.1f   %5.1f   %6.2f   %s"
              % (wave, rate, source, per_body, this_lead, fight, this_lead + fight,
                 spawn_rate, verdict))
        fight_total += this_lead + fight
    print("  roster fight time, zone %d: %.0f s over %d bodies" % (zone, fight_total, bodies_total))

    # An unmeasured wave gets a BAND, not a number. Holding the kill rate flat while the
    # health multiplier climbs is optimistic; scaling it inversely with shots-per-body is
    # pessimistic (wave 2 measured 0.77 where inverse scaling from wave 1 predicts 0.48).
    # The truth is between, and only a logged playthrough narrows it.
    unmeasured = [s for s in specs if s["wave"] not in calib]
    measured = sorted(w for w in calib if isinstance(w, int))
    if unmeasured and measured:
        anchor_wave = measured[0]
        anchor_spec = director.wave(anchor_wave - 1)
        anchor_rate = calib[anchor_wave]
        anchor_spb = shots_per_body(anchor_spec, gun, default, hunter, thief, cloaker)
        print()
        print("  BAND FOR THE UNMEASURED WAVES -- nobody has played wave %s"
              % ", ".join(str(s["wave"]) for s in unmeasured))
        print("  OPTIMISTIC: the kill rate stays flat at %.2f /s while the health multiplier"
              % default_rate)
        print("    climbs from %.2f to %.2f." % (specs[0]["hpMul"], specs[-1]["hpMul"]))
        print("  PESSIMISTIC: it falls in proportion to shots-per-body, anchored on wave %d's"
              % anchor_wave)
        print("    measured %.2f /s at %.2f shots/body -> rate = %.2f x %.2f / spb."
              % (anchor_rate, anchor_spb, anchor_rate, anchor_spb))
        for check in measured[1:]:
            spec_c = director.wave(check - 1)
            spb_c = shots_per_body(spec_c, gun, default, hunter, thief, cloaker)
            print("    CHECK on wave %d: inverse scaling predicts %.2f /s, the log measured"
                  % (check, anchor_rate * anchor_spb / spb_c))
            print("      %.2f -- so the pessimistic edge is a genuine floor, not the answer."
                  % calib[check])
        print()
        print("  wave  shots/body   optimistic /s  fight s   pessimistic /s  fight s")
        opt_total = pess_total = lead
        for spec in specs:
            spb = shots_per_body(spec, gun, default, hunter, thief, cloaker)
            opt = calib.get(spec["wave"], default_rate)
            pess = calib.get(spec["wave"], anchor_rate * anchor_spb / spb)
            opt_total += spec["count"] / opt
            pess_total += spec["count"] / pess
            print("   %2d      %8.2f        %8.2f   %6.1f         %8.2f   %6.1f"
                  % (spec["wave"], spb, opt, spec["count"] / opt, pess, spec["count"] / pess))
        print("  zone %d roster: %.0f s optimistic .. %.0f s pessimistic  (%.1f .. %.1f min)"
              % (zone, opt_total, pess_total, opt_total / 60.0, pess_total / 60.0))
        print("  The model CANNOT narrow this. One logged zone-%d playthrough would." % zone)

    # ---- the walk -------------------------------------------------------------
    rule("THE WALK TO %s -- the beat the trickle is supposed to hold" % term_id)
    start_dist = args.start_from
    remaining = walk_m - start_dist
    print("  Hatch z=%.1f, %s z=%.1f  ->  %.1f m of lane"
          % (hatch_z, term_id, terminals[term_id], walk_m))
    print("  player starts the walk %.1f m up the lane (--start-from; the 07:04 log's median"
          % start_dist)
    print("  wave-2 kill distance was 21.3 m, and its last kill landed at 16.8 m)")
    print("  distance still to cover: %.1f m" % remaining)
    t_sprint, t_walk = sprint_profile(
        remaining, move["walk"].value, move["sprint"].value, move["staminaMax"].value,
        move["drain"].value, move["staminaRegen"].value, move["staminaDelay"].value)
    print("    at a %.1f m/s walk : %.1f s" % (move["walk"].value, t_walk))
    print("    sprinting as hard as stamina allows (%.0f stamina / %.0f per s = %.1f s of"
          % (move["staminaMax"].value, move["drain"].value,
             move["staminaMax"].value / move["drain"].value))
    print("    sprint = %.0f m, then a %.0f-per-s refill cycle): %.1f s"
          % (move["staminaMax"].value / move["drain"].value * move["sprint"].value,
             move["staminaRegen"].value, t_sprint))

    # Which closets are open during this zone. Zone 1's are the always-live set; every
    # later zone has had the wing door opened, so every non-control closet is live —
    # including the two behind the player (BastionSketchEmitter.WireSealedSpawns).
    if zone == 1:
        live = {sid: s for sid, s in spawns.items() if sid in live_ids}
    else:
        live = {sid: s for sid, s in spawns.items() if "Control" not in sid}
    weight_total = sum(s["weight"] for s in live.values())
    print()
    print("  CLOSETS THE TRICKLE DRAWS FROM (WaveSpawner's weighted rotation)")
    print("  A closet is only AHEAD for the part of the walk the player has not passed it:")
    print("  ahead-fraction = (closet distance - walk start) / walk length, clamped 0..1.")
    print()
    print("  closet   z      m from Hatch  weight  share   ahead for")
    ahead_share = 0.0
    span = max(1e-6, walk_m - start_dist)
    for sid in sorted(live, key=lambda k: live[k]["z"]):
        s = live[sid]
        d_hatch = abs(s["z"] - hatch_z)
        share = s["weight"] / weight_total
        frac = min(1.0, max(0.0, (d_hatch - start_dist) / span))
        ahead_share += share * frac
        print("   %-7s %6.1f  %8.1f      %3d    %4.0f%%    %4.0f%% of the walk"
              % (sid, s["z"], d_hatch, s["weight"], 100 * share, 100 * frac))
    print("  share of trickle bodies that spawn AHEAD of the player: %.0f%%"
          % (100 * ahead_share))
    if ahead_share < 0.999:
        print("  NOTE a Default is a hatch-seeker. A body spawned BEHIND the player walks")
        print("       SOUTH to the Hatch, away from them, and is never met on this walk.")
        print("       Worse: RunSim.DamageHatch returns early while Mode == Terminal, so a")
        print("       field wave's Hatch is INVULNERABLE. Those bodies arrive and do nothing.")
        print("       They still count against FieldTrickle.MaxLive (%d), because WaveSpawner"
              % trickle["maxLive"].value)
        print("       hands its whole AliveCount to FieldTrickle.DueIn -- so about %.1f of the"
              % ((1 - ahead_share) * trickle["maxLive"].value))
        print("       %d cap slots end up parked at an invulnerable Hatch, THROTTLING the drip"
              % trickle["maxLive"].value)
        print("       that could have reached the player.")

    for label, seconds in (("walking", t_walk), ("sprinting", t_sprint)):
        met = seconds / trickle["gap"].value
        met_ahead = met * ahead_share
        print()
        print("  TRICKLE MET WHILE %s (%.1f s / %.1f s per body)"
              % (label.upper(), seconds, trickle["gap"].value))
        print("    bodies spawned during the walk : %.1f" % met)
        print("    of those, ahead of the player  : %.1f" % met_ahead)
        print("    cap on live trickle bodies     : %d (FieldTrickle.MaxLive)"
              % trickle["maxLive"].value)

        # Engagement geometry: can a trickle body reach swinging range?
        rate = calib.get(last, default_rate)
        trickle_mul = specs[-1]["hpMul"]     # the drip latches the LAST field wave's spec
        t_kill = shots_for(default["maxHp"].value * trickle_mul) / gun["shotsPerSecond"].value
        speed = move["sprint"].value if label == "sprinting" else move["walk"].value
        closing = speed + default["chaseSpeed"].value
        engage = enemy["notice"].value
        reach = engage - closing * (1.0 / rate)
        print("    engagement: a Default notices at %.0f m (noticeRange) down a straight lane."
              % engage)
        print("      trickle body HP = %.0f x %.2f = %.0f -> %d shot(s) = %.2f s of fire"
              % (default["maxHp"].value, trickle_mul,
                 default["maxHp"].value * trickle_mul,
                 shots_for(default["maxHp"].value * trickle_mul), t_kill))
        print("      at the calibrated %.2f kills/s one body costs %.2f s, during which it"
              % (rate, 1.0 / rate))
        print("      closes %.1f + %.1f = %.1f m/s x %.2f s = %.1f m"
              % (speed, default["chaseSpeed"].value, closing, 1.0 / rate,
                 closing * (1.0 / rate)))
        print("      so it dies at about %.1f m out; swinging range is %.1f m."
              % (reach, enemy["attackRange"].value))
        reaches = reach <= enemy["attackRange"].value
        print("      -> a lone trickle body %s the player."
              % ("REACHES" if reaches else "never reaches"))
        drain_rate = rate - 1.0 / trickle["gap"].value
        print("      drip is %.2f/s against a %.2f/s kill rate: net %+.2f/s, so the lane %s."
              % (1.0 / trickle["gap"].value, rate, -drain_rate,
                 "fills to the cap" if drain_rate < 0 else "never accumulates"))

        # HP: two damage models, both stated.
        geo_hp = 0.0 if not reaches else met_ahead * default["playerDamage"].value
        ratio_hp = (met_ahead * calib.get("hitsPerBody", 0.0)
                    * default["playerDamage"].value) if "hitsPerBody" in calib else float("nan")
        boost = (0.0 if args.no_trickle_regen
                 else average_boost(met_ahead / seconds if seconds else 0.0, regen))
        hits_taken = (met_ahead * calib["hitsPerBody"]) if "hitsPerBody" in calib else 0.0
        held = min(seconds, hits_taken * regen["damageDelay"].value)
        gained = (regen["base"].value + boost) * max(0.0, seconds - held)
        print("    HP LOST")
        print("      geometric model (does a body reach %.1f m?)      : %.0f HP"
              % (enemy["attackRange"].value, geo_hp))
        if "hitsPerBody" in calib:
            print("      log-ratio upper bound (%.3f hits/body x %.1f bodies x %.0f dmg): %.0f HP"
                  % (calib["hitsPerBody"], met_ahead, default["playerDamage"].value, ratio_hp))
        print("    HP REGAINED")
        if args.no_trickle_regen:
            print("      CANDIDATE: trickle kills do not feed the boost, so only the")
            print("      %.1f HP/s floor runs." % regen["base"].value)
        else:
            print("      trickle kills DO feed the regen boost: GameController.GatekeeperKilled")
            print("      calls PlayerHealth.NotifyKill on the same seam as the payout, and only")
            print("      the PAYOUT is zeroed while the drip is live (RunSim.CurrentKillAward).")
            print("      Nothing zeroes the heal.")
        print("      killing the %.1f bodies met = %.2f /s -> mean boost ="
              % (met_ahead, met_ahead / seconds if seconds else 0))
        print("        %.1f x %.2f x %.1f / ln2 = %.2f HP/s (cap %.1f)"
              % (regen["perKill"].value, met_ahead / seconds if seconds else 0,
                 regen["halfLife"].value, boost, regen["maxBoost"].value))
        print("      %.1f hit(s) taken hold regen for %.0f s each = %.1f s suppressed"
              % (hits_taken, regen["damageDelay"].value, held))
        print("      total rate %.2f + %.2f = %.2f HP/s over %.1f s = %.0f HP"
              % (regen["base"].value, boost, regen["base"].value + boost,
                 seconds - held, gained))
        worst = max(geo_hp, 0.0 if math.isnan(ratio_hp) else ratio_hp)
        net = gained - worst
        print("    NET ACROSS THE WALK: %+.0f HP of headroom"
              % net)
        print("      (regen would cover %.0f, worst-case damage is %.0f)" % (gained, worst))
        arrive = min(100.0, args.hp_at_roster_end + net)
        print("    arriving at %s with %.0f HP, from %.0f at roster's end (--hp-at-roster-end)"
              % (term_id, arrive, args.hp_at_roster_end))

    # ---- the composite seam ---------------------------------------------------
    if len(specs) > 1:
        gap = const(wd_rel, "InterWaveGap")
        src_gap = gap.cite
        rule("THE COMPOSITE SEAM -- how much HP the gap between waves buys back")
        print("  A composite runs its waves back to back with InterWaveGap = %.0f s between"
              % gap.value)
        print("  the end of one DELIVERY and the start of the next.")
        print("  (%s)" % src_gap)
        print("  the only pause inside a zone, and it is what the 22:35 loss fell through:")
        print("  the player entered wave 2 at 22 HP and was dead 4.1 s later.")
        print()
        rate1 = calib.get(specs[0]["wave"], default_rate)
        b0 = average_boost(rate1, regen)
        lam = math.log(2.0) / regen["halfLife"].value
        usable = max(0.0, gap.value - regen["damageDelay"].value)
        gained = regen["base"].value * usable + b0 * (1.0 - math.exp(-lam * usable)) / lam
        print("  boost standing at the end of wave %d (killing at %.2f /s):" % (specs[0]["wave"], rate1))
        print("    RegenPerKill x rate / lambda = %.1f x %.2f / %.4f = %.2f HP/s (cap %.1f)"
              % (regen["perKill"].value, rate1, lam,
                 min(regen["maxBoost"].value, regen["perKill"].value * rate1 / lam),
                 regen["maxBoost"].value))
        print("  a hit in the last second costs %.0f s of the gap (DamageDelaySeconds), so"
              % regen["damageDelay"].value)
        print("    usable gap = %.0f - %.0f = %.1f s" % (gap.value, regen["damageDelay"].value, usable))
        print("  HP back = base x t + B0 x (1 - e^-lambda*t) / lambda")
        print("          = %.1f x %.1f + %.2f x (1 - e^-%.4f) / %.4f = %.1f HP"
              % (regen["base"].value, usable, b0, lam * usable, lam, gained))
        print()
        print("  So the seam is worth about %.0f HP. A player who leaves wave %d at 22 HP"
              % (gained, specs[0]["wave"]))
        print("  enters wave %d at about %.0f HP. Wave %d's first bodies then need the"
              % (specs[1]["wave"], 22 + gained, specs[1]["wave"]))
        print("  lead-in to reach them, and regen keeps running through it.")
        print("  A %.0f HP burst is %d Default swings at %.0f HP each -- %.1f s of contact"
              % (22 + gained, math.ceil((22 + gained) / default_damage), default_damage,
                 math.ceil((22 + gained) / default_damage) * director.attack_interval.value))
        print("  from ONE body, or %.1f s from two. The 22:35 death took 4.1 s."
              % (math.ceil((22 + gained) / default_damage) * director.attack_interval.value / 2))

    # ---- assumptions ----------------------------------------------------------
    rule("ASSUMPTIONS -- what these numbers rest on")
    for i, line in enumerate((
        "Kill rate is an INPUT measured from a playthrough log, not a computed dps. It",
        "  bundles aim, movement, reloads, target selection and hesitation into one number.",
        "Waves with no measured rate use --kill-rate unchanged. The health multiplier rises",
        "  0.40 per wave, so an unmeasured deep wave is optimistic by however much more",
        "  ammunition its bodies cost -- the shots/body column is there to size that.",
        "Damage is modelled two ways and BOTH are printed, because neither is trustworthy",
        "  alone: a geometric reach test (can a body close to attackRange before it dies),",
        "  and the log's measured hits-per-body applied to the trickle count. The geometric",
        "  test ignores anything that stops the player shooting (a reload landing wrong,",
        "  turning a corner, a second body from the other closet).",
        "The model spreads kills evenly. Real killing is bursty: against the 07:04 log this",
        "  understates peak-alive by roughly half, so treat peak-alive predictions as a",
        "  floor, not a forecast.",
        "Enemy pathing is treated as a straight line down a one-lane corridor, because the",
        "  spine is one. Nothing here models cover, doors, corners or the navmesh.",
        "Player position when the roster empties is a parameter (--start-from), not a",
        "  prediction. It is the single largest lever on everything below it.",
        "No Primary, no repair rig, no meta upgrades, siphon dial 0, solo. Anything bought",
        "  between zones invalidates the fight times but not the walk arithmetic.",
        "Hunters chase at their own speed and Thieves ignore the player entirely; the walk",
        "  arithmetic uses the Default line because the trickle spawns Defaults only",
        "  (WaveSpawner.SpawnTrickleBody passes GatekeeperKind.Default).",
    )):
        print(("  - " if not line.startswith("  ") else "    ") + line.strip())

    rule("WHAT THIS MODEL CANNOT ANSWER")
    for line in (
        "Whether any of it is FUN, legible, or reads as a beat rather than a chore.",
        "Whether the player can tell the trickle apart from the wave they just finished.",
        "Whether 13 seconds of corridor feels like a walk or a wait.",
        "Whether an arriving body is SEEN in time -- the model gives it noticeRange and a",
        "  straight lane; a real player has a 60-degree FOV and is looking somewhere.",
        "Anything about a wave nobody has played. Waves 3-5 have never been logged.",
    ):
        print("  - " + line)
    print()


_DIRECTOR_REL = "Assets/Scripts/Sim/Waves/WaveDirector.cs"


def _probe_unreadable_interval():
    """Director must refuse to build when it cannot read the spawnInterval formula.

    This is the one reader that parses a FORMULA rather than a constant, so it is the one
    that breaks when the C# is restructured rather than retuned — which is exactly what
    happened on 2026-08-27 when the gap flipped from narrowing to widening. Everything else
    in this file would have kept printing a plausible delivery table.
    """
    body = _text(_DIRECTOR_REL)
    mangled = body.replace("spec.spawnInterval = Math.Max(", "spec.spawnInterval = Retuned(")
    if mangled == body:
        raise SourceError("selftest fixture is stale: no spawnInterval assignment to mangle")
    saved = dict(_CACHE)
    try:
        _CACHE[_DIRECTOR_REL] = mangled
        Director()
    finally:
        _CACHE.clear()
        _CACHE.update(saved)


def _check_interval_table():
    """Positive counterpart: the parser must reproduce the authored gaps off the real tree.

    A matcher can be wrong in two directions. The fixtures above catch 'reads nothing'; only
    this catches 'reads something, and it is the wrong sign' — the failure that would have
    let this tool report the old narrowing table as though nothing had changed.
    """
    d = Director()
    gaps = [d.wave(i)["spawnInterval"] for i in range(d.max_waves.value)]
    rising = all(b >= a for a, b in zip(gaps, gaps[1:]))
    held = abs(d.wave(d.max_waves.value + 40)["spawnInterval"] - gaps[-1]) < 1e-6
    print("selftest: spawnInterval table   -> %s  (%s, probe index %s)"
          % (" / ".join("%.2f" % g for g in gaps),
             "widening" if rising else "NARROWING",
             "holds" if held else "extrapolates"))
    return 0


def selftest():
    """A constant this tool cannot find must be FATAL.

    The whole value of reading the tree instead of restating it is lost the moment a
    failed read quietly falls back to a remembered number: the tool would keep printing
    the old value long after somebody retuned the real one. These fixtures ask each
    reader for something that is not there and require SourceError from every one.
    """
    cases = [
        ("const", lambda: const("Assets/Scripts/Sim/Waves/WaveDirector.cs", "NoSuchConstant")),
        ("field", lambda: field("Assets/Scripts/Game/Player/FirstPersonController.cs",
                                "noSuchField")),
        ("int[]", lambda: int_array("Assets/Scripts/Sim/Waves/WaveDirector.cs", "NoSuchTable")),
        ("weapon", lambda: weapon("No Such Gun")),
        ("StatsFor", lambda: stats_for("NoSuchKind")),
        ("missing file", lambda: const("Assets/Scripts/Sim/Waves/NoSuchFile.cs", "X")),
        ("spawnInterval formula", _probe_unreadable_interval),
    ]
    failures = 0
    for name, probe in cases:
        try:
            probe()
        except SourceError:
            print("selftest: unreadable %-12s -> SourceError (exit 2)" % name)
            continue
        except Exception as err:            # any other failure is a fault too
            print("selftest FAIL: unreadable %s raised %r, not SourceError" % (name, err))
            failures += 1
            continue
        print("selftest FAIL: unreadable %s returned a value instead of failing" % name)
        failures += 1
    if failures:
        sys.stderr.write("loop-model --selftest: FAILED (%d)\n" % failures)
        return 1
    _check_interval_table()
    print("loop-model --selftest: ok (%d unreadable constants, all fatal)" % len(cases))
    return 0


def main():
    p = argparse.ArgumentParser(
        description="Offline cost model for one Bastion zone. Reads live Sim constants.")
    p.add_argument("--zone", type=int, default=1, help="authored zone (default 1)")
    p.add_argument("--calibrate", metavar="LOG",
                   help="playthrough log to measure the kill rate and damage bursts from")
    p.add_argument("--calibrate-draws", type=int, default=0,
                   help="only the first N draws of the log feed the hits-per-body figure "
                        "(0 = all of them). A hatch-bank retrigger is a different fight.")
    p.add_argument("--kill-rate", type=float, default=0.83,
                   help="kills/s for waves with no measured rate (default 0.83, the mean of "
                        "the 07:04 run's two waves)")
    p.add_argument("--start-from", type=float, default=0.0,
                   help="metres up the lane the player stands when the roster empties "
                        "(default 0 = holding the Hatch)")
    p.add_argument("--hp-at-roster-end", type=float, default=100.0,
                   help="player HP when the last authored body falls (default 100)")
    p.add_argument("--weapon-range", type=float, default=45.0,
                   help="metres at which the player starts killing, for the lead-in "
                        "(default 45, the MK-1's range)")
    p.add_argument("--weapon", default="MK-1 Sidearm",
                   help="WeaponCatalog display name to price shots with")
    p.add_argument("--trickle-gap", type=float, default=None,
                   help="WHAT-IF: override FieldTrickle.SecondsBetweenBodies to price a "
                        "tuning candidate without editing Assets/Scripts. Announced in the "
                        "SOURCES block so it can never pass for an authored value.")
    p.add_argument("--no-trickle-regen", action="store_true",
                   help="WHAT-IF: model trickle kills NOT feeding PlayerRegen's kill boost")
    p.add_argument("--selftest", action="store_true",
                   help="prove that an unreadable constant is exit 2, never a default")
    args = p.parse_args()
    if args.selftest:
        return selftest()
    try:
        run_report(args)
    except SourceError as err:
        sys.stderr.write("loop-model: %s\n" % err)
        return 2
    return 0


if __name__ == "__main__":
    sys.exit(main())
