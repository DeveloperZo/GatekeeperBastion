using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GatekeepersProtocol.Tests
{
    public class SpawnRoomPrefabTests
    {
        [Test]
        public void Prefab_InteriorAndSpawnBehindDoor()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpawnRoom.PrefabPath);
            Assert.IsNotNull(prefab, SpawnRoom.PrefabPath);
            var room = prefab.GetComponent<SpawnRoom>();
            Assert.IsNotNull(room);
            var door = prefab.GetComponentInChildren<BastionDoor>(true);
            Assert.IsNotNull(door);
            Assert.AreEqual(BastionDoorOpensFor.Enemies, door.OpensFor);
            Assert.IsFalse(door.StartsLocked);
            var spawn = prefab.GetComponentInChildren<SpawnPoint>(true);
            Assert.IsNotNull(spawn);
            Assert.LessOrEqual(spawn.transform.localPosition.z, -2.5f);
            Transform floor = prefab.transform.Find("Floor");
            Assert.IsNotNull(floor);
            Assert.AreEqual(SpawnRoom.InteriorWidth, floor.localScale.x, 0.05f);
            Assert.AreEqual(SpawnRoom.InteriorDepth + BastionKit.WallThickness, floor.localScale.z, 0.05f);
            Transform ceiling = prefab.transform.Find("Ceiling");
            Assert.IsNotNull(ceiling);
            Assert.AreEqual(
                BastionKit.StoreyHeight,
                ceiling.localPosition.y + ceiling.localScale.y * 0.5f,
                0.05f);
        }

        [Test]
        public void PunchDoorEdge_West_PlacesDoorframeWhenKitMapped()
        {
            var kit = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bastion/Kit/Half.prefab");
            Assert.IsNotNull(kit);
            var src = kit.GetComponent<BastionSegment>();
            Assert.IsNotNull(src);

            var go = new GameObject("seg");
            var segment = go.AddComponent<BastionSegment>();
            segment.BlockKind = BastionBlockKind.Half;
            segment.FloorPiece = src.FloorPiece;
            segment.CeilingPiece = src.CeilingPiece;
            segment.WallPiece = src.WallPiece;
            segment.DoorframePiece = src.DoorframePiece;
            segment.WindowPiece = src.WindowPiece;
            segment.CornerPiece = src.CornerPiece;
            segment.CeilingLightPiece = src.CeilingLightPiece;
            segment.North = BastionSideKind.Wall;
            segment.East = BastionSideKind.Wall;
            segment.South = BastionSideKind.Wall;
            segment.West = BastionSideKind.Wall;
            segment.Rebuild();
            segment.PunchDoorEdge(BastionDoorEdge.West);
            Assert.IsTrue(segment.HasDoorOpeningOn(BastionDoorEdge.West));
            Assert.IsFalse(HasNamed(go.transform, "Gen_West_Wall"));
            Object.DestroyImmediate(go);
        }

        static bool HasNamed(Transform root, string exact)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == exact)
                    return true;
            }
            return false;
        }
    }
}
