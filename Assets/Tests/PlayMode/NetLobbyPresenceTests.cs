using System;
using System.Collections;
using System.Reflection;
using GatekeeperBastion.Game;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace GatekeeperBastion.PlayTests
{
    /// <summary>In-process host + one client. Locks the 2026-08-16 dual-box presence bugs:
    /// owner puppet tracks FPC after late bind; mirror Spawn is unparented; both seat
    /// avatars exist on host and client; remote Animator Speed drives from motion.</summary>
    public class NetLobbyPresenceTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        const float TrackMaxDistance = 0.5f;
        const string BoarderControllerPath = "Assets/Packs/Boarder/Boarder.controller";
        static readonly int SpeedHash = Animator.StringToHash("Speed");

        GameObject _avatarPrefab;
        GameObject _mirrorPrefab;
        GameObject _fpcGo;

        [UnityTearDown]
        public IEnumerator DestroyLocals()
        {
            if (_fpcGo != null)
            {
                UnityEngine.Object.Destroy(_fpcGo);
                _fpcGo = null;
            }
            yield return null;
        }

        protected override void OnServerAndClientsCreated()
        {
            for (int i = 0; i < m_NetworkManagers.Length; i++)
                m_NetworkManagers[i].NetworkConfig.EnableSceneManagement = false;

            _avatarPrefab = CreateNetworkObjectPrefab("NetLobbyPresenceAvatar");
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

            _mirrorPrefab = CreateNetworkObjectPrefab("NetLobbyPresenceMirror");
            _mirrorPrefab.AddComponent<NetRunMirror>();
        }

        /// <summary>Bug 1: host avatar spawned before FPC; after FPC appears, owner avatar
        /// must track (distance &lt; 0.5 m), not stay at origin.</summary>
        [UnityTest]
        public IEnumerator OwnerAvatar_TracksFpcAfterLateBind()
        {
            var serverGo = SpawnObject(_avatarPrefab, m_ServerNetworkManager);
            var serverNet = serverGo.GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(serverNet);
            AssertOnTimeout("avatar did not spawn on client");

            serverGo.transform.position = Vector3.zero;
            yield return null;
            Assert.Less(
                Vector3.Distance(serverGo.transform.position, Vector3.zero),
                TrackMaxDistance,
                "pre-FPC avatar should still be near origin");

            var target = new Vector3(12f, 0f, 7f);
            _fpcGo = CreateFpcAt(target);
            yield return null;
            yield return null;
            yield return WaitForConditionOrTimeOut(() =>
                Vector3.Distance(serverGo.transform.position, _fpcGo.transform.position)
                < TrackMaxDistance);
            AssertOnTimeout("owner avatar did not track FPC after late bind");
            Assert.Less(
                Vector3.Distance(serverGo.transform.position, _fpcGo.transform.position),
                TrackMaxDistance,
                "owner avatar must follow FPC within 0.5 m after LateUpdate rebind");
        }

        /// <summary>Bug 2: production NetSession.SpawnUnparentedMirror — Spawn before any
        /// parent; never parent under NetSession. destroyWithScene false; client has object.</summary>
        [UnityTest]
        public IEnumerator SpawnMirror_SpawnsUnparented_DestroyWithSceneFalse()
        {
            var net = NetSession.SpawnUnparentedMirror(_mirrorPrefab);
            Assert.IsNotNull(net, "SpawnUnparentedMirror must return a NetworkObject");
            Assert.IsTrue(net.IsSpawned, "mirror must spawn without a NetSession parent");
            Assert.IsNull(net.transform.parent, "mirror must stay unparented after Spawn");
            Assert.IsFalse(net.DestroyWithScene, "lobby mirror must survive scene loads");

            yield return WaitForSpawnedOnAllOrTimeOut(net);
            AssertOnTimeout("mirror did not spawn on client");
            Assert.IsTrue(
                m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.ContainsKey(
                    net.NetworkObjectId),
                "client has the mirror clone");
        }

        /// <summary>Bug 3: host (clientId 0) and client avatars exist as spawned
        /// NetworkObjects on host and client managers after connect.</summary>
        [UnityTest]
        public IEnumerator BothSeats_AvatarsPresentOnHostAndClient()
        {
            ulong hostId = m_ServerNetworkManager.LocalClientId;
            ulong clientId = m_ClientNetworkManagers[0].LocalClientId;
            Assert.AreEqual(0UL, hostId, "listen-server host is clientId 0");

            var hostGo = SpawnPlayerObject(_avatarPrefab, hostId);
            var clientGo = SpawnPlayerObject(_avatarPrefab, clientId);
            var hostNet = hostGo.GetComponent<NetworkObject>();
            var clientNet = clientGo.GetComponent<NetworkObject>();
            Assert.IsNotNull(hostNet);
            Assert.IsNotNull(clientNet);

            yield return WaitForSpawnedOnAllOrTimeOut(hostNet);
            AssertOnTimeout("host avatar did not spawn on client");
            yield return WaitForSpawnedOnAllOrTimeOut(clientNet);
            AssertOnTimeout("client avatar did not spawn on client");

            Assert.IsTrue(
                m_ServerNetworkManager.SpawnManager.SpawnedObjects.ContainsKey(
                    hostNet.NetworkObjectId),
                "host manager has host avatar");
            Assert.IsTrue(
                m_ServerNetworkManager.SpawnManager.SpawnedObjects.ContainsKey(
                    clientNet.NetworkObjectId),
                "host manager has client avatar");
            Assert.IsTrue(
                m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.ContainsKey(
                    hostNet.NetworkObjectId),
                "client manager has host avatar");
            Assert.IsTrue(
                m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.ContainsKey(
                    clientNet.NetworkObjectId),
                "client manager has client avatar");

            Assert.IsTrue(
                m_ServerNetworkManager.ConnectedClients.TryGetValue(hostId, out var hostClient)
                && hostClient.PlayerObject != null
                && hostClient.PlayerObject.NetworkObjectId == hostNet.NetworkObjectId,
                "host ConnectedClients PlayerObject is the host avatar");
            Assert.IsTrue(
                m_ServerNetworkManager.ConnectedClients.TryGetValue(clientId, out var guestClient)
                && guestClient.PlayerObject != null
                && guestClient.PlayerObject.NetworkObjectId == clientNet.NetworkObjectId,
                "host ConnectedClients PlayerObject is the client avatar");
        }

        /// <summary>Bug 4: after moving a remote avatar transform, Animator Speed &gt; 0.</summary>
        [UnityTest]
        public IEnumerator DriveRemoteAnimator_SetsSpeedWhenMoved()
        {
            var anim = _avatarPrefab.GetComponentInChildren<Animator>(true);
            if (anim == null || anim.runtimeAnimatorController == null)
            {
                Assert.Ignore("no Animator/controller on avatar prefab — Boarder.controller missing");
                yield break;
            }

            var serverGo = SpawnObject(_avatarPrefab, m_ServerNetworkManager);
            var serverNet = serverGo.GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(serverNet);
            AssertOnTimeout("avatar did not spawn on client");

            serverNet.ChangeOwnership(m_ClientNetworkManagers[0].LocalClientId);
            yield return WaitForConditionOrTimeOut(() =>
            {
                var avatar = serverGo.GetComponent<NetPlayerAvatar>();
                return avatar != null && !avatar.IsOwner;
            });
            AssertOnTimeout("server avatar did not become remote after ChangeOwnership");

            var remoteAnim = serverGo.GetComponentInChildren<Animator>(true);
            Assert.IsNotNull(remoteAnim, "remote avatar must carry the test Animator");

            // Owner-authority NetworkTransform would overwrite non-owner writes; this test
            // drives the remote LateUpdate path directly.
            var nt = serverGo.GetComponent<NetworkTransform>();
            if (nt != null)
                nt.enabled = false;

            // First LateUpdate samples position only (Speed stays 0).
            serverGo.transform.position = new Vector3(0f, 0f, 0f);
            yield return null;

            // Move well above MoveEpsMps (0.4) across several frames.
            for (int i = 0; i < 4; i++)
            {
                serverGo.transform.position += new Vector3(2f, 0f, 0f);
                yield return null;
            }

            float speed = remoteAnim.GetFloat(SpeedHash);
            Assert.Greater(speed, 0f, "DriveRemoteAnimator must set Speed > 0 after planar move");
        }

        GameObject SpawnPlayerObject(GameObject prefab, ulong ownerClientId)
        {
            var go = UnityEngine.Object.Instantiate(prefab);
            var net = go.GetComponent<NetworkObject>();
            Assert.IsNotNull(net);
            net.SpawnAsPlayerObject(ownerClientId, destroyWithScene: false);
            return go;
        }

        static GameObject CreateFpcAt(Vector3 position)
        {
            var go = new GameObject("PresenceTestFpc");
            go.SetActive(false);
            go.AddComponent<CharacterController>();
            var cam = new GameObject("Cam");
            cam.transform.SetParent(go.transform, false);
            var fpc = go.AddComponent<FirstPersonController>();
            fpc.cameraTransform = cam.transform;
            go.transform.position = position;
            go.SetActive(true);
            return go;
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
