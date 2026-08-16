using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GatekeeperBastion.Util;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: Unity Relay allocate / join. No NetworkManager — allocation
    /// happens on Main Menu, then <see cref="Apply"/> pushes the payload onto Bastion's
    /// UnityTransport. Anonymous sign-in. No Lobby.</summary>
    public static class RelayJoin
    {
        #region Constants
        const string Tag = "Net";
        public const int MaxGuestConnections = 3;
        #endregion

        #region Nested Types
        public struct Payload
        {
            public string Ipv4;
            public ushort Port;
            public byte[] AllocationId;
            public byte[] Key;
            public byte[] ConnectionData;
            public byte[] HostConnectionData;
            public bool IsHost;
        }

        public struct Result
        {
            public bool Ok;
            public string Error;
            public string JoinCode;
            public Payload Payload;
        }
        #endregion

        #region Public API
        public static bool HasPayload(Payload payload) =>
            payload.AllocationId != null && payload.AllocationId.Length > 0;

        public static async Task<Result> TryHost(int maxConnections = MaxGuestConnections)
        {
            Result init = await EnsureSignedIn();
            if (!init.Ok) return init;
            try
            {
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
                if (!TryEndpoint(allocation.ServerEndpoints, allocation.RelayServer, out string ipv4, out ushort port))
                    return Fail("Relay allocate failed — no dtls endpoint.");
                string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                Log.Event(Tag, "relay allocated joinCode=" + code);
                return new Result
                {
                    Ok = true,
                    JoinCode = code,
                    Payload = FromBytes(allocation.AllocationIdBytes, allocation.Key,
                        allocation.ConnectionData, ipv4, port, hostConnectionData: null, isHost: true)
                };
            }
            catch (Exception e)
            {
                return Fail("Relay allocate failed — " + Named(e));
            }
        }

        public static async Task<Result> TryJoin(string joinCode)
        {
            Result init = await EnsureSignedIn();
            if (!init.Ok) return init;
            try
            {
                JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                if (!TryEndpoint(allocation.ServerEndpoints, allocation.RelayServer, out string ipv4, out ushort port))
                    return Fail("Relay join failed — no dtls endpoint.");
                Log.Event(Tag, "relay joined joinCode=" + joinCode);
                return new Result
                {
                    Ok = true,
                    JoinCode = joinCode,
                    Payload = FromBytes(allocation.AllocationIdBytes, allocation.Key,
                        allocation.ConnectionData, ipv4, port, allocation.HostConnectionData, isHost: false)
                };
            }
            catch (Exception e)
            {
                return Fail("Relay join failed — " + Named(e));
            }
        }

        public static void Apply(UnityTransport transport, Payload payload)
        {
            if (transport == null) return;
            if (payload.IsHost)
            {
                transport.SetHostRelayData(
                    payload.Ipv4, payload.Port, payload.AllocationId,
                    payload.Key, payload.ConnectionData, isSecure: true);
                return;
            }
            transport.SetClientRelayData(
                payload.Ipv4, payload.Port, payload.AllocationId,
                payload.Key, payload.ConnectionData, payload.HostConnectionData, isSecure: true);
        }
        #endregion

        #region Private
        static async Task<Result> EnsureSignedIn()
        {
            if (string.IsNullOrEmpty(Application.cloudProjectId))
                return Fail("Unity project is not linked — Relay unavailable.");
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                    await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                return new Result { Ok = true };
            }
            catch (Exception e)
            {
                return Fail("Relay sign-in failed — " + Named(e));
            }
        }

        static bool TryEndpoint(
            IList<RelayServerEndpoint> endpoints, RelayServer server, out string ipv4, out ushort port)
        {
            ipv4 = null;
            port = 0;
            if (endpoints != null)
            {
                for (int i = 0; i < endpoints.Count; i++)
                {
                    var ep = endpoints[i];
                    if (ep == null) continue;
                    if (!string.Equals(ep.ConnectionType, "dtls", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (string.IsNullOrEmpty(ep.Host)) continue;
                    ipv4 = ep.Host;
                    port = (ushort)ep.Port;
                    return true;
                }
            }
            if (server == null || string.IsNullOrEmpty(server.IpV4)) return false;
            ipv4 = server.IpV4;
            port = (ushort)server.Port;
            return true;
        }

        static Payload FromBytes(
            byte[] allocationId, byte[] key, byte[] connectionData,
            string ipv4, ushort port, byte[] hostConnectionData, bool isHost)
        {
            return new Payload
            {
                Ipv4 = ipv4,
                Port = port,
                AllocationId = allocationId,
                Key = key,
                ConnectionData = connectionData,
                HostConnectionData = hostConnectionData,
                IsHost = isHost
            };
        }

        static Result Fail(string error)
        {
            Log.Event(Tag, error);
            return new Result { Ok = false, Error = error };
        }

        static string Named(Exception e)
        {
            if (e == null) return "unknown";
            string msg = e.Message;
            if (string.IsNullOrEmpty(msg)) return e.GetType().Name;
            return msg;
        }
        #endregion
    }
}
