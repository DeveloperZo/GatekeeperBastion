using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: menu and hub frame budget. Fight scenes leave QualitySettings
    /// alone; this only pins the non-fight path so a laptop is not cooked on the menu.</summary>
    public static class FrameBudget
    {
        const int OutsideFightTarget = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Apply()
        {
            Application.targetFrameRate = OutsideFightTarget;
            QualitySettings.vSyncCount = 1;
        }
    }
}
