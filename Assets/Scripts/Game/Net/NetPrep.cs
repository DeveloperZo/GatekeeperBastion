using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using Unity.Netcode;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY handlers / LOCAL-OWNER senders: Prep verbs from a client avatar.
    /// Juice and Wave start apply on the server sim once. Combat verbs stay on
    /// <see cref="NetVerbs"/>.</summary>
    public sealed class NetPrep : NetworkBehaviour
    {
        #region Constants
        const string Tag = "Net";
        const float PrepRange = 3.2f;
        #endregion

        #region State
        GameController _game;
        #endregion

        #region Unity Lifecycle
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsServer || _game != null) return;
            var session = NetworkManager.GetComponent<NetSession>();
            if (session != null)
                Bind(session.Game);
        }
        #endregion

        #region Public API
        public void Bind(GameController game)
        {
            _game = game;
        }

        public void RequestJuice(int delta)
        {
            RequestJuiceServerRpc(delta, transform.position);
        }

        public void RequestStartTerminal(string terminalId)
        {
            if (terminalId == null) terminalId = "";
            RequestStartTerminalServerRpc(terminalId, transform.position);
        }

        public void RequestStartHatchBank()
        {
            RequestStartHatchBankServerRpc(transform.position);
        }

        public void RequestStartMaster(string terminalId)
        {
            if (terminalId == null) terminalId = "";
            RequestStartMasterServerRpc(terminalId, transform.position);
        }
        #endregion

        #region Events
        [Rpc(SendTo.Server)]
        void RequestJuiceServerRpc(int delta, Vector3 from)
        {
            if (!IsServer) return;
            if (_game == null)
            {
                Log.Event(Tag, "juice refused — no host game");
                return;
            }
            _game.TryNetJuice(delta, from, PrepRange);
        }

        [Rpc(SendTo.Server)]
        void RequestStartTerminalServerRpc(string terminalId, Vector3 from)
        {
            if (!IsServer) return;
            if (_game == null)
            {
                Log.Event(Tag, "terminal start refused — no host game");
                return;
            }
            _game.TryNetStartTerminal(terminalId, from, PrepRange);
        }

        [Rpc(SendTo.Server)]
        void RequestStartHatchBankServerRpc(Vector3 from)
        {
            if (!IsServer) return;
            if (_game == null)
            {
                Log.Event(Tag, "hatch-bank start refused — no host game");
                return;
            }
            _game.TryNetStartHatchBank(from, PrepRange);
        }

        [Rpc(SendTo.Server)]
        void RequestStartMasterServerRpc(string terminalId, Vector3 from)
        {
            if (!IsServer) return;
            if (_game == null)
            {
                Log.Event(Tag, "master start refused — no host game");
                return;
            }
            _game.TryNetStartMaster(terminalId, from, PrepRange);
        }
        #endregion
    }
}
