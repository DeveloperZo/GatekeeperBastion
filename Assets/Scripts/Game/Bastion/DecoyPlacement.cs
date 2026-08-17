using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;
using UnityEngine.AI;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: two-press placement ghost for the decoy. First fire enters aiming;
    /// second fire confirms on a valid NavMesh sample within place range. Alt-fire (RMB) or
    /// leaving the bay cancels without spending a charge.</summary>
    public sealed class DecoyPlacement : MonoBehaviour
    {
        const string Tag = "Decoy";

        DecoyBay _bay;
        PlayerGun _gun;
        GameObject _ghost;
        Renderer _ghostRend;
        Material _validMat;
        Material _invalidMat;
        bool _aiming;
        bool _sampleValid;
        Vector3 _samplePos;

        public bool IsAiming => _aiming;

        public void Bind(DecoyBay bay, PlayerGun gun)
        {
            _bay = bay;
            _gun = gun;
        }

        void OnDestroy()
        {
            DestroyGhost();
            if (_validMat != null) Destroy(_validMat);
            if (_invalidMat != null) Destroy(_invalidMat);
        }

        /// <summary>True once the Bastion's tick owner drives this component; its own
        /// Update then yields so the written system order is the only order.</summary>
        bool _hosted;

        public void SetHosted(bool hosted) { _hosted = hosted; }

        void Update()
        {
            if (_hosted) return; // the Bastion tick owner calls Tick in the written order
            Tick(Time.deltaTime);
        }

        /// <summary>Placement aim state: cancel on slot switch or alt-fire, ghost update.</summary>
        public void Tick(float dt)
        {
            if (_gun == null || _bay == null) return;

            if (_gun.ActiveSlot != WeaponSlot.Drone)
            {
                if (_aiming) Cancel("slot switch");
                return;
            }

            if (_aiming && Controls.Down(ControlId.AltFire))
            {
                Cancel("alt fire");
                return;
            }

            if (!_aiming) return;

            UpdateGhost();
        }

        /// <summary>Called from PlayerGun on Fire Down while the bay is selected.</summary>
        public void OnFirePressed()
        {
            if (_bay == null || _gun == null) return;
            if (_gun.ActiveSlot != WeaponSlot.Drone) return;

            if (!_bay.HasPayload)
            {
                Log.Info(Tag, "fire refused — bay empty");
                return;
            }

            if (!_aiming)
            {
                if (_bay.State.ChargesRemaining <= 0)
                {
                    Log.Event(Tag, "place refused — no charges remaining");
                    return;
                }
                if (_bay.State.IsLive)
                {
                    Log.Event(Tag, "place refused — one decoy already live");
                    return;
                }
                BeginAim();
                return;
            }

            if (!_sampleValid)
            {
                Log.Info(Tag, "confirm refused — invalid placement");
                return;
            }

            if (ClientOnly())
            {
                var verbs = LocalVerbs();
                if (verbs == null)
                {
                    Log.Event(Tag, "place refused — no net verbs");
                    return;
                }
                verbs.RequestPlaceDecoy(_samplePos);
                EndAim();
                return;
            }

            if (_bay.TryPlace(_samplePos))
            {
                EndAim();
            }
        }

        void BeginAim()
        {
            _aiming = true;
            EnsureMats();
            if (_ghost == null)
            {
                _ghost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                _ghost.name = "DecoyGhost";
                _ghost.transform.localScale = new Vector3(0.9f, 0.6f, 0.9f);
                Object.Destroy(_ghost.GetComponent<Collider>());
                _ghostRend = _ghost.GetComponent<Renderer>();
            }
            _ghost.SetActive(true);
            UpdateGhost();
            Log.Event(Tag, "placement ghost — fire to confirm, RMB or slot-switch to cancel");
        }

        void Cancel(string why)
        {
            Log.Event(Tag, $"placement cancelled — {why}");
            EndAim();
        }

        void EndAim()
        {
            _aiming = false;
            _sampleValid = false;
            if (_ghost != null) _ghost.SetActive(false);
        }

        void DestroyGhost()
        {
            if (_ghost != null)
            {
                Destroy(_ghost);
                _ghost = null;
                _ghostRend = null;
            }
        }

        void UpdateGhost()
        {
            float range = _bay.State.PlaceRange > 0f
                ? _bay.State.PlaceRange
                : PayloadCatalog.Def(PayloadId.Decoy).placeRange;

            Ray ray = new Ray(transform.position, transform.forward);
            Vector3 aim;
            if (Physics.Raycast(ray, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Ignore))
                aim = hit.point;
            else
                aim = ray.origin + ray.direction * range;

            _sampleValid = NavMesh.SamplePosition(aim, out NavMeshHit nav, 2f, NavMesh.AllAreas)
                           && Vector3.Distance(transform.position, nav.position) <= range + 0.05f;
            _samplePos = _sampleValid ? nav.position : aim;

            if (_ghost != null)
            {
                _ghost.transform.position = _samplePos + Vector3.up * 0.6f;
                if (_ghostRend != null)
                    _ghostRend.sharedMaterial = _sampleValid ? _validMat : _invalidMat;
            }
        }

        void EnsureMats()
        {
            if (_validMat != null) return;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) return;
            _validMat = new Material(shader) { color = FxColors.DecoyGhostValid };
            _invalidMat = new Material(shader) { color = FxColors.DecoyGhostInvalid };
        }

        bool ClientOnly()
        {
            var gc = _bay != null ? _bay.Game : null;
            var net = gc != null ? gc.Net : null;
            return net != null && net.IsClientOnly;
        }

        NetVerbs LocalVerbs()
        {
            var gc = _bay != null ? _bay.Game : null;
            if (gc == null || gc.Net == null) return null;
            return gc.Net.LocalVerbs;
        }
    }
}
