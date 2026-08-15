using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: Main Menu host/join carried across the Bastion load. Static on
    /// purpose — MainMenu cannot hold a <see cref="NetSession"/> (that object lives on Bastion).
    /// Consumed once in <see cref="NetSession.Awake"/>. Not a field on NetSession; that type's
    /// no-statics contract stays.</summary>
    public static class NetJoinIntent
    {
        #region Constants
        public const ushort DefaultPort = 7777;
        #endregion

        #region Nested Types
        public enum Kind : byte
        {
            None = 0,
            Host = 1,
            Join = 2
        }
        #endregion

        #region State
        static Kind _pending;
        static string _address;
        static ushort _port;
        static string _lastError;
        #endregion

        #region Properties
        /// <summary>Last failed-join line for the Main Menu refusal label. Null when none.</summary>
        public static string LastError
        {
            get => _lastError;
            set => _lastError = value;
        }

        /// <summary>Address captured by <see cref="RequestJoin"/>. Still readable after Consume.</summary>
        public static string Address => _address;

        /// <summary>Port captured by <see cref="RequestHost"/> / <see cref="RequestJoin"/>.</summary>
        public static ushort Port => _port;
        #endregion

        #region Public API
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _pending = Kind.None;
            _address = null;
            _port = DefaultPort;
            _lastError = null;
        }

        public static void RequestHost()
        {
            _pending = Kind.Host;
            _address = null;
            _port = DefaultPort;
        }

        public static void RequestJoin(string address, ushort port)
        {
            _pending = Kind.Join;
            _address = address;
            _port = port;
        }

        /// <summary>Returns the pending kind and clears it so a second Bastion load is solo.</summary>
        public static Kind Consume()
        {
            Kind kind = _pending;
            _pending = Kind.None;
            return kind;
        }

        /// <summary>True when <paramref name="address"/> is a dotted quad a client can
        /// connect to. 0.0.0.0 is a listen bind, not a destination — same-machine join
        /// uses 127.0.0.1.</summary>
        public static bool IsJoinableAddress(string address)
        {
            if (!TryParseIpv4(address, out string normalized)) return false;
            return normalized != "0.0.0.0";
        }

        /// <summary>IPv4 dotted quad only — four octets 0–255. No hostnames.</summary>
        public static bool TryParseIpv4(string text, out string normalized)
        {
            normalized = null;
            if (string.IsNullOrEmpty(text)) return false;
            string trimmed = text.Trim();
            int start = 0;
            var octets = new int[4];
            int n = 0;
            for (int i = 0; i <= trimmed.Length; i++)
            {
                if (i < trimmed.Length && trimmed[i] != '.') continue;
                int len = i - start;
                if (len < 1 || len > 3) return false;
                int value = 0;
                for (int c = start; c < i; c++)
                {
                    char ch = trimmed[c];
                    if (ch < '0' || ch > '9') return false;
                    value = value * 10 + (ch - '0');
                }
                if (value > 255) return false;
                if (n >= 4) return false;
                octets[n++] = value;
                start = i + 1;
            }
            if (n != 4) return false;
            normalized = octets[0] + "." + octets[1] + "." + octets[2] + "." + octets[3];
            return true;
        }

        /// <summary>Empty uses <see cref="DefaultPort"/>. Otherwise 1–65535.</summary>
        public static bool TryParsePort(string text, out ushort port)
        {
            port = DefaultPort;
            if (string.IsNullOrEmpty(text)) return true;
            string trimmed = text.Trim();
            if (trimmed.Length == 0) return true;
            int value = 0;
            for (int i = 0; i < trimmed.Length; i++)
            {
                char ch = trimmed[i];
                if (ch < '0' || ch > '9') return false;
                value = value * 10 + (ch - '0');
                if (value > 65535) return false;
            }
            if (value < 1) return false;
            port = (ushort)value;
            return true;
        }
        #endregion
    }
}
