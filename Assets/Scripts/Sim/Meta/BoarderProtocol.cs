using System;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Sim
{
    /// <summary>Save-adjacent protocol identity. Never renumber, never reuse a retired id.</summary>
    public enum BoarderProtocolId
    {
        None = 0,
        Depot = 1,
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
        // 11 retired — the turret-pull passive. Never reuse.
        Emp = 12,
    }

    /// <summary>Catalog: protocol → doctrine / abilities, and every number as a function of
    /// ability id. Verbs and server clamps read only these. Authored copy lives in
    /// <see cref="BoarderSheets"/>.</summary>
    public static class BoarderProtocol
    {
        public const float BarrageFireMul = 2f;
        /// <summary>Retired as a fire multiplier 2026-08-31 — Feed regenerates clip instead.
        /// Constant kept so old compose call sites and tests that named the number still compile
        /// until they are deleted; <see cref="FireMul"/> returns 1 for Rush.</summary>
        public const float RushFireMul = 1f;
        const float BarrageMulCap = 2f;

        /// <summary>Dump's foot tax. Not a root — look and fire stay. 0.35 is the first cut;
        /// unknown, needs play data.</summary>
        public const float DumpMoveMul = 0.35f;

        /// <summary>Feed restores this fraction of effective fire rate as clip while the
        /// toggle window is open. Continuous fire yields about 1/(1−k) rounds from the
        /// magazine (k=0.22 → ~1.28×, ~28% more). Always keep k &lt; 1 so regen cannot
        /// outpace spending.</summary>
        public const float FeedRegenOfFireRate = 0.22f;

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

        /// <summary>Charge earned per round the turret actually fires. A quiet machine gains
        /// nothing. Low end, expected to move when the second machine lands — unknown, needs
        /// play data.</summary>
        public const float TurretChargePerShot = 1f;

        /// <summary>Charge the meter holds. 10 rounds at 2/s is 5 s of defending to fill.
        /// Low end, expected to move when the second machine lands — unknown, needs play
        /// data.</summary>
        public const float TurretChargeMax = 10f;

        /// <summary>Hold past this on the crew binding to Overcharge. Below it is a tap
        /// (deploy / redeploy / despawn Exhausted). Low end, expected to move when the
        /// second machine lands.</summary>
        public const float TurretHoldThresholdSeconds = 0.35f;

        /// <summary>Seconds an Exhausted turret stands silent before despawning. Low end,
        /// expected to move when the second machine lands — unknown, needs play data.</summary>
        public const float TurretExhaustedSeconds = 3f;

        /// <summary>The aura belongs to the machine, not to a window on the caster: Boarders
        /// near the turret fire faster for as long as it stands.</summary>
        public const float TurretAuraRadius = 8f;
        public const float TurretAuraFireMul = 1.35f;

        /// <summary>OVERCHARGE redlines the machine. Paid from earned charge, not a cooldown
        /// and not a life burn. The old per-second life burn was retired on 2026-08-28; the
        /// overcharge belt retired because charge is what the machine earned.</summary>
        public const float OverchargeTurretRateMul = 2.5f;
        public const float OverchargeSelfFireMul = 1.4f;

        /// <summary>FORGE's follower. Same output grammar as the turret so the two machines
        /// are comparable; numbers are low-end placeholders — unknown, needs play data.
        /// </summary>
        public const float OutriderMaxHp = 150f;
        public const float OutriderRangeMetres = 18f;
        public const float OutriderDamagePerShot = 6f;
        public const float OutriderShotsPerSecond = 2f;
        public const float OutriderChargePerShot = 1f;
        public const float OutriderChargeMax = 10f;
        public const float OutriderHoldThresholdSeconds = TurretHoldThresholdSeconds;
        public const float OutriderExhaustedSeconds = 3f;
        /// <summary>Orbit radius around a point slightly ahead of the Boarder. 4 m keeps
        /// the body readable without walking out of the 8 m pairing radius of a turret at
        /// the Boarder's feet.</summary>
        public const float OutriderFollowDistance = 4f;

        /// <summary>Planar metres between the two machines for each special to lift the
        /// other. Same figure as the turret aura so "near" is a radius the Boarder already
        /// has. Low end — unknown, needs play data.</summary>
        public const float PairingRadiusMetres = 8f;

        /// <summary>EMP blast catch radius, from the Outrider. Low end — unknown, needs
        /// play data.</summary>
        public const float EmpRadiusMetres = 8f;

        /// <summary>Light damage the blast deals to each body it catches. Low end —
        /// unknown, needs play data.</summary>
        public const float EmpDamage = 8f;

        /// <summary>Medium-length root. Low end on purpose: a long root does the whole
        /// job and the rest of the sheet is noise. Unknown, needs play data.</summary>
        public const float EmpRootSeconds = 2.5f;

        /// <summary>PARALLEL BAYS lets a second bay stand live. STOCK and PHANTOM keep the
        /// payload's authored maxLive.</summary>
        public const int ParallelBaysMaxLive = 2;

        static readonly BoarderProtocolId[] Shop =
        {
            BoarderProtocolId.Depot,
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
        /// <summary>The protocol every Boarder carries without claiming it (Zo, 2026-09-04:
        /// "add protocol as automatic grant"). Before this, a fresh Boarder embarked on
        /// <see cref="BoarderProtocolId.None"/> and had no crew ability, no personal ability
        /// and no doctrine, which split the player base into two populations the play data
        /// could not tell apart: the 2026-09-04 run carried FORGE and the log never said so.
        /// Depot is the grant because it is the 0-salvage claim already (2026-08-16) and it
        /// owns no passive, so granting it moves nothing else.
        /// <para>This is not a free protocol on top of the others. Claiming another still
        /// replaces it, because <see cref="MetaState.EquippedProtocol"/> IS the ownership —
        /// there is no owned-set beside it.</para></summary>
        public const BoarderProtocolId GrantedProtocol = BoarderProtocolId.Depot;

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
                case BoarderProtocolId.Depot: return DoctrineMode.Stock;
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
                case BoarderProtocolId.Depot: return BoarderAbilityId.Barrage;
                case BoarderProtocolId.Veil: return BoarderAbilityId.Ping;
                case BoarderProtocolId.Forge: return BoarderAbilityId.FieldDeploy;
                default: return BoarderAbilityId.None;
            }
        }

        public static BoarderAbilityId PersonalAbilityOf(BoarderProtocolId id)
        {
            switch (id)
            {
                case BoarderProtocolId.Depot: return BoarderAbilityId.Rush;
                case BoarderProtocolId.Veil: return BoarderAbilityId.Cloak;
                case BoarderProtocolId.Forge: return BoarderAbilityId.Emp;
                default: return BoarderAbilityId.None;
            }
        }

        public static int PassiveCount(BoarderProtocolId id)
        {
            switch (id)
            {
                case BoarderProtocolId.Veil: return 1;
                default: return 0;
            }
        }

        public static BoarderAbilityId PassiveAt(BoarderProtocolId id, int index)
        {
            if (index < 0 || index >= PassiveCount(id)) return BoarderAbilityId.None;
            switch (id)
            {
                case BoarderProtocolId.Veil: return BoarderAbilityId.Ghost;
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
                // Falls to the GRANT rather than to None (2026-09-04). This arm is the one
                // path every save passes through on load, so it is where an old save with no
                // protocol picks the grant up; a retired or over-sold id lands here too and
                // gets a working sheet instead of an empty one.
                b.EquippedProtocol = GrantedProtocol;
                b.EquippedPassive = PassiveCount(GrantedProtocol) > 0
                    ? PassiveAt(GrantedProtocol, 0)
                    : BoarderAbilityId.None;
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
                // at 6 s. A fall-through group is a fine way to say "these agree"; it is a
                // trap the moment one of them stops agreeing.
                //
                // FORGE no longer spends this window: Overcharge is a hold on the crew key
                // paid from charge. The figure stays so the catalog arm does not silently
                // regroup with Rush.
                case BoarderAbilityId.Overcharge: return 8f;
                // FIELD DEPLOY and EMP (the Outrider) are instant: each machine's own phase
                // is its uptime, not a window.
                case BoarderAbilityId.FieldDeploy:
                case BoarderAbilityId.Emp: return 0f;
                default: return 0f;
            }
        }

        /// <summary>VEIL's re-entry (Zo, 2026-09-04: "give it power through a reset or
        /// lowering cooldowns to re-enter stealth"). Seconds off Cloak's cooldown for each
        /// UNSPENT second of its window when the window is broken early.
        /// <para>VEIL was survival-only: Cloak leaves the acquisition lists and firing
        /// collapses it, so the sheet had no way to convert stealth into anything but distance.
        /// This does not give it damage — the 2026-08-15 ban on damage-granting rows and crit
        /// sources stands, and nothing here touches a number the gun reads. It buys TEMPO:
        /// break early and you are back in stealth sooner.</para>
        /// <para>The number makes the window a decision rather than a duration. At 2 per second
        /// against a 6 s window and a 40 s cooldown, hiding the full window costs the full 40,
        /// and breaking instantly refunds 12 for a 28 s wait. So a full-hide cycle is 46 s and
        /// a strike-and-vanish cycle is 28 s: the aggressive read re-enters about 1.6x as
        /// often, and pays for it in stealth it did not use.</para>
        /// <para>Cloak is the only ability with a refund, because it is the only window firing
        /// collapses — everywhere else "unspent window" is not a choice the player made.</para></summary>
        public const float CloakBreakRefundPerSecond = 2f;

        /// <summary>Seconds refunded per unspent window second when <paramref name="id"/>'s
        /// window is broken early. Zero for everything but Cloak.</summary>
        public static float BreakRefundPerSecond(BoarderAbilityId id)
        {
            return id == BoarderAbilityId.Cloak ? CloakBreakRefundPerSecond : 0f;
        }

        public static float Cooldown(BoarderAbilityId id)
        {
            switch (id)
            {
                case BoarderAbilityId.Anchor:
                case BoarderAbilityId.Barrage:
                case BoarderAbilityId.Ping:
                case BoarderAbilityId.FieldDeploy:
                case BoarderAbilityId.Emp: return 30f;
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
                // Feed (Rush id): clip regen, not fire rate.
                case BoarderAbilityId.Rush: return 1f;
                default: return 1f;
            }
        }

        /// <summary>Rounds per second Feed restores at the given effective fire rate.
        /// Never returns a value ≥ fire rate.</summary>
        public static float FeedRegenPerSecond(float effectiveFireRate)
        {
            if (effectiveFireRate <= 0f) return 0f;
            float r = effectiveFireRate * FeedRegenOfFireRate;
            float cap = effectiveFireRate * 0.99f;
            if (r > cap) r = cap;
            return r;
        }

        /// <summary>One self source times one zone source. Zone sources SET rather than stack
        /// (the strongest wins) and are capped; self sources are per-Boarder and exclusive by
        /// construction, since a Boarder has one protocol. Overcharge is self;
        /// Dump auras and turret auras are zone. Feed does not multiply fire rate.</summary>
        public static float ComposeFireMul(float selfMul, float zoneMul)
        {
            if (selfMul < 1f) selfMul = 1f;
            if (zoneMul < 1f) zoneMul = 1f;
            if (zoneMul > BarrageMulCap) zoneMul = BarrageMulCap;
            return selfMul * zoneMul;
        }

        /// <summary>Dump window and live fields SET (cap 2). Feed no longer contributes a
        /// self fire mul — pass false for the rush flag from new call sites.</summary>
        public static float ComposeFireMul(bool rushOpen, bool barrageWindow, float fieldMul)
        {
            float zone = barrageWindow ? BarrageFireMul : 1f;
            if (fieldMul > zone) zone = fieldMul;
            // rushOpen ignored for fire rate (Feed). Kept in the signature so Net/PlayMode
            // call sites compile until they drop the argument.
            return ComposeFireMul(1f, zone);
        }

        public static float Radius(BoarderAbilityId id)
        {
            if (id == BoarderAbilityId.Anchor || id == BoarderAbilityId.Barrage) return 8f;
            if (id == BoarderAbilityId.FieldDeploy) return TurretAuraRadius;
            if (id == BoarderAbilityId.Emp) return EmpRadiusMetres;
            return 0f;
        }

        /// <summary>Seconds between turret shots at the given rate multiplier. OVERCHARGE is
        /// the only caller that passes anything but 1.</summary>
        public static float TurretShotInterval(float rateMul)
        {
            float rate = TurretShotsPerSecond * (rateMul > 0f ? rateMul : 1f);
            return rate <= 0f ? 0f : 1f / rate;
        }

        /// <summary>Seconds between Outrider shots. Pairing passes
        /// <see cref="OverchargeTurretRateMul"/> while the turret's special is lifting it.
        /// </summary>
        public static float OutriderShotInterval(float rateMul)
        {
            float rate = OutriderShotsPerSecond * (rateMul > 0f ? rateMul : 1f);
            return rate <= 0f ? 0f : 1f / rate;
        }

        /// <summary>Planar: height does not break pairing. True when the two machines are
        /// inside <see cref="PairingRadiusMetres"/>.</summary>
        public static bool MachinesPaired(float ax, float ay, float az, float bx, float by, float bz)
        {
            float dx = ax - bx;
            float dz = az - bz;
            float r = PairingRadiusMetres;
            return dx * dx + dz * dz <= r * r;
        }

        /// <summary>Overcharge lifts the Outrider to max rate only while paired.
        /// "Maximum" is <see cref="OverchargeTurretRateMul"/> — one number, both machines.
        /// </summary>
        public static float PairedOutriderRateMul(bool turretOvercharged, bool paired)
        {
            return turretOvercharged && paired ? OverchargeTurretRateMul : 1f;
        }

        /// <summary>EMP converts the turret's fire to EMP rounds only while paired.
        /// Rounds keep turret damage and add the root; they do not add a second damage type.
        /// </summary>
        public static bool PairedTurretFiresEmp(bool outriderEmpActive, bool paired)
        {
            return outriderEmpActive && paired;
        }

        /// <summary>True when a body at the target sits inside the EMP blast.</summary>
        public static bool EmpCatches(float ox, float oy, float oz, float tx, float ty, float tz)
        {
            float dx = ox - tx;
            float dy = oy - ty;
            float dz = oz - tz;
            float r = EmpRadiusMetres;
            return dx * dx + dy * dy + dz * dz <= r * r;
        }

        public static string DisplayName(BoarderAbilityId id)
        {
            switch (id)
            {
                case BoarderAbilityId.Anchor: return "Anchor";
                case BoarderAbilityId.Bulkhead: return "Bulkhead";
                case BoarderAbilityId.Ghost: return "Ghost";
                case BoarderAbilityId.Foresight: return "Foresight";
                case BoarderAbilityId.Barrage: return "Dump";
                case BoarderAbilityId.Rush: return "Feed";
                case BoarderAbilityId.Ping: return "Ping";
                case BoarderAbilityId.Cloak: return "Cloak";
                case BoarderAbilityId.FieldDeploy: return "Field Deploy";
                case BoarderAbilityId.Overcharge: return "Overcharge";
                case BoarderAbilityId.Emp: return "Outrider";
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
            EndWindowEarly(0f);
        }

        /// <summary>Close the window early, refunding each UNSPENT window second against the
        /// cooldown at <paramref name="refundPerSecond"/> (2026-09-04, VEIL's re-entry — see
        /// <see cref="BoarderProtocol.CloakBreakRefundPerSecond"/>). A refund of 0 is the old
        /// behaviour exactly, which is why every other caller keeps the no-argument form.
        /// The cooldown floors at 0 rather than going negative and reading as ready-with-debt.</summary>
        public void EndWindowEarly(float refundPerSecond)
        {
            if (WindowRemaining <= 0f) return;
            float unspent = WindowRemaining;
            WindowRemaining = 0f;
            if (refundPerSecond <= 0f)
            {
                CooldownRemaining = _cooldownDuration;
                return;
            }
            float cd = _cooldownDuration - unspent * refundPerSecond;
            CooldownRemaining = cd > 0f ? cd : 0f;
        }
    }
}
