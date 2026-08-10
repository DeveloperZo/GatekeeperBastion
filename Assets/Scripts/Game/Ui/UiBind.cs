using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GatekeeperBastion.Game.Ui
{
    /// <summary>Lookup into the authored Shift hierarchy by name or component type, and the
    /// small set of writes a bound view is allowed to make: text, visibility, slider value.
    /// Resolution only — constructing UI is M35 criterion 1's forbidden path, so this class
    /// neither allocates scene objects nor attaches components.</summary>
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

        public static void SetText(TMP_Text label, string text)
        {
            if (label != null) label.text = text ?? string.Empty;
        }

        /// <summary>Writes the text and hides the label when it is empty.</summary>
        public static void SetTextOrHide(TMP_Text label, string text)
        {
            if (label == null) return;
            label.text = text ?? string.Empty;
            label.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        public static void Show(GameObject go, bool visible)
        {
            if (go != null && go.activeSelf != visible) go.SetActive(visible);
        }

        public static void Show(Component c, bool visible)
        {
            if (c != null) Show(c.gameObject, visible);
        }

        /// <summary>Drives a Shift bar. Criterion 10: the slider is display-only, so
        /// <c>interactable</c> is cleared here rather than trusted to the scene.</summary>
        public static void SetBar(Slider slider, float value, float max)
        {
            if (slider == null) return;
            if (slider.interactable) slider.interactable = false;
            slider.minValue = 0f;
            slider.maxValue = max > 0f ? max : 1f;
            slider.SetValueWithoutNotify(Mathf.Clamp(value, 0f, slider.maxValue));
        }

        /// <summary>Fades a block that is not currently relevant instead of hiding it, so the
        /// HUD keeps its shape. Falls back to activation when there is no CanvasGroup.</summary>
        public static void SetRelevant(GameObject block, CanvasGroup group, bool relevant, float dimAlpha)
        {
            if (group == null)
            {
                Show(block, relevant);
                return;
            }
            Show(block, true);
            group.alpha = relevant ? 1f : dimAlpha;
            group.interactable = relevant;
            group.blocksRaycasts = relevant;
        }
    }
}
