using GatekeepersProtocol.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

namespace GatekeepersProtocol.Tests
{
    public class BastionDoorOpensForTests
    {
        [Test]
        public void CountsAsOpener_Enemies_OnlyEnemyGatekeeper()
        {
            var enemy = new GameObject("gk");
            enemy.AddComponent<NavMeshAgent>();
            var gk = enemy.AddComponent<EnemyGatekeeper>();
            var boarder = new GameObject("boarder");
            var cc = boarder.AddComponent<CharacterController>();

            Assert.IsTrue(BastionDoor.CountsAsOpener(BastionDoorOpensFor.Enemies, gk));
            Assert.IsFalse(BastionDoor.CountsAsOpener(BastionDoorOpensFor.Enemies, cc));
            Assert.IsTrue(BastionDoor.CountsAsOpener(BastionDoorOpensFor.Anyone, gk));
            Assert.IsTrue(BastionDoor.CountsAsOpener(BastionDoorOpensFor.Anyone, cc));

            Object.DestroyImmediate(enemy);
            Object.DestroyImmediate(boarder);
        }

        [Test]
        public void InProximityBox_ContainsPointInsideHalfExtents()
        {
            var center = new Vector3(0f, 1.75f, 0f);
            var half = new Vector3(2.5f, 1.75f, 5f);
            Assert.IsTrue(BastionDoor.InProximityBox(
                new Vector3(0f, 0f, 2f), center, half, Quaternion.identity));
            Assert.IsFalse(BastionDoor.InProximityBox(
                new Vector3(0f, 0f, 8f), center, half, Quaternion.identity));
        }
    }
}
