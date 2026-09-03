namespace GatekeepersProtocol.Sim
{
    /// <summary>Master Control win sequence: <see cref="TerminalWaves"/> control-room waves,
    /// a quiet beat of <see cref="FinaleBeatSeconds"/>, then the finale elite and its escort as
    /// one more Terminal wave. Killing the elite ARMS the Master terminal; throwing that switch
    /// is what ends the Run (RunSim.ShutOffMasterControl). Live phase and remaining counts sit
    /// on <see cref="RunSim"/>.
    /// <para>2026-08-28: the sequence used to end in a Hatch-bank window back at the door.
    /// It ends at a terminal now, like every other wave in the game.
    /// <see cref="HatchWaves"/> is the Convert-window count the language table quotes; the
    /// finale does not read it (elite half is hardcoded to one wave).</para>
    /// <para>2026-09-03: five terminal waves became two shaped ones, the 3 s gap before the
    /// elite became a 12 s beat, and the elite arrives escorted and shielded. The finale is
    /// staged beats with a readable dip (Valve's finale script: horde, horde, a delay, the
    /// tank), not five identical wave-5 rosters — Docs/Design/4-Data.md "Master Control".</para>
    /// </summary>
    public static class MasterControl
    {
        /// <summary>Control-room waves before the elite. Each draws the wave-5 SIZE (the roster
        /// index clamps at the deepest table) with the finale's own shape,
        /// <see cref="ShapeForTerminalWave"/>.</summary>
        public const int TerminalWaves = 2;

        /// <summary>Hatch-bank windows in the second half. One Convert window.</summary>
        public const int HatchWaves = 1;

        /// <summary>Seconds of quiet between the last terminal wave's clear and the elite's
        /// draw. Between the terminal waves the gap is <see cref="WaveDirector.InterWaveGap"/>;
        /// this is longer on purpose — a gap is bookkeeping, a beat is something the player
        /// notices. Valve's finale script waits 12 s before the tank.</summary>
        public const float FinaleBeatSeconds = 12f;

        /// <summary>Elite bodies in the finale wave. One: the elite is the gate, the escort
        /// is what stands in front of it.</summary>
        public const int FinaleEliteCount = 1;

        /// <summary>The elite's escort. It lands before the elite and the elite refuses damage
        /// until every escort body is down (<see cref="EliteShielded"/>): kill the escort,
        /// then the boss. Hunters hunt the player and Cloakers hold the eye, so the escort is
        /// the two kinds that make the player move rather than stand and shoot.</summary>
        public const int EscortHunters = 4;
        public const int EscortCloakers = 2;

        /// <summary>Pre-multiplier HP of that elite. Wave-5 Thief is
        /// <c>StatsFor(Thief).maxHp</c> (120) × 2.60 = 312 spawned; this is 2× that Thief
        /// line (240 × 2.60 = 624). Damage and speed stay the Thief's — HP is the one lever.
        /// </summary>
        public const float FinaleEliteHp = 240f;

        /// <summary>How much bigger the elite's BODY is than a Thief's. Presentation only — it
        /// does not touch the NavMeshAgent, so the elite paths, avoids and shoves exactly like
        /// the Thief it is, and HP stays the one mechanical lever named above.
        /// Deliberately off the agent: the Bastion navmesh is baked at agentRadius 0.5 and a
        /// Thief already carries 0.48, so scaling the footprint as well would put 0.576 m of
        /// radius on a mesh carved for 0.5 and let the boss wedge in the doorway it exists to
        /// reach.
        /// <para>Derived: 1.2 (Thief root) × 1.875 = 2.25 of body, 2.25× a Default. Against the
        /// 4.5 m opening a door and ceiling leave (BastionKit.StoreyHeight 5 −
        /// FloorSlabThickness 0.5) that is ~4.55 m of elite, using the Thief skinned bind-pose
        /// AABB extent y of 1.010 — at the limit, and inside the error bar of an AABB that
        /// includes arms at their widest. Arithmetic cannot settle it; it is adjudicated by eye,
        /// at the elite, in the control room.</para></summary>
        public const float FinaleEliteScale = 1.875f;

        /// <summary>The shape a control-room wave draws, by its 1-based ordinal in the
        /// sequence: Mass, then Pack. The finale's shapes are ITS OWN — never
        /// <see cref="WaveShapes.For"/> of the wave number, which would hand the finale a
        /// wave-6/7 field draw by accident. Alternates past two so a longer sequence still
        /// reads as a sequence.</summary>
        public static WaveShape ShapeForTerminalWave(int ordinal)
        {
            if (ordinal < 1) ordinal = 1;
            return (ordinal - 1) % 2 == 0 ? WaveShape.Mass : WaveShape.Pack;
        }

        /// <summary>THE SHIELD RULE: is the elite refusing damage? True while any escort body
        /// stands. Pure so it is pinned offline; the Game layer only supplies the count
        /// (WaveSpawner's escort ledger) and applies the refusal (EnemyGatekeeper).</summary>
        public static bool EliteShielded(int escortAlive)
        {
            return escortAlive > 0;
        }

        /// <summary>Replace a control-room spec with the finale wave: the escort
        /// (<see cref="EscortHunters"/> Hunters, <see cref="EscortCloakers"/> Cloakers) and
        /// one Thief, <see cref="WaveSpec.finaleElite"/> set so the spawner applies
        /// <see cref="FinaleEliteHp"/> to the Thief and shields it behind the escort. The group
        /// width is the whole escort, so the roster lands as two groups — escort first, the
        /// elite alone last (<see cref="WaveShapes.GroupByKind"/> keeps the Thief last on a
        /// finale spec). Keeps the incoming health multiplier so spawned HP stays comparable
        /// to the Wave-5 Thief it sits above.</summary>
        public static WaveSpec ApplyFinaleElite(WaveSpec spec)
        {
            spec.defaultCount = 0;
            spec.hunterCount = EscortHunters;
            spec.cloakerCount = EscortCloakers;
            spec.thiefCount = FinaleEliteCount;
            spec.enemyCount = EscortHunters + EscortCloakers + FinaleEliteCount;
            spec.groupSize = EscortHunters + EscortCloakers;
            spec.finaleElite = true;
            return spec;
        }
    }

    /// <summary>Which half of the Master Control sequence is live.
    /// <para>2026-08-28: <c>Hatch</c> became <c>Elite</c>. The finale used to end with a
    /// Hatch-bank window back at the door; it now ends where every other wave in the game
    /// ends — at a terminal — with the elite standing between the player and the switch.
    /// The member kept its number so an in-flight save reads the same slot.</para></summary>
    public enum MasterControlPhase : byte
    {
        None = 0,
        Terminal = 1,

        /// <summary>The elite is called: the beat is running or the elite is on the floor.
        /// THE SEQUENCE DOES NOT END ON ITS OWN from here: killing it only arms the Master
        /// terminal, and the run ends when that is shut off.</summary>
        Elite = 2,
    }
}
