using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>
    /// LOCAL-OWNER: keeps a held shared-realm claim alive. One hidden object, created when a
    /// lock is taken, surviving scene loads, beating every <see cref="RealmShare.HeartbeatSeconds"/>
    /// — each beat carries the live realm body, which is the mid-session write-back — and
    /// releasing the lock when the session leaves the realm or the app quits. A crash skips
    /// the release on purpose: that is what the stale window is for.
    /// </summary>
    public sealed class RealmShareBeacon : MonoBehaviour
    {
        const string Tag = "RealmShare";

        static RealmShareBeacon _instance;

        float _nextBeat;
        bool _closing;

        /// <summary>Make sure the beacon exists. Called by the service when a hold installs.</summary>
        public static void Ensure()
        {
            // EditMode has no lifecycle to beat from, and DontDestroyOnLoad refuses outside
            // play — the fixtures drive Beat/Close directly instead.
            if (!Application.isPlaying) return;
            if (_instance != null) return;
            var go = new GameObject("RealmShareBeacon");
            Object.DontDestroyOnLoad(go);
            _instance = go.AddComponent<RealmShareBeacon>();
            Log.Event(Tag, "beacon up — beating every " + RealmShare.HeartbeatSeconds + "s");
        }

        void Awake()
        {
            _nextBeat = Time.unscaledTime + RealmShare.HeartbeatSeconds;
        }

        void Update()
        {
            if (_closing) return;
            if (!RealmShareService.Holding)
            {
                Retire("nothing held");
                return;
            }
            // The session left the realm (menu load clears CurrentRealmId, or another realm
            // was entered): close out and release rather than beating a claim for a world
            // nobody here is standing in.
            if (GameSession.CurrentRealmId != RealmShareService.HeldRealmId)
            {
                _closing = true;
                CloseThenRetire("session left the realm");
                return;
            }
            if (Time.unscaledTime < _nextBeat) return;
            _nextBeat = Time.unscaledTime + RealmShare.HeartbeatSeconds;
            _ = RealmShareService.Beat();
        }

        async void CloseThenRetire(string reason)
        {
            await RealmShareService.CloseAndRelease();
            Retire(reason);
        }

        void Retire(string reason)
        {
            Log.Event(Tag, "beacon down — " + reason);
            if (_instance == this) _instance = null;
            Destroy(gameObject);
        }

        void OnApplicationQuit()
        {
            // Best effort: the write races the process teardown and may not land. If it does
            // not, the claim goes stale and the next opener breaks it — criterion 6's path.
            if (RealmShareService.Holding)
                _ = RealmShareService.CloseAndRelease();
        }
    }
}
