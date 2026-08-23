using GatekeepersProtocol.Game;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>Authors the protocol readout into a Bastion HUD: the bottom-centre ability bar
    /// (Passive, Personal, Crew) and the protocol line above it.
    ///
    /// Both are referenced by shipped code that has never had anything to talk to.
    /// <c>BastionHudView</c> holds <c>_protocolLine</c> and <c>_protocolBar</c> as optional
    /// bindings and <c>HudController</c> fills them every frame, so the whole pipeline exists
    /// and ends in a null — which is why two tests have been failing for as long as anyone has
    /// looked, saying plainly "Protocol Bar GO missing".
    ///
    /// It is a builder rather than a hand edit because Bastion.unity is regenerated and is
    /// usually open in front of somebody. Re-runnable: an existing bar is replaced, so tuning
    /// the layout is a re-run.</summary>
    public static partial class SceneBuilder
    {
        const string BastionCoopScenePathForBar = "Assets/Scenes/Bastion_Coop.unity";

        /// <summary>The Shift border the HUD uses everywhere else. The chrome test names this
        /// sprite specifically, sliced, one frame per slot.</summary>
        const string FrameSpritePath =
            "Assets/Shift - Complete Sci-Fi UI/Textures/Border/Cut/Cut Frame Filled.png";

        [MenuItem("GatekeepersProtocol/Build/Protocol Bar", false, 63)]
        public static void BuildProtocolBarMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildProtocolBarInPlace();
        }

        /// <summary>Author the bar into every Bastion-shaped scene that has a HUD.</summary>
        public static void BuildProtocolBarInPlace()
        {
            Scene open = SceneManager.GetActiveScene();
            if (open.isDirty)
            {
                Debug.LogError("[SceneBuilder] Protocol bar refused — '" + open.name
                    + "' has unsaved changes. Save it first; nothing was touched.");
                return;
            }

            AuthorProtocolBarInto(ScenePath);
            if (System.IO.File.Exists(BastionCoopScenePathForBar))
                AuthorProtocolBarInto(BastionCoopScenePathForBar);
        }

        static void AuthorProtocolBarInto(string scenePath)
        {
            BastionSegment.SuppressValidateRebuild = true;
            try
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var view = Object.FindFirstObjectByType<BastionHudView>();
                if (view == null)
                {
                    Debug.LogWarning("[SceneBuilder] " + scenePath + " has no BastionHudView.");
                    return;
                }
                Transform canvas = FindInactive("Canvas - Dynamic");
                if (canvas == null)
                {
                    Debug.LogWarning("[SceneBuilder] " + scenePath + " has no Canvas - Dynamic.");
                    return;
                }

                ReplaceExisting(canvas, "Protocol Bar");
                ReplaceExisting(canvas, "Protocol Line");

                TMP_Text line = MakeLine(canvas);
                ProtocolAbilityBarView bar = MakeBar(canvas);

                // Private serialized fields, set the way the inspector would.
                var so = new SerializedObject(view);
                so.FindProperty("_protocolLine").objectReferenceValue = line;
                so.FindProperty("_protocolBar").objectReferenceValue = bar;
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, scenePath);
                Debug.Log("[SceneBuilder] Protocol bar authored into " + scenePath);
            }
            finally
            {
                BastionSegment.SuppressValidateRebuild = false;
            }
        }

        /// <summary>Objects authored by a previous run go, so a re-run tunes rather than
        /// stacks. Searched including inactive: the HUD canvas is authored closed.</summary>
        static void ReplaceExisting(Transform canvas, string name)
        {
            Transform old = canvas.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        static Transform FindInactive(string name)
        {
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].name == name) return all[i];
            return null;
        }

        /// <summary>One line of protocol text, above the bar.</summary>
        static TMP_Text MakeLine(Transform canvas)
        {
            var go = new GameObject("Protocol Line", typeof(TextMeshProUGUI));
            go.transform.SetParent(canvas, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = 22f;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.text = "";
            Anchor(go, new Vector2(0.5f, 0f), new Vector2(0f, 176f), new Vector2(760f, 26f));
            return text;
        }

        /// <summary>The bar itself: three slots across the bottom centre, Passive on the left,
        /// Crew — the ult — on the right, in the order the player earns them.</summary>
        static ProtocolAbilityBarView MakeBar(Transform canvas)
        {
            var go = new GameObject("Protocol Bar", typeof(ProtocolAbilityBarView));
            go.transform.SetParent(canvas, false);
            Anchor(go, new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(760f, 48f));

            var bar = go.GetComponent<ProtocolAbilityBarView>();
            var so = new SerializedObject(bar);
            BindSlot(so, "_passive", MakeSlot(go.transform, "Passive Slot", -250f));
            BindSlot(so, "_personal", MakeSlot(go.transform, "Personal Slot", 0f));
            BindSlot(so, "_crew", MakeSlot(go.transform, "Crew Slot", 250f));
            so.ApplyModifiedPropertiesWithoutUndo();
            return bar;
        }

        struct Slot
        {
            public GameObject Root;
            public TMP_Text Key, Name, Countdown;
            public Image Fill;
        }

        static void BindSlot(SerializedObject so, string field, Slot slot)
        {
            SerializedProperty p = so.FindProperty(field);
            p.FindPropertyRelative("root").objectReferenceValue = slot.Root;
            p.FindPropertyRelative("key").objectReferenceValue = slot.Key;
            p.FindPropertyRelative("name").objectReferenceValue = slot.Name;
            p.FindPropertyRelative("cooldownFill").objectReferenceValue = slot.Fill;
            p.FindPropertyRelative("countdown").objectReferenceValue = slot.Countdown;
        }

        /// <summary>One slot: the key that fires it, what it is called, a fill that drains while
        /// it recharges, and the seconds left. Every one of those is something HudController
        /// already computes and currently throws away.</summary>
        static Slot MakeSlot(Transform bar, string name, float x)
        {
            var root = new GameObject(name, typeof(Image));
            root.transform.SetParent(bar, false);
            var plate = root.GetComponent<Image>();
            plate.color = new Color(0.05f, 0.06f, 0.09f, 0.72f);
            plate.raycastTarget = false;
            Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(228f, 44f));

            var frameGo = new GameObject("Frame", typeof(Image));
            frameGo.transform.SetParent(root.transform, false);
            var frame = frameGo.GetComponent<Image>();
            frame.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FrameSpritePath);
            if (frame.sprite == null)
                Debug.LogWarning("[SceneBuilder] Protocol bar: no frame sprite at " + FrameSpritePath);
            frame.type = Image.Type.Sliced;   // m_Type: 1
            frame.raycastTarget = false;
            Stretch(frameGo);

            var fillGo = new GameObject("CooldownFill", typeof(Image));
            fillGo.transform.SetParent(root.transform, false);
            var fill = fillGo.GetComponent<Image>();
            fill.color = new Color(0.30f, 0.66f, 0.72f, 0.55f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.raycastTarget = false;
            Stretch(fillGo);

            return new Slot
            {
                Root = root,
                // 24 x 20 at font 16 — the chrome test pins these, so the keycaps stay the
                // size the rest of the HUD's caps are rather than whatever looked right here.
                Key = Label(root.transform, "Key", 16f, TextAlignmentOptions.Left,
                    new Vector2(10f, 0f), new Vector2(24f, 20f)),
                Name = Label(root.transform, "Name", 20f, TextAlignmentOptions.Left,
                    new Vector2(54f, 0f), new Vector2(120f, 40f)),
                Countdown = Label(root.transform, "Countdown", 20f, TextAlignmentOptions.Right,
                    new Vector2(-10f, 0f), new Vector2(56f, 40f)),
                Fill = fill,
            };
        }

        static TMP_Text Label(Transform parent, string name, float size,
            TextAlignmentOptions align, Vector2 offset, Vector2 size2)
        {
            var go = new GameObject(name, typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.alignment = align;
            text.raycastTarget = false;
            text.text = "";
            var rt = go.GetComponent<RectTransform>();
            bool right = align == TextAlignmentOptions.Right;
            rt.anchorMin = rt.anchorMax = new Vector2(right ? 1f : 0f, 0.5f);
            rt.pivot = new Vector2(right ? 1f : 0f, 0.5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = size2;
            return text;
        }

        static void Anchor(GameObject go, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
        }

        static void Stretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
