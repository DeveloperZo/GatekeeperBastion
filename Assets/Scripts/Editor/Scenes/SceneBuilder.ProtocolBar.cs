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

        /// <summary>One line of protocol text, above the squares.</summary>
        static TMP_Text MakeLine(Transform canvas)
        {
            var go = new GameObject("Protocol Line", typeof(TextMeshProUGUI));
            go.transform.SetParent(canvas, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = 20f;
            text.color = Warm;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.text = "";
            Anchor(go, new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(760f, 24f));
            return text;
        }

        /// <summary>The HUD's warm white. Everything else on this canvas is tinted with it.</summary>
        static readonly Color Warm = new Color(0.98f, 0.98f, 0.95f, 1f);

        /// <summary>Square edge, and the gap between squares. LoL reads at a glance because the
        /// tiles are square and identical — the eye finds "the third one" by position rather than
        /// by reading it, which a row of wide name-plates does not allow.</summary>
        const float Tile = 72f;
        const float Gap = 16f;

        /// <summary>The bar: three ability squares across the bottom centre, Passive on the left,
        /// Crew — the ult — on the right, in the order the player earns them.</summary>
        static ProtocolAbilityBarView MakeBar(Transform canvas)
        {
            var go = new GameObject("Protocol Bar", typeof(ProtocolAbilityBarView));
            go.transform.SetParent(canvas, false);
            float width = Tile * 3f + Gap * 2f;
            Anchor(go, new Vector2(0.5f, 0f), new Vector2(0f, 112f), new Vector2(width, Tile));

            var bar = go.GetComponent<ProtocolAbilityBarView>();
            var so = new SerializedObject(bar);
            float step = Tile + Gap;
            BindSlot(so, "_passive", MakeSlot(go.transform, "Passive Slot", -step));
            BindSlot(so, "_personal", MakeSlot(go.transform, "Personal Slot", 0f));
            BindSlot(so, "_crew", MakeSlot(go.transform, "Crew Slot", step));
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

        /// <summary>One ability square, stacked back to front: a dark plate, the Shift frame, the
        /// radial cooldown scrim, then the text. The scrim is UNDER the text on purpose — the
        /// seconds remaining are the one thing that has to stay readable while it sweeps.
        ///
        /// ProtocolAbilityBarView swaps the name for the countdown while an ability is cooling,
        /// so both sit centred in the same place rather than side by side.</summary>
        static Slot MakeSlot(Transform bar, string name, float x)
        {
            var root = new GameObject(name, typeof(Image));
            root.transform.SetParent(bar, false);
            var plate = root.GetComponent<Image>();
            plate.color = new Color(0.05f, 0.06f, 0.09f, 0.86f);
            plate.raycastTarget = false;
            Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(Tile, Tile));

            var frameGo = new GameObject("Frame", typeof(Image));
            frameGo.transform.SetParent(root.transform, false);
            var frame = frameGo.GetComponent<Image>();
            frame.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FrameSpritePath);
            if (frame.sprite == null)
                Debug.LogWarning("[SceneBuilder] Protocol bar: no frame sprite at " + FrameSpritePath);
            frame.type = Image.Type.Sliced;   // m_Type: 1
            // Cut Frame FILLED is a filled panel, not an outline. Left at white it paints a solid
            // white block over the whole square, which is exactly what the first version shipped.
            // The HUD's own idiom for this sprite as a SURFACE is a few percent alpha; this is
            // that, a little stronger, so the square reads as a lit tile on the dark plate.
            frame.color = new Color(Warm.r, Warm.g, Warm.b, 0.14f);
            frame.raycastTarget = false;
            Stretch(frameGo);

            var fillGo = new GameObject("CooldownFill", typeof(Image));
            fillGo.transform.SetParent(root.transform, false);
            var fill = fillGo.GetComponent<Image>();
            // The sweep: a dark wedge covering the share of the cooldown still to run, wiping
            // clockwise from twelve as it expires. HudController hands the view the REMAINING
            // fraction (BoarderProtocol.OverlayFill), so fillAmount is that number directly and
            // the wedge empties on its own — nothing here inverts or animates it.
            fill.color = new Color(0.02f, 0.03f, 0.06f, 0.80f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = true;
            fill.fillAmount = 0f;    // ready. The view shows this object only while cooling.
            fill.raycastTarget = false;
            Stretch(fillGo);

            TMP_Text nameText = Centred(root.transform, "Name", 15f, false);
            // Ability names are longer than a 72 px square: let them shrink rather than clip.
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 9f;
            nameText.fontSizeMax = 15f;

            return new Slot
            {
                Root = root,
                Name = nameText,
                Countdown = Centred(root.transform, "Countdown", 30f, true),
                // 24 x 20 at font 16 — the chrome test pins these, so the keycaps stay the size
                // the rest of the HUD's caps are rather than whatever looked right here. Bottom
                // right of the square, where an ability's key sits in every game that has one.
                Key = Corner(root.transform, "Key", 16f),
                Fill = fill,
            };
        }

        /// <summary>Name and countdown both live dead centre, inset off the frame edge. They are
        /// never up at the same time, so they do not have to share the room.</summary>
        static TMP_Text Centred(Transform parent, string name, float size, bool bold)
        {
            var go = new GameObject(name, typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.color = Warm;
            if (bold) text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.text = "";
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(6f, 6f);
            rt.offsetMax = new Vector2(-6f, -6f);
            return text;
        }

        /// <summary>The keycap, bottom right of the square.</summary>
        static TMP_Text Corner(Transform parent, string name, float size)
        {
            var go = new GameObject(name, typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.color = Warm;
            text.alignment = TextAlignmentOptions.Right;
            text.raycastTarget = false;
            text.text = "";
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-6f, 4f);
            rt.sizeDelta = new Vector2(24f, 20f);
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
