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
    public sealed partial class RealmSelectController : MonoBehaviour
    {
        const string Tag = "RealmSelect";

        [SerializeField] RealmSelectView _view;

        RealmRegistry.SaveRow[] _realms;
        RealmRegistry.SaveRow[] _boarders;
        int _pickedRealm = -1;
        int _pickedBoarder = -1;

        /// <summary>The shared-realm half of this screen: which rows are borrowed, and the
        /// store-gated flows in and out of them.</summary>
        readonly RealmSelectShared _shared = new RealmSelectShared();

        readonly ArmedAction _armedRealm = new ArmedAction();
        readonly ArmedAction _armedBoarder = new ArmedAction();

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
                // The remote tail belongs to the picked Boarder, so it changes with the pick.
                // The realm highlight follows its document across the rebuild where it can.
                string keptRealm = _pickedRealm >= 0 && _pickedRealm < _realms.Length
                    ? _realms[_pickedRealm].Id : null;
                RebuildRealmRows();
                _pickedRealm = IndexOfId(_realms, keptRealm);
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
            FrontEndMusic.CaptureOrJoin(null);
        }

        void Refresh()
        {
            _boarders = RealmRegistry.Boarders();
            _pickedBoarder = PreferredIndex(_boarders, RealmRegistry.LastBoarderId);
            RebuildRealmRows();
            _pickedRealm = PreferredIndex(_realms, RealmRegistry.LastRealmId);
            Repaint();
        }

        void RebuildRealmRows()
        {
            _realms = _shared.BuildRows(RealmRegistry.Realms(),
                Picked(_boarders, _pickedBoarder) ? _boarders[_pickedBoarder].Id : null);
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

        async void OnEnter()
        {
            if (_leaving || _busy) return;
            if (!CanEnter())
            {
                Log.Event(Tag, "enter refused — " + WhyNotEnter());
                Repaint();
                return;
            }
            string realmId = _realms[_pickedRealm].Id;
            string boarderId = _boarders[_pickedBoarder].Id;

            if (_shared.IsRemote(_pickedRealm))
            {
                var row = _realms[_pickedRealm];
                _busy = true;
                if (_view != null) _view.SetStatus("Opening \"" + row.Name + "\" from the store…");
                var outcome = await _shared.Enter(row, _pickedRealm, boarderId);
                _busy = false;
                if (outcome.Left) { _leaving = true; return; }
                if (outcome.RowsChanged) { RebuildRealmRows(); _pickedRealm = -1; }
                Repaint();
                if (_view != null && outcome.Status != null) _view.SetStatus(outcome.Status);
                return;
            }

            // An owned realm that has been SHARED opens under the lock, so a member playing
            // in it right now refuses this machine too — the owner is one member among the
            // members where the lock is concerned.
            _busy = true;
            string refusal = await RealmSelectShared.AcquireIfShared(realmId);
            _busy = false;
            if (refusal != null)
            {
                Log.Event(Tag, "enter refused — " + refusal);
                if (_view != null) _view.SetStatus(refusal);
                return;
            }

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
            FrontEndMusic.Stop();
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

            // A remote row is a memory, not a document: DELETE forgets it here. The realm
            // itself lives with its owner, and playing there again remembers it again.
            if (_shared.IsRemote(_pickedRealm))
            {
                string status;
                if (_shared.ArmedForget(_realms[_pickedRealm],
                    Picked(_boarders, _pickedBoarder) ? _boarders[_pickedBoarder].Id : null,
                    _armedRealm, Time.unscaledTime, out status))
                {
                    _pickedRealm = -1;
                    RebuildRealmRows();
                }
                Repaint();
                if (_view != null) _view.SetStatus(status);
                return;
            }

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
        /// <summary>Row index of a document by id, or -1. Holds a selection across a Refresh
        /// that would otherwise re-pick from the last-played pointer.</summary>
        static int IndexOfId(RealmRegistry.SaveRow[] rows, string id)
        {
            if (rows == null || string.IsNullOrEmpty(id)) return -1;
            for (int i = 0; i < rows.Length; i++) if (rows[i].Id == id) return i;
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

        /// <summary>The alternate way in. The first-Bastion demo keeps its own ship and its own
        /// path: it is a tutorial rather than a world, so it does not belong to a realm and
        /// entering it does not pick one (Docs/Milestones — the realm and the Boarder).</summary>
        void OnDemo()
        {
            FrontEndMusic.Stop();
            if (!SceneFlow.LoadShip())
                Log.Warn(Tag, "demo refused — the demo Ship scene would not load.");
        }
    }
}
