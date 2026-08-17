using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using UnityEditor;
using UnityEngine;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>Ship station graybox (Desk / Screen / Label) attached to a field
    /// <see cref="BastionTerminal"/>. Presentation only — no Ship panels.
    /// Sized for kit storey / room scale (not 1:1 Ship alcove).</summary>
    public static class BastionFieldConsole
    {
        const string MaterialDir = "Assets/Materials";
        /// <summary>Ship Desk/Screen/Label × this. <see cref="BastionKit.FieldConsoleScale"/>
        /// is 1 (usable). StoreyHeight/2.5 (=2) was rejected — room-fill, not use.
        /// Label Y is clamped under the ceiling.</summary>
        public static float Scale => BastionKit.FieldConsoleScale;

        static readonly Color LabelColor = new Color(242f / 255f, 128f / 255f, 26f / 255f);

        /// <summary>Idempotent: skips when a child named Desk already exists.</summary>
        public static bool AttachIfMissing(BastionTerminal terminal)
        {
            if (terminal == null)
                return false;
            Transform t = terminal.transform;
            if (t.Find("Desk") != null)
            {
                ApplyScale(t);
                return false;
            }

            Material body = LoadConsoleMaterial();
            string label = string.IsNullOrEmpty(terminal.TerminalId)
                ? "TERMINAL"
                : terminal.TerminalId.ToUpperInvariant();

            float s = Scale;
            MeshPart(t, "Desk",
                new Vector3(0f, BastionKit.FieldConsoleDeskCentreY * s, 0f),
                new Vector3(BastionKit.FieldConsoleDeskWidth * s,
                    BastionKit.FieldConsoleDeskHeight * s,
                    BastionKit.FieldConsoleDeskDepth * s), body);
            MeshPart(t, "Screen",
                new Vector3(0f, BastionKit.FieldConsoleScreenCentreY * s, -0.15f * s),
                new Vector3(BastionKit.FieldConsoleDeskWidth * s, 0.9f * s, 0.12f * s), body);
            FloatingLabel(t, label, s);
            return true;
        }

        /// <summary>Re-applies kit scale to an already-wired terminal (Desk/Screen/Label).</summary>
        public static void ApplyScale(Transform terminalRoot)
        {
            if (terminalRoot == null)
                return;
            float s = Scale;
            Transform desk = terminalRoot.Find("Desk");
            if (desk != null)
            {
                desk.localPosition = new Vector3(0f, BastionKit.FieldConsoleDeskCentreY * s, 0f);
                desk.localScale = new Vector3(BastionKit.FieldConsoleDeskWidth * s,
                    BastionKit.FieldConsoleDeskHeight * s,
                    BastionKit.FieldConsoleDeskDepth * s);
            }

            Transform screen = terminalRoot.Find("Screen");
            if (screen != null)
            {
                screen.localPosition = new Vector3(0f, BastionKit.FieldConsoleScreenCentreY * s, -0.15f * s);
                screen.localScale = new Vector3(BastionKit.FieldConsoleDeskWidth * s, 0.9f * s, 0.12f * s);
            }

            Transform label = terminalRoot.Find("Label");
            if (label != null)
            {
                float labelY = Mathf.Min(2.45f * s, BastionKit.StoreyHeight - 0.5f);
                label.localPosition = new Vector3(0f, labelY, 0f);
                var mesh = label.GetComponent<TextMesh>();
                if (mesh != null)
                    mesh.characterSize = 0.035f * s;
            }
        }

        static Material LoadConsoleMaterial()
        {
            // Field terminals use NAV MAP cyan so they read as "place" not ship shops.
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + "/Mat_ConsoleMap.mat");
            if (mat != null)
                return mat;
            mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + "/Mat_ConsoleArmory.mat");
            return mat;
        }

        static void MeshPart(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = localScale;
            if (mat != null)
            {
                var renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null)
                    renderer.sharedMaterial = mat;
            }
        }

        static void FloatingLabel(Transform parent, string text, float s)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            float labelY = Mathf.Min(2.45f * s, BastionKit.StoreyHeight - 0.5f);
            go.transform.localPosition = new Vector3(0f, labelY, 0f);
            // Face opposite the screen (Ship Armory label yaw 180).
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.characterSize = 0.035f * s;
            mesh.fontSize = 48;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = LabelColor;
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (mesh.font == null)
                mesh.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
