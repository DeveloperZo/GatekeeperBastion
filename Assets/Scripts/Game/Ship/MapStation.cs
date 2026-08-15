using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    public sealed class MapStation : StationTerminal
    {
        public const string PremiseBrief =
            "Gatekeepers hold this garrisoned Bastion. The Hatch is your way back to the Ship.";

        const string Tag = "MapStation";

        [SerializeField] NavMapPanelView _body;

        public override string StationName => "NAV MAP";
        public const string PurposeLine = "where you go, and going there";
        public override string Purpose => PurposeLine;

        protected override string PromptHint
        {
            get
            {
                var m = GameSession.Meta;
                if (m == null) return "embark on the run";
                bool noWeapon = !m.OwnsAnyWeapon;
                bool noRig = !m.HasRepairRigEquipped;
                if (noWeapon && noRig) return "EMBARK — no weapon, no repair rig";
                if (noWeapon) return "EMBARK — but you have NO WEAPON yet";
                if (noRig) return "EMBARK — no repair rig";
                return "embark on the run";
            }
        }

        bool _embarking;
        bool _loadFailed;
        bool _mapOpen;
        int _startWave = 1;

        protected override void Awake()
        {
            base.Awake();
            if (_body == null)
                Log.Error(Tag, StationName + ": missing serialized _body.");
        }

        protected override int ContentHash(MetaState m)
        {
            unchecked
            {
                return base.ContentHash(m) * 397 ^ _startWave * 17 ^ (_embarking ? 1 : 0) ^ (_loadFailed ? 2 : 0)
                    ^ (_mapOpen ? 1 << 12 : 0) ^ (NetShipWait.IsActive ? 1 << 13 : 0);
            }
        }

        protected override void BuildPanel(StationPanelView view, MetaState m)
        {
            if (_body == null) return; // Awake already logged the missing binding.

            // Nav panel first: picking the node arms _mapOpen, and ContentHash folds it in,
            // so the rebuild that follows swaps the nav panel out for the map panel.
            _body.BindMap(() => _mapOpen = true, _mapOpen);

            _body.SetDestination($"Bastion {SceneFlow.BastionName}");
            _body.SetPremise(PremiseBrief);

            int deepest = m.DeepestWaveReached < 1 ? 1 : m.DeepestWaveReached;
            if (deepest > WaveDirector.MaxWaves) deepest = WaveDirector.MaxWaves;
            if (_startWave < 1) _startWave = 1;
            if (_startWave > deepest) _startWave = deepest;

            if (NetShipWait.IsActive)
            {
                _body.ShowStartWavePicker(false);
                _body.ShowWarnings(false, false);
                _body.BindEmbark("EMBARK", () => TryEmbark(m));
                _body.ShowLoadFailed(false);
                return;
            }

            _body.ShowStartWavePicker(true);
            _body.SetStartWaveLine(StartWaveLine(_startWave, deepest));
            _body.BindWaveControls(
                $"  {_startWave}  ",
                () => _startWave--, _startWave > 1,
                () => _startWave++, _startWave < deepest);

            // NO WEAPON / NO REPAIR RIG texts are authored; toggled only.
            _body.ShowWarnings(!m.OwnsAnyWeapon, !m.HasRepairRigEquipped);

            bool warned = !m.OwnsAnyWeapon || !m.HasRepairRigEquipped;
            _body.BindEmbark(warned ? "EMBARK ANYWAY" : "EMBARK", () => TryEmbark(m));

            _body.ShowLoadFailed(_loadFailed);
        }

        void TryEmbark(MetaState m)
        {
            if (NetShipWait.IsActive)
            {
                var avatar = NetPlayerAvatar.LocalOwner;
                if (avatar == null)
                {
                    Log.Event("Net", "embark refused — no avatar");
                    return;
                }
                avatar.RequestEmbark();
                return;
            }

            if (_embarking) return;
            GameSession.PendingStartWave = _startWave;
            Log.Event("Ship",
                $"embarked for bastion {SceneFlow.BastionName} starting at wave {_startWave} — " +
                $"{m.BankedSalvage} banked, " +
                $"{(m.OwnsAnyWeapon ? LoadoutBrief(m) : "NO WEAPON (improvised cutter only)")}, " +
                $"{(m.HasRepairRigEquipped ? "repair rig equipped" : "NO REPAIR RIG")}");
            _embarking = SceneFlow.LoadBastion();
            _loadFailed = !_embarking;
            if (_loadFailed)
                Log.Error("Ship", "embark refused — Bastion is not in Build Settings / not loadable");
        }

        public static string StartWaveLine(int start, int deepest) =>
            "START WAVE  " + start + "  /  cleared " + deepest;

        static string LoadoutBrief(MetaState m)
        {
            string p = m.EquippedPrimary == WeaponId.None
                ? "Primary=—"
                : WeaponCatalog.Def(m.EquippedPrimary).displayName;
            string s = m.EquippedSecondary == WeaponId.None
                ? "Secondary=—"
                : WeaponCatalog.Def(m.EquippedSecondary).displayName;
            return $"{p} + {s}";
        }
    }
}
