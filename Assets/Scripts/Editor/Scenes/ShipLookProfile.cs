using UnityEngine;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>The Ship's look as data: the palette, the district fixtures, the deck and the
    /// motes. One asset so the numbers are edited in the Inspector and the pass that applies
    /// them holds none of its own. The Bastion pass will read the same type.</summary>
    /// <remarks>Editor-only by design. Nothing loads this at runtime; the pass writes authored
    /// scene content and the scene owns the result from then on.</remarks>
    /// <summary>One travelling pulse: on, signed metres per second along its axis, metres
    /// between heads, pulse length as a fraction of that, and the trough brightness.</summary>
    [System.Serializable]
    public struct FlowSettings
    {
        public bool on;
        public float speed;
        public float length;
        [Range(0.02f, 1f)] public float width;
        [Range(0f, 1f)] public float floor;

        public void Apply(Material m, string prefix, Vector3 dir)
        {
            m.SetFloat(prefix + "On", on ? 1f : 0f);
            m.SetVector(prefix + "Dir", new Vector4(dir.x, dir.y, dir.z, 0f));
            m.SetFloat(prefix + "Speed", speed);
            m.SetFloat(prefix + "Length", Mathf.Max(length, 0.01f));
            m.SetFloat(prefix + "Width", Mathf.Clamp(width, 0.02f, 1f));
            m.SetFloat(prefix + "Floor", Mathf.Clamp01(floor));
        }
    }

    [CreateAssetMenu(fileName = "ShipLookProfile", menuName = "Gatekeeper/Ship Look Profile")]
    public sealed class ShipLookProfile : ScriptableObject
    {
        [Header("Architecture")]
        [Tooltip("Near-black hull tint. The atlas is multiplied by this, so the generic pack " +
                 "disappears into shadow and only the fixtures read.")]
        public Color hullTint = new Color(0.039f, 0.051f, 0.078f);

        [Header("Districts — the cast")]
        [Tooltip("One hue per district. The ship is partitioned along its long axis into as " +
                 "many districts as there are hues here.")]
        public Color[] districtHues =
        {
            new Color(0.620f, 0.910f, 0.160f),   // lime
            new Color(0.180f, 0.880f, 1.000f),   // cyan
            new Color(1.000f, 0.180f, 0.660f),   // magenta
        };

        [Tooltip("Peak emission of the hot row. Doctrine ceiling for architecture is 1.4; this " +
                 "sits above it deliberately, which is why the enemy band was raised to match.")]
        [Range(0f, 6f)] public float districtPeak = 3.0f;

        [Tooltip("How much of the peak the upper row keeps. The base burns, the crown dies — " +
                 "two rows is the whole falloff, which keeps the shared material count at six.")]
        [Range(0f, 1f)] public float upperRowFraction = 0.35f;

        [Tooltip("Height of the hot row and the dim row above it, in metres off the floor.")]
        public Vector2 rowHeights = new Vector2(0.55f, 2.10f);

        [Tooltip("Rows of dashes per wall, from the floor up: 1 keeps the city at the waterline, " +
                 "0 dresses no walls at all and leaves the deck, motes and bloom.")]
        [Range(0, 2)] public int districtRows = 2;
        [Tooltip("Index into WallStrips.Pattern: the dash rhythm.")]
        public int stripPattern;
        [Range(0.005f, 0.12f)] public float stripDepth = 0.025f;

        [Header("Deck")]
        [Tooltip("The mirror's own tint, so the reflection joins the palette instead of being " +
                 "a neutral grey sheet.")]
        public Color deckTint = new Color(0.018f, 0.088f, 0.100f);
        [Tooltip("The colour of the deck's reflections. This shader is metallic workflow, so " +
                 "a reflection only takes a colour as Accent Amount raises metallic.")]
        public Color deckAccent = new Color(0.60f, 0.85f, 0.95f);
        [Tooltip("How metallic the deck is, which is what hands its reflections over to the " +
                 "accent hue. 0 is a plain dielectric floor with white-ish reflections.")]
        [Range(0f, 1f)] public float deckAccentStrength = 0.15f;
        [Range(0f, 1f)] public float deckWet = 0.80f;
        [Tooltip("How much of Deck Tint to blend over the tile's own atlas colour; 0 leaves it.")]
        [Range(0f, 1f)] public float deckTintAmount;

        [Header("Deck vents — the tile on the flow shader")]
        [Tooltip("Keep the Synty floor tile and pulse its vents on GKB/EmissiveFlowAtlas, with wet " +
                 "as the tile's smoothness. Off puts the tiles back on the pack material and lays " +
                 "the mirror sheet instead.")]
        public bool deckFlow;
        public FlowSettings deckPulse;
        [Tooltip("Linked: the along-Z vents take Deck Accent and its strength.")]
        public bool ventZLinked = true;
        public Color ventZColor = new Color(0.60f, 0.85f, 0.95f);
        [Range(0f, 4f)] public float ventZStrength = 0.35f;
        public FlowSettings ventZFlow;
        public bool ventXLinked = true;
        public Color ventXColor = new Color(0.60f, 0.85f, 0.95f);
        [Range(0f, 4f)] public float ventXStrength = 0.35f;
        public FlowSettings ventXFlow;

        [Header("Walls")]
        [Tooltip("Light the wall's strip in the deck accent instead of the pack's cyan. The " +
                 "strip is the only thing that emits on SM_Bld_Wall_01, so this recolours it " +
                 "and nothing else on the piece.")]
        public bool wallStripAccent = true;

        [Header("Post")]
        [Tooltip("Bloom on the shared post profile, at the values the gallery was judged under.")]
        public bool bloom = true;
        [Range(0f, 4f)] public float bloomIntensity = 1.6f;
        [Range(0f, 2f)] public float bloomThreshold = 0.70f;

        [Header("Motes — the splash grammar")]
        [Tooltip("Per district. The splash emitter runs 40.")]
        [Range(0, 200)] public int moteCount = 40;
        public Color moteColor = new Color(0.350f, 0.850f, 1.000f);
        [Range(0.02f, 1f)] public float moteAlpha = 0.32f;
        public Vector2 moteSize = new Vector2(0.10f, 0.20f);
        [Tooltip("Metres per second. The splash motes drift up at roughly 0.1.")]
        [Range(0f, 1f)] public float moteRise = 0.10f;
        [Tooltip("Splash runs its emitter at 0.75.")]
        [Range(0.1f, 2f)] public float moteSimulationSpeed = 0.75f;

        [Header("Scenes")]
        [Tooltip("Roots the pass looks in, in order, for the geometry to dress. The first one " +
                 "present in the open scene wins; if none are, every root is searched.")]
        public string[] dressRoots = { "ShipDress", "Environment", "Bastion", "Geometry" };

        [Header("Wall detection")]
        [Tooltip("A renderer thinner than this in one horizontal axis counts as a wall face.")]
        [Range(0.2f, 2f)] public float wallMaxThickness = 0.9f;
        [Tooltip("Shorter than this and it is trim, not a wall worth dressing.")]
        [Range(0.5f, 6f)] public float wallMinLength = 2.0f;
        [Tooltip("Below this height it is a floor, a kerb or a step.")]
        [Range(0.5f, 4f)] public float wallMinHeight = 1.5f;
    }
}
