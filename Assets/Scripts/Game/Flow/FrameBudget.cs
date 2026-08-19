using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: menu and hub frame budget. The fight scene is left alone; every other
    /// scene is pinned so a laptop is not cooked on the menu.
    /// <remarks>This was a single <c>RuntimeInitializeOnLoadMethod</c>, which fires ONCE per
    /// process and was never re-run, so the 60 fps cap leaked into the Bastion and the class's own
    /// comment was false (2026-08-18 defect). It re-applies per scene load now. It also used to
    /// write <c>QualitySettings.vSyncCount = 1</c>, which clobbered the authored quality level AND
    /// made the 60 target a no-op, because vSync outranks it. That write is gone: the frame cap
    /// alone does the job this class exists for, and vSync belongs to the quality level.</remarks>
    /// </summary>
    public static class FrameBudget
    {
        const int OutsideFightTarget = 60;

        /// <summary>-1 is Unity's "no cap"; the display and the quality level decide.</summary>
        const int NoCap = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded; // idempotent: never double-subscribe
            SceneManager.sceneLoaded += OnSceneLoaded;
            Apply();
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Apply();
        }

        static void Apply()
        {
            Application.targetFrameRate = SceneFlow.InCombatScene ? NoCap : OutsideFightTarget;
        }
    }
}
