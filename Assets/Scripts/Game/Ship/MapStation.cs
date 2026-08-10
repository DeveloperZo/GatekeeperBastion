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
            view.AddLabel($"Bastion {SceneFlow.BastionName}", centered: true);
            view.AddLabel(PremiseBrief, centered: true);
            view.AddSpacer();

            int deepest = m.DeepestWaveReached < 1 ? 1 : m.DeepestWaveReached;
            if (deepest > WaveDirector.MaxWaves) deepest = WaveDirector.MaxWaves;
            if (_startWave < 1) _startWave = 1;
            if (_startWave > deepest) _startWave = deepest;

            view.AddLabel($"START AT WAVE {_startWave}  (reached {deepest})", centered: true);
            if (_startWave > 1)
                view.AddLabel($"waves 1..{_startWave} arrive back to back — no Prep between them");
            else
                view.AddLabel("normal run — Prep between every wave");

            view.BeginRow();
            view.AddButton("◀", () => _startWave--, _startWave > 1);
            view.AddRowLabel($"  {_startWave}  ");
            view.AddButton("▶", () => _startWave++, _startWave < deepest);
            view.EndRow();
            view.AddSpacer();

            if (!m.OwnsAnyWeapon)
            {
                view.AddLabel("NO WEAPON — you will be defenseless.", warning: true);
                view.AddLabel("Buy the free MK-1 at the ARMORY first.", centered: true);
                view.AddSpacer();
            }
            if (!m.HasRepairRigEquipped)
            {
                view.AddLabel("NO REPAIR RIG — the Hatch cannot be fixed this Run.", warning: true);
                view.AddLabel("Buy the equipment slot and Repair Rig at FABRICATION.", centered: true);
                view.AddSpacer();
            }

            bool warned = !m.OwnsAnyWeapon || !m.HasRepairRigEquipped;
            view.AddButton(warned ? "EMBARK ANYWAY" : "EMBARK", () => TryEmbark(m));

            if (_loadFailed)
                view.AddLabel(
                    "EMBARK FAILED — Bastion scene is not in the build settings. Run GatekeeperBastion > Build All Scenes.",
                    warning: true);
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
