using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.EditorTools
{
    /// <summary>Normalizes Zo's MainMenu authoring: one path per action, centered
    /// Content stack, title centered. Menu or batch via <see cref="NormalizeFromBatch"/>.</summary>
    public static class MainMenuNormalizer
    {
        const string ScenePath = "Assets/Scenes/MainMenu.unity";
        const string ReportPath = "gkb_mainmenu_normalize_report.txt";

        [MenuItem("GatekeeperBastion/M35/Normalize MainMenu")]
        public static void NormalizeFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            NormalizeAndSave();
        }

        /// <summary>Headless entry: -executeMethod GatekeeperBastion.EditorTools.MainMenuNormalizer.NormalizeFromBatch</summary>
        public static void NormalizeFromBatch()
        {
            NormalizeAndSave();
            EditorApplication.Exit(0);
        }

        static void NormalizeAndSave()
        {
            var log = new System.Text.StringBuilder();
            void L(string msg)
            {
                log.AppendLine(msg);
                Debug.Log("[MainMenuNormalizer] " + msg);
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();

            Transform FindDeep(string name)
            {
                foreach (var root in roots)
                {
                    var t = FindRecursive(root.transform, name);
                    if (t != null) return t;
                }
                return null;
            }

            // --- One path: delete alternate Exit / Options chrome ---
            var buttonList = FindDeep("Button List");
            if (buttonList != null && buttonList.parent != null && buttonList.parent.name == "Top Panel")
            {
                Object.DestroyImmediate(buttonList.gameObject);
                L("Deleted Top Panel/Button List (duplicate Exit).");
            }
            else L("SKIP Button List (missing or unexpected parent).");

            foreach (var panelName in new[] { "Home", "Settings" })
            {
                var panel = FindDeep(panelName);
                if (panel == null)
                {
                    L($"SKIP {panelName} (missing).");
                    continue;
                }
                foreach (var chrome in new[] { "Shortcut Keys", "Hotkeys" })
                {
                    var child = panel.Find(chrome);
                    if (child == null) continue;
                    Object.DestroyImmediate(child.gameObject);
                    L($"Deleted {panelName}/{chrome}.");
                }
            }

            // --- Title centered on Top Panel ---
            var title = FindDeep("Title");
            if (title != null && title.parent != null && title.parent.name == "Top Panel")
            {
                var rt = title as RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(720f, 64f);
                    L("Centered Top Panel/Title.");
                }
            }
            else L("SKIP Title center (missing).");

            // --- Home/Content: centered column ---
            var home = FindDeep("Home");
            var content = home != null ? home.Find("Content") as RectTransform : null;
            if (content != null)
            {
                content.anchorMin = new Vector2(0.5f, 0.5f);
                content.anchorMax = new Vector2(0.5f, 0.5f);
                content.pivot = new Vector2(0.5f, 0.5f);
                content.anchoredPosition = Vector2.zero;
                // Wide enough for CONTINUE (250) + gap + campaign strip (~900).
                content.sizeDelta = new Vector2(1200f, 420f);

                var vlg = content.GetComponent<VerticalLayoutGroup>();
                if (vlg != null)
                {
                    vlg.childAlignment = TextAnchor.UpperCenter;
                    vlg.spacing = 12f;
                    vlg.padding = new RectOffset(0, 0, 0, 0);
                    vlg.childControlWidth = false;
                    vlg.childControlHeight = false;
                    vlg.childForceExpandWidth = false;
                    vlg.childForceExpandHeight = false;
                }
                L("Normalized Home/Content rect + VLG UpperCenter.");

                var continueRow = content.Find("Continue Row") as RectTransform;
                if (continueRow != null)
                {
                    continueRow.anchorMin = new Vector2(0.5f, 0.5f);
                    continueRow.anchorMax = new Vector2(0.5f, 0.5f);
                    continueRow.pivot = new Vector2(0.5f, 0.5f);
                    continueRow.anchoredPosition = Vector2.zero;
                    continueRow.sizeDelta = new Vector2(1162f, 70f);

                    var le = continueRow.GetComponent<LayoutElement>();
                    if (le != null)
                    {
                        le.minHeight = 70f;
                        le.preferredHeight = 70f;
                        le.preferredWidth = 1162f;
                    }

                    var hlg = continueRow.GetComponent<HorizontalLayoutGroup>();
                    if (hlg != null)
                    {
                        hlg.childAlignment = TextAnchor.MiddleCenter;
                        hlg.spacing = 24f;
                        hlg.childControlWidth = false;
                        hlg.childControlHeight = false;
                        hlg.childForceExpandWidth = false;
                        hlg.childForceExpandHeight = false;
                    }
                    L("Normalized Continue Row size + HLG MiddleCenter.");
                }
                else L("SKIP Continue Row (missing).");
            }
            else L("SKIP Home/Content (missing).");

            // Keep Main Panels root visible without SplashScreenManager.
            var mainPanels = FindDeep("Main Panels");
            if (mainPanels != null)
            {
                var anim = mainPanels.GetComponent<Animator>();
                if (anim != null) anim.enabled = false;
                var cg = mainPanels.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
                L("Main Panels Animator off, CanvasGroup alpha 1.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            L("Saved " + ScenePath);
            File.WriteAllText(ReportPath, log.ToString());
        }

        static Transform FindRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var hit = FindRecursive(parent.GetChild(i), name);
                if (hit != null) return hit;
            }
            return null;
        }
    }
}
