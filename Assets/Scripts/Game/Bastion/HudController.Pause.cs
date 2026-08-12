using GatekeeperBastion.Util;
using Michsky.UI.Shift;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>Bastion Esc dispatcher and pause overlay (the Escape contract).
    /// Does not touch Time.timeScale and does not disable the fight canvas.</summary>
    public sealed partial class HudController
    {
        void TickEscape()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            // Priority: open modal -> Settings -> pause -> end Confirm -> open pause.
            var modal = FindOpenModal();
            if (modal != null)
            {
                modal.ModalWindowOut();
                if (_pauseBlur != null) _pauseBlur.BlurOutAnim();
                return;
            }

            if (OptionsScreen.IsOpen)
            {
                OptionsScreen.Close();
                return;
            }

            if (IsPauseOpen)
            {
                ClosePause();
                return;
            }

            var gc = GameController.Instance;
            if (gc != null && gc.Sim != null
                && (gc.Sim.Phase == Sim.RunPhase.Lost || gc.Sim.Phase == Sim.RunPhase.Escaped))
            {
                gc.ReturnToShip();
                return;
            }

            OpenPause();
        }

        /// <summary>Wired from the pause RESUME button (and Esc while paused).</summary>
        public void ResumePause() => ClosePause();

        void OpenPause()
        {
            if (IsPauseOpen) return;
            if (_pauseAnimator == null) ResolvePauseHosts();
            if (_pauseAnimator == null)
            {
                Log.Error(Tag, "Cannot open pause — Animator missing on Canvas - Pause Menu.");
                return;
            }

            if (_fpc == null && Player != null) _fpc = Player.GetComponent<FirstPersonController>();
            if (_fpc != null)
            {
                _pauseCapturedLock = _fpc.MovementLocked;
                _fpc.MovementLocked = true;
            }
            else _pauseCapturedLock = false;

            _pauseCapturedCursor = Cursor.lockState;
            _pauseCapturedVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (_pauseGroup != null)
            {
                _pauseGroup.interactable = true;
                _pauseGroup.blocksRaycasts = true;
            }
            _pauseAnimator.Play("Window In");
            if (_pauseBlur != null) _pauseBlur.BlurInAnim();
            IsPauseOpen = true;
            // Criterion 31: never touch timeScale; fight canvas stays enabled.
        }

        void ClosePause()
        {
            if (!IsPauseOpen) return;
            if (_pauseAnimator == null) ResolvePauseHosts();

            if (_pauseAnimator != null) _pauseAnimator.Play("Window Out");
            if (_pauseBlur != null) _pauseBlur.BlurOutAnim();
            if (_pauseGroup != null)
            {
                _pauseGroup.interactable = false;
                _pauseGroup.blocksRaycasts = false;
            }

            if (_fpc != null) _fpc.MovementLocked = _pauseCapturedLock;
            Cursor.lockState = _pauseCapturedCursor;
            Cursor.visible = _pauseCapturedVisible;
            IsPauseOpen = false;

            var sim = GameController.Instance != null ? GameController.Instance.Sim : null;
            if (sim != null && sim.Phase == Sim.RunPhase.Tethered && _fpc != null
                && !_pauseCapturedLock)
            {
                if (_pauseCapturedCursor == CursorLockMode.Locked)
                {
                    _fpc.SetCursorLock(true);
                    _fpc.MovementLocked = false;
                }
            }
        }

        ModalWindowManager FindOpenModal()
        {
            var modals = FindObjectsByType<ModalWindowManager>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < modals.Length; i++)
            {
                if (modals[i] != null && modals[i].gameObject.activeInHierarchy)
                    return modals[i];
            }
            return null;
        }
    }
}
