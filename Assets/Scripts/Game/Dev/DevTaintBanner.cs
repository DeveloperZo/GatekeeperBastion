#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: persistent on-screen warning while <see cref="GameSession.DevTainted"/>
    /// is true. Drawn by absolute GUI rects so both the Bastion HUD (no GUILayout) and ship
    /// station panels can share one string and one draw path. Entire file compiles out of release
    /// with Dev mode — call sites are wrapped in the same ifdef so StationTerminal has no
    /// dangling reference.</summary>
    public static class DevTaintBanner
    {
        const string Message = "DEV MODE — progress will not be saved";

        /// <summary>True when the banner would draw. Reads <see cref="GameSession.DevTainted"/>
        /// and nothing else — latched the moment the first Dev command runs.</summary>
        public static bool IsVisible => GameSession.DevTainted;

        /// <summary>Top-centre strip in virtual HUD space. No-op when the session is clean.
        /// Caller must already have set <c>GUI.matrix</c> for DPI scaling.</summary>
        public static void Draw(float vw)
        {
            if (!GameSession.DevTainted) return;

            var prev = GUI.color;
            GUI.color = new Color(0.85f, 0.2f, 0.15f, 0.92f);
            GUI.DrawTexture(new Rect(vw / 2f - 260f, 4f, 520f, 28f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var style = Style();
            GUI.Label(new Rect(vw / 2f - 260f, 4f, 520f, 28f), Message, style);
            GUI.color = prev;
        }

        static GUIStyle _style;

        static GUIStyle Style()
        {
            if (_style != null) return _style;
            _style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            _style.normal.textColor = Color.white;
            return _style;
        }
    }
}
#endif

