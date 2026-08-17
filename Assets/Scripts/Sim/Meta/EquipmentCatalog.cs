namespace GatekeepersProtocol.Sim
{
    /// <summary>Static pure catalog of equipment. One row today (the repair rig); upgrade shape
    /// lands here when designed. Holds no state.</summary>
    public static class EquipmentCatalog
    {
        /// <summary>Salvage price of the equipment slot itself (bought once after unlock).</summary>
        public const long SlotPrice = 300L;

        /// <summary>One equipment row. Unknown ids return an empty stub.</summary>
        public struct EquipmentDef
        {
            public EquipmentId id;
            public string displayName;
            public long cost;
            /// <summary>Flat hatch repair rate in HP/s when this piece is equipped. 0 when the
            /// piece does not repair.</summary>
            public float repairPerSecond;
        }

        public static EquipmentDef Def(EquipmentId id)
        {
            switch (id)
            {
                case EquipmentId.RepairRig:
                    return new EquipmentDef
                    {
                        id = EquipmentId.RepairRig,
                        displayName = "Repair Rig",
                        cost = 300L,
                        repairPerSecond = 8f
                    };
                default:
                    return new EquipmentDef
                    {
                        id = EquipmentId.None,
                        displayName = "None",
                        cost = 0L,
                        repairPerSecond = 0f
                    };
            }
        }

        /// <summary>Bit in <see cref="MetaState.OwnedEquipmentMask"/> for this id. None → 0.</summary>
        public static int OwnershipBit(EquipmentId id)
        {
            if (id == EquipmentId.None) return 0;
            return 1 << (int)id;
        }
    }
}
