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
            if (!SceneFlow.LoadShip())
                Log.Warn(Tag, "realm entered but the Ship scene would not load.");
        }

        void OnBack()
        {
            SceneFlow.LoadMainMenu();
        }
    }
}
