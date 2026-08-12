using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>Terminal marker — player activation completes it in the unlock graph.</summary>
    public sealed class BastionTerminal : MonoBehaviour
    {
        [SerializeField] string terminalId;
        [SerializeField] string doorId;
        [SerializeField] string[] pressureSpawnIds;
        [SerializeField] SpawnPoint[] pressureSpawns;

        BastionLayout _layout;

        public string TerminalId => terminalId;
        public string DoorId => doorId;
        public string[] PressureSpawnIds => pressureSpawnIds;
        public SpawnPoint[] PressureSpawns => pressureSpawns;

        /// <summary>Editor/sketch emit — assigns ids before play.</summary>
        public void Configure(string id, string contributesToDoorId, SpawnPoint[] pressure)
        {
            terminalId = id;
            doorId = contributesToDoorId;
            pressureSpawns = pressure;
            if (pressure != null)
            {
                pressureSpawnIds = new string[pressure.Length];
                for (int i = 0; i < pressure.Length; i++)
                    pressureSpawnIds[i] = pressure[i] != null ? pressure[i].name : "";
            }
        }

        void OnEnable()
        {
            _layout = GetComponentInParent<BastionLayout>();
            if (_layout != null)
                _layout.RegisterTerminal(this);
        }

        void OnDisable()
        {
            if (_layout != null)
                _layout.UnregisterTerminal(this);
        }

        /// <summary>POC: called by interact wiring later; completes this terminal in the graph.</summary>
        public void Activate()
        {
            if (_layout == null)
            {
                Log.Warn("Terminal", terminalId + " has no BastionLayout");
                return;
            }

            if (!_layout.TryCompleteTerminal(terminalId, out string refuseReason))
            {
                Log.Event("Terminal", terminalId + " refused — " + refuseReason);
                return;
            }

            EnablePressureSpawns();
        }

        public void SetPressureSpawnsActive(bool active)
        {
            if (pressureSpawns == null)
                return;

            for (int i = 0; i < pressureSpawns.Length; i++)
            {
                SpawnPoint sp = pressureSpawns[i];
                if (sp == null)
                    continue;
                sp.gameObject.SetActive(active);
            }
        }

        void EnablePressureSpawns()
        {
            SetPressureSpawnsActive(true);
        }
    }
}
