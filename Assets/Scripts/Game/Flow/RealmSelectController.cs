using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>
    /// The realm chooser's rules: what is on disk, which pair is picked, and what happens when
    /// the player creates one or enters one.
    /// </summary>
    /// <remarks>
    /// This screen is the only place a player chooses which world to play in and which Boarder
    /// to take there, which is the whole point of splitting the save
    /// (Docs/Milestones — the realm and the Boarder).
    ///
    /// It refuses out loud. A player with no Boarder, or one who has picked a realm this build
    /// cannot open, is told which of those it is rather than being left pressing a button that
    /// does nothing.
    /// </remarks>
    public sealed class RealmSelectController : MonoBehaviour
    {
        const string Tag = "RealmSelect";

        [SerializeField] RealmSelectView _view;

        RealmRegistry.SaveRow[] _realms;
        RealmRegistry.SaveRow[] _boarders;
        int _pickedRealm = -1;
        int _pickedBoarder = -1;

        RelayJoin.Payload _relay;
        string _joinCode;
        bool _busy;
        bool _leaving;

        void Awake()
        {
            SceneFlow.EnsureEventSystem();
            if (_view == null)
            {
                // Bound in RealmSelect.unity. Searching the scene for one instead would hide a
                // broken binding behind a lookup that happens to succeed today.
                Debug.LogError("[" + Tag + "] is missing serialized reference '_view' "
                    + "(wire it in RealmSelect.unity); the chooser cannot draw.", this);
                return;
            }
            _view.onCreate = OnCreate;
            _view.onEnter = OnEnter;
            _view.onBack = OnBack;
            _view.onDemo = OnDemo;
            _view.onOpen = OnOpenToFriends;
            _view.onCopy = OnCopyCode;
            _view.onToShip = OnToShip;
            _view.onLan = OnHostLan;
            _view.onSessionBack = OnSessionBack;
            _view.onPickRealm = i => { _pickedRealm = i; Repaint(); };
            _view.onPickBoarder = i => { _pickedBoarder = i; Repaint(); };
        }

        void Start()
        {
            // A player who has never opened this screen still has to be able to leave it with a
            // session, so a machine with no Boarder at all gets one rather than a dead end.
            if (RealmRegistry.Boarders().Length == 0)
                RealmRegistry.CreateBoarder(null);
            Refresh();
        }

        void Refresh()
        {
            _realms = RealmRegistry.Realms();
            _boarders = RealmRegistry.Boarders();
            _pickedRealm = PreferredIndex(_realms, RealmRegistry.LastRealmId);
            _pickedBoarder = PreferredIndex(_boarders, RealmRegistry.LastBoarderId);
            Repaint();
        }

        /// <summary>The row to start on: the one played last, else the first that can be opened,
        /// else nothing picked at all.</summary>
        static int PreferredIndex(RealmRegistry.SaveRow[] rows, string preferred)
        {
            if (rows == null || rows.Length == 0) return -1;
            for (int i = 0; i < rows.Length; i++)
                if (rows[i].Loadable && rows[i].Id == preferred) return i;
            for (int i = 0; i < rows.Length; i++)
                if (rows[i].Loadable) return i;
            return -1;
        }

        void Repaint()
        {
            if (_view == null) return;
            _view.SetRealms(_realms, _pickedRealm);
            _view.SetBoarders(_boarders, _pickedBoarder);
            _view.SetEnterEnabled(CanEnter());
            _view.SetOpenEnabled(CanEnter());
            _view.SetStatus(WhyNotEnter());
        }

        bool CanEnter()
        {
            return Picked(_realms, _pickedRealm) && Picked(_boarders, _pickedBoarder);
        }

        static bool Picked(RealmRegistry.SaveRow[] rows, int index)
        {
            return rows != null && index >= 0 && index < rows.Length && rows[index].Loadable;
        }

        /// <summary>Empty when Enter is available; otherwise the one thing standing in the way.</summary>
        string WhyNotEnter()
        {
            if (_realms == null || _realms.Length == 0)
                return "No realms yet — name one and create it.";
            if (!Picked(_realms, _pickedRealm))
                return "Pick a realm. One that cannot be opened was written by another build.";
            if (!Picked(_boarders, _pickedBoarder))
                return "Pick a Boarder to take in.";
            return "";
        }

        void OnCreate()
        {
            string typed = _view != null ? _view.NameText : null;
            var realm = RealmRegistry.CreateRealm(typed);
            if (_view != null) _view.ClearName();
            Refresh();
            // Land the player on the realm they just made rather than wherever the sort put it.
            for (int i = 0; i < _realms.Length; i++)
            {
                if (_realms[i].Id != realm.Id) continue;
                _pickedRealm = i;
                break;
            }
            Repaint();
            if (_view != null) _view.SetStatus("Created \"" + realm.Name + "\".");
        }

        void OnEnter()
        {
            if (!CanEnter())
            {
                Log.Event(Tag, "enter refused — " + WhyNotEnter());
                Repaint();
                return;
            }
            string realmId = _realms[_pickedRealm].Id;
            string boarderId = _boarders[_pickedBoarder].Id;
            if (!GameSession.EnterRealm(realmId, boarderId))
            {
                // SaveService has already named the document and the reason in the log; the
                // player needs the short version, here, next to the button they pressed.
                if (_view != null)
                    _view.SetStatus("That realm could not be opened. Nothing was changed.");
                Refresh();
                return;
            }
            // A realm's ship is the co-op hull, not the demo one. A realm is the world you
            // and anyone you invite share, so it opens on the ship that can hold a party —
            // the alternative is the same realm looking different depending on how you
            // entered it. Solo is fine there: the party panel and the hatch assignment both
            // check for a live session and fall back to one seat.
            if (!SceneFlow.LoadShipCoop())
                Log.Warn(Tag, "realm entered but the co-op Ship scene would not load.");
        }

        void OnBack()
        {
            SceneFlow.LoadMainMenu();
        }

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
        void StartHosting(bool relay)
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
            }
        }

        /// <summary>The alternate way in. The first-Bastion demo keeps its own ship and its own
        /// path: it is a tutorial rather than a world, so it does not belong to a realm and
        /// entering it does not pick one (Docs/Milestones — the realm and the Boarder).</summary>
        void OnDemo()
        {
            if (!SceneFlow.LoadShip())
                Log.Warn(Tag, "demo refused — the demo Ship scene would not load.");
        }
    }
}
