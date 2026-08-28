using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>Authors the three-card protocol pick into the CREW QUARTERS station panel:
    /// a heading, three cards, and the way back to the catalog.
    ///
    /// It rides the existing Build/Protocol Bar row rather than claiming a new one. The row
    /// list in Docs/Architecture/README.md is a fenced, permission-gated surface, and this is
    /// the same subject that row already owns — the protocol UI that shipped code holds
    /// bindings for and has nothing to talk to. <c>CrewQuartersPanelView._cardDeck</c> is the
    /// binding; it is [OptionalBinding], so a scene that has not been through this row still
    /// validates and the station simply shows the catalog it always has.
    ///
    /// It is a builder rather than a hand edit for the reason 8-Scene-Authoring.md gives:
    /// Ship - CoOp.unity is 8 MB of which the station canvases are the overwhelming majority,
    /// and hand-placing eleven objects in it is how bindings get missed. Re-runnable: an
    /// existing deck is destroyed first, so tuning the layout is a re-run.</summary>
    public static partial class SceneBuilder
    {
        const string ShipScenePathForCards = "Assets/Scenes/Ship - CoOp.unity";
        const string CardDeckName = "Protocol Card Deck";

        /// <summary>Card plate and the gutter between cards, in canvas units. Three equal
        /// thirds: the eye picks "the middle one" by position, the same reason the ability
        /// bar's tiles are identical squares.</summary>
        const float CardGutter = 8f;

        const float DeckInset = 12f;
        const float HeadingHeight = 28f;
        const float BrowseHeight = 34f;
        const float BrowseWidth = 320f;

        public static void AuthorProtocolCardsInto(string scenePath)
        {
            BastionSegment.SuppressValidateRebuild = true;
            try
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var panel = Object.FindFirstObjectByType<CrewQuartersPanelView>(
                    FindObjectsInactive.Include);
                if (panel == null)
                {
                    Debug.LogWarning("[SceneBuilder] " + scenePath
                        + " has no CrewQuartersPanelView — no protocol cards authored.");
                    return;
                }

                ReplaceExisting(panel.transform, CardDeckName);
                ProtocolCardDeckView deck = MakeDeck(panel.transform);

                var so = new SerializedObject(panel);
                so.FindProperty("_cardDeck").objectReferenceValue = deck;
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, scenePath);
                Debug.Log("[SceneBuilder] Protocol cards authored into " + scenePath);
            }
            finally
            {
                BastionSegment.SuppressValidateRebuild = false;
            }
        }

        /// <summary>The deck: heading, a row of three cards, and SEE THE FULL CATALOG.
        ///
        /// It ships INACTIVE. The station turns it on only while a pick is pending, and the
        /// deck replays its entrance on every activation — an authored-active deck would run
        /// that animation once, invisibly, before the panel is ever opened.</summary>
        static ProtocolCardDeckView MakeDeck(Transform panel)
        {
            var go = new GameObject(CardDeckName, typeof(RectTransform),
                typeof(ProtocolCardDeckView), typeof(LayoutElement));
            go.transform.SetParent(panel, false);
            StretchInset(go, DeckInset);

            // The CREW panel drives its rows through a VerticalLayoutGroup. The deck is not a
            // row and must keep the anchors set below (the entrance writes anchoredPosition),
            // so it opts out of any layout group above it. Inert where there is none.
            go.GetComponent<LayoutElement>().ignoreLayout = true;

            TMP_Text heading = Line(go.transform, "Heading", 20f, TextAlignmentOptions.Center);
            heading.fontStyle = FontStyles.Bold;
            var headRect = heading.GetComponent<RectTransform>();
            headRect.anchorMin = new Vector2(0f, 1f);
            headRect.anchorMax = new Vector2(1f, 1f);
            headRect.pivot = new Vector2(0.5f, 1f);
            headRect.anchoredPosition = Vector2.zero;
            headRect.sizeDelta = new Vector2(0f, HeadingHeight);

            var cardsGo = new GameObject("Cards", typeof(RectTransform));
            cardsGo.transform.SetParent(go.transform, false);
            var cardsRect = cardsGo.GetComponent<RectTransform>();
            cardsRect.anchorMin = Vector2.zero;
            cardsRect.anchorMax = Vector2.one;
            cardsRect.offsetMin = new Vector2(0f, BrowseHeight + 10f);
            cardsRect.offsetMax = new Vector2(0f, -(HeadingHeight + 6f));

            var cards = new ProtocolCardView[ProtocolOffer.CardCount];
            for (int i = 0; i < cards.Length; i++)
                cards[i] = MakeCard(cardsRect, i, cards.Length);

            Button browse = MakeButton(go.transform, "Browse", ProtocolCardDeckView.BrowseLabel);
            var browseRect = browse.GetComponent<RectTransform>();
            browseRect.anchorMin = browseRect.anchorMax = new Vector2(0.5f, 0f);
            browseRect.pivot = new Vector2(0.5f, 0f);
            browseRect.anchoredPosition = new Vector2(0f, 0f);
            browseRect.sizeDelta = new Vector2(BrowseWidth, BrowseHeight);

            var deck = go.GetComponent<ProtocolCardDeckView>();
            var so = new SerializedObject(deck);
            so.FindProperty("_heading").objectReferenceValue = heading;
            so.FindProperty("_browse").objectReferenceValue = browse;
            SerializedProperty arr = so.FindProperty("_cards");
            arr.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            go.SetActive(false);
            return deck;
        }

        /// <summary>One card: a dark plate under the Shift frame, the sheet's five rows top to
        /// bottom, a refusal line, and CHOOSE pinned to the bottom edge.
        ///
        /// Bands are pixel offsets from the top rather than fractions, and every body row
        /// auto-sizes down: FORGE's crew row is the longest copy in the catalog and it has to
        /// shrink rather than clip when the panel is short.</summary>
        static ProtocolCardView MakeCard(RectTransform row, int index, int count)
        {
            var go = new GameObject("Protocol Card " + (index + 1), typeof(Image),
                typeof(ProtocolCardView));
            go.transform.SetParent(row, false);
            var plate = go.GetComponent<Image>();
            plate.color = new Color(0.05f, 0.06f, 0.09f, 0.86f);
            plate.raycastTarget = false;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(index / (float)count, 0f);
            rect.anchorMax = new Vector2((index + 1) / (float)count, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(CardGutter, 0f);
            rect.offsetMax = new Vector2(-CardGutter, 0f);

            var frameGo = new GameObject("Frame", typeof(Image));
            frameGo.transform.SetParent(go.transform, false);
            var frame = frameGo.GetComponent<Image>();
            frame.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FrameSpritePath);
            if (frame.sprite == null)
                Debug.LogWarning("[SceneBuilder] Protocol cards: no frame sprite at " + FrameSpritePath);
            frame.type = Image.Type.Sliced;
            // Cut Frame FILLED is a surface, not an outline — the HUD's idiom for it as a
            // surface is a few percent alpha. Left at white it paints a solid block.
            frame.color = new Color(Warm.r, Warm.g, Warm.b, 0.10f);
            frame.raycastTarget = false;
            Stretch(frameGo);

            TMP_Text title = Band(go.transform, "Name", 10f, 32f, 22f);
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            TMP_Text doctrine = Band(go.transform, "Doctrine", 46f, 74f, 14f);
            TMP_Text crew = Band(go.transform, "Crew", 126f, 92f, 13f);
            TMP_Text personal = Band(go.transform, "Personal", 222f, 92f, 13f);
            TMP_Text passive = Band(go.transform, "Passive", 318f, 92f, 13f);

            TMP_Text refusal = Line(go.transform, "Refusal", 12f, TextAlignmentOptions.Center);
            refusal.color = new Color(0.75f, 0.42f, 0.42f, 1f);
            var refusalRect = refusal.GetComponent<RectTransform>();
            refusalRect.anchorMin = new Vector2(0f, 0f);
            refusalRect.anchorMax = new Vector2(1f, 0f);
            refusalRect.pivot = new Vector2(0.5f, 0f);
            refusalRect.anchoredPosition = new Vector2(0f, 56f);
            refusalRect.sizeDelta = new Vector2(-16f, 18f);

            Button pick = MakeButton(go.transform, "Pick", ProtocolCardView.PickLabel);
            var pickRect = pick.GetComponent<RectTransform>();
            pickRect.anchorMin = new Vector2(0f, 0f);
            pickRect.anchorMax = new Vector2(1f, 0f);
            pickRect.pivot = new Vector2(0.5f, 0f);
            pickRect.anchoredPosition = new Vector2(0f, 10f);
            pickRect.sizeDelta = new Vector2(-32f, 40f);

            var card = go.GetComponent<ProtocolCardView>();
            var so = new SerializedObject(card);
            so.FindProperty("_name").objectReferenceValue = title;
            so.FindProperty("_doctrine").objectReferenceValue = doctrine;
            so.FindProperty("_crew").objectReferenceValue = crew;
            so.FindProperty("_personal").objectReferenceValue = personal;
            so.FindProperty("_passive").objectReferenceValue = passive;
            so.FindProperty("_pick").objectReferenceValue = pick;
            so.FindProperty("_refusal").objectReferenceValue = refusal;
            so.ApplyModifiedPropertiesWithoutUndo();
            return card;
        }

        /// <summary>A body row: full width of the card, inset, hung from the top at
        /// <paramref name="top"/>, auto-sizing down to 9 so long copy shrinks.</summary>
        static TMP_Text Band(Transform card, string name, float top, float height, float size)
        {
            TMP_Text text = Line(card, name, size, TextAlignmentOptions.TopLeft);
            text.enableAutoSizing = true;
            text.fontSizeMin = 9f;
            text.fontSizeMax = size;
            text.textWrappingMode = TextWrappingModes.Normal;
            var rt = text.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -top);
            rt.sizeDelta = new Vector2(-24f, height);
            return text;
        }

        static TMP_Text Line(Transform parent, string name, float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.color = Warm;
            text.alignment = align;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.text = "";
            return text;
        }

        /// <summary>A plain UGUI Button with one TMP child. UiBind.SetButtonLabel writes every
        /// TMP under a button, so no Shift component is needed for the label to read back.</summary>
        static Button MakeButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var bg = go.GetComponent<Image>();
            bg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FrameSpritePath);
            bg.type = Image.Type.Sliced;
            bg.color = new Color(Warm.r, Warm.g, Warm.b, 0.22f);
            bg.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = bg;

            TMP_Text text = Line(go.transform, "Text", 16f, TextAlignmentOptions.Center);
            text.fontStyle = FontStyles.Bold;
            text.text = label;
            Stretch(text.gameObject);
            return button;
        }

        static void StretchInset(GameObject go, float inset)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
