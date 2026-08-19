using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    public sealed class MapStation : StationTerminal
    {
        public const string PremiseBrief =
            "Gatekeepers hold this garrisoned Bastion. The Hatch is your way back to the Ship.";

        const string Tag = "MapStation";

        [SerializeField] NavMapPanelView _body;

        public override string StationName => "NAV MAP";
        public const string PurposeLine = "where you go, and going there";
        public override string Purpose => PurposeLine;

        protected override string PromptHint
        {
            get
            {
                var m = GameSession.Meta;
                if (m == null) return "embark on the run";
                bool noWeapon = !m.OwnsAnyWeapon;
                bool noRig = !m.HasRepairRigEquipped;
                bool noProtocol = m.EquippedProtocol == BoarderProtocolId.None;
                if (!noWeapon && !noRig && !noProtocol) return "embark on the run";
                if (noWeapon && !noRig && !noProtocol) return "EMBARK — but you have NO WEAPON yet";
                if (!noWeapon && noRig && !noProtocol) return "EMBARK — no repair rig";
                if (!noWeapon && !noRig && noProtocol) return "EMBARK — no protocol";
                string parts = "";
                if (noWeapon) parts = "no weapon";
                if (noRig) parts = parts.Length == 0 ? "no repair rig" : parts + ", no repair rig";
                if (noProtocol) parts = parts.Length == 0 ? "no protocol" : parts + ", no protocol";
                return "EMBARK — " + parts;
            }
        }

        bool _embarking;
        bool _loadFailed;
        bool _mapOpen;
        int _startWave = 1;

        // Demo embark-refuse lines: first play per reason per Ship visit (instance lifetime
        // is the visit — beat 0 refuse table).
        bool _refusedTerminal;
        bool _refusedWeapon;
        bool _refusedIntruders;

        protected override void Awake()
        {
            base.Awake();
            if (_body == null)
                Log.Error(Tag, StationName + ": missing serialized _body.");
        }

        protected override int ContentHash(MetaState m)
        {
            unchecked
            {
                var session = NetSession.FindLive();
                var lobby = session != null ? session.Lobby : null;
                int ready = 0;
                var avatar = NetPlayerAvatar.LocalOwner;
                if (avatar != null && avatar.LobbyReady.Value) ready = 1;
                int counting = lobby != null && lobby.IsCountingDown ? 1 : 0;
                int remain = lobby != null ? (int)(lobby.CountdownRemaining.Value * 10f) : 0;
                return base.ContentHash(m) * 397 ^ _startWave * 17 ^ (_embarking ? 1 : 0) ^ (_loadFailed ? 2 : 0)
                    ^ (_mapOpen ? 1 << 12 : 0) ^ (NetShipWait.IsActive ? 1 << 13 : 0)
                    ^ (ready << 14) ^ (counting << 15) ^ remain;
            }
        }

        protected override void BuildPanel(StationPanelView view, MetaState m)
        {
            if (_body == null) return; // Awake already logged the missing binding.

            _body.BindMap(() => _mapOpen = true, _mapOpen);

            _body.SetDestination($"Bastion {SceneFlow.BastionName}");
            _body.SetPremise(PremiseBrief);

            int deepest = m.DeepestWaveReached < 1 ? 1 : m.DeepestWaveReached;
            if (deepest > WaveDirector.MaxWaves) deepest = WaveDirector.MaxWaves;
            if (_startWave < 1) _startWave = 1;
            if (_startWave > deepest) _startWave = deepest;

            if (NetShipWait.IsActive)
            {
                _body.ShowStartWavePicker(false);
                _body.ShowWarnings(false, false, false);
                _body.BindPartyLobby(false, null, false, null, null);
                _body.BindEmbark("EMBARK", () => TryEmbark(m));
                _body.ShowLoadFailed(false);
                return;
            }

            var session = NetSession.FindLive();
            if (session != null && session.Active)
            {
                BuildPartyPanel(m, session, deepest);
                return;
            }

            _body.BindPartyLobby(false, null, false, null, null);
            _body.ShowStartWavePicker(true);
            _body.SetStartWaveLine(StartWaveLine(_startWave, deepest));
            _body.BindWaveControls(
                $"  {_startWave}  ",
                () => _startWave--, _startWave > 1,
                () => _startWave++, _startWave < deepest);

            if (m.DemoActive && m.DemoShipFightDone)
            {
                _body.ShowWarnings(false, false, false);
                _body.BindEmbark("EMBARK", () => TryEmbark(m));
            }
            else
            {
                _body.ShowWarnings(!m.OwnsAnyWeapon, !m.HasRepairRigEquipped,
                    m.EquippedProtocol == BoarderProtocolId.None);

                bool warned = !m.OwnsAnyWeapon || !m.HasRepairRigEquipped
                    || m.EquippedProtocol == BoarderProtocolId.None;
                _body.BindEmbark(warned ? "EMBARK ANYWAY" : "EMBARK", () => TryEmbark(m));
            }

            _body.ShowLoadFailed(_loadFailed);
        }

        void BuildPartyPanel(MetaState m, NetSession session, int deepest)
        {
            var lobby = session.Lobby;
            bool lead = session.IsPartyLead;
            bool counting = lobby != null && lobby.IsCountingDown;
            var avatar = NetPlayerAvatar.LocalOwner;
            bool ready = avatar != null && avatar.LobbyReady.Value;

            if (lead && !counting)
            {
                _body.ShowStartWavePicker(true);
                _body.SetStartWaveLine(StartWaveLine(_startWave, deepest));
                _body.BindWaveControls(
                    $"  {_startWave}  ",
                    () => _startWave--, _startWave > 1,
                    () => _startWave++, _startWave < deepest);
            }
            else
            {
                _body.ShowStartWavePicker(false);
            }

            int leadWave = lobby != null ? lobby.PendingStartWave.Value : _startWave;
            if (leadWave < 1) leadWave = 1;

            string countdown = null;
            if (counting && lobby != null)
            {
                countdown = "EMBARK in " + lobby.CountdownRemaining.Value.ToString("0.0") + "s";
                // Premise carries countdown so guests see it without an authored _countdownLine.
                _body.SetPremise("Start wave " + leadWave + " — " + countdown);
            }
            else if (!lead)
            {
                _body.SetPremise("Party lead embarks — start wave " + leadWave + ".");
            }

            _body.ShowWarnings(!m.OwnsAnyWeapon, !m.HasRepairRigEquipped,
                m.EquippedProtocol == BoarderProtocolId.None);

            _body.BindPartyLobby(
                showReady: !counting,
                onReady: () => ToggleReady(avatar),
                readyOn: ready,
                onCancel: counting ? CancelPartyCountdown : null,
                countdownLine: countdown);

            if (counting)
            {
                _body.BindEmbark("CANCEL", CancelPartyCountdown);
                _body.ShowLoadFailed(false);
                return;
            }

            if (lead)
            {
                bool warned = !m.OwnsAnyWeapon || !m.HasRepairRigEquipped
                    || m.EquippedProtocol == BoarderProtocolId.None;
                _body.BindEmbark(warned ? "EMBARK ANYWAY" : "EMBARK", () => TryPartyEmbark(m, session));
            }
            else
            {
                // Ready control: optional _readyButton, else the embark button.
                _body.BindEmbark(ready ? "READY OK" : "READY", () => ToggleReady(avatar));
            }

            _body.ShowLoadFailed(_loadFailed);
        }

        void ToggleReady(NetPlayerAvatar avatar)
        {
            var session = NetSession.FindLive();
            if (session == null || session.Lobby == null)
            {
                Log.Event("Net", "ready refused — no party lobby");
                return;
            }
            if (avatar == null)
            {
                Log.Event("Net", "ready refused — no avatar");
                return;
            }
            session.Lobby.SetReady(!avatar.LobbyReady.Value);
        }

        void CancelPartyCountdown()
        {
            var session = NetSession.FindLive();
            if (session == null || session.Lobby == null)
            {
                Log.Event("Net", "cancel refused — no party lobby");
                return;
            }
            session.Lobby.CancelCountdown();
        }

        void TryPartyEmbark(MetaState m, NetSession session)
        {
            if (!session.IsPartyLead)
            {
                Log.Event("Net", "embark refused — not party lead");
                return;
            }
            if (session.Lobby == null)
            {
                Log.Event("Net", "embark refused — no party lobby");
                return;
            }
            GameSession.PendingStartWave = _startWave;
            Log.Event("Ship",
                $"party embark armed for bastion {SceneFlow.BastionName} starting at wave {_startWave} — " +
                $"{m.BankedSalvage} banked, " +
                $"{(m.OwnsAnyWeapon ? LoadoutBrief(m) : "NO WEAPON (improvised cutter only)")}, " +
                $"{(m.HasRepairRigEquipped ? "repair rig equipped" : "NO REPAIR RIG")}");
            session.Lobby.RequestPartyEmbark(_startWave);
        }

        void TryEmbark(MetaState m)
        {
            if (NetShipWait.IsActive)
            {
                var avatar = NetPlayerAvatar.LocalOwner;
                if (avatar == null)
                {
                    Log.Event("Net", "embark refused — no avatar");
                    return;
                }
                avatar.RequestEmbark();
                return;
            }

            var session = NetSession.FindLive();
            if (session != null && session.Active)
            {
                if (!session.IsPartyLead)
                {
                    Log.Event("Net", "embark refused — not party lead");
                    return;
                }
                TryPartyEmbark(m, session);
                return;
            }

            if (_embarking) return;
            if (RefuseDemoEmbark(m)) return;
            if (m.DemoActive && m.TotalRuns == 0)
                Log.Event("Demo", "first embark");
            GameSession.PendingStartWave = _startWave;
            Log.Event("Ship",
                $"embarked for bastion {SceneFlow.BastionName} starting at wave {_startWave} — " +
                $"{m.BankedSalvage} banked, " +
                $"{(m.OwnsAnyWeapon ? LoadoutBrief(m) : "NO WEAPON (improvised cutter only)")}, " +
                $"{(m.HasRepairRigEquipped ? "repair rig equipped" : "NO REPAIR RIG")}");
            _embarking = SceneFlow.LoadBastion();
            _loadFailed = !_embarking;
            if (_loadFailed)
                Log.Error("Ship", "embark refused — Bastion is not in Build Settings / not loadable");
        }

        /// <summary>Beat 0's named EMBARK refuses. One id, three reason strings — copy rides
        /// the Play override; each reason's line plays once per Ship visit, the log always.</summary>
        bool RefuseDemoEmbark(MetaState m)
        {
            switch (DemoProgress.EmbarkRefusal(m))
            {
                case DemoEmbarkRefusal.ShipTerminal:
                    Log.Event("Demo", "embark refused — ship terminal");
                    if (!_refusedTerminal)
                    {
                        _refusedTerminal = true;
                        DemoVoice.Play(DemoConversationIds.EmbarkBlocked, DemoVoiceMode.Lock,
                            "You are not ready. Start the ship terminal first.");
                    }
                    return true;
                case DemoEmbarkRefusal.NoWeapon:
                    // Unreachable on a post-revision save (the terminal's start grants
                    // the MK-1 with the same write) — kept for pre-revision saves.
                    Log.Event("Demo", "embark refused — no weapon");
                    if (!_refusedWeapon)
                    {
                        _refusedWeapon = true;
                        DemoVoice.Play(DemoConversationIds.EmbarkBlocked, DemoVoiceMode.Lock,
                            "You are unarmed. Start the ship terminal.");
                    }
                    return true;
                case DemoEmbarkRefusal.Intruders:
                    Log.Event("Demo", "embark refused — intruders aboard");
                    if (!_refusedIntruders)
                    {
                        _refusedIntruders = true;
                        DemoVoice.Play(DemoConversationIds.EmbarkBlocked, DemoVoiceMode.Lock,
                            "Gatekeepers are still aboard. Clear the ship first.");
                    }
                    return true;
                default:
                    return false;
            }
        }

        public static string StartWaveLine(int start, int deepest) =>
            "START WAVE  " + start + "  /  cleared " + deepest;

        static string LoadoutBrief(MetaState m)
        {
            string p = m.EquippedPrimary == WeaponId.None
                ? "Primary=—"
                : WeaponCatalog.Def(m.EquippedPrimary).displayName;
            string s = m.EquippedSecondary == WeaponId.None
                ? "Secondary=—"
                : WeaponCatalog.Def(m.EquippedSecondary).displayName;
            return $"{p} + {s}";
        }
    }
}
