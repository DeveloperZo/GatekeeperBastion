// Gatekeeper Bastion — docgen input shims.
//
// Controls.cs is a Game-layer file: it names KeyCodes and polls UnityEngine.Input. The
// generator only reads the binding LIST, never polls, so Input is stubbed to "nothing is
// pressed" and KeyCode carries just the members the list uses. A binding added with a
// KeyCode not listed here fails to compile the generator — which is the intended alarm,
// not a bug: the manual should not silently omit a key.
//
// Compiled only by Tools/docgen/run.sh, alongside Tools/verify/Shims.cs.

namespace UnityEngine
{
    public enum KeyCode
    {
        None = 0,
        Backspace, Delete, Tab, Return, Escape, Space,
        Alpha0, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8, Alpha9,
        Keypad0, Keypad1, Keypad2, Keypad3, Keypad4, Keypad5, Keypad6, Keypad7, Keypad8,
        Keypad9, KeypadEnter, KeypadPlus, KeypadMinus,
        A, B, C, D, E, F, G, H, I, J, K, L, M,
        N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        LeftBracket, RightBracket, Semicolon, Comma, Period, Slash, Backslash, Minus, Equals,
        LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt,
        Mouse0, Mouse1, Mouse2,
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
        UpArrow, DownArrow, LeftArrow, RightArrow,
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
    }

    /// <summary>Nothing is ever pressed: the generator reads the list, it does not play.</summary>
    public static class Input
    {
        public static bool GetKeyDown(KeyCode key) => false;
        public static bool GetKey(KeyCode key) => false;
        public static bool GetKeyUp(KeyCode key) => false;
        public static float GetAxis(string name) => 0f;
        public static float GetAxisRaw(string name) => 0f;
        public static bool GetMouseButtonDown(int button) => false;
        public static bool GetMouseButton(int button) => false;
        public static bool anyKeyDown => false;
        public static Vector2 mouseScrollDelta => new Vector2(0f, 0f);
    }

    /// <summary>Frozen clock for the same reason: the generator never advances a frame.</summary>
    public static class Time
    {
        public static int frameCount => 0;
    }
}
