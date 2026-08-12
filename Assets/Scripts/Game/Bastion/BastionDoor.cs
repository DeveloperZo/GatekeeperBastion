using UnityEngine;
using UnityEngine.AI;

namespace GatekeeperBastion.Game
{
    /// <summary>Locked door gate — blocks walk until the unlock graph opens it.</summary>
    public sealed class BastionDoor : MonoBehaviour
    {
        [SerializeField] string doorId;
        [SerializeField] string[] requiredTerminalIds;
        [SerializeField] bool startsLocked = true;
        [SerializeField] bool opensControlRoom;
        [SerializeField] SpawnPoint[] sealedSpawns;

        BoxCollider _blockCollider;
        NavMeshObstacle _obstacle;
        BastionLayout _layout;

        public string DoorId => doorId;
        public string[] RequiredTerminalIds => requiredTerminalIds;
        public bool StartsLocked => startsLocked;
        public bool OpensControlRoom => opensControlRoom;
        public SpawnPoint[] SealedSpawns => sealedSpawns;

        /// <summary>Editor/sketch emit — assigns ids before play.</summary>
        public void Configure(string id, string[] requiredTerminals, bool controlGate, bool locked = true)
        {
            doorId = id;
            requiredTerminalIds = requiredTerminals != null
                ? requiredTerminals
                : System.Array.Empty<string>();
            opensControlRoom = controlGate;
            startsLocked = locked;
            EnsureBlockers();
        }

        public void SetSealedSpawns(SpawnPoint[] spawns)
        {
            sealedSpawns = spawns;
        }

        void Awake()
        {
            EnsureBlockers();
        }

        void EnsureBlockers()
        {
            _blockCollider = GetComponent<BoxCollider>();
            if (_blockCollider == null)
                _blockCollider = gameObject.AddComponent<BoxCollider>();
            _blockCollider.size = new Vector3(3f, 3.5f, 0.5f);
            _blockCollider.center = new Vector3(0f, 1.75f, 0f);

            _obstacle = GetComponent<NavMeshObstacle>();
            if (_obstacle == null)
                _obstacle = gameObject.AddComponent<NavMeshObstacle>();
            _obstacle.carving = true;
            _obstacle.size = new Vector3(3f, 3.5f, 0.5f);
            _obstacle.center = new Vector3(0f, 1.75f, 0f);
        }

        void OnEnable()
        {
            _layout = GetComponentInParent<BastionLayout>();
            if (_layout != null)
                _layout.RegisterDoor(this);
        }

        void OnDisable()
        {
            if (_layout != null)
                _layout.UnregisterDoor(this);
        }

        /// <summary>When closed, block walk; when open, clear block and unseal spawns.</summary>
        public void SetOpen(bool open)
        {
            if (_blockCollider != null)
                _blockCollider.enabled = !open;

            if (_obstacle != null)
            {
                _obstacle.enabled = !open;
                _obstacle.carving = !open;
            }

            if (sealedSpawns == null)
                return;

            for (int i = 0; i < sealedSpawns.Length; i++)
            {
                SpawnPoint sp = sealedSpawns[i];
                if (sp == null)
                    continue;
                sp.gameObject.SetActive(open);
            }
        }
    }
}
