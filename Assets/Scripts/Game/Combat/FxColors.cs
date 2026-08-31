using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>World/VFX colour literals. UI colours live only in Shift UI Manager.</summary>
    public static class FxColors
    {
        public static readonly Color HatchBlink = new Color(1f, 0.95f, 0.85f);
        public static readonly Color PlayerHp = new Color(0.25f, 0.85f, 0.35f);
        public static readonly Color Stamina = new Color(0.25f, 0.50f, 0.85f);
        public static readonly Color StaminaActive = new Color(0.45f, 0.80f, 1f);
        public static readonly Color AbilitySurge = new Color(0.55f, 0.85f, 1f);
        public static readonly Color AbilityCooldown = new Color(0.45f, 0.65f, 0.95f);
        public static readonly Color VignetteDamage = new Color(0.8f, 0f, 0f, 0.35f);
        public static readonly Color VignetteDown = new Color(0.4f, 0f, 0f, 0.55f);
        public static readonly Color AlarmText = new Color(1f, 0.42f, 0.32f);
        public static readonly Color Accent = new Color(0.388f, 0.776f, 1f);
        public static readonly Color Warn = new Color(0.95f, 0.78f, 0.28f);
        public static readonly Color Ink = new Color(0.98f, 0.98f, 0.95f);
        public static readonly Color InkDim = new Color(0.72f, 0.71f, 0.67f);
        public static readonly Color InkFaint = new Color(0.52f, 0.51f, 0.48f);
        public static readonly Color WheelCentre = new Color(0.95f, 0.78f, 0.28f, 1f);
        public static readonly Color WheelNeighbour = new Color(0.35f, 0.42f, 0.52f, 1f);
        public static readonly Color WheelOpposite = new Color(0.14f, 0.15f, 0.17f, 1f);
        public static readonly Color WheelSpokeLit = new Color(0.95f, 0.78f, 0.28f, 0.85f);
        public static readonly Color WheelSpokeDark = new Color(0.30f, 0.32f, 0.36f, 0.55f);
        public static readonly Color WheelTextOnCentre = new Color(0.10f, 0.10f, 0.10f, 1f);
        public const float Head = 30f;
        public const float Body = 28f;
        public const float Small = 25f;

        public static void Apply(TMPro.TextMeshProUGUI tmp, float size, Color color,
            TMPro.FontStyles style = TMPro.FontStyles.Normal,
            TMPro.TextAlignmentOptions align = TMPro.TextAlignmentOptions.Left)
        {
            if (tmp == null) return;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = align;
        }

        public static readonly Color FxSparks = new Color(1f, 0.6f, 0.15f);
        public static readonly Color FxBurst = new Color(1f, 0.92f, 0.6f);
        public static readonly Color FxMuzzle = new Color(1f, 0.9f, 0.4f);
        public static readonly Color FxBlood = new Color(1f, 0.3f, 0.1f);
        public static readonly Color FxOvercharge = new Color(1f, 0.38f, 0.08f);
        public static readonly Color FxEmp = new Color(0.55f, 1f, 0.72f);
        public static readonly Color EnemyAlert = new Color(0.9f, 0.2f, 0.2f);
        public static readonly Color TracerDefault = new Color(1f, 0.9f, 0.35f);
        public static readonly Color SparkDefault = new Color(1f, 0.55f, 0.1f);
        public static readonly Color CritTracer = new Color(1f, 1f, 0.92f);
        public static readonly Color CritSpark = new Color(1f, 0.98f, 0.8f);
        public static readonly Color DecoyGhostValid = new Color(0.2f, 0.9f, 0.4f, 0.55f);
        public static readonly Color DecoyGhostInvalid = new Color(0.9f, 0.2f, 0.2f, 0.55f);
        public static readonly Color DecoyBody = new Color(0.35f, 0.75f, 0.95f, 1f);

        public static Color Rgb(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
    }
}
