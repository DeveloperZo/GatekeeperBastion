using System.Collections.Generic;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using Unity.Netcode;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>Firing modes (pellets / pierce / falloff) and tracer/spark FX — partial of
    /// <see cref="PlayerGun"/>. One method branches on def fields; never a subclass per weapon.</summary>
    public sealed partial class PlayerGun
    {
        void Fire()
        {
            ref var s = ref _slots[(int)_activeSlot];
            WeaponDef def = s.configured ? s.def : DefaultDefFromSerialized();
            int pellets = Mathf.Max(1, def.pellets);

            // First-person aim is the camera forward; iso aim is unit-level off the cursor
            // ray with the muzzle at the player body. aimDir == transform.forward keeps the
            // first-person math identical to before the iso path existed.
            Vector3 aimDir;
            Vector3 spreadRight;
            if (IsoAimActive)
            {
                aimDir = ResolveIsoAimDirection(def.range > 0.01f ? def.range : range);
                spreadRight = Vector3.Cross(Vector3.up, aimDir);
                spreadRight = spreadRight.sqrMagnitude > 0.0001f
                    ? spreadRight.normalized
                    : transform.right; // aiming straight up/down: any horizontal axis serves
            }
            else
            {
                aimDir = transform.forward;
                spreadRight = transform.right;
            }
            Vector3 origin = transform.position + aimDir * 0.3f;

            bool railshot = TryConsumeRailshot(out int railshotPierce);
            int pierceOverride = railshot ? railshotPierce : -1;
            float pierceMulOverride = railshot ? 1f : -1f;

            bool anyCrit = false;
            Vector3 lastEnd = origin + aimDir * def.range;
            int bodiesDamaged = 0;

            for (int p = 0; p < pellets; p++)
            {
                bool crit = RollCrit();
                if (crit) anyCrit = true;
                float baseDmg = crit ? EffectiveDamage * CritDamageMultiplier : EffectiveDamage;
                Vector3 dir = PelletDirection(def, p, pellets, aimDir, spreadRight);
                lastEnd = FireRay(origin, dir, def, baseDmg, crit, pierceOverride, pierceMulOverride,
                    out int rayBodies);
                bodiesDamaged += rayBodies;
            }

            if (railshot && _ability != null)
                _ability.NotifyRailshotFired(bodiesDamaged, railshotPierce);

            LastShotWasCrit = anyCrit;
            Effects.MuzzleFlash(origin + aimDir * 0.45f + Vector3.down * 0.08f);
            if (_view != null) _view.NotifyFired();
            if (pellets == 1)
                SpawnTracer(origin + aimDir * 0.4f, lastEnd, anyCrit, def);
            else
                SpawnTracer(origin + aimDir * 0.4f,
                    origin + aimDir * Mathf.Min(def.range, 20f), anyCrit, def);
        }

        // Cached so the aim resolve adds no per-shot allocation.
        static readonly IComparer<RaycastHit> HitByDistance =
            Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

        /// <summary>Unit-level cursor aim: the first live Damageable along the cursor ray wins
        /// and the shot is aimed at its collider bounds centre; bare geometry aims at the
        /// cursor's surface point; open space aims down the ray at max cast range. Bodies are
        /// resolved the way FireRay resolves them (own colliders and corpses skipped). The
        /// muzzle is the player body (transform.position), never the camera forward.</summary>
        Vector3 ResolveIsoAimDirection(float maxRange)
        {
            // Pointer comes from the input producer's frame, never an ambient engine poll.
            var frame = Controls.Sample();
            Ray ray = _isoRig.ScreenRay(new Vector3(frame.pointerX, frame.pointerY, 0f));
            // The cursor ray starts at the iso camera, FollowDistance back from the ship.
            float castRange = ShipCamera.FollowDistance + maxRange;
            Vector3 aimPoint = ray.origin + ray.direction * castRange;

            int hitCount = Physics.RaycastNonAlloc(ray.origin, ray.direction, _pierceHits,
                castRange, ~0, QueryTriggerInteraction.Ignore);
            if (hitCount > 0)
            {
                System.Array.Sort(_pierceHits, 0, hitCount, HitByDistance);
                for (int i = 0; i < hitCount; i++)
                {
                    var hit = _pierceHits[i];
                    if (hit.collider == null) continue;
                    if (IsOwnCollider(hit.collider)) continue;
                    var damageable = hit.collider.GetComponentInParent<Damageable>();
                    if (damageable != null && _owner != null &&
                        damageable.transform.IsChildOf(_owner.transform))
                        continue;
                    if (damageable != null && damageable.IsDead)
                        continue; // same as FireRay: a corpse never stops body resolution
                    aimPoint = damageable != null ? hit.collider.bounds.center : hit.point;
                    break;
                }
            }

            Vector3 dir = aimPoint - transform.position;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : ray.direction;
        }

        WeaponDef DefaultDefFromSerialized()
        {
            return new WeaponDef
            {
                damage = damage,
                shotsPerSecond = shotsPerSecond,
                clipSize = clipSize,
                reloadSeconds = reloadSeconds,
                range = range,
                pellets = 1,
                tracerWidth = tracerWidth,
                tracerR = 1f,
                tracerG = 0.9f,
                tracerB = 0.35f,
                crosshair = WeaponCrosshair.Cross
            };
        }

        Vector3 PelletDirection(WeaponDef def, int index, int pellets,
            Vector3 forward, Vector3 right)
        {
            if (pellets <= 1 || def.spreadDegrees <= 0.01f)
                return forward;

            float half = def.spreadDegrees;
            float angle = (360f / pellets) * index + Random.Range(-8f, 8f);
            float radius = Random.Range(0.15f, 1f) * half;
            Quaternion yaw = Quaternion.AngleAxis(angle, forward);
            Quaternion pitch = Quaternion.AngleAxis(radius, right);
            return (yaw * pitch * forward).normalized;
        }

        Vector3 FireRay(Vector3 origin, Vector3 dir, WeaponDef def, float baseDamage, bool crit,
            int pierceCountOverride, float pierceMulOverride, out int bodiesDamaged)
        {
            bodiesDamaged = 0;
            float maxRange = def.range > 0.01f ? def.range : range;
            Vector3 end = origin + dir * maxRange;
            int pierceLeft = pierceCountOverride >= 0 ? pierceCountOverride : def.pierceCount;
            float pierceMul = pierceMulOverride > 0f ? pierceMulOverride : def.pierceDamageMul;
            _pierceScratch.Clear();

            int hitCount = Physics.RaycastNonAlloc(origin, dir, _pierceHits, maxRange, ~0,
                QueryTriggerInteraction.Ignore);
            if (hitCount <= 0) return end;

            System.Array.Sort(_pierceHits, 0, hitCount, Comparer<RaycastHit>.Create(
                (a, b) => a.distance.CompareTo(b.distance)));

            for (int i = 0; i < hitCount; i++)
            {
                var hit = _pierceHits[i];
                // Own capsule / child colliders sit on the ray origin; never stop a shot on ourselves.
                if (hit.collider != null && IsOwnCollider(hit.collider))
                    continue;

                end = hit.point;
                var damageable = hit.collider != null
                    ? hit.collider.GetComponentInParent<Damageable>()
                    : null;

                if (damageable != null && _owner != null &&
                    damageable.transform.IsChildOf(_owner.transform))
                    continue;

                if (damageable == null || damageable.IsDead || _pierceScratch.Contains(damageable))
                {
                    if (damageable == null)
                    {
                        if (crit) Effects.CritSparks(hit.point);
                        else Effects.ImpactSparks(hit.point);
                        SpawnSpark(hit.point, crit);
                        break;
                    }
                    continue;
                }

                bool firstDamaged = _pierceScratch.Count == 0;
                float dmg = baseDamage * WeaponWheel.FalloffFactor(def, hit.distance);
                if (!firstDamaged)
                    dmg *= pierceMul;
                if (dmg > 0f)
                {
                    var netEnemy = damageable.GetComponent<NetEnemy>();
                    if (netEnemy != null && IsClientOnlyNetworkPeer())
                    {
                        netEnemy.SubmitHitServerRpc(dmg, (byte)_activeSlot);
                        Log.Event("Net", "hit rpc dmg=" + dmg + " slot=" + (byte)_activeSlot);
                    }
                    else
                        damageable.TakeDamage(dmg, _owner != null ? _owner.transform : transform, _activeSlot);
                    bodiesDamaged++;
                    if (crit)
                        Log.Event("Combat", $"CRITICAL HIT — {dmg:0} damage (x{CritDamageMultiplier:0.#})");
                    // Pin charge is once per shot on the first damaged body only. Pierce
                    // bodies must not each RegisterHit or a single ray spends the whole threshold.
                    if (firstDamaged)
                        RegisterPinHitIfNeeded(damageable);
                }
                if (crit) Effects.CritSparks(hit.point);
                else Effects.ImpactSparks(hit.point);
                SpawnSpark(hit.point, crit);
                _pierceScratch.Add(damageable);

                if (pierceLeft <= 0) break;
                pierceLeft--;
            }

            if (hitCount >= _pierceHits.Length)
                Log.Warn(Tag, $"FireRay hit buffer full ({_pierceHits.Length}); far bodies may be dropped.");

            return end;
        }

        /// <summary>NGO client that is not the server. Uses NetworkManager.Singleton so a
        /// missing GameController.Net cannot fall through to local TakeDamage on a clone.</summary>
        static bool IsClientOnlyNetworkPeer()
        {
            var nm = NetworkManager.Singleton;
            return nm != null && nm.IsClient && !nm.IsServer;
        }

        bool IsOwnCollider(Collider col)
        {
            if (_owner == null) _owner = GetComponentInParent<PlayerHealth>();
            if (_owner == null) return false;
            return col.transform == _owner.transform || col.transform.IsChildOf(_owner.transform);
        }

        void RegisterPinHitIfNeeded(Damageable damageable)
        {
            if (_ability == null) _ability = GetComponent<PlayerAbility>();
            if (_ability == null) return;
            var gatekeeper = damageable.GetComponentInParent<EnemyGatekeeper>();
            if (gatekeeper != null) _ability.RegisterPinHit(gatekeeper);
        }

        bool RollCrit()
        {
            float chance = CritChance;
            return chance > 0f && Random.value < chance;
        }

        void ApplyTracerFromDef(WeaponDef def)
        {
            if (_tracerMat != null)
                _tracerMat.color = FxColors.Rgb(def.tracerR, def.tracerG, def.tracerB);
            if (_critTracerMat != null)
            {
                _critTracerMat.color = FxColors.Rgb(
                    Mathf.Min(1f, def.tracerR + 0.35f),
                    Mathf.Min(1f, def.tracerG + 0.35f),
                    Mathf.Min(1f, def.tracerB + 0.25f));
            }
            tracerWidth = def.tracerWidth > 0f ? def.tracerWidth : tracerWidth;
        }

        void SpawnTracer(Vector3 from, Vector3 to, bool crit, WeaponDef def)
        {
            var mat = crit && _critTracerMat != null ? _critTracerMat : _tracerMat;
            if (mat == null || Effects.Suspended) return;
            Vector3 delta = to - from;
            if (delta.sqrMagnitude < 0.01f) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = crit ? "TracerCrit" : "Tracer";
            Destroy(go.GetComponent<Collider>());
            var t = go.transform;
            float width = def.tracerWidth > 0f ? def.tracerWidth : tracerWidth;
            if (crit) width *= 2.2f;
            t.position = (from + to) * 0.5f;
            t.rotation = Quaternion.LookRotation(delta.normalized);
            t.localScale = new Vector3(width, width, delta.magnitude);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Destroy(go, crit ? 0.1f : 0.06f);
        }

        void SpawnSpark(Vector3 at, bool crit)
        {
            var mat = crit && _critSparkMat != null ? _critSparkMat : _sparkMat;
            if (mat == null || Effects.Suspended) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = crit ? "HitSparkCrit" : "HitSpark";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = at;
            go.transform.localScale = Vector3.one * (crit ? 0.34f : 0.16f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Destroy(go, crit ? 0.14f : 0.08f);
        }
    }
}
