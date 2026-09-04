using System.Collections.Generic;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>Grants the kit and puts bodies on the lane.
    ///
    /// Every grant goes through <see cref="GameSession.AdoptDevState"/>, which taints the session
    /// BEFORE it swaps the state, so a save cannot be written from here by construction rather
    /// than by care. That is the milestone's stated trap: a tier or a protocol granted for a test
    /// that survived into a played save would make every later economy observation wrong, and
    /// nothing in the game would report it.
    ///
    /// The range state is fresh rather than the player's own: judging a weapon while carrying
    /// whatever you last equipped answers a question about your save rather than about the
    /// weapon (Docs/Milestones — a range you can fight in). Through the door it owns exactly
    /// the kit chosen there — an owned weapon at the realm's points, an unowned one at base —
    /// so the question is about the gun the Boarder would buy or already holds. Opened
    /// directly, with no kit pending, it owns the whole catalogue at full focus as it did
    /// before the door existed (Zo, 2026-08-22: until the game is tuned, everything has to be
    /// reachable without earning it first).</summary>
    public sealed class RangeConsole : MonoBehaviour
    {
        const string Tag = "Range";

        [SerializeField, Tooltip("The range's GameController. Spawns route through its spawner.")]
        GameController _game;

        [SerializeField, Tooltip("The range's rules — cooldown bypass and respawn.")]
        RangeRules _rules;

        [SerializeField, Tooltip("Parent of the RangeMark_<metres> markers.")]
        Transform _marks;

        /// <summary>Bodies this console put on the lane, and the mark each came from, so a
        /// respawn returns to where it was rather than to the nearest one.</summary>
        readonly Dictionary<int, Standing> _standing = new Dictionary<int, Standing>(8);
        readonly List<int> _scratch = new List<int>(8);

        bool _adopted;

        struct Standing
        {
            public EnemyGatekeeper Body;
            public GatekeeperKind Kind;
            public int Metres;
        }

        /// <summary>How many bodies the lane holds at once. A range is for reading one thing at a
        /// time; past this it is a wave, and wave behaviour is what the Bastion is for.</summary>
        public const int LaneCap = 6;

        public int Standing_Count
        {
            get { return _standing.Count; }
        }

        public void Bind(GameController game, RangeRules rules, Transform marks)
        {
            _game = game;
            _rules = rules;
            _marks = marks;
        }

        /// <summary>Adopt the unlocked kit as the lane opens, rather than waiting for the first
        /// grant. EnsureRangeState used to be reached only through a Grant call, so a range you
        /// simply loaded and shot in was running the DEFAULT state — nothing owned, no protocol —
        /// and "everything is unlocked" was only true once you had asked for something.
        ///
        /// Safe in either order against GameController.Start: if this lands first, the
        /// controller's own ApplyMetaEffects picks the new state up; if it lands second, the
        /// ReapplyKit here pushes it onto the rig the controller already built.</summary>
        void Start()
        {
            EnsureRangeState();
            ReapplyKit();
        }

        // ---- Grants ----

        /// <summary>Own the weapon, set its focus ring to a flat tier, and hold it. Tier is
        /// clamped to what the wheel can actually reach — an ability at 0 points and the same
        /// ability at full focus are different abilities, which is why the dial is settable at
        /// all (criterion 3).</summary>
        public void GrantWeapon(WeaponId id, int tier)
        {
            if (!WeaponCatalog.IsAvailable(id))
            {
                Log.Warn(Tag, "grant refused — " + id + " is not a catalogued weapon");
                return;
            }
            MetaState m = EnsureRangeState();
            if (m == null) return;

            int clamped = Mathf.Clamp(tier, 0, WeaponWheel.MaxPurchases);
            m.SetOwned(id, true);
            m.Boarder.ReplacePointsFromFlatTier(id, clamped);
            m.SetEquipped(id);
            Log.Event(Tag, "granted " + id + " at tier " + clamped);
            ReapplyKit();
        }

        /// <summary>Set a protocol, including ones no save has ever unlocked. Veil and Forge
        /// exist in code and cannot be earned yet; the range is where they are judged.
        ///
        /// This SETS rather than buys, and deliberately does not go through
        /// <c>BoarderProtocol.TryClaim</c>. TryClaim is the shop's purchase and carries the
        /// shop's three rules, all of which are wrong here:
        ///
        /// <list type="bullet">
        /// <item>it refuses while a Run holds the loadout lock — and a range is Tethered from its
        /// first frame, so the lock is on the entire time the lane exists. Every cycle after the
        /// first was refused with "BuyProtocol refused — run in progress track=crew";</item>
        /// <item>it refuses a protocol that is already equipped, so cycling back to one you had
        /// would stop working too;</item>
        /// <item>it refuses anything outside the shop catalogue, which is the exact opposite of
        /// what a range is for — the protocols worth judging are the ones not yet on sale.</item>
        /// </list>
        ///
        /// The passive is seeded the same way TryClaim seeds it, so a set protocol arrives with
        /// its first passive rather than with whatever the last one left behind.</summary>
        public void GrantProtocol(BoarderProtocolId id)
        {
            if (id == BoarderProtocolId.None)
            {
                Log.Warn(Tag, "grant refused — None is not a protocol");
                return;
            }
            MetaState m = EnsureRangeState();
            if (m == null) return;

            SetProtocol(m, id);
            Log.Event(Tag, "set protocol " + id);
            ReapplyKit();
        }

        public void GrantPayload(PayloadId id)
        {
            if (!PayloadCatalog.IsCatalogued(id))
            {
                Log.Warn(Tag, "grant refused — " + id + " is not a catalogued payload");
                return;
            }
            MetaState m = EnsureRangeState();
            if (m == null) return;
            m.SetPayloadOwned(id, true);
            m.Boarder.EquippedDrone = id;
            Log.Event(Tag, "granted payload " + id);
            ReapplyKit();
        }

        public void GrantEquipment(EquipmentId id)
        {
            if (!EquipmentCatalog.IsCatalogued(id))
            {
                Log.Warn(Tag, "grant refused — " + id + " is not catalogued equipment");
                return;
            }
            MetaState m = EnsureRangeState();
            if (m == null) return;
            m.Boarder.EquipmentSlotPurchased = true;
            m.SetEquipmentOwned(id, true);
            m.Boarder.SetEquippedEquipment(id);
            Log.Event(Tag, "granted equipment " + id);
            ReapplyKit();
        }

        /// <summary>The range's own state, adopted once: fresh rather than copied from the
        /// player. Through the door it owns exactly the kit chosen there
        /// (<see cref="GameSession.PendingRangeLoadout"/>, consumed here); opened directly it
        /// owns the whole catalogue as it always has, so nothing regresses.</summary>
        MetaState EnsureRangeState()
        {
            if (_adopted) return GameSession.Meta;

            var m = MetaState.CreateNew();
            // Not a tutorial. DemoActive would gate stations and lock multiplayer, neither of
            // which means anything on a lane.
            m.Realm.DemoActive = false;
            RangeLoadout? pending = GameSession.PendingRangeLoadout;
            GameSession.PendingRangeLoadout = null;
            if (pending.HasValue) AdoptLoadout(m, pending.Value);
            else UnlockEverything(m);
            GameSession.AdoptDevState(m);
            _adopted = true;
            Log.Event(Tag, pending.HasValue
                ? "range kit adopted — " + pending.Value.Summary()
                : "range kit adopted — everything unlocked, dev-tainted, will not save");
            EnterPractice();
            return GameSession.Meta;
        }

        /// <summary>The practice rule goes on with the kit, on both paths — through the door
        /// and opened directly — so a range wave pays nothing and the Hatch is out of the
        /// fight from the first frame. The sim exists from the controller's Awake, so this
        /// is safe in either Start order.</summary>
        void EnterPractice()
        {
            var game = _game;
            if (game == null || game.Sim == null)
            {
                Log.Warn(Tag, "practice rule not set — no sim to set it on");
                return;
            }
            game.Sim.EnterPracticeMode();
        }

        /// <summary>Own the two weapons at the points the door carried — the realm's own for
        /// an owned weapon, base for one it did not own — and set the protocol. The payload,
        /// the rig and the wallet are issued as they are for the unlocked kit: the door chose
        /// the kit, and the lane still has to refuse nothing for price.</summary>
        static void AdoptLoadout(MetaState m, RangeLoadout kit)
        {
            m.SetOwned(kit.Primary, true);
            m.ReplacePoints(kit.Primary, kit.PrimaryPoints);
            m.SetEquipped(kit.Primary);
            m.SetOwned(kit.Secondary, true);
            m.ReplacePoints(kit.Secondary, kit.SecondaryPoints);
            m.SetEquipped(kit.Secondary);
            SetProtocol(m, kit.Protocol);
            IssueStandardKit(m);
        }

        /// <summary>Own the catalogue outright.
        ///
        /// Nothing on a lane should refuse for want of a purchase — the point is to reach any
        /// weapon, protocol, payload or piece of equipment and feel it, so every grant below is
        /// ownership rather than a transaction. Weapons arrive at FULL focus because an ability
        /// at zero points and the same ability maxed are different abilities; the door's panel
        /// and the F7 loadout picker dial that back down when the flat comparison is what you want.
        ///
        /// This runs on state that <see cref="GameSession.AdoptDevState"/> is about to taint, so
        /// none of it can reach a save.</summary>
        static void UnlockEverything(MetaState m)
        {
            foreach (WeaponId id in WeaponCatalog.All())
            {
                m.SetOwned(id, true);
                // Disabled and test-only rows are OWNED but stay unequippable — GrantWeapon still
                // asks IsAvailable. Owning them costs nothing and keeps "unlocked" honest.
                if (WeaponCatalog.IsAvailable(id))
                    m.Boarder.ReplacePointsFromFlatTier(id, WeaponWheel.MaxPurchases);
            }
            // Something in hand on arrival, so the lane is shootable before touching a menu.
            m.SetEquipped(WeaponId.Mk1Sidearm);

            // A protocol from the first frame, so the ability bar has something to show before
            // anything is cycled. Set, not claimed — see GrantProtocol for why.
            SetProtocol(m, BoarderProtocolId.Depot);
            IssueStandardKit(m);
        }

        /// <summary>Set, not claimed, with the passive seeded the way TryClaim seeds it.</summary>
        static void SetProtocol(MetaState m, BoarderProtocolId id)
        {
            m.EquippedProtocol = id;
            m.EquippedPassive = BoarderProtocol.PassiveCount(id) > 0
                ? BoarderProtocol.PassiveAt(id, 0)
                : BoarderAbilityId.None;
        }

        /// <summary>The Decoy, the equipment slot with the Repair Rig, and the wallet — issued
        /// to every range kit, chosen or unlocked.</summary>
        static void IssueStandardKit(MetaState m)
        {
            m.SetPayloadOwned(PayloadId.Decoy, true);
            m.Boarder.EquipmentSlotPurchased = true;
            m.SetEquipmentOwned(EquipmentId.RepairRig, true);

            // Salvage for anything that still charges. Earned is raised with it: RealmState keeps
            // the ledger invariant earned == banked + spent, and a wallet conjured without it
            // would put the range's own state permanently out of balance.
            m.Realm.BankedSalvage = RangeSalvage;
            m.Realm.LifetimeSalvageEarned = RangeSalvage;
        }

        /// <summary>Enough that nothing on the lane ever refuses for price.</summary>
        const long RangeSalvage = 1000000L;

        /// <summary>Push the changed kit onto the live rig. Without this a grant sits in the
        /// state and the weapon in your hands is still the last one.</summary>
        void ReapplyKit()
        {
            var game = _game;
            if (game == null)
            {
                Log.Warn(Tag, "grant applied to state but no controller to re-equip from");
                return;
            }
            game.ApplyMetaEffects();
        }

        // ---- The lane ----

        /// <summary>Kinds a post can stand. The shipped three — the range exercises the game's
        /// Gatekeepers or it is not measuring the game.</summary>
        static readonly GatekeeperKind[] Kinds =
        {
            GatekeeperKind.Default, GatekeeperKind.Hunter, GatekeeperKind.Thief
        };

        int _kindIndex;

        /// <summary>The kind the next post press stands. One selection for every post, held
        /// here so the posts and the control console read the same thing.</summary>
        public GatekeeperKind SelectedKind
        {
            get { return Kinds[_kindIndex]; }
        }

        public GatekeeperKind CycleKind()
        {
            _kindIndex = (_kindIndex + 1) % Kinds.Length;
            Log.Event(Tag, "range target — " + Kinds[_kindIndex]);
            return Kinds[_kindIndex];
        }

        /// <summary>True while a standing body this console put at <paramref name="metres"/>
        /// is still alive. Read by the posts every frame; the dictionary walk allocates
        /// nothing.</summary>
        public bool HasStanding(int metres)
        {
            foreach (var kv in _standing)
            {
                if (kv.Value.Metres == metres && kv.Value.Body != null) return true;
            }
            return false;
        }

        /// <summary>Everything standing at one mark off the lane, and nothing queued to come
        /// back to it. Wave bodies are the spawner's and are not touched.</summary>
        public void ClearAt(int metres)
        {
            var game = _game;
            _scratch.Clear();
            foreach (var kv in _standing)
            {
                if (kv.Value.Metres == metres) _scratch.Add(kv.Key);
            }
            for (int i = 0; i < _scratch.Count; i++)
            {
                Standing s;
                if (!_standing.TryGetValue(_scratch[i], out s)) continue;
                _standing.Remove(_scratch[i]);
                if (s.Body != null && game != null && game.Spawner != null)
                    game.Spawner.Despawn(s.Body);
            }
            var rules = _rules;
            if (rules != null)
            {
                for (int k = 0; k < Kinds.Length; k++) rules.ForgetBody(Pack(Kinds[k], metres));
            }
            Log.Event(Tag, "range target cleared — " + metres + " m");
        }

        /// <summary>Stand one Gatekeeper of <paramref name="kind"/> at the
        /// <paramref name="metres"/> mark. Null when there is no such mark or the lane is
        /// full.</summary>
        public EnemyGatekeeper SpawnAt(GatekeeperKind kind, int metres)
        {
            var game = _game;
            if (game == null || game.Spawner == null)
            {
                Log.Warn(Tag, "spawn refused — no spawner in this scene");
                return null;
            }
            if (_standing.Count >= LaneCap)
            {
                Log.Event(Tag, "spawn refused — the lane already holds " + LaneCap);
                return null;
            }
            Transform mark = MarkAt(metres);
            if (mark == null)
            {
                Log.Warn(Tag, "spawn refused — no mark at " + metres + " m");
                return null;
            }

            EnemyGatekeeper body = game.Spawner.SpawnForRange(kind, mark);
            if (body == null) return null;
            Track(body, kind, metres);
            return body;
        }

        /// <summary>Everything off the lane, and nothing queued to come back to it.</summary>
        public void ClearLane()
        {
            var game = _game;
            _scratch.Clear();
            foreach (var kv in _standing) _scratch.Add(kv.Key);
            for (int i = 0; i < _scratch.Count; i++)
            {
                Standing s;
                if (!_standing.TryGetValue(_scratch[i], out s)) continue;
                if (s.Body != null && game != null && game.Spawner != null)
                    game.Spawner.Despawn(s.Body);
            }
            _standing.Clear();
            if (_rules != null) _rules.ClearLane();
            Log.Event(Tag, "lane cleared");
        }

        int[] _marksCache;

        /// <summary>The authored marks in metres, nearest first, read once off the
        /// <c>RangeMark_&lt;m&gt;</c> markers. Empty when the scene carries none.</summary>
        public int[] Marks
        {
            get
            {
                if (_marksCache != null) return _marksCache;
                var marks = _marks;
                var list = new List<int>(8);
                if (marks != null)
                {
                    for (int i = 0; i < marks.childCount; i++)
                    {
                        Transform c = marks.GetChild(i);
                        int m;
                        if (c != null && c.name.StartsWith("RangeMark_")
                            && int.TryParse(c.name.Substring("RangeMark_".Length), out m))
                            list.Add(m);
                    }
                }
                list.Sort();
                _marksCache = list.ToArray();
                return _marksCache;
            }
        }

        Transform MarkAt(int metres)
        {
            var marks = _marks;
            if (marks == null) return null;
            string want = "RangeMark_" + metres;
            for (int i = 0; i < marks.childCount; i++)
            {
                Transform c = marks.GetChild(i);
                if (c != null && c.name == want) return c;
            }
            return null;
        }

        void Track(EnemyGatekeeper body, GatekeeperKind kind, int metres)
        {
            int id = body.GetInstanceID();
            _standing[id] = new Standing { Body = body, Kind = kind, Metres = metres };

            var damageable = body.GetComponent<Damageable>();
            if (damageable == null) return;
            // Captured per body: the closure has to know which mark to put THIS one back on.
            int captured = id;
            damageable.onDeath.AddListener(() => OnBodyDown(captured));
        }

        void OnBodyDown(int id)
        {
            Standing s;
            if (!_standing.TryGetValue(id, out s)) return;
            _standing.Remove(id);
            if (_rules == null) return;
            // The queue holds the MARK, not the actor: the body is about to be destroyed, and
            // what comes back is a fresh one standing where that one stood.
            if (_rules.NoteDown(Pack(s.Kind, s.Metres)))
                Log.Event(Tag, "down at " + s.Metres + " m — returning");
        }

        void Update()
        {
            var rules = _rules;
            if (rules == null) return;
            List<int> due = rules.TakeDueRespawns();
            for (int i = 0; i < due.Count; i++)
            {
                GatekeeperKind kind;
                int metres;
                Unpack(due[i], out kind, out metres);
                SpawnAt(kind, metres);
            }
        }

        /// <summary>Kind and distance in one int, because the respawn queue holds ids rather than
        /// actors — the actor that went down is destroyed before it can come back.</summary>
        static int Pack(GatekeeperKind kind, int metres)
        {
            return ((int)kind << 16) | (metres & 0xFFFF);
        }

        static void Unpack(int packed, out GatekeeperKind kind, out int metres)
        {
            kind = (GatekeeperKind)((packed >> 16) & 0xFF);
            metres = packed & 0xFFFF;
        }
    }
}
