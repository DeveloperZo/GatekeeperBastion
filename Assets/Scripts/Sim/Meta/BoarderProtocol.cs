using System;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Sim
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
        Barrage = 5,
        Rush = 6,
    }

    /// <summary>Catalog: protocol → doctrine / abilities, and every number as a function of
    /// ability id. Verbs and server clamps read only these.</summary>
    public static class BoarderProtocol
    {
        public const float BarrageFireMul = 2f;
        public const float RushFireMul = 1.5f;
        const float BarrageMulCap = 2f;

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
            return id == BoarderProtocolId.Default ? BoarderAbilityId.Barrage : BoarderAbilityId.None;
        }

        public static BoarderAbilityId PersonalAbilityOf(BoarderProtocolId id)
        {
            return id == BoarderProtocolId.Default ? BoarderAbilityId.Rush : BoarderAbilityId.None;
        }

        public static int PassiveCount(BoarderProtocolId id)
        {
            return 0;
        }

        public static BoarderAbilityId PassiveAt(BoarderProtocolId id, int index)
        {
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
            return protocol == BoarderProtocolId.Default && passive == BoarderAbilityId.None;
        }

        /// <summary>v8 equip fields, or None/None on older saves. Returns the crew-track refund.</summary>
        public static long ApplySaveEquip(MetaState m, SaveData d)
        {
            if (d.SaveVersion >= 8)
            {
                m.EquippedProtocol = (BoarderProtocolId)d.EquippedProtocol;
                m.EquippedPassive = (BoarderAbilityId)d.EquippedPassive;
                if (m.EquippedProtocol == BoarderProtocolId.Default)
                {
                    if (!IsLegalPair(m.EquippedProtocol, m.EquippedPassive))
                        m.EquippedPassive = BoarderAbilityId.None;
                    return 0;
                }
                if (!IsLegalPair(m.EquippedProtocol, m.EquippedPassive))
                {
                    m.EquippedProtocol = BoarderProtocolId.None;
                    m.EquippedPassive = BoarderAbilityId.None;
                }
                return 0;
            }
            m.EquippedProtocol = BoarderProtocolId.None;
            m.EquippedPassive = BoarderAbilityId.None;
            return MetaState.SpentOnTrack(d.VitalityLevel)
                + MetaState.SpentOnTrack(d.StaminaLevel)
                + MetaState.SpentOnTrack(d.ScavengingLevel)
                + MetaState.SpentOnTrack(d.LuckLevel);
        }

        public static float Window(BoarderAbilityId id)
        {
            switch (id)
            {
                case BoarderAbilityId.Anchor:
                case BoarderAbilityId.Barrage: return 8f;
                case BoarderAbilityId.Bulkhead:
                case BoarderAbilityId.Rush: return 6f;
                default: return 0f;
            }
        }

        public static float Cooldown(BoarderAbilityId id)
        {
            switch (id)
            {
                case BoarderAbilityId.Anchor:
                case BoarderAbilityId.Barrage: return 30f;
                case BoarderAbilityId.Bulkhead:
                case BoarderAbilityId.Rush: return 40f;
                default: return 0f;
            }
        }

        /// <summary>LoL overlay: 0 when ready, remaining/window while open, remaining/cooldown
        /// while cooling. HUD fillAmount.</summary>
        public static float OverlayFill(float window, float cooldown, float windowDur, float cdDur)
        {
            const float eps = 0.05f;
            if (window > eps && windowDur > 0f) return Clamp01(window / windowDur);
            if (cooldown > eps && cdDur > 0f) return Clamp01(cooldown / cdDur);
            return 0f;
        }

        static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
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

        public static float FireMul(BoarderAbilityId id)
        {
            switch (id)
            {
                case BoarderAbilityId.Barrage: return BarrageFireMul;
                case BoarderAbilityId.Rush: return RushFireMul;
                default: return 1f;
            }
        }

        /// <summary>Rush is protocolMul. Barrage window and live fields SET (cap 2), then
        /// multiply. Caster in both = 3.0; overlapping plants stay 2.0.</summary>
        public static float ComposeFireMul(bool rushOpen, bool barrageWindow, float fieldMul)
        {
            float protocolMul = rushOpen ? RushFireMul : 1f;
            float barrageMul = barrageWindow ? BarrageFireMul : 1f;
            if (fieldMul > barrageMul) barrageMul = fieldMul;
            if (barrageMul > BarrageMulCap) barrageMul = BarrageMulCap;
            if (barrageMul < 1f) barrageMul = 1f;
            return protocolMul * barrageMul;
        }

        public static float Radius(BoarderAbilityId id)
        {
            if (id == BoarderAbilityId.Anchor || id == BoarderAbilityId.Barrage) return 8f;
            return 0f;
        }

        public static string DisplayName(BoarderAbilityId id)
        {
            switch (id)
            {
                case BoarderAbilityId.Anchor: return "Anchor";
                case BoarderAbilityId.Bulkhead: return "Bulkhead";
                case BoarderAbilityId.Ghost: return "Ghost";
                case BoarderAbilityId.Foresight: return "Foresight";
                case BoarderAbilityId.Barrage: return "Barrage";
                case BoarderAbilityId.Rush: return "Rush";
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

        public const string CrewSheetDefault =
            "G Barrage — Rooted. Fire ×2.0 for 8s. Other Boarders within 8 m ×2.0, not rooted. G again or window end unplants. Cooldown 30s.";
        public const string PersonalSheetDefault =
            "H Rush — You move. Fire ×1.5 for 6s. Cooldown 40s.";
        public const string PassiveSheetDefault = "None.";

        public static string CrewSheet(BoarderProtocolId id)
        {
            return id == BoarderProtocolId.Default ? CrewSheetDefault : "";
        }

        public static string PersonalSheet(BoarderProtocolId id)
        {
            return id == BoarderProtocolId.Default ? PersonalSheetDefault : "";
        }

        public static string PassiveSheet(BoarderProtocolId id)
        {
            return id == BoarderProtocolId.Default ? PassiveSheetDefault : "";
        }

        /// <summary>One template's fields. Same keys for every protocol; values change.</summary>
        public readonly struct Sheet
        {
            public readonly string Name;
            public readonly string Doctrine;
            public readonly string Crew;
            public readonly string Personal;
            public readonly string Passive;

            public Sheet(string name, string doctrine, string crew, string personal, string passive)
            {
                Name = name != null ? name : "";
                Doctrine = doctrine != null ? doctrine : "";
                Crew = crew != null ? crew : "";
                Personal = personal != null ? personal : "";
                Passive = passive != null ? passive : "";
            }
        }

        public static Sheet SheetOf(BoarderProtocolId id)
        {
            return new Sheet(
                ProtocolName(id),
                DoctrineText(id),
                CrewSheet(id),
                PersonalSheet(id),
                PassiveSheet(id));
        }

        /// <summary>Own and equip a shop protocol. Cost is 0. Sim has no shop UI.</summary>
        public static bool TryClaim(MetaState m, BoarderProtocolId id)
        {
            if (m.PayloadLoadoutLocked)
            {
                Log.Event("Ship", "BuyProtocol refused — run in progress track=crew");
                return false;
            }
            if (id == BoarderProtocolId.None)
            {
                Log.Event("Ship", "BuyProtocol refused — None");
                return false;
            }
            if (m.EquippedProtocol == id)
            {
                Log.Event("Ship", "BuyProtocol refused — already owned");
                return false;
            }
            var shop = ShopProtocols();
            bool listed = false;
            for (int i = 0; i < shop.Length; i++)
            {
                if (shop[i] == id) { listed = true; break; }
            }
            if (!listed)
            {
                Log.Event("Ship", "BuyProtocol refused — not in shop");
                return false;
            }
            m.EquippedProtocol = id;
            m.EquippedPassive = BoarderAbilityId.None;
            return true;
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
