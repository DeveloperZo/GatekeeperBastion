// The pixel-art identity, in one component. Renders the camera at a low internal resolution
// and scales it back with point sampling, which is the whole of the read.
//
// Point sampling BOTH ways is what separates this from a downscaled photograph: averaging on
// the way down gives soft, anti-aliased pixels rather than authored ones.
//
// Order matters. This is OnRenderImage and so is PostProcessLayer, and Unity runs them in
// component order, so this must sit BELOW PostProcessLayer on the camera — bloom and tonemap
// resolve at full resolution, then the frame is crushed. Above it, bloom smears across pixel
// blocks and the edges go soft.
//
// Screen Space Overlay canvases are composited after this and are unaffected, so the HUD stays
// crisp — ShipHud projects the station names onto that same canvas. World Space canvases are
// part of the 3D render and pixelate with the room.
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public sealed class PixelRender : MonoBehaviour
    {
        [Tooltip("Internal render height in pixels; width follows the camera's aspect. Below " +
                 "roughly 240 a Gatekeeper silhouette stops reading at range, which is what " +
                 "Fellowship and Expression are paying for. 0 disables without removing.")]
        [Range(0, 1080)] public int verticalResolution = DefaultHeight;

        /// <summary>The value the look converged on. Here rather than only in a field
        /// initializer, because a serialized prefab value silently outranks an initializer.
        /// </summary>
        public const int DefaultHeight = 360;

        void Reset() { verticalResolution = DefaultHeight; }

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            int h = verticalResolution;
            if (h <= 0 || h >= src.height) { Graphics.Blit(src, dst); return; }

            // width from the source rather than Screen, so this is right in the Game view, the
            // Scene view, and any off-screen render
            int w = Mathf.Max(1, Mathf.RoundToInt(h * ((float)src.width / src.height)));

            var small = RenderTexture.GetTemporary(w, h, 0, src.format);

            // the filter on the texture being READ governs sampling, so point mode goes on the
            // source for the downscale and on `small` for the upscale
            var srcFilter = src.filterMode;
            src.filterMode = FilterMode.Point;
            Graphics.Blit(src, small);
            src.filterMode = srcFilter;

            small.filterMode = FilterMode.Point;
            Graphics.Blit(small, dst);

            RenderTexture.ReleaseTemporary(small);
        }
    }
}
