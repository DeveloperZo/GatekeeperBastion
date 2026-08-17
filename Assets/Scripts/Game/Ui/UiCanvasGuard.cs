using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>
    /// Rescues a Canvas that would render nothing, and changes nothing else.
    /// Shift's Main Menu Canvas is Screen Space – Camera at 1920×1280 on purpose (measured
    /// from `Main Menu (Desktop).unity`), so forcing Overlay would break its
    /// Background and BlurManager. The two states that are always faults are a
    /// Screen Space – Camera Canvas with no camera, and localScale 0 — which the package
    /// ships on its prefab sources and which kills TMP content and raycasts while some
    /// Image chrome still draws.
    /// </summary>
    public static class UiCanvasGuard
    {
        public static void RepairIfBroken(GameObject host)
        {
            if (host == null) return;
            var canvas = host.GetComponent<Canvas>();
            if (canvas == null) canvas = host.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var t = canvas.transform;
            Vector3 s = t.localScale;
            if (s.x < 0.5f || s.y < 0.5f || s.z < 0.5f)
                t.localScale = Vector3.one;
        }
    }
}
