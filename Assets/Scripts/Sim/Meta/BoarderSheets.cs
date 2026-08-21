namespace GatekeepersProtocol.Sim
{
    /// <summary>Authored CREW-sheet copy, one row set per protocol. Separate from the catalog
    /// because this is prose under the doc contract and the catalog is numbers — they change
    /// for different reasons and review differently. Every protocol answers every row; an
    /// empty row on a catalogued id is the panel reading as broken.</summary>
    public static class BoarderSheets
    {
        public const string NameDefault = "Default Protocol";
        public const string NameVeil = "VEIL Protocol";
        public const string NameForge = "FORGE Protocol";

        public const string DoctrineDefault = "STOCK — hardware as sold";
        public const string DoctrineVeil =
            "PHANTOM — a decoy you place spawns holding the aggro you are carrying, instead of "
            + "pulling neutrally.";
        public const string DoctrineForge =
            "PARALLEL BAYS — two of your decoys may stand live at once.";

        public const string CrewDefault =
            "G Barrage — Rooted. Fire ×2.0 for 8s. Other Boarders within 8 m ×2.0, not rooted. G again or window end unplants. Cooldown 30s.";
        public const string CrewVeil =
            "G Ping — Every Gatekeeper renders through walls, for every Boarder, for 8s. Every Default and Hunter targets you while it runs. The crew gets the map; you get the room. Cooldown 30s.";
        public const string CrewForge =
            "G Field Rearm — Rooted beside a deployed machine within 4 m, yours or a teammate's. Pours 20 unbanked salvage per second into it as 30 HP per second, up to 8s. Ends early when you run dry. Cooldown 30s.";

        public const string PersonalDefault =
            "H Rush — You move. Fire ×1.5 for 6s. Cooldown 40s.";
        public const string PersonalVeil =
            "H Cloak — Off every targeting list for 6s. Gatekeepers already swinging finish the swing. Firing exits instantly. Cooldown 40s.";
        public const string PersonalForge =
            "H Redeploy — Lift your placed hardware and carry it, disarmed, at ×0.6 speed. Press again to set it down anywhere legal. It keeps the life it had. Cooldown 40s.";

        public const string PassiveDefault = "None.";
        public const string PassiveVeil =
            "Ghost — Gatekeepers notice you at 0.70× their normal range. Standing still was never the requirement.";
        public const string PassiveForge =
            "Magnet Hands — when a Gatekeeper's choice of target is ambiguous, it prefers your deployed hardware over you.";

        public static string Name(BoarderProtocolId id)
        {
            switch (id)
            {
                case BoarderProtocolId.Default: return NameDefault;
                case BoarderProtocolId.Veil: return NameVeil;
                case BoarderProtocolId.Forge: return NameForge;
                default: return "";
            }
        }

        public static string Doctrine(BoarderProtocolId id)
        {
            switch (id)
            {
                case BoarderProtocolId.Default: return DoctrineDefault;
                case BoarderProtocolId.Veil: return DoctrineVeil;
                case BoarderProtocolId.Forge: return DoctrineForge;
                default: return "";
            }
        }

        public static string Crew(BoarderProtocolId id)
        {
            switch (id)
            {
                case BoarderProtocolId.Default: return CrewDefault;
                case BoarderProtocolId.Veil: return CrewVeil;
                case BoarderProtocolId.Forge: return CrewForge;
                default: return "";
            }
        }

        public static string Personal(BoarderProtocolId id)
        {
            switch (id)
            {
                case BoarderProtocolId.Default: return PersonalDefault;
                case BoarderProtocolId.Veil: return PersonalVeil;
                case BoarderProtocolId.Forge: return PersonalForge;
                default: return "";
            }
        }

        public static string Passive(BoarderProtocolId id)
        {
            switch (id)
            {
                case BoarderProtocolId.Default: return PassiveDefault;
                case BoarderProtocolId.Veil: return PassiveVeil;
                case BoarderProtocolId.Forge: return PassiveForge;
                default: return "";
            }
        }
    }
}
