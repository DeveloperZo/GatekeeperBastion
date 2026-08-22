namespace GatekeepersProtocol.Sim
{
    /// <summary>
    /// The one place the persistence schema version lives, and the one place that decides
    /// whether a document on disk may be loaded at all.
    /// </summary>
    /// <remarks>
    /// Version 13 split one document into two: a Boarder that travels with the player and a
    /// realm that stays with the world (Docs/Milestones — the realm and the Boarder). With the
    /// split came the end of migration: a document below <see cref="CurrentVersion"/> is
    /// REFUSED, plainly and by name, rather than reshaped into something the current build can
    /// read. There is no shim and none is wanted — a Boarder assembled out of a single-document
    /// save has to guess which half of that save was the world's, and a wrong guess is a silently
    /// corrupted campaign rather than a loud one.
    ///
    /// The history below is kept because it is the record of why keys are spelled as they are.
    /// None of it is still executable.
    ///   1  — banked salvage + run tallies
    ///   2  — lifetime salvage ledger (Earned/Spent)
    ///   3  — per-weapon ownership, Tiers, equipped slots
    ///   4  — equipment slot + wheel points (replaces the repair/engineering tracks)
    ///   5  — third weapon slot renamed Melee -> Drone, and the JSON key with it
    ///   6  — EquippedDrone holds a PayloadId; OwnedPayloadMask added
    ///   7  — CompletedTerminalIds: the last successful Hatch-bank unlock graph
    ///   8  — Boarder Protocol + Passive; the four crew tracks became parse-only and refunded
    ///   9  — first-Bastion demo flags plus per-zone DemoZoneDeepestWave
    ///  10  — OpenDoorIds: door state stopped being derivable and became its own saved fact
    ///  11  — HatchConversions: the wheel re-keyed off PodEjections onto Hatch conversions
    ///  12  — WeaponWheelCentres: centred-purchase counts per weapon per spoke, so the cost
    ///         of the next focus buy keys on the centred spoke rather than derived Tier. That
    ///         field survives at 13 and rides the Boarder, where kit belongs.
    ///  13  — the Boarder / realm split. Two documents, no migration.
    /// </remarks>
    public static class SaveFormat
    {
        /// <summary>Schema version written by the current build, shared by both documents.</summary>
        public const int CurrentVersion = 13;

        /// <summary>Only the current version loads. Older documents are refused, not migrated.</summary>
        public static bool IsSupported(int version)
        {
            return version == CurrentVersion;
        }

        /// <summary>Why a document was refused, in words a player can act on. Callers log this
        /// rather than failing silently — a refusal the player never sees reads as data loss.</summary>
        public static string RefusalFor(int version)
        {
            if (version <= 0)
                return "save carries no readable version and cannot be loaded";
            if (version < CurrentVersion)
                return "save version " + version + " was written before the Boarder and realm "
                    + "became separate saves (version " + CurrentVersion + ") and cannot be loaded";
            return "save version " + version + " was written by a newer build than this one "
                + "(this build reads version " + CurrentVersion + ") and cannot be loaded";
        }
    }
}
