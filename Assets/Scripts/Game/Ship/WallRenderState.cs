using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>How a camera-facing wall group is drawn under the isometric camera. Side kind
    /// (Wall / Door / Window / None) is what a face DOES; this is only how it is drawn.</summary>
    public enum WallRenderMode
    {
        Full,
        Half,
        None
    }

    /// <summary>Render state for one wall group. RENDERERS ONLY — collision lives on separate
    /// invisible graybox objects, so this class must never touch a Collider and never
    /// GameObject.SetActive (deactivating would take child state with it and open the wall to
    /// gunfire). Shots over a "hidden" wall must still hit the full-height collider.</summary>
    public sealed class WallRenderState : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Tooltip("Full-height wall renderers; drawn only in Full mode.")]
        Renderer[] _fullPieces;

        [SerializeField, Tooltip("Half-height wall renderers; drawn only in Half mode.")]
        Renderer[] _halfPieces;

        [SerializeField, Tooltip("How this group draws. Authored default is Half (Reset).")]
        WallRenderMode _mode;

        #endregion

        #region Properties

        /// <summary>The mode currently applied to the renderers.</summary>
        public WallRenderMode Mode => _mode;

        #endregion

        #region Unity Lifecycle

        void Reset()
        {
            _mode = WallRenderMode.Half;
        }

        void OnEnable()
        {
            Apply();
        }

        void OnValidate()
        {
            // Play mode only: an edit-mode apply would dirty renderer enabled state in the
            // saved scene, turning an inspector glance into an unintended scene diff.
            if (!Application.isPlaying) return;
            Apply();
        }

        #endregion

        #region Public API

        /// <summary>Set and apply the render mode. Idempotent; no per-frame caller exists.</summary>
        public void SetMode(WallRenderMode mode)
        {
            _mode = mode;
            Apply();
        }

        #endregion

        #region Private

        void Apply()
        {
            SetEnabled(_fullPieces, _mode == WallRenderMode.Full);
            SetEnabled(_halfPieces, _mode == WallRenderMode.Half);
        }

        static void SetEnabled(Renderer[] pieces, bool on)
        {
            if (pieces == null) return;
            for (int i = 0; i < pieces.Length; i++)
            {
                Renderer r = pieces[i];
                if (r == null) continue;
                if (r.enabled != on) r.enabled = on;
            }
        }

        #endregion
    }
}
