using System.IO;
using System.Text;
using GatekeeperBastion.Game.Ui;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GatekeeperBastion.EditorTools
{
    /// <summary>Copies the working Shift Simple HUD <c>Armor &amp; Health</c> block into Bastion
    /// unchanged — no NormalizeBarRow, no homemade chrome. Source:
    /// <c>Assets/Shift - Complete Sci-Fi UI/Scenes/Simple HUD/URP &amp; HDRP/Simple HUD (Desktop).unity</c>.
    /// Menu: <c>Gatekeeper/HUD/Restore Shift Armor &amp; Health</c>.</summary>
    public static class RestoreShiftArmorHealth
    {
        const string BastionPath = "Assets/Scenes/Bastion.unity";
        const string SamplePath =
            "Assets/Shift - Complete Sci-Fi UI/Scenes/Simple HUD/URP & HDRP/Simple HUD (Desktop).unity";
        const string DynamicCanvas = "Canvas - Dynamic";

        static string LogPath =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "gkb_restore_armor_health_log.txt");

        [MenuItem("Gatekeeper/HUD/Restore Shift Armor & Health")]
        public static void RunFromMenu() => Run();

        public static void Run()
        {
            var log = new StringBuilder();
            log.AppendLine("[RestoreShiftArmorHealth]");

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Finish(log, "REFUSED: Play Mode is active");
                return;
            }
            if (EditorApplication.isCompiling)
            {
                Finish(log, "REFUSED: scripts are compiling");
                return;
            }

            Scene bastion = default;
            bool bastionWasOpen = false;
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                Scene s = EditorSceneManager.GetSceneAt(i);
                if (s.path != BastionPath) continue;
                bastion = s;
                bastionWasOpen = true;
                break;
            }
            if (!bastionWasOpen)
            {
                bastion = EditorSceneManager.OpenScene(BastionPath, OpenSceneMode.Single);
                log.AppendLine("Opened Bastion.unity");
            }
            else
                log.AppendLine("Bastion.unity already open");

            if (!bastion.IsValid() || !bastion.isLoaded)
            {
                Finish(log, "FAIL: could not load Bastion.unity");
                return;
            }

            RectTransform dynamic = FindNamed(bastion, DynamicCanvas);
            var view = Object.FindFirstObjectByType<BastionHudView>(FindObjectsInactive.Include);
            if (dynamic == null || view == null)
            {
                Finish(log, "FAIL: Canvas - Dynamic or BastionHudView missing");
                return;
            }

            Scene sample = EditorSceneManager.OpenScene(SamplePath, OpenSceneMode.Additive);
            if (!sample.IsValid() || !sample.isLoaded)
            {
                Finish(log, "FAIL: could not open sample " + SamplePath);
                return;
            }
            log.AppendLine("Sample opened additive: " + SamplePath);

            try
            {
                RectTransform sampleBlock = FindNamed(sample, "Armor & Health");
                if (sampleBlock == null)
                {
                    Finish(log, "FAIL: Armor & Health not found in sample");
                    return;
                }

                // Hollow Bastion leftovers — Player Status host stays; empty shell and homemade
                // Hud Vitals bars go away so only the Shift block paints.
                DestroyNamedUnder(dynamic, "Armor & Health", log);
                RectTransform hudVitals = FindNamed(bastion, "Hud Vitals");
                if (hudVitals != null)
                {
                    hudVitals.gameObject.SetActive(false);
                    log.AppendLine("Hud Vitals deactivated (homemade bars)");
                }

                RectTransform host = FindNamed(bastion, "Player Status");
                if (host == null)
                {
                    var hostGo = new GameObject("Player Status", typeof(RectTransform));
                    host = hostGo.GetComponent<RectTransform>();
                    host.SetParent(dynamic, false);
                    log.AppendLine("Player Status created under Canvas - Dynamic");
                }
                host.gameObject.SetActive(true);
                host.SetParent(dynamic, false);
                host.anchorMin = Vector2.zero;
                host.anchorMax = Vector2.zero;
                host.pivot = Vector2.zero;
                host.anchoredPosition = new Vector2(24f, 24f);
                host.sizeDelta = new Vector2(350f, 150f);
                host.localScale = Vector3.one;

                var cloneGo = Object.Instantiate(sampleBlock.gameObject, host, false);
                cloneGo.name = "Armor & Health";
                cloneGo.SetActive(true);
                var clone = cloneGo.transform as RectTransform;
                clone.anchorMin = Vector2.zero;
                clone.anchorMax = Vector2.one;
                clone.pivot = new Vector2(0f, 0f);
                clone.anchoredPosition = Vector2.zero;
                clone.sizeDelta = Vector2.zero;
                clone.localScale = Vector3.one;
                log.AppendLine("Cloned Shift Armor & Health under Player Status");

                RectTransform health = FindDeep(clone, "Health");
                RectTransform armor = FindDeep(clone, "Armor");
                if (health == null || armor == null)
                {
                    Finish(log, "FAIL: clone missing Health/Armor rows");
                    return;
                }

                // Game uses Health + Stamina. Sample's second row is Armor — keep the Shift
                // chrome, only rename the label so the controller's STAMINA string matches.
                TMP_Text armorLabel = LabelIn(armor);
                if (armorLabel != null) armorLabel.text = "STAMINA";

                WireVitals(view, host.gameObject, health, armor, log);

                // Strip raycasts on the clone so it does not steal the crosshair.
                foreach (var g in clone.GetComponentsInChildren<Graphic>(true))
                    g.raycastTarget = false;

                EditorSceneManager.MarkSceneDirty(bastion);
                EditorSceneManager.SaveScene(bastion);
                Finish(log, "OK — Shift Armor & Health restored and wired");
            }
            finally
            {
                EditorSceneManager.CloseScene(sample, true);
            }
        }

        static void WireVitals(BastionHudView view, GameObject root, RectTransform health,
            RectTransform stamina, StringBuilder log)
        {
            var so = new SerializedObject(view);
            SetRef(so, "_vitalsRoot", root);
            SetRef(so, "_playerLabel", LabelIn(health));
            SetRef(so, "_playerBar", BarIn(health));
            SetRef(so, "_staminaBlock", stamina.gameObject);
            var group = stamina.GetComponent<CanvasGroup>() ?? stamina.gameObject.AddComponent<CanvasGroup>();
            SetRef(so, "_staminaGroup", group);
            SetRef(so, "_staminaLabel", LabelIn(stamina));
            SetRef(so, "_staminaBar", BarIn(stamina));
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
            log.AppendLine("BastionHudView vitals → Player Status / Armor & Health (Health + Armor-as-STAMINA)");
        }

        static void SetRef(SerializedObject so, string prop, Object value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.objectReferenceValue = value;
        }

        static TMP_Text LabelIn(RectTransform row)
        {
            if (row == null) return null;
            for (int i = 0; i < row.childCount; i++)
            {
                Transform c = row.GetChild(i);
                if (c.name != "Text") continue;
                var tmp = c.GetComponent<TMP_Text>();
                if (tmp != null) return tmp;
            }
            return row.GetComponentInChildren<TMP_Text>(true);
        }

        static Slider BarIn(RectTransform row) =>
            row == null ? null : row.GetComponentInChildren<Slider>(true);

        static void DestroyNamedUnder(RectTransform root, string name, StringBuilder log)
        {
            var doomed = new System.Collections.Generic.List<GameObject>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == null || t.name != name || t == (Transform)root) continue;
                doomed.Add(t.gameObject);
            }
            foreach (var go in doomed)
            {
                log.AppendLine("destroyed hollow: " + PathOf(go.transform));
                Object.DestroyImmediate(go);
            }
        }

        static RectTransform FindNamed(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == name) return t as RectTransform;
                }
            }
            return null;
        }

        static RectTransform FindDeep(RectTransform root, string name)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return t as RectTransform;
            }
            return null;
        }

        static string PathOf(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }

        static void Finish(StringBuilder log, string status)
        {
            log.AppendLine(status);
            File.WriteAllText(LogPath, log.ToString());
            Debug.Log(log.ToString());
        }
    }
}
