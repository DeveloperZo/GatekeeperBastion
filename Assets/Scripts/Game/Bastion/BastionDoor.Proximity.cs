using UnityEngine;
using UnityEngine.AI;

namespace GatekeepersProtocol.Game
{
    public sealed partial class BastionDoor
    {
        const int ProximityHitCap = 64;

        static readonly Collider[] ProximityHits = new Collider[ProximityHitCap];

        /// <summary>True when <paramref name="onHit"/> may raise a slab of this kind.
        /// EditMode seeds this without a live door.</summary>
        public static bool CountsAsOpener(BastionDoorOpensFor forWhom, Component onHit)
        {
            if (onHit == null)
                return false;
            if (forWhom == BastionDoorOpensFor.Enemies)
                return onHit.GetComponentInParent<EnemyGatekeeper>() != null;
            return onHit.GetComponentInParent<CharacterController>() != null
                || onHit.GetComponentInParent<NavMeshAgent>() != null
                || onHit.GetComponentInParent<BoarderPresentation>() != null;
        }

        bool AnyoneNear()
        {
            Vector3 center = transform.TransformPoint(0f, _apertureSize.y * 0.5f, 0f);
            var half = new Vector3(_apertureSize.x * 0.5f + 1f, _apertureSize.y * 0.5f, ProximityRange);
            Quaternion rot = transform.rotation;
            // Walls, pack, and a second Generated stamp fill OverlapBoxNonAlloc's buffer
            // before the CharacterController. Passage doors still read the Boarder here.
            if (opensFor != BastionDoorOpensFor.Enemies && PlayerInProximity(center, half, rot))
                return true;

            int count = Physics.OverlapBoxNonAlloc(center, half, ProximityHits, rot);
            for (int i = 0; i < count; i++)
            {
                Collider hit = ProximityHits[i];
                if (hit == null || hit == _blockCollider)
                    continue;
                if (CountsAsOpener(opensFor, hit))
                    return true;
            }
            return false;
        }

        /// <summary>True when a registered Boarder is inside the proximity OBB.
        /// EditMode seeds this without a live door.</summary>
        public static bool PlayerInProximity(Vector3 center, Vector3 halfExtents, Quaternion rotation)
        {
            var players = PlayerRegistry.All;
            for (int i = 0; i < players.Count; i++)
            {
                PlayerHealth player = players[i];
                if (player == null)
                    continue;
                if (InProximityBox(player.transform.position, center, halfExtents, rotation))
                    return true;
            }
            return false;
        }

        public static bool InProximityBox(
            Vector3 worldPoint, Vector3 center, Vector3 halfExtents, Quaternion rotation)
        {
            Vector3 local = Quaternion.Inverse(rotation) * (worldPoint - center);
            return Mathf.Abs(local.x) <= halfExtents.x
                && Mathf.Abs(local.y) <= halfExtents.y
                && Mathf.Abs(local.z) <= halfExtents.z;
        }
    }
}
