using System;

namespace GatekeeperBastion.Sim
{
    /// <summary>Save-adjacent protocol identity. Never renumber, never reuse a retired id.</summary>
    public enum BoarderProtocolId
    {
        None = 0,
        Default = 1,
    }

    /// <summary>Doctrine written onto placed decoys. STOCK is 0; later protocols append.</summary>
    public enum DoctrineMode
    {
        Stock = 0,
    }

    /// <summary>Crew / Personal / Passive row ids. Save-adjacent — never renumber.</summary>
    public enum BoarderAbilityId
    {
        None = 0,
        Anchor = 1,
        Bulkhead = 2,
        Ghost = 3,
        Foresight = 4,
    }

    /// <summary>Catalog: protocol → doctrine / abilities, and every number as a function of
    /// ability id. Verbs and server clamps read only these.</summary>
    public static class BoarderProtocol
    {
        public static BoarderProtocolId[] ShopProtocols()
        {
            return new[] { BoarderProtocolId.Default };
        }

        public static DoctrineMode DoctrineOf(BoarderProtocolId id)
        {
            return DoctrineMode.Stock;
        }

        public static BoarderAbilityId CrewAbilityOf(BoarderProtocolId id)
        {
            return id == BoarderProtocolId.Default ? BoarderAbilityId.Anchor : BoarderAbilityId.None;
        }

        public static BoarderAbilityId PersonalAbilityOf(BoarderProtocolId id)
        {
            return id == BoarderProtocolId.Default ? BoarderAbilityId.Bulkhead : BoarderAbilityId.None;
        }

        public static int PassiveCount(BoarderProtocolId id)
        {
            return id == BoarderProtocolId.Default ? 2 : 0;
        }

        public static BoarderAbilityId PassiveAt(BoarderProtocolId id, int index)
        {
            if (id != BoarderProtocolId.Default) return BoarderAbilityId.None;
            if (index == 0) return BoarderAbilityId.Ghost;
            if (index == 1) return BoarderAbilityId.Foresight;
            return BoarderAbilityId.None;
        }

        public static bool OwnsPassive(BoarderProtocolId protocol, BoarderAbilityId passive)
        {
            int n = PassiveCount(protocol);
            for (int i = 0; i < n; i++)
                if (PassiveAt(protocol, i) == passive) return true;
            return false;
        }

        public static bool IsLegalPair(BoarderProtocolId protocol, BoarderAbilityId passive)
        {
            return protocol == BoarderProtocolId.Default && OwnsPassive(protocol, passive);
        }

        /// <summary>v8 equip fields, or Default/Ghost on older saves. Returns the crew-track refund.</summary>
        public static long ApplySaveEquip(MetaState m, SaveData d)
        {
            if (d.SaveVersion >= 8)
            {
                m.EquippedProtocol = (BoarderProtocolId)d.EquippedProtocol;
                m.EquippedPassive = (BoarderAbilityId)d.EquippedPassive;
                if (IsLegalPair(m.EquippedProtocol, m.EquippedPassive)) return 0;
                m.EquippedProtocol = BoarderProtocolId.Default;
                m.EquippedPassive = BoarderAbilityId.Ghost;
                return 0;
            }
            m.EquippedProtocol = BoarderProtocolId.Default;
            m.EquippedPassive = BoarderAbilityId.Ghost;
            return MetaState.SpentOnTrack(d.VitalityLevel)
                + MetaState.SpentOnTrack(d.StaminaLevel)
                + MetaState.SpentOnTrack(d.ScavengingLevel)
                + MetaState.SpentOnTrack(d.LuckLevel);
        }

        public static float Window(BoarderAbilityId id)
        {
            switch (id)
            {
                case BoarderAbilityId.Anchor: return 8f;
                case BoarderAbilityId.Bulkhead: return 6f;
                default: return 0f;
            }
        }

        public static float Cooldown(BoarderAbilityId id)
        {
            switch (id)
            {
                case BoarderAbilityId.Anchor: return 30f;
                case BoarderAbilityId.Bulkhead: return 40f;
                default: return 0f;
            }
        }

        /// <summary>Anchor: slow multiplier (0.5). Ghost: acquisition range multiplier (0.70).</summary>
        public static float Magnitude(BoarderAbilityId id)
        {
            switch (id)
            {
                case BoarderAbilityId.Anchor: return 0.5f;
                case BoarderAbilityId.Ghost: return 0.70f;
                default: return 0f;
            }
        }

        public static float Radius(BoarderAbilityId id)
        {
            return id == BoarderAbilityId.Anchor ? 8f : 0f;
        }

        public static string DisplayName(BoarderAbilityId id)
        {
            switch (id)
            {
                case BoarderAbilityId.Anchor: return "Anchor";
                case BoarderAbilityId.Bulkhead: return "Bulkhead";
                case BoarderAbilityId.Ghost: return "Ghost";
                case BoarderAbilityId.Foresight: return "Foresight";
                default: return "";
            }
        }

        public static string ProtocolName(BoarderProtocolId id)
        {
            return id == BoarderProtocolId.Default ? "Default Protocol" : "";
        }

        public static string DoctrineText(BoarderProtocolId id)
        {
            return "STOCK — hardware as sold";
        }
    }

    /// <summary>Pure window/cooldown machine for one active slot. Trigger policy stays with
    /// the caller — later twists change who calls, not this struct.</summary>
    public struct BoarderAbilityState
    {
        public float WindowRemaining;
        public float CooldownRemaining;
        public float SecondsSatReady;
        float _cooldownDuration;

        public bool IsReady => WindowRemaining <= 0f && CooldownRemaining <= 0f;
        public bool WindowOpen => WindowRemaining > 0f;

        public void Tick(float dt)
        {
            if (dt < 0f) dt = 0f;
            if (WindowRemaining > 0f)
            {
                WindowRemaining -= dt;
                if (WindowRemaining > 0f) return;
                WindowRemaining = 0f;
                CooldownRemaining = _cooldownDuration;
                return;
            }
            if (CooldownRemaining > 0f)
            {
                CooldownRemaining -= dt;
                if (CooldownRemaining < 0f) CooldownRemaining = 0f;
                return;
            }
            SecondsSatReady += dt;
        }

        /// <summary>Start the window. False when cooling down or already open.</summary>
        public bool TryActivate(float window, float cooldown)
        {
            if (!IsReady) return false;
            if (window <= 0f) return false;
            WindowRemaining = window;
            _cooldownDuration = cooldown < 0f ? 0f : cooldown;
            SecondsSatReady = 0f;
            return true;
        }

        /// <summary>Close the window early and start cooldown. No-op when the window is shut.</summary>
        public void EndWindowEarly()
        {
            if (WindowRemaining <= 0f) return;
            WindowRemaining = 0f;
            CooldownRemaining = _cooldownDuration;
        }
    }
}
