using System.Collections;
using GatekeeperBastion.Util;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GatekeeperBastion.Game
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
        static Host _host;
        #endregion

        #region Properties
        /// <summary>True on the local client while the additive Ship wait is up.</summary>
        public static bool IsActive => _active;
        #endregion

        #region Public API
        /// <summary>Hide the Bastion rig and HUD, then load Ship additively. No-op outside
        /// Bastion so PlayMode fixtures never pull the hub scene.</summary>
        public static void Enter()
        {
            if (_active) return;
            if (!SceneFlow.InCombatScene) return;

            _active = true;
            CaptureAndHideCombatUi();

            var local = PlayerRegistry.Local;
            if (local != null)
            {
                _rig = local.gameObject;
                _rig.SetActive(false);
            }

            if (!SceneFlow.CanLoad(SceneFlow.Ship))
            {
                Log.Error(Tag, "client down — Ship is not in Build Settings");
                return;
            }

            SceneManager.LoadSceneAsync(SceneFlow.Ship, LoadSceneMode.Additive);
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

            if (!SceneManager.GetSceneByName(SceneFlow.Ship).IsValid())
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
            if (_rig == null) return;
            _rig.SetActive(true);
            _rig = null;
        }

        static IEnumerator UnloadThenEnableRig()
        {
            var op = SceneManager.UnloadSceneAsync(SceneFlow.Ship);
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
