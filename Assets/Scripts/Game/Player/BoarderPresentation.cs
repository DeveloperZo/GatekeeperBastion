using GatekeepersProtocol.Sim;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: hide the Boarder's skinned mesh from the local camera (layer
    /// LocalHead on the SkinnedMeshRenderer, not the head bone), and hide all body
    /// renderers while a Ship station panel has movement locked.</summary>
    public sealed class BoarderPresentation : MonoBehaviour
    {
        [SerializeField] Camera _localCamera;
        [SerializeField] Renderer[] _bodyRenderers;

        [SerializeField, OptionalBinding, Tooltip("Isometric rig (Ship probe). When wired, the " +
            "body must stay visible to that camera: the skins keep their authored layer and no " +
            "camera mask is edited. Null means first-person behavior, unchanged.")]
        IsometricCameraRig _isoRig;

        FirstPersonController _fpc;
        int _headLayer = -1;
        bool _hiddenForPanel;

        void Awake()
        {
            _fpc = GetComponent<FirstPersonController>();
            if (_localCamera == null && _fpc != null && _fpc.cameraTransform != null)
                _localCamera = _fpc.cameraTransform.GetComponent<Camera>();
            _headLayer = LayerMask.NameToLayer(WeaponView.LocalHeadLayer);
            // Iso scenes SHOW the local body: skins keep their authored layer and no camera
            // mask is touched (presence of the rig reference is the gate — enabled state can
            // settle after this Awake, and the layer move is a one-shot either way).
            bool isoBody = _isoRig != null;
            // Unity culls by the renderer's GameObject layer, not bone layers. This
            // astronaut is one SkinnedMeshRenderer (Mesh1); putting mixamorig:Head on
            // LocalHead left the helmet in the local camera.
            if (_headLayer >= 0 && !isoBody)
            {
                SkinnedMeshRenderer[] skins = GetComponentsInChildren<SkinnedMeshRenderer>(true);
                for (int i = 0; i < skins.Length; i++)
                {
                    if (skins[i] == null) continue;
                    skins[i].gameObject.layer = _headLayer;
                }
            }
            if (_localCamera != null && _headLayer >= 0 && !isoBody)
                _localCamera.cullingMask &= ~(1 << _headLayer);
            if (_bodyRenderers == null || _bodyRenderers.Length == 0)
                _bodyRenderers = CollectBodyRenderers();
        }

        /// <summary>Hide the body while a Ship station panel is open. Pause on the Bastion
        /// must not call this — zoom and the view-model stay.</summary>
        public void SetStationHidden(bool hidden)
        {
            if (hidden == _hiddenForPanel) return;
            _hiddenForPanel = hidden;
            if (_bodyRenderers == null) return;
            for (int i = 0; i < _bodyRenderers.Length; i++)
            {
                Renderer r = _bodyRenderers[i];
                if (r == null) continue;
                r.enabled = !hidden;
            }
        }

        /// <summary>Boarder skins and weapon meshes. Skip the Player root so SetStationHidden
        /// cannot re-enable a capsule renderer that used to sit around the camera (the mesh
        /// is gone; the skip stays in case a leftover root renderer appears).</summary>
        Renderer[] CollectBodyRenderers()
        {
            Renderer[] all = GetComponentsInChildren<Renderer>(true);
            int count = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                if (all[i].gameObject == gameObject) continue;
                count++;
            }
            Renderer[] body = new Renderer[count];
            int w = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                if (all[i].gameObject == gameObject) continue;
                body[w++] = all[i];
            }
            return body;
        }
    }
}
