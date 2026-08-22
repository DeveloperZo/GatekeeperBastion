using System;
using System.Collections;
using System.Reflection;
using GatekeepersProtocol.Game;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeepersProtocol.PlayTests
{
    /// <summary>In-process host + one client. Remote Boarder.controller Fire/Reload
    /// triggers on non-owner avatars; the owner animator is not driven.</summary>
    public class NetAvatarAnimTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        const string BoarderControllerPath = "Assets/Packs/Boarder/Boarder.controller";

        GameObject _avatarPrefab;

        protected override void OnServerAndClientsCreated()
        {
            for (int i = 0; i < m_NetworkManagers.Length; i++)
                m_NetworkManagers[i].NetworkConfig.EnableSceneManagement = false;

            _avatarPrefab = CreateNetworkObjectPrefab("NetAvatarAnimAvatar");
            _avatarPrefab.SetActive(false);
            _avatarPrefab.AddComponent<CapsuleCollider>();
            var health = _avatarPrefab.AddComponent<PlayerHealth>();
            health.ContributeToRegistry = false;
            health.EndsTheRunOnDeath = false;
            var nt = _avatarPrefab.AddComponent<NetworkTransform>();
            nt.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            AttachTestAnimator(_avatarPrefab);
            _avatarPrefab.AddComponent<NetPlayerAvatar>();
            _avatarPrefab.SetActive(true);
        }

        [UnityTest]
        public IEnumerator NotifyFired_RemoteEntersFire_OwnerDoesNot()
        {
            if (!ControllerPresent())
            {
                Assert.Ignore("no Animator/controller on avatar prefab — Boarder.controller missing");
                yield break;
            }

            SeatPair seats = default;
            yield return SpawnBothSeats(result => seats = result);

            var guestOwner = OwnerAvatar(seats.GuestNet, m_ClientNetworkManagers[0]);
            Assert.IsNotNull(guestOwner);
            Assert.IsTrue(guestOwner.IsOwner, "NotifyFired must run on the owning client");
            guestOwner.NotifyFired();

            yield return WaitForConditionOrTimeOut(() =>
                InState(RemoteAnimator(seats.GuestNet, m_ServerNetworkManager), "Fire"));
            AssertOnTimeout("host clone of guest avatar did not enter Fire");
            Assert.IsFalse(
                InState(OwnerAnimator(seats.GuestNet, m_ClientNetworkManagers[0]), "Fire"),
                "guest owner animator must not take the Fire trigger");

            var hostOwner = seats.HostGo.GetComponent<NetPlayerAvatar>();
            Assert.IsNotNull(hostOwner);
            Assert.IsTrue(hostOwner.IsOwner);
            hostOwner.NotifyFired();

            yield return WaitForConditionOrTimeOut(() =>
                InState(RemoteAnimator(seats.HostNet, m_ClientNetworkManagers[0]), "Fire"));
            AssertOnTimeout("client clone of host avatar did not enter Fire");
            Assert.IsFalse(
                InState(seats.HostGo.GetComponentInChildren<Animator>(true), "Fire"),
                "host owner animator must not take the Fire trigger");
        }

        [UnityTest]
        public IEnumerator NotifyReloadStarted_RemoteEntersReload_OwnerDoesNot()
        {
            if (!ControllerPresent())
            {
                Assert.Ignore("no Animator/controller on avatar prefab — Boarder.controller missing");
                yield break;
            }

            SeatPair seats = default;
            yield return SpawnBothSeats(result => seats = result);

            var guestOwner = OwnerAvatar(seats.GuestNet, m_ClientNetworkManagers[0]);
            Assert.IsNotNull(guestOwner);
            guestOwner.NotifyReloadStarted();

            yield return WaitForConditionOrTimeOut(() =>
                InState(RemoteAnimator(seats.GuestNet, m_ServerNetworkManager), "Reload"));
            AssertOnTimeout("host clone of guest avatar did not enter Reload");
            Assert.IsFalse(
                InState(OwnerAnimator(seats.GuestNet, m_ClientNetworkManagers[0]), "Reload"),
                "guest owner animator must not take the Reload trigger");
        }

        struct SeatPair
        {
            public GameObject HostGo;
            public NetworkObject HostNet;
            public GameObject GuestGo;
            public NetworkObject GuestNet;
        }

        IEnumerator SpawnBothSeats(Action<SeatPair> done)
        {
            ulong hostId = m_ServerNetworkManager.LocalClientId;
            ulong guestId = m_ClientNetworkManagers[0].LocalClientId;
            var hostGo = SpawnPlayerObject(_avatarPrefab, hostId);
            var guestGo = SpawnPlayerObject(_avatarPrefab, guestId);
            var hostNet = hostGo.GetComponent<NetworkObject>();
            var guestNet = guestGo.GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(hostNet);
            AssertOnTimeout("host avatar did not spawn on client");
            yield return WaitForSpawnedOnAllOrTimeOut(guestNet);
            AssertOnTimeout("guest avatar did not spawn on client");
            done(new SeatPair
            {
                HostGo = hostGo,
                HostNet = hostNet,
                GuestGo = guestGo,
                GuestNet = guestNet
            });
        }

        GameObject SpawnPlayerObject(GameObject prefab, ulong ownerClientId)
        {
            var go = UnityEngine.Object.Instantiate(prefab);
            var net = go.GetComponent<NetworkObject>();
            Assert.IsNotNull(net);
            net.SpawnAsPlayerObject(ownerClientId, destroyWithScene: false);
            return go;
        }

        static NetPlayerAvatar OwnerAvatar(NetworkObject serverNet, NetworkManager ownerNm)
        {
            if (!ownerNm.SpawnManager.SpawnedObjects.TryGetValue(serverNet.NetworkObjectId, out var clone))
                return null;
            return clone.GetComponent<NetPlayerAvatar>();
        }

        static Animator OwnerAnimator(NetworkObject serverNet, NetworkManager ownerNm)
        {
            if (!ownerNm.SpawnManager.SpawnedObjects.TryGetValue(serverNet.NetworkObjectId, out var clone))
                return null;
            return clone.GetComponentInChildren<Animator>(true);
        }

        static Animator RemoteAnimator(NetworkObject serverNet, NetworkManager observerNm)
        {
            if (!observerNm.SpawnManager.SpawnedObjects.TryGetValue(serverNet.NetworkObjectId, out var clone))
                return null;
            return clone.GetComponentInChildren<Animator>(true);
        }

        static bool InState(Animator animator, string state)
        {
            if (animator == null) return false;
            if (animator.GetCurrentAnimatorStateInfo(0).IsName(state)) return true;
            if (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName(state))
                return true;
            return false;
        }

        bool ControllerPresent()
        {
            var anim = _avatarPrefab.GetComponentInChildren<Animator>(true);
            return anim != null && anim.runtimeAnimatorController != null;
        }

        static void AttachTestAnimator(GameObject prefab)
        {
            var ctrl = LoadBoarderController();
            var child = new GameObject("Anim");
            child.transform.SetParent(prefab.transform, false);
            var animator = child.AddComponent<Animator>();
            if (ctrl != null)
                animator.runtimeAnimatorController = ctrl;
        }

        static RuntimeAnimatorController LoadBoarderController()
        {
            var adb = Type.GetType("UnityEditor.AssetDatabase,UnityEditor");
            if (adb == null) return null;
            var load = adb.GetMethod(
                "LoadAssetAtPath",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(Type) },
                null);
            if (load == null) return null;
            return load.Invoke(null, new object[] { BoarderControllerPath, typeof(RuntimeAnimatorController) })
                as RuntimeAnimatorController;
        }
    }
}
