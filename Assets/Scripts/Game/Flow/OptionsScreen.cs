using System;
using System.Collections.Generic;
using GatekeeperBastion.Game.Ui;
using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: one options screen for the main menu, Ship and Bastion, presented
    /// through the scene's registered <see cref="OptionsView"/> (M35 criterion 14). Opening
    /// locks movement and frees the cursor; closing restores both. Does not touch
    /// <c>Time.timeScale</c> and does not poll Esc — each scene's dispatcher owns Esc.
    /// Settings auto-apply; display mode arms a 15s revert — closing Options reverts an
    /// unconfirmed change; "Keep this mode" confirms it.</summary>
    public static class OptionsScreen
    {
        const string Tag = "Settings";
        const float DisplayRevertSeconds = 15f;

        // Every context, so the generated Controls rows show the full binding table.
        const ControlContext AllContexts =
            ControlContext.Fight | ControlContext.Prep | ControlContext.Assault |
            ControlContext.NearInteract | ControlContext.Ship | ControlContext.EndScreen;

        static bool _open;
        static FirstPersonController _fpc;
        static bool _capturedLock;
        static CursorLockMode _capturedCursor;
        static bool _capturedVisible;
        static bool[] _dirty;
        static bool _missingViewLogged;
        static readonly List<ControlBinding> _controlScratch = new List<ControlBinding>(24);

        static bool _displayRevertArmed;
        static float _displayRevertDeadline;
        static int _displayRevertPrevious;

        /// <summary>True while the screen is up.</summary>
        public static bool IsOpen => _open;

        /// <summary>Advance display-mode revert countdown (safe to call every frame).</summary>
        public static void TickDisplayRevert()
        {
            if (!_displayRevertArmed) return;
            float left = _displayRevertDeadline - Time.unscaledTime;
            View?.SetDisplayRevertSeconds(left);
            if (left > 0f) return;
            RevertDisplayMode("timeout");
        }

        /// <summary>Open the screen, capturing cursor and <see cref="FirstPersonController.MovementLocked"/>
        /// so close can restore them. No-op when already open.</summary>
        public static void Open(FirstPersonController fpc)
        {
            if (_open) return;
            var view = RequireView();
            if (view == null) return;

            _fpc = fpc;
            if (fpc != null)
            {
                _capturedLock = fpc.MovementLocked;
                fpc.MovementLocked = true;
            }
            else _capturedLock = false;
            _capturedCursor = Cursor.lockState;
            _capturedVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneFlow.EnsureEventSystem();
            EnsureDirtyBuffer();
            Array.Clear(_dirty, 0, _dirty.Length);

            WireView(view);
            view.Open();
            view.RefreshRowValues(BuildRowSpecs());
            view.SetControlRows(BuildControlRows());
            _open = true;
            AudioBuses.ApplyFromSettings();
        }

        /// <summary>Close, log any uncommitted slider changes, restore lock and cursor.
        /// Unconfirmed display-mode changes revert immediately.</summary>
        public static void Close()
        {
            if (!_open) return;
            CommitAllDirty();
            if (_displayRevertArmed) RevertDisplayMode("options closed");
            View?.SetOpen(false);
            if (_fpc != null)
            {
                _fpc.MovementLocked = _capturedLock;
            }
            Cursor.lockState = _capturedCursor;
            Cursor.visible = _capturedVisible;
            _fpc = null;
            _open = false;
        }

        static OptionsView View => OptionsView.Registered;

        /// <summary>The registered view, logging one error (not one per frame) when a scene
        /// ticks the options binding with no <see cref="OptionsView"/> registered.</summary>
        static OptionsView RequireView()
        {
            var view = OptionsView.Registered;
            if (view == null)
            {
                if (!_missingViewLogged)
                {
                    _missingViewLogged = true;
                    Log.Error(Tag,
                        "No OptionsView registered in this scene — author one on the Shift " +
                        "canvas and wire its serialized references (M35 criterion 14).");
                }
                return null;
            }
            _missingViewLogged = false;
            return view;
        }

        static void WireView(OptionsView view)
        {
            view.OnFloatChanged = (rowIndex, value) =>
            {
                var id = (SettingId)rowIndex;
                Settings.SetFloat(id, value);
                _dirty[rowIndex] = true;
                ApplyAudioIfNeeded(id);
            };
            view.OnFloatDragEnd = rowIndex =>
            {
                if (!_dirty[rowIndex]) return;
                LogCommit(SettingIdOf(rowIndex));
                _dirty[rowIndex] = false;
            };
            view.OnBoolChanged = (rowIndex, value) =>
            {
                var id = (SettingId)rowIndex;
                Settings.SetBool(id, value);
                LogCommit(Settings.DeclarationOf(id));
            };
            view.OnIntChanged = (rowIndex, value) =>
            {
                var id = (SettingId)rowIndex;
                if (id == SettingId.DisplayMode)
                    BeginDisplayModeChange(value);
                else
                    Settings.SetInt(id, value);
                LogCommit(Settings.DeclarationOf(id));
            };
            view.OnResetPage = tab =>
            {
                var cat = (SettingCategory)tab;
                Settings.RestoreDefaults(cat);
                if (cat == SettingCategory.Graphics)
                {
                    ConfirmDisplayMode();
                    ApplyDisplayMode(Settings.GetInt(SettingId.DisplayMode));
                }
                if (cat == SettingCategory.Audio) ApplyAllAudio();
                view.RefreshRowValues(BuildRowSpecs());
                for (int i = 0; i < Settings.Declarations.Length; i++)
                {
                    if (Settings.Declarations[i].Category != cat) continue;
                    LogCommit(Settings.Declarations[i]);
                    int idx = (int)Settings.Declarations[i].Id;
                    if (idx >= 0 && idx < _dirty.Length) _dirty[idx] = false;
                }
            };
            view.OnConfirmDisplay = ConfirmDisplayMode;
            view.OnBack = Close;
        }

        static void BeginDisplayModeChange(int mode)
        {
            if (!_displayRevertArmed)
                _displayRevertPrevious = Screen.fullScreen ? 1 : 0;
            Settings.SetInt(SettingId.DisplayMode, mode);
            ApplyDisplayMode(mode);
            _displayRevertArmed = true;
            _displayRevertDeadline = Time.unscaledTime + DisplayRevertSeconds;
            View?.SetDisplayRevertSeconds(DisplayRevertSeconds);
        }

        static void ConfirmDisplayMode()
        {
            _displayRevertArmed = false;
            View?.SetDisplayRevertSeconds(0f);
        }

        static void RevertDisplayMode(string reason)
        {
            if (!_displayRevertArmed) return;
            _displayRevertArmed = false;
            View?.SetDisplayRevertSeconds(0f);
            Settings.SetInt(SettingId.DisplayMode, _displayRevertPrevious);
            ApplyDisplayMode(_displayRevertPrevious);
            if (_open) View?.RefreshRowValues(BuildRowSpecs());
            Log.Event(Tag, $"Display mode reverted to {_displayRevertPrevious} ({reason})");
        }

        static void ApplyDisplayMode(int mode)
        {
            Screen.fullScreen = mode != 0;
        }

        static void ApplyAudioIfNeeded(SettingId id)
        {
            if (id == SettingId.MasterVolume || id == SettingId.MusicVolume || id == SettingId.SfxVolume)
                ApplyAllAudio();
        }

        static void ApplyAllAudio() => AudioBuses.ApplyFromSettings();

        static OptionsRowSpec[] BuildRowSpecs()
        {
            var decls = Settings.Declarations;
            var specs = new OptionsRowSpec[decls.Length];
            for (int i = 0; i < decls.Length; i++)
            {
                var d = decls[i];
                specs[i] = new OptionsRowSpec
                {
                    RowIndex = (int)d.Id,
                    Label = d.Label,
                    Category = (int)d.Category,
                    IsFloat = d.Type == SettingType.Float,
                    IsInt = d.Type == SettingType.Int,
                    FloatValue = d.Type == SettingType.Float ? Settings.GetFloat(d.Id) : 0f,
                    Min = d.Min,
                    Max = d.Max,
                    DefaultFloat = d.DefaultFloat,
                    BoolValue = d.Type == SettingType.Bool && Settings.GetBool(d.Id),
                    DefaultBool = d.DefaultBool,
                    IntValue = d.Type == SettingType.Int ? Settings.GetInt(d.Id) : 0,
                    DefaultInt = d.DefaultInt,
                };
            }
            return specs;
        }

        /// <summary>Controls-tab rows from the single-source binding table (criterion 13) —
        /// hand-authored rows would drift from <c>Controls</c>.</summary>
        static OptionsControlRow[] BuildControlRows()
        {
            Controls.CollectPanelRows(AllContexts, _controlScratch);
            var rows = new OptionsControlRow[_controlScratch.Count];
            for (int i = 0; i < _controlScratch.Count; i++)
            {
                rows[i] = new OptionsControlRow
                {
                    Key = _controlScratch[i].Display,
                    Label = _controlScratch[i].Label,
                };
            }
            return rows;
        }

        static SettingDecl SettingIdOf(int rowIndex)
        {
            var id = (SettingId)rowIndex;
            return Settings.DeclarationOf(id);
        }

        static void CommitAllDirty()
        {
            if (_dirty == null) return;
            for (int i = 0; i < Settings.Declarations.Length; i++)
            {
                var d = Settings.Declarations[i];
                int idx = (int)d.Id;
                if (idx < 0 || idx >= _dirty.Length || !_dirty[idx]) continue;
                LogCommit(d);
                _dirty[idx] = false;
            }
        }

        static void LogCommit(SettingDecl d)
        {
            switch (d.Type)
            {
                case SettingType.Float:
                    Log.Event(Tag, $"{d.Label} = {Settings.GetFloat(d.Id):0.##}");
                    break;
                case SettingType.Bool:
                    Log.Event(Tag, $"{d.Label} = {(Settings.GetBool(d.Id) ? "on" : "off")}");
                    break;
                case SettingType.Int:
                    Log.Event(Tag, $"{d.Label} = {Settings.GetInt(d.Id)}");
                    break;
            }
        }

        static void EnsureDirtyBuffer()
        {
            int n = Enum.GetValues(typeof(SettingId)).Length;
            if (_dirty == null || _dirty.Length != n) _dirty = new bool[n];
        }
    }
}
