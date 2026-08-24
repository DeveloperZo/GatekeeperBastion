using GatekeepersProtocol.Util;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: static scene-name registry and loaders for the game flow:
    /// Splash → MainMenu → Ship hub → Bastion (the fight). The hub is two scenes:
    /// <see cref="Ship"/> (demo / solo) and <see cref="ShipCoop"/> (party lobby).</summary>
    /// <remarks>
    /// Every loader returns bool and logs its outcome. A scene missing from the build settings
    /// makes SceneManager.LoadScene throw, which used to strand the player on the lose screen
    /// with no feedback ("never allowed me to properly respawn"); now the failure is logged as
    /// an error and the caller can recover instead of soft-locking.
    /// </remarks>
    public static class SceneFlow
    {
        const string Tag = "SceneFlow";

        /// <summary>Boot splash scene (slot 0): title card only, auto-advances to
        /// <see cref="MainMenu"/>. Owned by <see cref="SplashController"/>.</summary>
        public const string Splash = "Splash";

        /// <summary>Campaign-menu scene. Owns the Shift Main Menu Canvas
        /// (Continue / Start / Host / Join / Options / Quit). START NEW opens
        /// <see cref="RealmSelect"/>; CONTINUE re-enters the realm played last. Both, and
        /// Host/Join, land on <see cref="ShipCoop"/> — a realm's ship is the co-op hull
        /// whichever way you reached it. Bastion is EMBARK from the nav map.</summary>
        public const string MainMenu = "MainMenu";

        /// <summary>Realm chooser. Stands between the menu and a ship hub: a session is a
        /// Boarder paired with a realm, and this is where that pair is chosen
        /// (Docs/Milestones — the realm and the Boarder).</summary>
        public const string RealmSelect = "RealmSelect";

        /// <summary>The first-Bastion tutorial's ship hub
        /// (<c>Assets/Scenes/Ship - Demo.unity</c>). No longer the destination of normal play:
        /// realms open <see cref="ShipCoop"/>. Two things reach it: the realm chooser's DEMO
        /// button, which is the alternate way in, and the F12 dev checkpoints, which build a
        /// demo beat directly.</summary>
        public const string Ship = "Ship - Demo";

        /// <summary>A realm's ship hub (<c>Assets/Scenes/Ship - CoOp.unity</c>). Named for
        /// the party it can hold, but it is where solo play lives too: the party panel and the
        /// hatch assignment both check for a live session and fall back to a single seat.</summary>
        public const string ShipCoop = "Ship - CoOp";

        /// <summary>Solo / demo combat scene (<c>Assets/Scenes/Bastion.unity</c>).</summary>
        public const string Bastion = "Bastion";

        /// <summary>Co-op combat scene (<c>Assets/Scenes/Bastion_Coop.unity</c>).
        /// <see cref="LoadBastion"/> opens this from <see cref="ShipCoop"/>.</summary>
        public const string BastionCoop = "Bastion_Coop";

        /// <summary>The practice range. Bastion-shaped so a Gatekeeper can think in it — the
        /// brain reads run state off a live controller — but it never draws a Wave
        /// (Docs/Milestones — a range you can fight in).
        ///
        /// The scene is built and in the build settings. What it still has no door: nothing in
        /// Ship - CoOp reaches it, so today it is entered by opening Range.unity directly.</summary>
        public const string Range = "Range";

        /// <summary>In-fiction name of the solo bastion, for the nav map and the run-start log line.</summary>
        public const string BastionName = "ALPHA-7";

        /// <summary>In-fiction name of the co-op layout, so run-start / embark logs name
        /// which combat scene loaded.</summary>
        public const string BastionCoopName = "COOP-1";

        /// <summary>True when <paramref name="sceneName"/> is either ship hub.</summary>
        public static bool IsShipHub(string sceneName) =>
            sceneName == Ship || sceneName == ShipCoop;

        /// <summary>Name of the currently active scene ("" while no scene is valid).</summary>
        public static string ActiveSceneName
        {
            get
            {
                var scene = SceneManager.GetActiveScene();
                return scene.IsValid() ? scene.name : string.Empty;
            }
        }

        /// <summary>True when the active scene is a combat scene (weapons live). The ship hub
        /// is weapons-holstered; PlayerGun gates firing on this.</summary>
        /// <remarks>Combat destinations are Assets/Scenes/Bastion.unity and
        /// Assets/Scenes/Bastion_Coop.unity, so the active name is "Bastion" or "Bastion_Coop"
        /// — including when that scene is opened and played directly in the editor.
        /// PlayerGun additionally treats "a live GameController is in the scene" as a
        /// direct-play fallback, so a renamed or duplicated combat scene still fires.</remarks>
        public static bool InCombatScene =>
            ActiveSceneName == Bastion || ActiveSceneName == BastionCoop;

        /// <summary>Combat scene <see cref="LoadBastion"/> opens: <see cref="BastionCoop"/>
        /// from <see cref="ShipCoop"/>, otherwise <see cref="Bastion"/>.</summary>
        public static string CombatScene =>
            ActiveSceneName == ShipCoop ? BastionCoop : Bastion;

        /// <summary>Fiction name of the combat scene this embark opens, or of the active
        /// combat scene. <see cref="BastionCoopName"/> on <see cref="ShipCoop"/> and in
        /// <see cref="BastionCoop"/>; otherwise <see cref="BastionName"/>.</summary>
        public static string CombatDisplayName =>
            ActiveSceneName == ShipCoop || ActiveSceneName == BastionCoop
                ? BastionCoopName
                : BastionName;

        /// <summary>Campaign menu scene — always MainMenu. Never Ship.</summary>
        public static string FrontEndScene => MainMenu;

        /// <summary>True when the named scene is registered in the build settings and loadable.</summary>
        public static bool CanLoad(string sceneName) =>
            !string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName);

        /// <summary>Go to the campaign menu (MainMenu).</summary>
        public static bool LoadMainMenu() => Load(MainMenu);

        /// <summary>Leave the process. Editor stops Play mode instead of calling
        /// <see cref="Application.Quit"/>, which does nothing there.</summary>
        public static void QuitGame()
        {
            Log.Event(Tag, "Quit.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>Load the solo / demo ship hub. Returns false (and logs an error) if it cannot load.</summary>
        public static bool LoadRealmSelect() => Load(RealmSelect);

        public static bool LoadShip() => Load(Ship);

        /// <summary>Load the party ship hub. Returns false (and logs an error) if it cannot load.</summary>
        public static bool LoadShipCoop() => Load(ShipCoop);

        /// <summary>Load the practice range. Refused while a Run is live:
        /// the range is a practice scene with no Wave and no accounting, so walking into it
        /// from a Run would end that Run without any of the banking a Docked return does, and
        /// the salvage would simply be gone.
        ///
        /// The check is here rather than on the door so a dev command or a checkpoint that
        /// loads the range is refused on the same terms. One scene-wide lookup on a scene
        /// change is not the per-frame Find the architecture rejects.</summary>
        public static bool LoadRange()
        {
            var live = Object.FindFirstObjectByType<GameController>();
            if (live != null && live.Sim != null
                && live.Sim.Phase == GatekeepersProtocol.Sim.RunPhase.Tethered)
            {
                Log.Event("Range", "range refused — a Run is live; dock or lose it first");
                return false;
            }
            return Load(Range);
        }

        /// <summary>Load the bastion combat scene. Returns false (and logs an error) if it cannot load.
        /// Guests in an active friends session must use party EMBARK — local load is refused.</summary>
        public static bool LoadBastion()
        {
            var session = NetSession.FindLive();
            if (session != null && session.Active)
            {
                if (session.IsClientOnly)
                    Log.Event("Net", "embark refused — not party lead");
                else
                    Log.Event("Net", "embark refused — use party EMBARK");
                return false;
            }
            return Load(CombatScene);
        }

        /// <summary>Exactly one EventSystem for uGUI. Shift Canvas variants nest one; scenes may
        /// bake another — keep a single live system and destroy extras.</summary>
        public static void EnsureEventSystem()
        {
            var systems = Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (systems.Length == 0)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();
                return;
            }

            EventSystem keep = null;
            for (int i = 0; i < systems.Length; i++)
            {
                var es = systems[i];
                if (es == null || !es.isActiveAndEnabled) continue;
                if (es.GetComponent<StandaloneInputModule>() == null) continue;
                keep = es;
                break;
            }
            if (keep == null) keep = systems[0];

            for (int i = 0; i < systems.Length; i++)
            {
                var es = systems[i];
                if (es == null || es == keep) continue;
                Object.Destroy(es.gameObject);
            }

            if (keep.GetComponent<StandaloneInputModule>() == null)
                keep.gameObject.AddComponent<StandaloneInputModule>();
        }

        /// <summary>Rescue a Canvas that would render nothing. Shift's Main Menu Canvas is
        /// Screen Space – Camera on purpose, so this only repairs the two always-broken states —
        /// see <see cref="UiCanvasGuard"/>.</summary>
        public static void RepairCanvas(GameObject root)
        {
            if (root == null) return;
            var canvas = root.GetComponent<Canvas>();
            if (canvas == null) canvas = root.GetComponentInChildren<Canvas>(true);
            UiCanvasGuard.RepairIfBroken(canvas != null ? canvas.gameObject : root);
        }

        static bool Load(string sceneName)
        {
            if (!CanLoad(sceneName))
            {
                // Guard trip: LoadScene would throw here and strand the caller (lose-screen soft-lock).
                Log.Error(Tag, $"Cannot load scene '{sceneName}' — it is not in the build settings. " +
                               "Run GatekeepersProtocol/Build/All Scenes to register Splash, MainMenu, " +
                               "RealmSelect, Ship - Demo, Ship - CoOp, Bastion, Bastion_Coop.");
                return false;
            }

            // Deliberately NOT an Event: Effects' sceneLoaded hook logs the single scene-entry line
            // a moment later, and two lines per transition is the structural noise we removed.
            Log.Info(Tag, $"Scene transition: '{ActiveSceneName}' -> '{sceneName}'.");
            if (sceneName == MainMenu || sceneName == Splash)
                LevelMusic.Stop();
            Effects.SuspendForSceneChange(); // no FX GameObjects created into a scene that is unloading
            try
            {
                SceneManager.LoadScene(sceneName);
                return true;
            }
            catch (System.Exception e)
            {
                Effects.Resume();
                Log.Error(Tag, $"LoadScene('{sceneName}') threw {e.GetType().Name}: {e.Message}");
                return false;
            }
        }
    }
}
