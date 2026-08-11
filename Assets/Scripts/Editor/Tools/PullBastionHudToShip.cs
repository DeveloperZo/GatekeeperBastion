using System.IO;
using System.Text;
using GatekeeperBastion.Game;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GatekeeperBastion.EditorTools
{
    /// <summary>Copies Bastion's authored corner HUD (Profile / Score / Weapon) into the Ship
    /// always-on canvas via <see cref="Object.Instantiate"/> — same hosts Zo locked for Bastion,
    /// no Vitals, no homemade shortcut strip. Menu:
    /// <c>Gatekeeper/HUD/Pull Bastion Corners Into Ship</c>.</summary>
    public static class PullBastionHudToShip
    {
        const string BastionPath = "Assets/Scenes/Bastion.unity";
        const string ShipPath = "Assets/Scenes/Ship.unity";

        static readonly string[] CornerNames = { "Hud Profile", "Hud Score", "Hud Weapon" };

        static readonly string[] KillNames =
        {
            "Hud Profile", "Hud Score", "Hud Weapon", "Hud Vitals",
            "Shortcut Keys", "Directory", "Title", "Salvage", "Objective",
        };

        static string LogPath =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "gkb_pull_bastion_hud_log.txt");

        [MenuItem("Gatekeeper/HUD/Pull Bastion Corners Into Ship")]
        public static void RunFromMenu() => Run();

        public static void Run()
        {
            var log = new StringBuilder();
            log.AppendLine("[PullBastionHudToShip]");

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

            Scene ship = EnsureOpen(ShipPath, OpenSceneMode.Single, log);
            if (!ship.IsValid() || !ship.isLoaded)
            {
                Finish(log, "FAIL: could not load Ship.unity");
                return;
            }

            Scene bastion = EditorSceneManager.OpenScene(BastionPath, OpenSceneMode.Additive);
            if (!bastion.IsValid() || !bastion.isLoaded)
            {
                Finish(log, "FAIL: could not open Bastion.unity additive");
                return;
            }

            try
            {
                RectTransform shipHud = FindNamed(ship, "Ship HUD");
                var view = Object.FindFirstObjectByType<ShipHudView>(FindObjectsInactive.Include);
                if (shipHud == null || view == null)
                {
                    Finish(log, "FAIL: Ship HUD or ShipHudView missing");
                    return;
                }

                foreach (string name in KillNames)
                    DestroyNamedDirectChild(shipHud, name, log);

                var sources = new RectTransform[CornerNames.Length];
                for (int i = 0; i < CornerNames.Length; i++)
                {
                    sources[i] = FindNamed(bastion, CornerNames[i]);
                    if (sources[i] == null)
                    {
                        Finish(log, "FAIL: Bastion missing " + CornerNames[i]);
                        return;
                    }
                }

                GameObject profile = null, score = null, weapon = null;
                for (int i = 0; i < sources.Length; i++)
                {
                    var src = sources[i];
                    var cloneGo = Object.Instantiate(src.gameObject, shipHud, false);
                    cloneGo.name = CornerNames[i];
                    cloneGo.SetActive(true);
                    CopyRect(src, cloneGo.GetComponent<RectTransform>());
                    StripRaycasts(cloneGo);
                    log.AppendLine("Instantiated " + CornerNames[i] + " from Bastion under Ship HUD");
                    if (i == 0) profile = cloneGo;
                    else if (i == 1) score = cloneGo;
                    else weapon = cloneGo;
                }

                // Decisions lock: Ship positions (Bastion hosts may stretch under Primary).
                PlaceCorner(profile.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(64f, -24f), new Vector2(320f, 64f));
                PlaceCorner(score.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(320f, 72f));
                PlaceCorner(weapon.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f),
                    new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(400f, 150f));

                var reload = FindDeep(weapon.transform, "Reload");
                if (reload != null) reload.gameObject.SetActive(false);

                WireView(view, profile, score, weapon, log);

                EditorSceneManager.MarkSceneDirty(ship);
                EditorSceneManager.SaveScene(ship);
                Finish(log, "OK — Bastion Profile/Score/Weapon pulled into Ship; shortcut strip removed");
            }
            finally
            {
                EditorSceneManager.CloseScene(bastion, true);
            }
        }

        static void WireView(ShipHudView view, GameObject profile, GameObject score, GameObject weapon,
            StringBuilder log)
        {
            var so = new SerializedObject(view);
            SetRef(so, "_profileRoot", profile);
            SetRef(so, "_profileLevel", TmpNamed(profile.transform, "Profile Level"));
            SetRef(so, "_profileExp", TmpNamed(profile.transform, "Profile Exp"));
            SetRef(so, "_scoreRoot", score);
            SetRef(so, "_scoreSalvage", TmpNamed(score.transform, "Score Salvage"));
            SetRef(so, "_scoreMeta", TmpNamed(score.transform, "Wave Counter")
                ?? TmpNamed(score.transform, "Score Meta"));
            SetRef(so, "_weaponCardRoot", weapon);
            SetRef(so, "_weaponName", TmpNamed(weapon.transform, "Weapon Name"));
            SetRef(so, "_weaponAmmo", TmpNamed(weapon.transform, "Weapon Ammo"));
            var reload = FindDeep(weapon.transform, "Reload");
            SetRef(so, "_reloadBlock", reload != null ? reload.gameObject : null);
            SetRef(so, "_shortcuts", null);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
            log.AppendLine("ShipHudView wired to pulled corners; _shortcuts cleared");
        }

        static void PlaceCorner(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size)
        {
            if (rt == null) return;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        static void CopyRect(RectTransform src, RectTransform dst)
        {
            if (src == null || dst == null) return;
            dst.anchorMin = src.anchorMin;
            dst.anchorMax = src.anchorMax;
            dst.pivot = src.pivot;
            dst.anchoredPosition = src.anchoredPosition;
            dst.sizeDelta = src.sizeDelta;
            dst.offsetMin = src.offsetMin;
            dst.offsetMax = src.offsetMax;
            dst.localScale = src.localScale;
            dst.localRotation = src.localRotation;
        }

        static void StripRaycasts(GameObject root)
        {
            foreach (var g in root.GetComponentsInChildren<Graphic>(true))
                g.raycastTarget = false;
        }

        static void DestroyNamedDirectChild(RectTransform parent, string name, StringBuilder log)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform c = parent.GetChild(i);
                if (c == null || c.name != name) continue;
                log.AppendLine("destroyed: Ship HUD/" + name);
                Object.DestroyImmediate(c.gameObject);
            }
        }

        static Scene EnsureOpen(string path, OpenSceneMode mode, StringBuilder log)
        {
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                Scene s = EditorSceneManager.GetSceneAt(i);
                if (s.path != path) continue;
                log.AppendLine(path + " already open");
                return s;
            }
            log.AppendLine("Opening " + path);
            return EditorSceneManager.OpenScene(path, mode);
        }

        static void SetRef(SerializedObject so, string prop, Object value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.objectReferenceValue = value;
        }

        static TMP_Text TmpNamed(Transform root, string name)
        {
            var t = FindDeep(root, name);
            return t != null ? t.GetComponent<TMP_Text>() : null;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return t;
            }
            return null;
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

        static void Finish(StringBuilder log, string status)
        {
            log.AppendLine(status);
            File.WriteAllText(LogPath, log.ToString());
            Debug.Log(log.ToString());
        }
    }
}
