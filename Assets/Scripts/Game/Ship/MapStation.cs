using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    public sealed class MapStation : StationTerminal
    {
        public const string PremiseBrief =
            "Gatekeepers hold this garrisoned Bastion. The Hatch is your way back to the Ship.";

        protected override string StationName => "NAV MAP";

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
        int _startWave = 1;

        protected override int ContentHash(MetaState m)
        {
            unchecked
            {
                return base.ContentHash(m) * 397 ^ _startWave * 17 ^ (_embarking ? 1 : 0) ^ (_loadFailed ? 2 : 0);
            }
        }

        protected override void BuildPanel(StationPanelView view, MetaState m)
        {
            // Settings-shell baseline: destination, wave, warnings, EMBARK. No extra copy.
            view.AddLabel($"Bastion {SceneFlow.BastionName}", centered: true);
            view.AddLabel(PremiseBrief, centered: true);
            view.AddSpacer();

            int deepest = m.DeepestWaveReached < 1 ? 1 : m.DeepestWaveReached;
            if (deepest > WaveDirector.MaxWaves) deepest = WaveDirector.MaxWaves;
            if (_startWave < 1) _startWave = 1;
            if (_startWave > deepest) _startWave = deepest;

            view.AddLabel($"START WAVE  {_startWave}  /  {deepest}", centered: true);
            view.BeginRow();
            view.AddButton("◀", () => _startWave--, _startWave > 1);
            view.AddRowLabel($"  {_startWave}  ");
            view.AddButton("▶", () => _startWave++, _startWave < deepest);
            view.EndRow();
            view.AddSpacer();

            if (!m.OwnsAnyWeapon)
                view.AddLabel("NO WEAPON", warning: true);
            if (!m.HasRepairRigEquipped)
                view.AddLabel("NO REPAIR RIG", warning: true);
            if (!m.OwnsAnyWeapon || !m.HasRepairRigEquipped)
                view.AddSpacer();

            bool warned = !m.OwnsAnyWeapon || !m.HasRepairRigEquipped;
            view.AddButton(warned ? "EMBARK ANYWAY" : "EMBARK", () => TryEmbark(m));

            if (_loadFailed)
                view.AddLabel("EMBARK FAILED — Bastion not in Build Settings.", warning: true);
        }

        void TryEmbark(MetaState m)
        {
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
