using System;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Sim
{
    /// <summary>Save-adjacent protocol identity. Never renumber, never reuse a retired id.</summary>
    public enum BoarderProtocolId
    {
        None = 0,
        Default = 1,
        Veil = 2,
        Forge = 3,
    }

    /// <summary>Doctrine written onto placed decoys. STOCK is 0; later protocols append.</summary>
    public enum DoctrineMode
    {
        Stock = 0,
        Phantom = 1,
        ParallelBays = 2,
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
        Ping = 7,
        Cloak = 8,
        FieldRearm = 9,
        Redeploy = 10,
        MagnetHands = 11,
    }

    /// <summary>Catalog: protocol → doctrine / abilities, and every number as a function of
    /// ability id. Verbs and server clamps read only these. Authored copy lives in
    /// <see cref="BoarderSheets"/>.</summary>
    public static class BoarderProtocol
    {
        public const float BarrageFireMul = 2f;
        public const float RushFireMul = 1.5f;
        const float BarrageMulCap = 2f;

        /// <summary>VEIL. Ghost biases acquisition ranges; Cloak leaves the lists outright and
        /// so has no multiplier to name.</summary>
        public const float GhostAcquisitionMul = 0.70f;

        /// <summary>FORGE. The channel spends unbanked at a fixed clock and buys hardware HP;
        /// carrying a machine costs pace.</summary>
        public const float FieldRearmSalvagePerSecond = 20f;
        public const float FieldRearmHpPerSalvage = 1.5f;
        public const float FieldRearmReachMetres = 4f;
        public const float RedeployCarrySpeedMul = 0.6f;

        /// <summary>PARALLEL BAYS lets a second bay stand live. STOCK and PHANTOM keep the
        /// payload's authored maxLive.</summary>
        public const int ParallelBaysMaxLive = 2;

        static readonly BoarderProtocolId[] Shop =
        {
            BoarderProtocolId.Default,
            BoarderProtocolId.Veil,
            BoarderProtocolId.Forge,
        };

        public static BoarderProtocolId[] ShopProtocols()
        {
            var copy = new BoarderProtocolId[Shop.Length];
            Array.Copy(Shop, copy, Shop.Length);
            return copy;
        }

        /// <summary>True when the id is a protocol this build sells. A save carrying a retired
        /// or unknown id answers false and is sanitised to None rather than throwing.</summary>
        public static bool IsCatalogued(BoarderProtocolId id)
        {
            for (int i = 0; i < Shop.Length; i++)
                if (Shop[i] == id) return true;
            return false;
        }

        public static DoctrineMode DoctrineOf(BoarderProtocolId id)
        {
            switch (id)
            {
                case BoarderProtocolId.Default: return DoctrineMode.Stock;
                case BoarderProtocolId.Veil: return DoctrineMode.Phantom;
                case BoarderProtocolId.Forge: return DoctrineMode.ParallelBays;
                default: return DoctrineMode.Stock;
            }
        }

        /// <summary>Live bays under a doctrine. The payload's own maxLive is the floor —
        /// a doctrine may widen the bay, never narrow it.</summary>
        public static int MaxLiveUnder(DoctrineMode mode, int payloadMaxLive)
        {
            if (mode != DoctrineMode.ParallelBays) return payloadMaxLive;
            return payloadMaxLive > ParallelBaysMaxLive ? payloadMaxLive : ParallelBaysMaxLive;
        }

        public static BoarderAbilityId CrewAbilityOf(BoarderProtocolId id)
        {
            switch (id)
            {
                case BoarderProtocolId.Default: return BoarderAbilityId.Barrage;
                case BoarderProtocolId.Veil: return BoarderAbilityId.Ping;
                case BoarderProtocolId.Forge: return BoarderAbilityId.FieldRearm;
                default: return BoarderAbilityId.None;
            }
        }

        public static BoarderAbilityId PersonalAbilityOf(BoarderProtocolId id)
        {
            switch (id)
            {
                case BoarderProtocolId.Default: return BoarderAbilityId.Rush;
                case BoarderProtocolId.Veil: return BoarderAbilityId.Cloak;
                case BoarderProtocolId.Forge: return BoarderAbilityId.Redeploy;
                default: return BoarderAbilityId.None;
            }
        }

        public static int PassiveCount(BoarderProtocolId id)
        {
            switch (id)
            {
                case BoarderProtocolId.Veil: return 1;
                case BoarderProtocolId.Forge: return 1;
                default: return 0;
            }
        }

        public static BoarderAbilityId PassiveAt(BoarderProtocolId id, int index)
        {
            if (index < 0 || index >= PassiveCount(id)) return BoarderAbilityId.None;
            switch (id)
            {
                case BoarderProtocolId.Veil: return BoarderAbilityId.Ghost;
                case BoarderProtocolId.Forge: return BoarderAbilityId.MagnetHands;
                default: return BoarderAbilityId.None;
            }
        }

        public static bool OwnsPassive(BoarderProtocolId protocol, BoarderAbilityId passive)
        {
            int n = PassiveCount(protocol);
            for (int i = 0; i < n; i++)
                if (PassiveAt(protocol, i) == passive) return true;
            return false;
        }

        /// <summary>The general rule the one-protocol expression was a special case of: a
        /// catalogued protocol wearing a passive it owns, or wearing none.</summary>
        public static bool IsLegalPair(BoarderProtocolId protocol, BoarderAbilityId passive)
        {
            if (!IsCatalogued(protocol)) return false;
            return passive == BoarderAbilityId.None || OwnsPassive(protocol, passive);
        }

        /// <summary>v8 equip fields, or None/None on older saves. Returns the crew-track refund.</summary>
        public static long ApplySaveEquip(MetaState m, SaveData d)
        {
            if (d.SaveVersion >= 8)
            {
                m.EquippedProtocol = (BoarderProtocolId)d.EquippedProtocol;
                m.EquippedPassive = (BoarderAbilityId)d.EquippedPassive;
                // An uncatalogued id — retired, or written by a build that sold more — resolves
                // to None here rather than reaching a switch that has no arm for it.
                if (!IsCatalogued(m.EquippedProtocol))
                {
                    m.EquippedProtocol = BoarderProtocolId.None;
                    m.EquippedPassive = BoarderAbilityId.None;
                    return 0;
                }
                if (!IsLegalPair(m.EquippedProtocol, m.EquippedPassive))
                    m.EquippedPassive = BoarderAbilityId.None;
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
                case BoarderAbilityId.Barrage:
                case BoarderAbilityId.Ping:
                case BoarderAbilityId.FieldRearm: return 8f;
                case BoarderAbilityId.Bulkhead:
                case BoarderAbilityId.Rush:
                case BoarderAbilityId.Cloak:
                case BoarderAbilityId.Redeploy: return 6f;
                default: return 0f;
            }
        }

        public static float Cooldown(BoarderAbilityId id)
        {
            switch (id)
            {
                case BoarderAbilityId.Anchor:
                case BoarderAbilityId.Barrage:
                case BoarderAbilityId.Ping:
                case BoarderAbilityId.FieldRearm: return 30f;
                case BoarderAbilityId.Bulkhead:
                case BoarderAbilityId.Rush:
                case BoarderAbilityId.Cloak:
                case BoarderAbilityId.Redeploy: return 40f;
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

        /// <summary>Anchor: slow multiplier (0.5). Ghost: acquisition range multiplier (0.70).
        /// Redeploy: carry speed multiplier (0.6).</summary>
        public static float Magnitude(BoarderAbilityId id)
        {
            switch (id)
            {
                case BoarderAbilityId.Anchor: return 0.5f;
                case BoarderAbilityId.Ghost: return GhostAcquisitionMul;
                case BoarderAbilityId.Redeploy: return RedeployCarrySpeedMul;
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

        /// <summary>Hardware HP a span of channel buys. Salvage spent is the clock.</summary>
        public static float RearmHpForSeconds(float seconds)
        {
            if (seconds <= 0f) return 0f;
            return seconds * FieldRearmSalvagePerSecond * FieldRearmHpPerSalvage;
        }

        /// <summary>Unbanked the channel spends over a span, clamped to what is carried.</summary>
        public static float RearmSpendForSeconds(float seconds, float unbanked)
        {
            if (seconds <= 0f || unbanked <= 0f) return 0f;
            float want = seconds * FieldRearmSalvagePerSecond;
            return want > unbanked ? unbanked : want;
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
                case BoarderAbilityId.Ping: return "Ping";
                case BoarderAbilityId.Cloak: return "Cloak";
                case BoarderAbilityId.FieldRearm: return "Field Rearm";
                case BoarderAbilityId.Redeploy: return "Redeploy";
                case BoarderAbilityId.MagnetHands: return "Magnet Hands";
                default: return "";
            }
        }

        public static string ProtocolName(BoarderProtocolId id)
        {
            return BoarderSheets.Name(id);
        }

        public static string DoctrineText(BoarderProtocolId id)
        {
            return BoarderSheets.Doctrine(id);
        }

        public const string CrewSheetDefault = BoarderSheets.CrewDefault;
        public const string PersonalSheetDefault = BoarderSheets.PersonalDefault;
        public const string PassiveSheetDefault = BoarderSheets.PassiveDefault;

        public static string CrewSheet(BoarderProtocolId id)
        {
            return BoarderSheets.Crew(id);
        }

        public static string PersonalSheet(BoarderProtocolId id)
        {
            return BoarderSheets.Personal(id);
        }

        public static string PassiveSheet(BoarderProtocolId id)
        {
            return BoarderSheets.Passive(id);
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

        /// <summary>Own and equip a shop protocol. Cost is 0. Sim has no shop UI. A protocol
        /// that owns passives arrives wearing its first — a freshly claimed sheet with an
        /// empty passive row is the CREW panel reading as broken.</summary>
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
            if (!IsCatalogued(id))
            {
                Log.Event("Ship", "BuyProtocol refused — not in shop");
                return false;
            }
            m.EquippedProtocol = id;
            m.EquippedPassive = PassiveCount(id) > 0 ? PassiveAt(id, 0) : BoarderAbilityId.None;
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
