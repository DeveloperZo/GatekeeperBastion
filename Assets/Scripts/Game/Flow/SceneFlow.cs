using GatekeeperBastion.Util;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: static scene-name registry and loaders for the game flow:
    /// Splash → MainMenu → Ship (walkable hub) → Bastion (the fight).</summary>
    /// <remarks>
    /// Every loader returns bool and logs its outcome. A scene missing from the build settings
    /// makes SceneManager.LoadScene throw, which used to strand the player on the lose screen
    /// with no feedback ("never allowed me to properly respawn"); now the failure is logged as
    /// an error and the caller can recover instead of soft-locking.
    /// </remarks>
    public static class SceneFlow
    {
        const string Tag = "SceneFlow";

        /// <summary>Boot splash scene name.</summary>
        public const string Splash = "Splash";

        /// <summary>Main menu scene name.</summary>
        public const string MainMenu = "MainMenu";

        /// <summary>Walkable ship-hub scene name
        /// (stations: Armory / Fabrication / Nav Map / Crew Quarters).</summary>
        public const string Ship = "Ship";

        /// <summary>Combat scene name (the bastion run).</summary>
        public const string Bastion = "Bastion";

        /// <summary>In-fiction name of the v0 bastion, for the nav map and the run-start log line.</summary>
        public const string BastionName = "ALPHA-7";

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
        /// <remarks>Verified against SceneBuilder: the bastion is saved as
        /// Assets/Scenes/Bastion.unity, so the active scene name in play mode is exactly
        /// "Bastion" — including when that scene is opened and played directly in the editor.
        /// PlayerGun additionally treats "a live GameController is in the scene" as a
        /// direct-play fallback, so a renamed or duplicated combat scene still fires.</remarks>
        public static bool InCombatScene => ActiveSceneName == Bastion;

        /// <summary>True when the named scene is registered in the build settings and loadable.</summary>
        public static bool CanLoad(string sceneName) =>
            !string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName);

        /// <summary>Load the main menu scene. Returns false (and logs an error) if it cannot load.</summary>
        public static bool LoadMainMenu() => Load(MainMenu);

        /// <summary>Load the ship hub scene. Returns false (and logs an error) if it cannot load.</summary>
        public static bool LoadShip() => Load(Ship);

        /// <summary>Load the bastion combat scene. Returns false (and logs an error) if it cannot load.</summary>
        public static bool LoadBastion() => Load(Bastion);

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

        static bool Load(string sceneName)
        {
            if (!CanLoad(sceneName))
            {
                // Guard trip: LoadScene would throw here and strand the caller (lose-screen soft-lock).
                Log.Error(Tag, $"Cannot load scene '{sceneName}' — it is not in the build settings. " +
                               "Run GatekeeperBastion/Build All Scenes to register Splash, MainMenu, Ship, Bastion.");
                return false;
            }

            // Deliberately NOT an Event: Effects' sceneLoaded hook logs the single scene-entry line
            // a moment later, and two lines per transition is the structural noise we removed.
            Log.Info(Tag, $"Scene transition: '{ActiveSceneName}' -> '{sceneName}'.");
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
