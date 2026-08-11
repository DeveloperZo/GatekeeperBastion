using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GatekeeperBastion.Game.Ui
{
    /// <summary>Lookup into the authored Shift hierarchy by name or component type, and the
    /// small set of writes a bound view is allowed to make: text, visibility, slider value,
    /// button label. Resolution only — constructing UI is forbidden (scenes are source),
    /// so this class neither allocates scene objects nor attaches components.</summary>
    /// <remarks>Views serialize their references where a scene author wired them and call these
    /// helpers for whatever is still null, so a view keeps working while the Canvas copy from
    /// <c>Main Menu (Desktop).unity</c> / <c>Simple HUD (Desktop).unity</c> is still landing.</remarks>
    public static class UiBind
    {
        /// <summary>Root GameObject named <paramref name="name"/> in any loaded scene,
        /// active or not. Shift's roots are "Canvas", "Canvas - In Game", "Canvas - Pause Menu".</summary>
        public static Transform Root(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                var scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;
                var roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    if (roots[i] != null && roots[i].name == name) return roots[i].transform;
                }
            }
            return null;
        }

        /// <summary>First root whose name matches any of <paramref name="names"/>, in order.</summary>
        public static Transform AnyRoot(params string[] names)
        {
            if (names == null) return null;
            for (int i = 0; i < names.Length; i++)
            {
                var t = Root(names[i]);
                if (t != null) return t;
            }
            return null;
        }

        /// <summary>Descendant named <paramref name="name"/>, inactive included. Returns
        /// <paramref name="root"/>'s own transform when the name matches it.</summary>
        public static Transform Find(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == name) return all[i];
            }
            return null;
        }

        /// <summary>Descendant matching the first name in <paramref name="names"/> that exists.
        /// Shift reuses "Text", "Bar", "Filled" widely; pass the specific name first.</summary>
        public static Transform FindAny(Transform root, params string[] names)
        {
            if (names == null) return null;
            for (int i = 0; i < names.Length; i++)
            {
                var t = Find(root, names[i]);
                if (t != null) return t;
            }
            return null;
        }

        /// <summary>Component of type <typeparamref name="T"/> on the named descendant,
        /// or on any of its children when the named object does not carry it itself.</summary>
        public static T Get<T>(Transform root, string name) where T : Component
        {
            var t = Find(root, name);
            if (t == null) return null;
            var direct = t.GetComponent<T>();
            return direct != null ? direct : t.GetComponentInChildren<T>(true);
        }

        /// <summary>First <typeparamref name="T"/> under <paramref name="root"/>, inactive included.</summary>
        public static T Get<T>(Transform root) where T : Component =>
            root == null ? null : root.GetComponentInChildren<T>(true);

        /// <summary>First <typeparamref name="T"/> anywhere in the loaded scenes, inactive
        /// included. The way to reach Shift's single managers (MainPanelManager, QualityManager).</summary>
        public static T First<T>() where T : Component =>
            Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        /// <summary>Change-gated text write: an equal value touches
        /// nothing, so per-frame pushes do not dirty the canvas. Returns true when it wrote.</summary>
        public static bool SetText(TMP_Text label, string text)
        {
            if (label == null) return false;
            text = text ?? string.Empty;
            if (label.text == text) return false;
            label.text = text;
            return true;
        }

        /// <summary>Change-gated numeric write for per-frame numbers: formats and writes only
        /// when <paramref name="value"/> differs from <paramref name="cache"/>, so steady state
        /// allocates nothing. Seed the cache with <see cref="long.MinValue"/>.</summary>
        public static bool SetNumber(TMP_Text label, ref long cache, long value,
                                     string prefix = "", string suffix = "")
        {
            if (label == null) return false;
            if (cache == value) return false;
            cache = value;
            label.text = prefix + value + suffix;
            return true;
        }

        /// <summary>Writes the text and hides the label when it is empty. Change-gated.</summary>
        public static void SetTextOrHide(TMP_Text label, string text)
        {
            if (label == null) return;
            SetText(label, text);
            Show(label.gameObject, !string.IsNullOrEmpty(text));
        }

        /// <summary>Writes a button's label through Shift's MainButton / MainPanelButton when
        /// present (they own the visible text) and into the TMP child either way, so plain
        /// Buttons and Shift buttons both read back correctly. The TMP write is change-gated.</summary>
        public static void SetButtonLabel(Button button, string label)
        {
            if (button == null) return;
            string text = label ?? "";
            var main = button.GetComponent<Michsky.UI.Shift.MainButton>();
            if (main != null)
            {
                main.buttonText = text;
                main.useCustomText = false;
            }
            var panel = button.GetComponent<Michsky.UI.Shift.MainPanelButton>();
            if (panel != null)
            {
                panel.buttonText = text;
                panel.useCustomText = false;
            }
            // Shift buttons carry several state labels (normal/highlighted/pressed). An
            // authored button is enabled once, so writing only the first would leave the
            // visible state stale — write them all.
            var tmps = button.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < tmps.Length; i++) SetText(tmps[i], text);
        }

        public static void Show(GameObject go, bool visible)
        {
            if (go != null && go.activeSelf != visible) go.SetActive(visible);
        }

        public static void Show(Component c, bool visible)
        {
            if (c != null) Show(c.gameObject, visible);
        }

        /// <summary>Drives a Shift bar. The slider is display-only, so
        /// <c>interactable</c> is cleared here rather than trusted to the scene.
        /// Change-gated: equal values touch nothing.</summary>
        public static bool SetBar(Slider slider, float value, float max)
        {
            if (slider == null) return false;
            float clampedMax = max > 0f ? max : 1f;
            float clamped = Mathf.Clamp(value, 0f, clampedMax);
            bool changed = false;
            if (slider.interactable) { slider.interactable = false; changed = true; }
            if (slider.minValue != 0f) { slider.minValue = 0f; changed = true; }
            if (slider.maxValue != clampedMax) { slider.maxValue = clampedMax; changed = true; }
            if (slider.value != clamped) { slider.SetValueWithoutNotify(clamped); changed = true; }
            return changed;
        }

        /// <summary>Fades a block that is not currently relevant instead of hiding it, so the
        /// HUD keeps its shape. Falls back to activation when there is no CanvasGroup.
        /// Change-gated: equal values touch nothing.</summary>
        public static void SetRelevant(GameObject block, CanvasGroup group, bool relevant, float dimAlpha)
        {
            if (group == null)
            {
                Show(block, relevant);
                return;
            }
            Show(block, true);
            float alpha = relevant ? 1f : dimAlpha;
            if (group.alpha != alpha) group.alpha = alpha;
            if (group.interactable != relevant) group.interactable = relevant;
            if (group.blocksRaycasts != relevant) group.blocksRaycasts = relevant;
        }
    }
}
