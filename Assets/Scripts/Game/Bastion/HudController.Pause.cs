using GatekeeperBastion.Sim;
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
            // Defensive sample: free when the Bastion tick already sampled this frame.
            if (!Controls.Sample().escapeDown) return;

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

            if (_compendium != null && _compendium.IsOpen)
            {
                _compendium.Close();
                return;
            }

            if (IsPauseOpen)
            {
                ClosePause();
                return;
            }

            var gc = _game;
            if (gc != null && (_endPresented
                || (gc.Sim != null
                    && (gc.Sim.Phase == Sim.RunPhase.Lost || gc.Sim.Phase == Sim.RunPhase.Escaped))))
            {
                gc.ReturnToShip();
                return;
            }

            OpenPause();
        }

        /// <summary>Wired from the pause RESUME button (and Esc while paused).</summary>
        public void ResumePause() => ClosePause();

        /// <summary>Wired from the pause Compendium button — reader stays under pause input ownership.</summary>
        public void OpenCompendium()
        {
            if (!IsPauseOpen) OpenPause();
            if (_compendium == null)
            {
                Log.Error(Tag, "CompendiumView not authored on Bastion pause.");
                return;
            }
            _compendium.Open();
        }

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
            var sim = _game != null ? _game.Sim : null;
            if (sim != null)
            {
                var ctx = FightShortcutContext(sim);
                int dialLevel = sim.Assault == AssaultState.Prep ? sim.Siphon.Level : -1;
                FillPauseShortcuts(sim, ctx, dialLevel);
            }
            // Criterion 31: never touch timeScale; fight canvas stays enabled.
        }

        void ClosePause()
        {
            if (!IsPauseOpen) return;
            if (_pauseAnimator == null) ResolvePauseHosts();

            if (_compendium != null && _compendium.IsOpen)
                _compendium.Close();

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

            var sim = _game != null ? _game.Sim : null;
            if (sim != null && sim.Phase == RunPhase.Tethered && _fpc != null
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
            // Authored list (_modals); an entry counts as "open" only while active in hierarchy.
            if (_modals == null) return null;
            for (int i = 0; i < _modals.Length; i++)
            {
                if (_modals[i] != null && _modals[i].gameObject.activeInHierarchy)
                    return _modals[i];
            }
            return null;
        }
    }
}
