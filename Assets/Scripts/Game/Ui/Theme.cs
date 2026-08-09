// Fonts (UIManager asset): body/light/regular/medium/semibold → LiberationSans SDF
// (Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset). Display headers
// keep Shift Rajdhani-Bold SDF only; no Bold LiberationSans SDF ships in-repo.
// Motif: signature is the L-bracket corner (single corner treatment) on every panelled screen.
using TMPro;
using UnityEngine;

namespace GatekeeperBastion.Game.Ui
{
    /// <summary>Game-owned colour and type tokens. Must agree with the Shift UIManager asset
    /// under Assets/Shift - Complete Sci-Fi UI/ (v2.0.12): Ink→primaryColor, InkDim→secondaryColor,
    /// Panel→backgroundColor, Danger→negativeColor. Accent/Warn/Good/InkFaint/Line have no Shift
    /// field and live only here. Accent also drives UIManager cyan replacements (tint/logo/particle).</summary>
    public static class Theme
    {
        public static readonly Color Ink = new Color(0.98f, 0.98f, 0.95f);
        public static readonly Color InkDim = new Color(0.72f, 0.71f, 0.67f);
        public static readonly Color InkFaint = new Color(0.52f, 0.51f, 0.48f);
        public static readonly Color Accent = new Color(1f, 0.55f, 0.10f);
        public static readonly Color Warn = new Color(0.95f, 0.78f, 0.28f);
        public static readonly Color Danger = new Color(0.86f, 0.30f, 0.28f);
        public static readonly Color Good = new Color(0.35f, 0.78f, 0.45f);
        public static readonly Color Panel = new Color(0f, 0f, 0f, 0.62f);
        public static readonly Color Line = new Color(1f, 1f, 1f, 0.12f);

        public static readonly Color PanelHighlight = new Color(0f, 0f, 0f, 0.82f);
        public static readonly Color ModalDim = new Color(0f, 0f, 0f, 0.75f);

        public static readonly Color HatchBlink = new Color(1f, 0.95f, 0.85f);
        public static readonly Color PlayerHp = new Color(0.25f, 0.85f, 0.35f);
        public static readonly Color Stamina = new Color(0.25f, 0.50f, 0.85f);
        public static readonly Color StaminaActive = new Color(0.45f, 0.80f, 1f);
        public static readonly Color AbilitySurge = new Color(0.55f, 0.85f, 1f);
        public static readonly Color AbilityCooldown = new Color(0.45f, 0.65f, 0.95f);
        public static readonly Color BarTrack = new Color(0f, 0f, 0f, 0.6f);
        public static readonly Color VignetteDamage = new Color(0.8f, 0f, 0f, 0.35f);
        public static readonly Color VignetteDown = new Color(0.4f, 0f, 0f, 0.55f);
        public static readonly Color AlarmText = new Color(1f, 0.42f, 0.32f);

        // M21 ARMORY wheel (former ArmoryStation IMGUI literals).
        public static readonly Color WheelCentre = new Color(0.95f, 0.78f, 0.28f, 1f);
        public static readonly Color WheelNeighbour = new Color(0.35f, 0.42f, 0.52f, 1f);
        public static readonly Color WheelOpposite = new Color(0.14f, 0.15f, 0.17f, 1f);
        public static readonly Color WheelHub = new Color(0.10f, 0.11f, 0.13f, 1f);
        public static readonly Color WheelRing = new Color(0.45f, 0.48f, 0.55f, 0.55f);
        public static readonly Color WheelSpokeLit = new Color(0.95f, 0.78f, 0.28f, 0.85f);
        public static readonly Color WheelSpokeDark = new Color(0.30f, 0.32f, 0.36f, 0.55f);
        public static readonly Color WheelTextOnCentre = new Color(0.10f, 0.10f, 0.10f, 1f);

        // World / VFX (criterion 2: all new Color( in Game live here).
        public static readonly Color FxSparks = new Color(1f, 0.6f, 0.15f);
        public static readonly Color FxBurst = new Color(1f, 0.92f, 0.6f);
        public static readonly Color FxMuzzle = new Color(1f, 0.9f, 0.4f);
        public static readonly Color FxBlood = new Color(1f, 0.3f, 0.1f);
        public static readonly Color HitFlash = new Color(1f, 0.15f, 0.1f);
        public static readonly Color EnemyAlert = new Color(0.9f, 0.2f, 0.2f);
        public static readonly Color TracerDefault = new Color(1f, 0.9f, 0.35f);
        public static readonly Color SparkDefault = new Color(1f, 0.55f, 0.1f);
        public static readonly Color CritTracer = new Color(1f, 1f, 0.92f);
        public static readonly Color CritSpark = new Color(1f, 0.98f, 0.8f);
        public static readonly Color DecoyGhostValid = new Color(0.2f, 0.9f, 0.4f, 0.55f);
        public static readonly Color DecoyGhostInvalid = new Color(0.9f, 0.2f, 0.2f, 0.55f);
        public static readonly Color DecoyBody = new Color(0.35f, 0.75f, 0.95f, 1f);

        public static Color Rgb(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        // Shift-measured type sizes (Section B): 25 / 28 / 30 / 36 / 40.
        public const float Display = 40f;
        public const float Title = 36f;
        public const float Head = 30f;
        public const float Body = 28f;
        public const float Small = 25f;

        public static void Apply(TextMeshProUGUI tmp, float size, Color color,
            FontStyles style = FontStyles.Normal,
            TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            if (tmp == null) return;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = align;
        }
    }
}
