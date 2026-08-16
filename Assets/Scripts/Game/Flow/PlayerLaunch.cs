using System;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: player-build command line for same-machine host/join.
    /// Splash consumes this and loads Bastion with a <see cref="NetJoinIntent"/>.
    /// Not a field on NetSession; that type's no-statics contract stays.</summary>
    public static class PlayerLaunch
    {
        #region Constants
        public const string HostArg = "-gkb-host";
        public const string JoinArg = "-gkb-join";
        public const string QuitAfterArg = "-gkb-quit-after";
        const string Tag = "Launch";
        #endregion

        #region Public API
        /// <summary>Reads process args. Host/join loads the campaign and Bastion.
        /// Returns true when Splash should skip the key wait.</summary>
        public static bool TryBootFromCommandLine()
        {
            if (!TryParse(Environment.GetCommandLineArgs(), out Parsed parsed))
                return false;
            if (parsed.Kind == NetJoinIntent.Kind.None)
                return false;

            if (GameSession.HasSave) GameSession.ContinueGame();
            else GameSession.StartNew();

            if (parsed.Kind == NetJoinIntent.Kind.Host)
            {
                NetJoinIntent.LastError = null;
                NetJoinIntent.RequestHost();
                Log.Event(Tag, "host boot port=" + parsed.Port);
            }
            else
            {
                if (!NetJoinIntent.IsJoinableAddress(parsed.Address))
                {
                    Log.Warn(Tag, "join boot refused — 0.0.0.0 is not a destination");
                    return false;
                }
                NetJoinIntent.LastError = null;
                NetJoinIntent.RequestJoin(parsed.Address, parsed.Port);
                Log.Event(Tag, "join boot address=" + parsed.Address + ":" + parsed.Port);
            }

            if (parsed.QuitAfterSeconds > 0f)
                ArmQuit(parsed.QuitAfterSeconds);

            return SceneFlow.LoadBastion();
        }

        /// <summary>Parses <paramref name="args"/> without touching session state.
        /// Join value is IPv4 or IPv4:port. Missing join value is a parse miss.</summary>
        public static bool TryParse(string[] args, out Parsed parsed)
        {
            parsed = new Parsed
            {
                Kind = NetJoinIntent.Kind.None,
                Address = null,
                Port = NetJoinIntent.DefaultPort,
                QuitAfterSeconds = 0f
            };
            if (args == null) return false;

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                if (a == HostArg)
                {
                    parsed.Kind = NetJoinIntent.Kind.Host;
                    continue;
                }
                if (a == JoinArg)
                {
                    if (i + 1 >= args.Length) return false;
                    if (!TryParseJoinTarget(args[++i], out parsed.Address, out parsed.Port))
                        return false;
                    parsed.Kind = NetJoinIntent.Kind.Join;
                    continue;
                }
                if (a == QuitAfterArg)
                {
                    if (i + 1 >= args.Length) return false;
                    if (!TryParseSeconds(args[++i], out parsed.QuitAfterSeconds))
                        return false;
                }
            }

            return parsed.Kind != NetJoinIntent.Kind.None;
        }
        #endregion

        #region Private
        static bool TryParseJoinTarget(string text, out string address, out ushort port)
        {
            address = null;
            port = NetJoinIntent.DefaultPort;
            if (string.IsNullOrEmpty(text)) return false;
            string trimmed = text.Trim();
            int colon = trimmed.LastIndexOf(':');
            if (colon < 0)
            {
                if (!NetJoinIntent.TryParseIpv4(trimmed, out address)) return false;
            }
            else
            {
                if (!NetJoinIntent.TryParseIpv4(trimmed.Substring(0, colon), out address))
                    return false;
                if (!NetJoinIntent.TryParsePort(trimmed.Substring(colon + 1), out port))
                    return false;
            }
            return NetJoinIntent.IsJoinableAddress(address);
        }

        static bool TryParseSeconds(string text, out float seconds)
        {
            seconds = 0f;
            if (string.IsNullOrEmpty(text)) return false;
            int value = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (ch < '0' || ch > '9') return false;
                value = value * 10 + (ch - '0');
                if (value > 600) return false;
            }
            if (value < 1) return false;
            seconds = value;
            return true;
        }

        static void ArmQuit(float seconds)
        {
            var go = new GameObject("PlayerLaunchQuit");
            UnityEngine.Object.DontDestroyOnLoad(go);
            var quit = go.AddComponent<PlayerLaunchQuit>();
            quit.Seconds = seconds;
        }
        #endregion

        #region Nested Types
        public struct Parsed
        {
            public NetJoinIntent.Kind Kind;
            public string Address;
            public ushort Port;
            public float QuitAfterSeconds;
        }

        /// <summary>LOCAL-OWNER: quits after the launch harness has had time to read logs.</summary>
        sealed class PlayerLaunchQuit : MonoBehaviour
        {
            public float Seconds;
            float _elapsed;

            void Update()
            {
                _elapsed += Time.unscaledDeltaTime;
                if (_elapsed < Seconds) return;
                enabled = false;
                SceneFlow.QuitGame();
            }
        }
        #endregion
    }
}
