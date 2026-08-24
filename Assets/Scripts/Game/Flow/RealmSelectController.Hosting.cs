using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>
    /// The chooser's session panel: OPEN TO FRIENDS, the Relay code, LAN fallback, and the
    /// host start. Partial for the same reason GameController is — one screen, one
    /// MonoBehaviour, more than one concern; a second real type here would be a second
    /// component and a scene edit for a split the Hierarchy has no reason to see.
    /// </summary>
    public sealed partial class RealmSelectController
    {
        RelayJoin.Payload _relay;
        string _joinCode;

        // ---- Open to friends ----

        /// <summary>Ask Relay for a code for the picked realm. Nothing is committed yet: the
        /// realm is entered on TO SHIP, at the same moment ENTER would have entered it, so
        /// backing out of this panel leaves the session exactly as it was found.</summary>
        async void OnOpenToFriends()
        {
            if (_leaving || _busy) return;
            if (!CanEnter())
            {
                Log.Event(Tag, "open refused — " + WhyNotEnter());
                Repaint();
                return;
            }
            if (_shared.IsRemote(_pickedRealm))
            {
                // Hosting a borrowed realm for a party is real but its own change — ENTER
                // opens it solo today (Docs/Milestones — a realm you can open while its
                // owner is offline).
                Log.Event(Tag, "open refused — shared realms open solo with ENTER for now");
                if (_view != null)
                    _view.SetStatus("A friend's realm opens with ENTER — hosting it for a party is not built yet.");
                return;
            }

            // Read the picked realm off disk and apply the multiplayer rule BEFORE Relay is
            // asked for anything. Every FRESH realm is DemoActive with two wheel buys left, so
            // this is the default path, not an edge case — and refusing after the fact meant the
            // player had already copied a code and sent it to a friend who could never use it.
            var picked = SaveService.LoadRealm(_realms[_pickedRealm].Id);
            if (picked != null
                && !DemoProgress.MultiplayerUnlocked(picked.DemoActive, picked.DemoWheelBuysLeft))
            {
                Log.Event("Demo", "multiplayer locked — buy an upgrade first");
                if (_view != null)
                    _view.SetStatus("\"" + _realms[_pickedRealm].Name
                        + "\" is still in the opening — multiplayer unlocks on your first upgrade.");
                return;
            }

            _busy = true;
            _relay = default;
            _joinCode = null;
            if (_view != null)
            {
                _view.SetCode("");
                _view.SetLanVisible(false);
                _view.SetSessionStatus("Requesting join code…");
                _view.ShowSession();
            }

            RelayJoin.Result result = await RelayJoin.TryHost();
            _busy = false;
            if (_view == null) return;
            if (!result.Ok)
            {
                // Relay is down or unreachable. LAN is still a way to play together on one
                // network, so the panel stays up and offers it rather than closing.
                _view.SetSessionStatus(result.Error);
                _view.SetLanVisible(true);
                return;
            }
            _relay = result.Payload;
            _joinCode = result.JoinCode;
            _view.SetCode(_joinCode);
            _view.SetSessionStatus("Send this code to a friend, then go aboard.");
        }

        void OnCopyCode()
        {
            if (string.IsNullOrEmpty(_joinCode)) return;
            GUIUtility.systemCopyBuffer = _joinCode;
            if (_view != null) _view.SetSessionStatus("Copied to clipboard.");
        }

        void OnToShip()
        {
            if (!RelayJoin.HasPayload(_relay) || string.IsNullOrEmpty(_joinCode))
            {
                if (_view != null)
                    _view.SetSessionStatus("No join code yet — wait, or host on LAN.");
                return;
            }
            StartHosting(relay: true);
        }

        void OnHostLan()
        {
            StartHosting(relay: false);
        }

        /// <summary>Back out without having hosted. The realm was never entered, so there is
        /// nothing to undo.</summary>
        void OnSessionBack()
        {
            if (_busy) return;
            _relay = default;
            _joinCode = null;
            if (_view != null) _view.ShowChooser();
        }

        /// <summary>Enter the picked realm, then start the session in it. The order is the
        /// point: the realm is chosen before anyone is hosting it, so "which world are we in"
        /// is answered by the player rather than by whichever one was opened last.</summary>
        async void StartHosting(bool relay)
        {
            if (_leaving || _busy) return;
            var existing = NetSession.FindLive();
            if (existing != null && existing.Active)
            {
                if (_view != null) _view.SetSessionStatus("Already hosting — leave that session first.");
                Log.Warn(Tag, "host refused — session already active.");
                return;
            }
            if (!CanEnter())
            {
                if (_view != null) _view.SetSessionStatus(WhyNotEnter());
                return;
            }

            string realmId = _realms[_pickedRealm].Id;
            string boarderId = _boarders[_pickedBoarder].Id;

            // A shared realm is opened under the lock even by its owner — hosting it while a
            // member has it open elsewhere would fork the history the lock exists to keep single.
            _busy = true;
            string refusal = await RealmSelectShared.AcquireIfShared(realmId);
            _busy = false;
            if (refusal != null)
            {
                Log.Event(Tag, "host refused — " + refusal);
                if (_view != null) _view.SetSessionStatus(refusal);
                return;
            }

            if (!GameSession.EnterRealm(realmId, boarderId))
            {
                if (_view != null)
                    _view.SetSessionStatus("That realm could not be opened. Nothing was changed.");
                Refresh();
                return;
            }

            if (!DemoProgress.MultiplayerUnlocked(GameSession.Meta))
            {
                if (_view != null)
                    _view.SetSessionStatus("Multiplayer unlocks after your first upgrade — play the opening solo.");
                Log.Event("Demo", "multiplayer locked — buy an upgrade first");
                return;
            }

            _leaving = true;
            NetJoinIntent.LastError = null;
            NetJoinIntent.Clear();
            FrontEndMusic.Stop();

            var session = NetSession.CreateBootstrap();
            if (session == null)
            {
                _leaving = false;
                if (_view != null) _view.SetSessionStatus("Net session bootstrap missing.");
                return;
            }
            if (relay) session.StartHostRelay(_relay, _joinCode);
            else session.StartHost();
            if (!session.Active)
            {
                _leaving = false;
                if (_view != null) _view.SetSessionStatus("Host start failed.");
                return;
            }
            if (!session.LoadLobbyShip())
            {
                _leaving = false;
                if (_view != null) _view.SetSessionStatus("The co-op Ship scene would not load.");
                return;
            }
            if (relay)
            {
                // Hosting over Relay is what shares a realm: the first upload happens here,
                // and each guest that connects is admitted. Failure is logged and the party
                // still plays — sharing simply has not happened yet. LAN never shares.
                string shareRefusal = await RealmShareService.EnsureSharedForHost();
                if (shareRefusal != null)
                    Log.Warn(Tag, "sharing skipped — " + shareRefusal);
            }
        }
    }
}
