using System.Collections;
using System.Collections.Generic;
using GatekeepersProtocol.Util;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: client-only additive Ship load while a downed Boarder waits.
    /// Bastion stays loaded and active so NGO objects stay put. Fixture tests no-op because
    /// they are not in the Bastion scene.</summary>
    public static class NetShipWait
    {
        #region Constants
        const string Tag = "Net";
        static readonly string[] CombatCanvasNames =
        {
            "Canvas - Dynamic",
            "Canvas - In Game",
            "Canvas - Pause Menu"
        };
        #endregion

        #region State
        static bool _active;
        static GameObject _rig;
        static HudController _hud;
        static readonly GameObject[] HiddenCanvases = new GameObject[3];
        static readonly List<Behaviour> HiddenBehaviours = new List<Behaviour>(64);
        static readonly List<Renderer> HiddenRenderers = new List<Renderer>(256);
        static Host _host;
        #endregion

        #region Properties
        /// <summary>True on the local client while the additive Ship wait is up.</summary>
        public static bool IsActive => _active;
        #endregion

        #region Public API
        /// <summary>Hide the Bastion view (cameras, listeners, lights, renderers) and the local
        /// rig, then load Ship additively. Does not SetActive NetworkObjects. No-op outside
        /// Bastion so PlayMode fixtures never pull the hub scene.</summary>
        public static void Enter()
        {
            if (_active) return;
            if (!SceneFlow.InCombatScene) return;

            _active = true;
            CaptureAndHideCombatUi();
            HideBastionView();
            HideLocalRig();
            Log.Event(Tag, "ship wait — Bastion view hidden");

            if (!SceneFlow.CanLoad(SceneFlow.ShipCoop))
            {
                Log.Error(Tag, "client down — Ship - CoOp is not in Build Settings");
                return;
            }

            SceneManager.LoadSceneAsync(SceneFlow.ShipCoop, LoadSceneMode.Additive);
            SceneFlow.EnsureEventSystem();
        }

        /// <summary>Unload the additive Ship and restore the Bastion rig and HUD.
        /// Idempotent. HUD restore is synchronous so an end-screen RPC can paint.
        /// The Bastion rig waits until Ship is gone so PlayerRegistry.Local is not the Ship body.</summary>
        public static void Exit()
        {
            if (!_active) return;
            _active = false;

            RestoreCombatUi();

            if (!SceneManager.GetSceneByName(SceneFlow.ShipCoop).IsValid())
            {
                EnableRig();
                return;
            }

            EnsureHost().StartCoroutine(UnloadThenEnableRig());
        }
        #endregion

        #region Private
        static void CaptureAndHideCombatUi()
        {
            _hud = UiBind.First<HudController>();
            if (_hud != null) _hud.enabled = false;

            for (int i = 0; i < CombatCanvasNames.Length; i++)
            {
                var t = UiBind.Root(CombatCanvasNames[i]);
                HiddenCanvases[i] = t != null ? t.gameObject : null;
                UiBind.Show(HiddenCanvases[i], false);
            }
        }

        static void RestoreCombatUi()
        {
            for (int i = 0; i < HiddenCanvases.Length; i++)
            {
                UiBind.Show(HiddenCanvases[i], true);
                HiddenCanvases[i] = null;
            }

            if (_hud != null)
            {
                _hud.enabled = true;
                _hud = null;
            }
        }

        /// <summary>Disable Bastion-scene cameras, listeners, lights, and renderers.
        /// Component disable only — SetActive on a spawned NetworkObject despawns it.</summary>
        static void HideBastionView()
        {
            var scene = SceneManager.GetSceneByName(SceneFlow.BastionCoop);
            if (!scene.IsValid() || !scene.isLoaded) return;

            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null) continue;
                HideBehaviours(root.GetComponentsInChildren<Camera>(true));
                HideBehaviours(root.GetComponentsInChildren<AudioListener>(true));
                HideBehaviours(root.GetComponentsInChildren<Light>(true));
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                for (int r = 0; r < renderers.Length; r++)
                    HideRenderer(renderers[r]);
            }
        }

        static void HideLocalRig()
        {
            var local = PlayerRegistry.Local;
            if (local == null) return;

            _rig = local.gameObject;
            HideBehaviour(local.GetComponent<FirstPersonController>());
            // CharacterController is a Collider, not a Behaviour — TickClientLocal and
            // FirstPersonController.Tick already refuse Move when the wait is active.

            if (!_rig.TryGetComponent<NetworkObject>(out _))
                _rig.SetActive(false);
        }

        static void HideBehaviours(Behaviour[] items)
        {
            for (int i = 0; i < items.Length; i++)
                HideBehaviour(items[i]);
        }

        static void HideBehaviour(Behaviour b)
        {
            if (b == null || !b.enabled) return;
            HiddenBehaviours.Add(b);
            b.enabled = false;
        }

        static void HideRenderer(Renderer r)
        {
            if (r == null || !r.enabled) return;
            HiddenRenderers.Add(r);
            r.enabled = false;
        }

        static void RestoreBastionView()
        {
            for (int i = 0; i < HiddenBehaviours.Count; i++)
            {
                Behaviour b = HiddenBehaviours[i];
                if (b != null) b.enabled = true;
            }
            HiddenBehaviours.Clear();

            for (int i = 0; i < HiddenRenderers.Count; i++)
            {
                Renderer r = HiddenRenderers[i];
                if (r != null) r.enabled = true;
            }
            HiddenRenderers.Clear();
        }

        static Host EnsureHost()
        {
            if (_host != null) return _host;
            var go = new GameObject("NetShipWait");
            Object.DontDestroyOnLoad(go);
            _host = go.AddComponent<Host>();
            return _host;
        }

        static void EnableRig()
        {
            RestoreBastionView();
            if (_rig == null) return;
            _rig.SetActive(true);
            _rig = null;
        }

        static IEnumerator UnloadThenEnableRig()
        {
            var op = SceneManager.UnloadSceneAsync(SceneFlow.ShipCoop);
            if (op != null)
            {
                while (!op.isDone)
                    yield return null;
            }
            EnableRig();
            SceneFlow.EnsureEventSystem();
        }
        #endregion

        #region Nested Types
        sealed class Host : MonoBehaviour { }
        #endregion
    }
}
