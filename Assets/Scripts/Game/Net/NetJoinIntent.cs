using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: Main Menu host/join intent for CLI / Splash smoke, plus
    /// LastError for the Join panel. Friends Host/Join starts the DDOL
    /// <see cref="NetSession"/> from the menu directly — intent is not held across an
    /// offline Ship. Pending is consumed when a session bootstraps (Awake), not deferred
    /// until Bastion. Cleared on domain reload
    /// (<see cref="RuntimeInitializeLoadType.SubsystemRegistration"/>).</summary>
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
        static string _joinCode;
        static RelayJoin.Payload _relay;
        #endregion

        #region Properties
        /// <summary>True when a host/join request is waiting to be consumed.</summary>
        public static bool HasPending => _pending != Kind.None;

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

        /// <summary>Relay join code. Still readable after Consume so pause can show it.</summary>
        public static string JoinCode => _joinCode;

        /// <summary>Relay payload captured by RequestHostRelay / RequestJoinRelay.</summary>
        public static RelayJoin.Payload Relay => _relay;

        /// <summary>True when the pending (or last consumed) intent carries Relay data.</summary>
        public static bool HasRelay => RelayJoin.HasPayload(_relay);
        #endregion

        #region Public API
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Clear();

        /// <summary>Drop pending host/join and Relay bytes. Fixtures call this so a prior
        /// menu intent cannot bind Relay onto an explicit LAN StartHost/StartClient.</summary>
        public static void Clear()
        {
            _pending = Kind.None;
            _address = null;
            _port = DefaultPort;
            _lastError = null;
            _joinCode = null;
            _relay = default;
        }

        public static void RequestHost()
        {
            _pending = Kind.Host;
            _address = null;
            _port = DefaultPort;
            _joinCode = null;
            _relay = default;
        }

        public static void RequestHostRelay(RelayJoin.Payload payload, string joinCode)
        {
            _pending = Kind.Host;
            _address = null;
            _port = DefaultPort;
            _joinCode = joinCode;
            _relay = payload;
        }

        public static void RequestJoin(string address, ushort port)
        {
            _pending = Kind.Join;
            _address = address;
            _port = port;
            _joinCode = null;
            _relay = default;
        }

        public static void RequestJoinRelay(RelayJoin.Payload payload, string joinCode)
        {
            _pending = Kind.Join;
            _address = null;
            _port = DefaultPort;
            _joinCode = joinCode;
            _relay = payload;
        }

        /// <summary>Returns the pending kind and clears it so a second session start is solo.</summary>
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

        /// <summary>Six alphanumeric characters, case-insensitive. Empty is not a code.</summary>
        public static bool TryParseJoinCode(string text, out string normalized)
        {
            normalized = null;
            if (string.IsNullOrEmpty(text)) return false;
            string trimmed = text.Trim();
            if (trimmed.Length != 6) return false;
            var chars = new char[6];
            for (int i = 0; i < 6; i++)
            {
                char ch = trimmed[i];
                if (ch >= 'a' && ch <= 'z') ch = (char)(ch - 32);
                bool letter = ch >= 'A' && ch <= 'Z';
                bool digit = ch >= '0' && ch <= '9';
                if (!letter && !digit) return false;
                chars[i] = ch;
            }
            normalized = new string(chars);
            return true;
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
