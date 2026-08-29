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
        // 9 and 10 briefly named FieldRearm / Redeploy while FORGE was authored against
        // decoys. Neither reached a save — only protocol and passive ids are persisted, and
        // crew/personal ids are always derived — so the numbers are reused, not retired.
        FieldDeploy = 9,
        Overcharge = 10,
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

        /// <summary>FORGE's turret. It is the only player-owned thing that deals damage, so its
        /// output is stated here as damage per shot and shots per second rather than a DPS
        /// figure — the two are tuned separately and a single number hides which one moved.</summary>
        public const float TurretMaxHp = 150f;
        public const float TurretRangeMetres = 18f;
        public const float TurretDamagePerShot = 6f;
        public const float TurretShotsPerSecond = 2f;

        /// <summary>Rounds the machine carries — ONE OF TWO LIMITS, with
        /// <see cref="TurretLifeSeconds"/>, whichever runs out first (Zo, 2026-08-28). This
        /// briefly read "not damage and not a clock", from the few hours when rounds were the
        /// only limit; the clock was added the same day and is in practice the one that binds.
        /// <para>Damage was never going to do it. Before 2026-08-27 nothing could target a
        /// turret at all without one specific passive; making it mortal helped and did not fix
        /// the feel, because a machine that only dies when the enemy chooses to shoot it is
        /// still the player's to keep. FIELD DEPLOY's cooldown is 30 s, so anything that
        /// outlives 30 s of use is a fixture rather than a deployable.</para>
        /// <para>60 rounds is 30 s of firing at the base rate, so against a 20 s clock the
        /// magazine is a CEILING on how much one machine can ever do rather than the thing that
        /// ends it — a turret in a busy lane dies on the clock with about 20 rounds unspent, and
        /// one in a quiet lane dies on the clock having spent almost none. OVERCHARGE DOES NOT
        /// TOUCH THIS: it draws on <see cref="OverchargeShotBudget"/>, its own belt, so a spike
        /// costs the spike and leaves the machine's ordinary life intact.</para>
        /// <para>UNTESTED. This is arithmetic against a cooldown, not a measurement; it is the
        /// first dial to turn after a run with FORGE.</para></summary>
        public const int TurretShotBudget = 60;

        /// <summary>Seconds a turret stands, regardless of what it has left in the magazine.
        /// TWO LIMITS, WHICHEVER COMES FIRST (Zo, 2026-08-28: "expire after 60 rounds and 20
        /// seconds"). Rounds alone let a turret covering a quiet lane stand forever; a clock
        /// alone let one in a firefight outlive its usefulness. 60 rounds at the base 2/s is
        /// exactly 30 s of firing, so in a busy lane the CLOCK is what ends it at 20 s with
        /// rounds to spare, and in a quiet one the clock ends it too. The magazine is the
        /// ceiling on how much it can ever do; the clock is the floor under how often you
        /// redeploy.</summary>
        public const float TurretLifeSeconds = 20f;

        /// <summary>OVERCHARGE FIRES FROM ITS OWN BELT. Redlining used to empty the turret's
        /// magazine 2.5x faster, which punished the ability with the machine's ordinary
        /// lifespan — use it and the turret was gone. These rounds are separate, so a spike
        /// costs the spike and nothing else and the turret is still standing afterwards with
        /// what it had. 8 s at the overcharged rate (2 x 2.5 = 5/s) is exactly 40.</summary>
        public const int OverchargeShotBudget = 40;

        /// <summary>The aura belongs to the machine, not to a window on the caster: Boarders
        /// near the turret fire faster for as long as it stands.</summary>
        public const float TurretAuraRadius = 8f;
        public const float TurretAuraFireMul = 1.35f;

        /// <summary>OVERCHARGE redlines the machine. The cost is the BELT it draws from
        /// (<see cref="OverchargeShotBudget"/>) and nothing else — the old per-second life burn
        /// was retired on 2026-08-28 because it charged the player twice for one spike and made
        /// using the ability shorten the turret it was meant to make better.</summary>
        public const float OverchargeTurretRateMul = 2.5f;
        public const float OverchargeSelfFireMul = 1.4f;
        /// <summary>DEAD as of 2026-08-28. <see cref="OverchargeBurnForSeconds"/> returns 0
        /// and nothing else reads this. Kept for one release so a save or net message carrying
        /// the old shape still resolves; delete it once nothing deserialises it.</summary>
        public const float OverchargeHpPerSecond = 18f;

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
                case BoarderProtocolId.Forge: return BoarderAbilityId.FieldDeploy;
                default: return BoarderAbilityId.None;
            }
        }

        public static BoarderAbilityId PersonalAbilityOf(BoarderProtocolId id)
        {
            switch (id)
            {
                case BoarderProtocolId.Default: return BoarderAbilityId.Rush;
                case BoarderProtocolId.Veil: return BoarderAbilityId.Cloak;
                case BoarderProtocolId.Forge: return BoarderAbilityId.Overcharge;
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
        /// <summary>Make a loaded Boarder's equipped pair legal before anything reads it.
        /// An uncatalogued id — retired, or written by a build that sold more — resolves to
        /// None here rather than reaching a switch that has no arm for it, and a passive that
        /// does not belong to the surviving protocol is dropped with it.</summary>
        public static void ValidateEquip(BoarderState b)
        {
            if (b == null) return;
            if (!IsCatalogued(b.EquippedProtocol))
            {
                b.EquippedProtocol = BoarderProtocolId.None;
                b.EquippedPassive = BoarderAbilityId.None;
                return;
            }
            if (!IsLegalPair(b.EquippedProtocol, b.EquippedPassive))
                b.EquippedPassive = BoarderAbilityId.None;
        }

        public static float Window(BoarderAbilityId id)
        {
            switch (id)
            {
                case BoarderAbilityId.Anchor:
                case BoarderAbilityId.Barrage:
                case BoarderAbilityId.Ping: return 8f;
                case BoarderAbilityId.Bulkhead:
                case BoarderAbilityId.Rush:
                case BoarderAbilityId.Cloak: return 6f;

                // OVERCHARGE HAS ITS OWN ARM, and that is the point of it being written out
                // separately. Its 6 -> 8 was first made by editing the shared arm above, which
                // moved Bulkhead, Rush and Cloak to 8 s as well — three silent retunes riding
                // on one intended change, caught only because BoarderProtocolTests pins Rush
                // at the 6 s M70 authored. A fall-through group is a fine way to say "these
                // agree"; it is a trap the moment one of them stops agreeing.
                //
                // The window and OverchargeShotBudget are one number expressed twice: 8 s at
                // the overcharged rate (2 x 2.5 = 5 rounds/s) is the 40-round belt exactly.
                // Move either and re-derive the other.
                case BoarderAbilityId.Overcharge: return 8f;
                // FIELD DEPLOY is instant: the turret's life is its uptime, not a window.
                case BoarderAbilityId.FieldDeploy: return 0f;
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
                case BoarderAbilityId.FieldDeploy: return 30f;
                case BoarderAbilityId.Bulkhead:
                case BoarderAbilityId.Rush:
                case BoarderAbilityId.Cloak:
                case BoarderAbilityId.Overcharge: return 40f;
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
                case BoarderAbilityId.Ghost: return GhostAcquisitionMul;
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

        /// <summary>One self source times one zone source. Zone sources SET rather than stack
        /// (the strongest wins) and are capped; self sources are per-Boarder and exclusive by
        /// construction, since a Boarder has one protocol. Rush and Overcharge are self;
        /// Barrage fields and turret auras are zone.</summary>
        public static float ComposeFireMul(float selfMul, float zoneMul)
        {
            if (selfMul < 1f) selfMul = 1f;
            if (zoneMul < 1f) zoneMul = 1f;
            if (zoneMul > BarrageMulCap) zoneMul = BarrageMulCap;
            return selfMul * zoneMul;
        }

        /// <summary>Rush is protocolMul. Barrage window and live fields SET (cap 2), then
        /// multiply. Caster in both = 3.0; overlapping plants stay 2.0.</summary>
        public static float ComposeFireMul(bool rushOpen, bool barrageWindow, float fieldMul)
        {
            float zone = barrageWindow ? BarrageFireMul : 1f;
            if (fieldMul > zone) zone = fieldMul;
            return ComposeFireMul(rushOpen ? RushFireMul : 1f, zone);
        }

        public static float Radius(BoarderAbilityId id)
        {
            if (id == BoarderAbilityId.Anchor || id == BoarderAbilityId.Barrage) return 8f;
            if (id == BoarderAbilityId.FieldDeploy) return TurretAuraRadius;
            return 0f;
        }

        /// <summary>Seconds between turret shots at the given rate multiplier. OVERCHARGE is
        /// the only caller that passes anything but 1.</summary>
        public static float TurretShotInterval(float rateMul)
        {
            float rate = TurretShotsPerSecond * (rateMul > 0f ? rateMul : 1f);
            return rate <= 0f ? 0f : 1f / rate;
        }

        /// <summary>Turret life OVERCHARGE burns over a span. The machine pays for the spike.
        /// <para>RETIRED 2026-08-28 and kept only so a save or a net message carrying the old
        /// shape still resolves. Overcharge no longer bills a separate per-second toll: the
        /// turret's life is its magazine, and the rate multiplier already empties it 2.5x
        /// faster. Two costs for one spike double-charged it and made the burn read as an
        /// arbitrary timer bolted onto a machine that had no other clock.</para></summary>
        public static float OverchargeBurnForSeconds(float seconds)
        {
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
                case BoarderAbilityId.Ping: return "Ping";
                case BoarderAbilityId.Cloak: return "Cloak";
                case BoarderAbilityId.FieldDeploy: return "Field Deploy";
                case BoarderAbilityId.Overcharge: return "Overcharge";
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

        /// <summary>Spend the slot on an instant act — no window, straight to cooldown. FIELD
        /// DEPLOY is the case: the turret's own life is its uptime, so a window would be a
        /// second clock claiming to mean something it does not.</summary>
        public bool TrySpendInstant(float cooldown)
        {
            if (!IsReady) return false;
            WindowRemaining = 0f;
            _cooldownDuration = cooldown < 0f ? 0f : cooldown;
            CooldownRemaining = _cooldownDuration;
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
