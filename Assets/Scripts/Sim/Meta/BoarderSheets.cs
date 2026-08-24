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
            "PARALLEL BAYS — two of your decoys may stand live at once. Not yet active: the bay still holds one.";

        public const string CrewDefault =
            "G Barrage — Rooted. Fire ×2.0 for 8s. Other Boarders within 8 m ×2.0, not rooted. G again or window end unplants. Cooldown 30s.";
        public const string CrewVeil =
            "G Ping — Every Gatekeeper renders through walls, for every Boarder, for 8s. Every Default and Hunter targets you while it runs. The crew gets the map; you get the room. Cooldown 30s.";
        public const string CrewForge =
            "G Field Deploy — Stand a turret where you are looking. It acquires and fires on its own out to 18 m. While it stands, every Boarder within 8 m of it fires ×1.35. Deploying again replaces it. Cooldown 30s.";

        public const string PersonalDefault =
            "H Rush — You move. Fire ×1.5 for 6s. Cooldown 40s.";
        public const string PersonalVeil =
            "H Cloak — Off every targeting list for 6s. Gatekeepers already swinging finish the swing. Firing exits instantly. Cooldown 40s.";
        public const string PersonalForge =
            "H Overcharge — Redline your standing turret for 6s: it fires ×2.5 and your own gun fires ×1.4. The machine pays 18 life per second and can burn itself out. Refuses with no turret standing. Cooldown 40s.";

        public const string PassiveDefault = "None.";
        public const string PassiveVeil =
            "Ghost — Gatekeepers notice you at 0.70× their normal range. Standing still was never the requirement.";
        public const string PassiveForge =
            "Magnet Hands — a Gatekeeper that would come for you comes for your turret instead, whenever both are in reach. The machine takes the swings, and the machine can be destroyed.";

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
