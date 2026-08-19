using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: the demo's Ship-scene host — binds the fight wires into
    /// <see cref="DemoShipFight"/>, ticks it, and resolves the beat 0 reload matrix on
    /// load: capsule wake, on-deck-at-capsule, deck-at-NAV, mid-fight respawn, or
    /// dormant when the demo is off. Intruders respawn from flags — instances are never
    /// serialized (beat 0, Reload table).</summary>
    public sealed class DemoShipDirector : MonoBehaviour
    {
        const string Tag = "Demo";
        const float BindRetrySeconds = 0.25f;

        [SerializeField, Tooltip("Wake capsule in the crew alcove. Authored by the scratch scene pass.")]
        ShipCapsule _capsule;

        [SerializeField, Tooltip("Bastion gatekeeper prefab — body only; ShipIntruder drives it here.")]
        GameObject _intruderPrefab;

        [SerializeField, Tooltip("FAB-side corridor intruder spawn.")]
        Transform _spawnIntruder1;

        [SerializeField, Tooltip("Mid-corridor intruder spawn.")]
        Transform _spawnIntruder2;

        [SerializeField, Tooltip("Ship Directional Light. Demo dims to 0.15 (A6); demo-off restores the authored intensity.")]
        Light _hull;

        [SerializeField, Tooltip("Deck pose at the capsule exit — quit-after-unlock, no reseal.")]
        Transform _spawnCapsule;

        [SerializeField, Tooltip("Deck pose near NAV for post-capsule reloads (beat 0 reload matrix).")]
        Transform _spawnNav;

        bool _placed;
        float _retryTimer;
        float _hullLive = -1f;

        void OnEnable()
        {
            DemoShipFight.BindScene(_intruderPrefab, _spawnIntruder1, _spawnIntruder2);
        }

        void OnDisable()
        {
            DemoShipFight.ClearScene();
        }

        void Update()
        {
            if (!_placed)
            {
                _retryTimer -= Time.deltaTime;
                if (_retryTimer <= 0f)
                {
                    _retryTimer = BindRetrySeconds;
                    TryResolveSpawn();
                }
            }
            DemoShipFight.Tick();
        }

        /// <summary>Beat 0 reload matrix. Runs once, as soon as the rig has registered.</summary>
        void TryResolveSpawn()
        {
            var m = GameSession.Meta;
            if (m == null) return;

            if (!m.DemoActive)
            {
                _placed = true;
                ApplyHullLight(dead: false);
                if (_capsule != null) _capsule.BeginDormant();
                return;
            }

            ApplyHullLight(dead: true);

            var player = PlayerRegistry.Local;
            if (player == null) return; // rig not registered yet — retry
            var fpc = player.GetComponent<FirstPersonController>();
            _placed = true;

            if (!m.DemoCapsuleOpened)
            {
                if (_capsule != null) _capsule.BeginSealed(fpc);
                else Log.Error(Tag, "no capsule wired — author the Ship scene pieces "
                    + "(scratch: Author Demo Wake Ship).");
                return;
            }

            if (_capsule != null) _capsule.BeginOpen();

            if (!m.DemoNavPowered)
            {
                // Quit after unlock, before NAV: on deck at the capsule, no reseal.
                PlaceAt(player, _spawnCapsule);
                DemoShipFight.SetStanding("Repair the NAV MAP");
                return;
            }

            PlaceAt(player, _spawnNav);

            if (!m.DemoArmoryPowered)
            {
                DemoShipFight.SetStanding("Start the ship terminal");
                return;
            }

            if (!m.DemoShipFightDone)
            {
                // Terminal started, fight unresolved: both intruders respawn from flags.
                // The start granted the MK-1 in the same save; a pre-revision save that
                // powered the ARMORY without the grant gets it here, and replays the
                // line it never heard (release gated on its Continue).
                bool lateGrant = !m.DemoWeaponGranted;
                if (lateGrant) DemoShipFight.GrantWeapon(m);
                DemoShipFight.ResumeArmed(replayLine: lateGrant);
                DemoShipFight.SetStanding("Kill the Gatekeepers");
                return;
            }

            DemoShipFight.SetStanding("EMBARK from the NAV MAP");
        }

        static void PlaceAt(PlayerHealth player, Transform pose)
        {
            if (player == null || pose == null) return;
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            // XZ from the marker; Y stays the rig's own authored height. The deck is one
            // flat level, and a marker Y below the rig's pivot height starts the
            // CharacterController inside the floor — fall-through on the first physics
            // step (2026-08-18 defect, same class as the bed pose).
            Vector3 at = pose.position;
            at.y = player.transform.position.y;
            // YAW ONLY on the body: FirstPersonController owns pitch on the camera, and a
            // marker rotation with pitch tilts the yaw axis and skews mouse look
            // (2026-08-18 orientation defect).
            float yaw = pose.rotation.eulerAngles.y;
            player.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            if (cc != null) cc.enabled = true;
        }

        /// <summary>A6: demo hull is 0.15; live Ship is the authored intensity on
        /// <see cref="_hull"/> (scratch wires the scene Directional Light).</summary>
        void ApplyHullLight(bool dead)
        {
            if (_hull == null) return;
            if (_hullLive < 0f) _hullLive = _hull.intensity;
            _hull.intensity = dead ? 0.15f : _hullLive;
        }
    }
}
