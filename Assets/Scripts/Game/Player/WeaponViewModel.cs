using GatekeepersProtocol.Sim;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: the gun mesh on the Boarder's right hand, fire/reload one-shots,
    /// and animator triggers. The sim ray still comes from the camera; this does not aim.</summary>
    public sealed class WeaponViewModel : MonoBehaviour
    {
        static readonly int FireHash = Animator.StringToHash("Fire");
        static readonly int ReloadHash = Animator.StringToHash("Reload");
        static readonly int SpeedHash = Animator.StringToHash("Speed");

        [SerializeField] Animator _animator;
        [SerializeField] GameObject _mk1;
        [SerializeField] GameObject _secD;
        [SerializeField] GameObject _br12;
        [SerializeField] GameObject _mr35;
        [SerializeField] GameObject _priC;
        [SerializeField] GameObject _cutter;
        [SerializeField] GameObject _arms;
        [SerializeField] Transform _grip;

        [SerializeField, OptionalBinding, Tooltip("Isometric rig (Ship probe). While wired and " +
            "enabled, IsometricBodyPresentation owns the animator Speed parameter — two " +
            "writers would fight. Null or disabled means first-person behavior, unchanged.")]
        IsometricCameraRig _isoRig;

        PlayerGun _gun;
        FirstPersonController _fpc;
        AudioSource _sfx;
        AudioClip _firePistol;
        AudioClip _fireShotgun;
        AudioClip _fireRifle;
        AudioClip _reload;
        WeaponId _shown = (WeaponId)(-99);
        bool _hidden;
        float _reloadClipLength = 1f;
        bool _reloadSpeedSet;

        void Awake()
        {
            _gun = GetComponentInChildren<PlayerGun>(true);
            _fpc = GetComponent<FirstPersonController>();
            if (_grip == null)
            {
                Transform named = FindNamed(transform, "Grip");
                _grip = named;
            }
            if (_mk1 == null) _mk1 = FindGo("Mk1");
            if (_secD == null) _secD = FindGo("SecD");
            if (_br12 == null) _br12 = FindGo("Br12");
            if (_mr35 == null) _mr35 = FindGo("Mr35");
            if (_priC == null) _priC = FindGo("PriC");
            if (_cutter == null) _cutter = FindGo("Cutter");
            // Resolved here, never activated here: EnableOnly owns the arms on/off state so
            // it cannot be set from two places.
            if (_arms == null) _arms = FindGo("Arms");
            // Grip only. GetComponentInChildren binds the Boarder body Animator and
            // Fire/Reload never reach the gun.
            if (_animator == null && _grip != null)
                _animator = _grip.GetComponent<Animator>();
            CacheReloadClipLength();
            _sfx = GetComponent<AudioSource>();
            if (_sfx == null)
            {
                _sfx = gameObject.AddComponent<AudioSource>();
                _sfx.playOnAwake = false;
                _sfx.spatialBlend = 0f;
            }
            _firePistol = Resources.Load<AudioClip>(WeaponView.FirePistol);
            _fireShotgun = Resources.Load<AudioClip>(WeaponView.FireShotgun);
            _fireRifle = Resources.Load<AudioClip>(WeaponView.FireRifle);
            _reload = Resources.Load<AudioClip>(WeaponView.Reload);
            AudioBuses.ApplyFromSettings();
            ApplyShown();
            PoseLocalGun();
        }

        void LateUpdate()
        {
            // Speed has one writer: IsometricBodyPresentation owns it while the iso rig runs.
            if (_animator != null && _fpc != null
                && !(_isoRig != null && _isoRig.isActiveAndEnabled))
            {
                float speed = 0f;
                if (_fpc.IsMoving)
                    speed = _fpc.IsSprinting ? 1f : 0.5f;
                _animator.SetFloat(SpeedHash, speed);
            }

            ApplyShown();
            PoseLocalGun();
            PoseArms();
            RestoreReloadSpeedIfIdle();
        }

        /// <summary>Ship has WeaponViewModel and no PlayerGun — LateUpdate used to leave the
        /// prefab Mk1 visible while ARMORY said buy a Secondary. Meta owns the mesh; the gun
        /// only supplies the fight-hosted cutter fallback.</summary>
        void ApplyShown()
        {
            if (_gun != null && _gun.ActiveSlot == WeaponSlot.Drone)
            {
                if (!_hidden) ShowHidden();
                return;
            }

            var meta = GameSession.Meta;
            bool owns = meta != null && meta.OwnsAnyWeapon;
            bool fight = _gun != null && _gun.IsHosted;
            WeaponId equipped = WeaponId.None;
            if (_gun != null && !_gun.UsingSalvageCutter)
                equipped = _gun.ActiveWeaponId;
            else if (meta != null)
            {
                equipped = meta.EquippedPrimary != WeaponId.None
                    ? meta.EquippedPrimary
                    : meta.EquippedSecondary;
            }

            WeaponId id;
            if (!WeaponView.TryShownWeapon(owns, fight, equipped, out id))
            {
                if (!_hidden) ShowHidden();
                return;
            }
            if (id != _shown || _hidden) Show(id);
        }

        /// <summary>Keep the grip parented to the right hand, but place it in camera space so
        /// the local owner sees a gun after LocalHead culls the body SMR. Remote avatars have
        /// no WeaponViewModel; their mesh stays on the hand.</summary>
        void PoseLocalGun()
        {
            if (_grip == null || _fpc == null) return;
            Transform cam = _fpc.cameraTransform;
            if (cam == null) return;
            var hold = WeaponView.Hold(_shown, _fpc.IsZooming);
            _grip.position = cam.TransformPoint(new Vector3(
                hold.Right, hold.Down, hold.Forward));
            _grip.rotation = cam.rotation * Quaternion.Euler(hold.Pitch, hold.Yaw, hold.Roll);
        }

        /// <summary>Write the arms prop's local TRS on the wrapper root it is parented to.
        /// Per frame, not at switch time: EnableOnly re-runs only when the shown weapon
        /// changes, so sampling IsZooming there would read the stance once and never again,
        /// and the zoom pose could never differ from the hip pose.</summary>
        void PoseArms()
        {
            // activeSelf, not _hidden: EnableOnly is what turns the arms off, and it also
            // turns them off when MeshFor returns null on a scene missing a mesh field —
            // a case _hidden does not cover. Parent is never null; Arms is authored under Grip.
            if (_arms == null || !_arms.activeSelf) return;
            Transform arms = _arms.transform;
            var offset = WeaponView.ArmsOffset(_shown, _fpc != null && _fpc.IsZooming);
            arms.localPosition = new Vector3(offset.Right, offset.Down, offset.Forward);
            arms.localRotation = Quaternion.Euler(offset.Pitch, offset.Yaw, offset.Roll);
        }

        /// <summary>Enable the mapped mesh. Reached only from ApplyShown in this file, which
        /// LateUpdate and Awake call; there is no caller outside.</summary>
        public void Show(WeaponId id)
        {
            _shown = id;
            _hidden = false;
            GameObject keep = MeshFor(id);
            EnableOnly(keep);
        }

        public void NotifyFired()
        {
            if (_animator != null)
            {
                RestoreReloadSpeed();
                _animator.ResetTrigger(ReloadHash);
                _animator.SetTrigger(FireHash);
            }
            if (_sfx == null) return;
            AudioClip clip = FireClip(_shown);
            if (clip != null) _sfx.PlayOneShot(clip, WeaponView.SfxOneShotScale);
        }

        public void NotifyReload()
        {
            if (_animator != null)
            {
                float duration = _gun != null
                    ? Mathf.Max(0.01f, _gun.ActiveReloadSeconds)
                    : 1f;
                _animator.speed = _reloadClipLength / duration;
                _reloadSpeedSet = true;
                _animator.SetTrigger(ReloadHash);
            }
            if (_sfx != null && _reload != null) _sfx.PlayOneShot(_reload, WeaponView.SfxOneShotScale);
        }

        void RestoreReloadSpeedIfIdle()
        {
            if (!_reloadSpeedSet) return;
            if (_gun != null && _gun.IsReloading) return;
            RestoreReloadSpeed();
        }

        void RestoreReloadSpeed()
        {
            if (!_reloadSpeedSet) return;
            if (_animator != null) _animator.speed = 1f;
            _reloadSpeedSet = false;
        }

        void CacheReloadClipLength()
        {
            if (_animator == null) return;
            var controller = _animator.runtimeAnimatorController;
            if (controller == null) return;
            AnimationClip[] clips = controller.animationClips;
            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip == null) continue;
                if (clip.name != "ViewModel_Reload") continue;
                _reloadClipLength = Mathf.Max(0.01f, clip.length);
                return;
            }
        }

        void ShowHidden()
        {
            _hidden = true;
            EnableOnly(null);
        }

        GameObject MeshFor(WeaponId id)
        {
            switch (id)
            {
                case WeaponId.Mk1Sidearm: return _mk1;
                case WeaponId.SecD: return _secD;
                case WeaponId.Br12Shotgun: return _br12;
                case WeaponId.Mr35Rifle: return _mr35;
                case WeaponId.PriC: return _priC;
                default: return _cutter;
            }
        }

        AudioClip FireClip(WeaponId id)
        {
            string path = WeaponView.FireClip(id);
            if (path == WeaponView.FireShotgun) return _fireShotgun;
            if (path == WeaponView.FireRifle) return _fireRifle;
            return _firePistol;
        }

        /// <summary>The one choke point for what the viewmodel shows, arms included. Both
        /// callers (Show and ShowHidden) come through here, so the arms cannot be turned on in
        /// one place and off in another — Awake activating them once would leave them off for
        /// the session after a Drone-slot or unowned episode.</summary>
        void EnableOnly(GameObject keep)
        {
            SetActive(_mk1, keep == _mk1);
            SetActive(_secD, keep == _secD);
            SetActive(_br12, keep == _br12);
            SetActive(_mr35, keep == _mr35);
            SetActive(_priC, keep == _priC);
            SetActive(_cutter, keep == _cutter);

            if (_arms == null) return;
            // The prop stands in for the body's own arms only while the body is culled. Iso
            // scenes show the body (BoarderPresentation skips the LocalHead cull when the
            // iso rig is wired), so there the prop would be a third arm.
            bool bodyVisible = _isoRig != null && _isoRig.isActiveAndEnabled;
            if (keep == null || bodyVisible)
            {
                SetActive(_arms, false);
                return;
            }
            SetActive(_arms, true);
            // Parent to the shown wrapper root, not to the Grip: the clips key the roots, so
            // a child of the Grip would sit still while the gun kicks out of its hands.
            Transform arms = _arms.transform;
            Transform root = keep.transform;
            if (arms.parent != root) arms.SetParent(root, false);
            // One child per arms class. Show the one this weapon's class baked and none if
            // it has not been baked yet: no arms beats another class's hands on this gun.
            string cls = WeaponView.ArmsClassName(WeaponView.ArmsClassFor(_shown));
            for (int i = 0; i < arms.childCount; i++)
            {
                Transform child = arms.GetChild(i);
                SetActive(child.gameObject, child.name == cls);
            }
        }

        static void SetActive(GameObject go, bool on)
        {
            if (go == null) return;
            if (go.activeSelf == on) return;
            go.SetActive(on);
        }

        static GameObject FindGo(Transform root, string name)
        {
            Transform t = FindNamed(root, name);
            return t != null ? t.gameObject : null;
        }

        GameObject FindGo(string name) => FindGo(transform, name);

        static Transform FindNamed(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindNamed(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
