namespace GatekeepersProtocol.Sim
{
    /// <summary>
    /// Everything one Run owns, in one bag: the campaign meta it reads and writes, the run state
    /// machine, the decoy payload state, and the counter that hands out stable spawn ids. The
    /// scene host (GameController in the Bastion) constructs exactly one and passes it down;
    /// nothing here is static, so two contexts in one process are independent by construction —
    /// which is the property co-op integration will lean on, and the property the two-Run
    /// isolation test asserts.
    ///
    /// Production runs share the campaign MetaState (one purse per save); isolation tests hand
    /// each context its own instance. The context does not know the difference.
    /// </summary>
    public sealed class RunContext
    {
        /// <summary>Campaign / cross-run state this run banks into. Never null.</summary>
        public MetaState Meta { get; private set; }

        /// <summary>Phase, waves, hatch, salvage, Rng, Tick — the run state machine. Never null.</summary>
        public RunSim Sim { get; private set; }

        /// <summary>Decoy payload state (charges, live prop, absorb tally). Never null.</summary>
        public DecoyState Decoy { get; private set; }

        /// <summary>Next stable spawn id. Read via <see cref="TakeSpawnId"/>.</summary>
        public int NextSpawnId { get; private set; }

        public RunContext(MetaState meta, int seed)
        {
            if (meta == null) throw new System.ArgumentNullException("meta");
            Meta = meta;
            Sim = new RunSim(meta, seed);
            Decoy = new DecoyState();
            NextSpawnId = 1;
        }

        /// <summary>Sequential stable id for a gameplay spawn (enemies today). Ids are unique
        /// within one run and never reused, so a late despawn log line still names its spawn.</summary>
        public int TakeSpawnId()
        {
            return NextSpawnId++;
        }
    }
}
