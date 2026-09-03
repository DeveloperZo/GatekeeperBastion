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

        // Lost-player nudge: 45 quiet seconds before the first NAV repair replays the way to
        // the NAV MAP. Still earns its keep on one deck (2026-08-26) — the capsule faces AFT
        // and the map is at the far bow end, so a player who misses the "turn around" in the
        // wake line can walk the whole hull with their back to it. Instance state on purpose
        // — a player who quits and returns still lost hears it again next visit; anyone past
        // the repair never does.
        const float LostNudgeSeconds = 45f;
        float _lostNudgeTimer;
        bool _lostNudged;

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
            TickLostNudge();
        }

        void TickLostNudge()
        {
            if (_lostNudged) return;
            var m = GameSession.Meta;
            if (m == null || !m.DemoActive || m.DemoNavPowered) return;
            _lostNudgeTimer += Time.deltaTime;
            if (_lostNudgeTimer < LostNudgeSeconds) return;
            _lostNudged = true;
            DemoVoice.Play(DemoConversationIds.ShipLost, DemoVoiceMode.Subtitle);
        }

        /// <summary>Beat 0 reload matrix. Runs once, as soon as the rig has registered.</summary>
        void TryResolveSpawn()
        {
            var m = GameSession.Meta;
            if (m == null) return;

            if (GameSession.PendingEscapedLine)
            {
                GameSession.PendingEscapedLine = false;
                DemoVoice.PlayLocalAndBroadcast(
                    DemoConversationIds.Escaped, DemoVoiceMode.Lock);
            }

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

            // The first-loss line keys on the LOSS, any cause, per the authored beat
            // (Docs/Demo - Single/2-first-hatch-bank.md: "first Docked after this save's
            // first Lost Run"). Keyed on conversions it missed a first loss that converted
            // nothing — the respawn recut's common case — and played loss copy over a
            // first VICTORY dock, where "the rest is gone" is simply false.
            if (m.PodEjections >= 1 || m.LifetimeSalvageEarned > 0L)
            {
                if (m.PodEjections >= 1)
                    ResolveFirstReturn(m);
                else
                    DemoShipStanding.Reevaluate();
                ResolveRungTwo(m);
                return;
            }

            DemoShipFight.SetStanding("EMBARK from the NAV MAP");
        }

        /// <summary>The first Ship arrival after this save's first Lost Boarding, any cause.
        /// Plays the first-loss line, queues the CREW line behind it
        /// (DemoVoice's stated same-frame order), and writes the CREW menu unlock — the
        /// flag write is what makes both once-per-save. The standing is state-keyed from
        /// here on (DemoShipStanding).</summary>
        void ResolveFirstReturn(MetaState m)
        {
            if (!m.DemoCrewMenuUnlocked)
            {
                DemoVoice.Play(DemoConversationIds.FirstHatchDown, DemoVoiceMode.Lock);
                DemoVoice.Play(DemoConversationIds.CrewReady, DemoVoiceMode.Subtitle);
                m.DemoCrewMenuUnlocked = true;
                GameSession.Save();
                Log.Event(Tag, "CREW menu unlocked");
            }
            DemoShipStanding.Reevaluate();
        }

        /// <summary>The first Ship load after the rack opened: the rack-opened line plays.
        /// Keyed on the rack gate itself (DemoProgress.PrimaryRowsOpen — Salvage earned since
        /// 2026-09-02), never on a count of endings and no longer on zone 2's bank, which the
        /// shipped loop never opened. The two extra wheel buys this used to hand out went with
        /// the wheel ladder the same day. DemoRungTwoOpened is the once-per-save latch, and the
        /// flag write is what makes the line once-per-save too.
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
            if (!m.DemoActive || m.DemoRungTwoOpened) return;
            if (!DemoProgress.PrimaryRowsOpen(m)) return;
            DemoVoice.Play(DemoConversationIds.SecondLoss, DemoVoiceMode.Lock);
            Log.Event(Tag, "primary rack opened — " + DemoProgress.PrimaryRackSalvage + " earned");
            m.DemoRungTwoOpened = true;
            GameSession.Save();
            DemoShipStanding.Reevaluate();
        }

        static void PlaceAt(PlayerHealth player, Transform pose)
        {
            if (player == null || pose == null) return;
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            // XZ from the marker. Y: markers are authored at inconsistent heights (pad-pivot
            // vs floor level), so resolve the floor under the marker and stand on it — a pivot
            // below floor level is the 2026-08-18 fall-through defect, and keeping the rig's
            // own Y carries whatever height the player happened to load at into the pose.
            Vector3 at = pose.position;
            RaycastHit floorHit;
            if (Physics.Raycast(pose.position + Vector3.up * 2f, Vector3.down, out floorHit,
                    12f, ~0, QueryTriggerInteraction.Ignore))
                at.y = floorHit.point.y + 1.0f; // capsule half height + skin: feet on the floor
            else
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
