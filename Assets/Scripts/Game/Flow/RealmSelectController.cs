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

        readonly ArmedAction _armedRealm = new ArmedAction();
        readonly ArmedAction _armedBoarder = new ArmedAction();

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
            _view.onNewBoarder = OnNewBoarder;
            _view.onDeleteRealm = OnDeleteRealm;
            _view.onDeleteBoarder = OnDeleteBoarder;
            _view.onOpen = OnOpenToFriends;
            _view.onCopy = OnCopyCode;
            _view.onToShip = OnToShip;
            _view.onLan = OnHostLan;
            _view.onSessionBack = OnSessionBack;
            _view.onPickRealm = i =>
            {
                _pickedRealm = i;
                // An arming names a document, not a row, but disarming on any pick is the
                // honest reading: the player has moved on from what they were about to delete.
                _armedRealm.Disarm();
                Repaint();
            };
            _view.onPickBoarder = i =>
            {
                _pickedBoarder = i;
                _armedBoarder.Disarm();
                Repaint();
            };
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
            _view.SetDeleteEnabled(Deletable(_realms, _pickedRealm),
                Deletable(_boarders, _pickedBoarder));
            _view.SetDeleteLabels(ArmedName(_realms, _pickedRealm, _armedRealm),
                ArmedName(_boarders, _pickedBoarder, _armedBoarder));
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
            bool written;
            var realm = RealmRegistry.CreateRealm(typed, out written);
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
            if (_view != null)
                _view.SetStatus(written
                    ? "Created \"" + realm.Name + "\"."
                    : "\"" + realm.Name + "\" could not be written to disk — it will not be here next time.");
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
                // Repaint FIRST: Refresh ends in Repaint, which writes WhyNotEnter over the
                // status line, so setting the message before it painted the refusal away — and
                // Refresh also re-picks from the last-played pointer, silently moving the
                // highlight off the realm that just failed onto a different one, where a second
                // press would enter a world the player never chose. Keep their pick.
                Refresh();
                _pickedRealm = IndexOfId(_realms, realmId);
                Repaint();
                // SaveService has already named the document and the reason in the log; the
                // player needs the short version, here, next to the button they pressed.
                if (_view != null)
                    _view.SetStatus("That realm could not be opened. Nothing was changed.");
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

        // ---- Housekeeping ----

        void Update()
        {
            // An arming that has run out must stop claiming it is armed, or the label goes on
            // offering a confirmation the next press would not honour.
            bool a = _armedRealm.TickExpiry(Time.unscaledTime);
            bool b = _armedBoarder.TickExpiry(Time.unscaledTime);
            if (a || b) Repaint();
        }

        void OnNewBoarder()
        {
            string typed = _view != null ? _view.NameText : null;
            bool written;
            var boarder = RealmRegistry.CreateBoarder(typed, out written);
            if (_view != null) _view.ClearName();
            Refresh();
            for (int i = 0; i < _boarders.Length; i++)
            {
                if (_boarders[i].Id != boarder.Id) continue;
                _pickedBoarder = i;
                break;
            }
            Repaint();
            if (_view != null)
                _view.SetStatus(written
                    ? "Created \"" + boarder.Name + "\"."
                    : "\"" + boarder.Name + "\" could not be written to disk — it will not be here next time.");
        }

        /// <summary>First press arms, second deletes. Deleting a realm throws away every run
        /// anyone banked into it, so it is the one action here that asks twice.</summary>
        void OnDeleteRealm()
        {
            if (!Deletable(_realms, _pickedRealm)) return;
            // Arming one destructive button disarms the other. Without this, DELETE REALM stayed
            // armed while the player moved to DELETE BOARDER — which also overwrote the status
            // line — so a press meant for the Boarder's confirmation deleted a realm instead.
            _armedBoarder.Disarm();
            var row = _realms[_pickedRealm];
            string refusal = WhyNotDelete(row.Id, GameSession.CurrentRealmId, "realm");
            if (refusal != null)
            {
                _armedRealm.Disarm();
                Repaint();
                if (_view != null) _view.SetStatus(refusal);
                return;
            }
            if (!_armedRealm.Fire(row.Id, Time.unscaledTime))
            {
                Repaint();
                if (_view != null)
                    _view.SetStatus("Press again to delete \"" + row.Name + "\" — this cannot be undone in game.");
                return;
            }
            bool gone = RealmRegistry.DeleteRealm(row.Id);
            _pickedRealm = -1;
            Refresh();
            if (_view != null)
                _view.SetStatus(gone
                    ? "Deleted \"" + row.Name + "\"."
                    : "That realm could not be deleted. Nothing was changed.");
        }

        void OnDeleteBoarder()
        {
            if (!Deletable(_boarders, _pickedBoarder)) return;
            _armedRealm.Disarm();
            var row = _boarders[_pickedBoarder];
            string refusal = WhyNotDelete(row.Id, GameSession.CurrentBoarderId, "Boarder");
            if (refusal != null)
            {
                _armedBoarder.Disarm();
                Repaint();
                if (_view != null) _view.SetStatus(refusal);
                return;
            }
            if (!_armedBoarder.Fire(row.Id, Time.unscaledTime))
            {
                Repaint();
                if (_view != null)
                    _view.SetStatus("Press again to delete \"" + row.Name + "\" — the kit goes with it.");
                return;
            }
            bool gone = RealmRegistry.DeleteBoarder(row.Id);
            _pickedBoarder = -1;
            // The screen guarantees a Boarder to take in, so deleting the last one mints a
            // fresh one rather than leaving a chooser nobody can leave.
            if (gone && RealmRegistry.Boarders().Length == 0)
            {
                RealmRegistry.CreateBoarder(null);
                Refresh();
                if (_view != null)
                    _view.SetStatus("Deleted \"" + row.Name + "\". A new Boarder was made — you need one to go in.");
                return;
            }
            Refresh();
            if (_view != null)
                _view.SetStatus(gone
                    ? "Deleted \"" + row.Name + "\"."
                    : "That Boarder could not be deleted. Nothing was changed.");
        }

        /// <summary>A row is deletable whether or not this build can open it — a document an
        /// older build wrote is exactly the kind a player wants rid of, and refusing to delete
        /// what we already refuse to load would leave it on the list forever.</summary>
        /// <summary>Row index of a document by id, or -1. Used to hold a selection across a
        /// Refresh that would otherwise re-pick from the last-played pointer.</summary>
        static int IndexOfId(RealmRegistry.SaveRow[] rows, string id)
        {
            if (rows == null || string.IsNullOrEmpty(id)) return -1;
            for (int i = 0; i < rows.Length; i++)
                if (rows[i].Id == id) return i;
            return -1;
        }

        static bool Deletable(RealmRegistry.SaveRow[] rows, int index)
        {
            return rows != null && index >= 0 && index < rows.Length;
        }

        /// <summary>The picked row's name when the delete is armed against that exact
        /// document, else null. Names are not unique — nothing stops two realms being called
        /// ORION — so the arming holds the id and only the label uses the name.</summary>
        static string ArmedName(RealmRegistry.SaveRow[] rows, int index, ArmedAction armed)
        {
            if (!Deletable(rows, index)) return null;
            return armed.Target == rows[index].Id ? rows[index].Name : null;
        }

        /// <summary>Null when the delete may proceed. Deleting what the live session is
        /// standing in would have the next Save write the document straight back, so the
        /// refusal is about honesty as much as safety.</summary>
        static string WhyNotDelete(string id, string openId, string kind)
        {
            var live = NetSession.FindLive();
            if (live != null && live.Active)
                return "Leave the session first — you cannot delete a " + kind + " while playing.";
            if (!string.IsNullOrEmpty(openId) && openId == id)
                return "That " + kind + " is the one this session has open. Go back to the menu first.";
            return null;
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
