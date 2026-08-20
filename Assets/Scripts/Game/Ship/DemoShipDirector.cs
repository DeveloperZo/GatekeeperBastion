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

        [SerializeField, Tooltip("Wake teleporter in the crew alcove. Authored by the scratch scene pass.")]
        ShipTeleporter _teleporter;

        [SerializeField, Tooltip("Bastion gatekeeper prefab — body only; ShipIntruder drives it here.")]
        GameObject _intruderPrefab;

        [SerializeField, Tooltip("FAB-side corridor intruder spawn.")]
        Transform _spawnIntruder1;

        [SerializeField, Tooltip("Mid-corridor intruder spawn.")]
        Transform _spawnIntruder2;

        [SerializeField, Tooltip("Ship Directional Light. Demo dims to 0.15 (A6); demo-off restores the authored intensity.")]
        Light _hull;

        [SerializeField, Tooltip("Deck pose at the teleporter exit — quit-after-release, no reseal.")]
        Transform _spawnTeleporter;

        [SerializeField, Tooltip("Deck pose near NAV for post-teleporter reloads (beat 0 reload matrix).")]
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
                if (_teleporter != null) _teleporter.BeginDormant();
                return;
            }

            ApplyHullLight(dead: true);

            var player = PlayerRegistry.Local;
            if (player == null) return; // rig not registered yet — retry
            var fpc = player.GetComponent<FirstPersonController>();
            _placed = true;

            if (!m.DemoCapsuleOpened)
            {
                if (_teleporter != null) _teleporter.BeginSealed(fpc);
                else Log.Error(Tag, "no teleporter wired — author the Ship scene pieces "
                    + "(scratch: Author Demo Wake Ship).");
                return;
            }

            if (_teleporter != null) _teleporter.BeginOpen();

            if (!m.DemoNavPowered)
            {
                // Quit after release, before NAV: on deck at the teleporter, no reseal.
                PlaceAt(player, _spawnTeleporter);
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

            if (m.HatchConversions >= 1)
            {
                ResolveFirstReturn(m);
                ResolveRungTwo(m);
                return;
            }

            DemoShipFight.SetStanding("EMBARK from the NAV MAP");
        }

        /// <summary>Docked with salvage banked (whether that Run ended Lost or Returned).
        /// The first such Docked plays the first-loss line, queues the CREW line behind it
        /// (DemoVoice's stated same-frame order), and writes the CREW menu unlock — the
        /// flag write is what makes both once-per-save. The standing is state-keyed from
        /// here on (DemoShipStanding).</summary>
        void ResolveFirstReturn(MetaState m)
        {
            if (!m.DemoCrewMenuUnlocked)
            {
                DemoVoice.Play(DemoConversationIds.FirstHatchDown, DemoVoiceMode.Lock);
                DemoVoice.Play(DemoConversationIds.CrewReady, DemoVoiceMode.Lock);
                m.DemoCrewMenuUnlocked = true;
                GameSession.Save();
                Log.Event(Tag, "CREW menu unlocked");
            }
            DemoShipStanding.Reevaluate();
        }

        /// <summary>Docked after zone 2's bank fell: the ladder's second rung opens — the
        /// PRIMARY rows go on sale and two more wheel buys land on their own counter, outside
        /// the rung-1 cap. Keyed on the bank loss the player just played, never on a count of
        /// endings, so a save that docked through zone 1 is offered the rack on its first
        /// ejection (Docs/Demo - Single/5-repeat-to-control-room.md). DemoRungTwoOpened is the
        /// once-per-save latch, and the flag write is what makes the line once-per-save too.
        ///
        /// Ordering — deliberately AFTER ResolveFirstReturn. Both can resolve on one Ship load:
        /// bank in zone 1 (D1 opens), push through without docking, lose zone 2's bank. That is
        /// three Lock lines against DemoVoice's one live slot and one queue slot, so one is
        /// displaced whichever order they arrive in. demo.first_hatch_down wins the frame by a
        /// hardcoded rule in DemoVoice, and the queue is latest-wins, so playing this line last
        /// puts it in the queue and displaces demo.crew_ready — the recoverable loss: the CREW
        /// menu still unlocks and CREW QUARTERS speaks on the walk, while nothing else would
        /// tell the player the rack changed. The displacement is logged by DemoVoice.</summary>
        void ResolveRungTwo(MetaState m)
        {
            if (!m.DemoZone2BankLost || m.DemoRungTwoOpened) return;
            DemoVoice.Play(DemoConversationIds.SecondLoss, DemoVoiceMode.Lock);
            Log.Event(Tag, "ladder rung=2 opened — primary + 2 buys");
            m.DemoRungTwoBuysLeft = 2;
            m.DemoRungTwoOpened = true;
            GameSession.Save();
            DemoShipStanding.Reevaluate();
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
