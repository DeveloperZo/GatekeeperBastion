using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>CLIENT-LOCAL / HOST-ONLY (partial of GameController): the client tick path
    /// and the net repair command. Split so GameController.cs does not grow past cap —
    /// these members share hosted component refs and the RunSim, so they are not a
    /// separate type.</summary>
    public sealed partial class GameController
    {
        #region Public API
        /// <summary>Client-requested hatch repair. Spends the host wallet at the catalog
        /// rate; does not require the host to have the rig equipped. The requester already
        /// gated on their own <see cref="RepairInteractable.Configure"/>.</summary>
        public float RepairHatchFromNet(float requestedHp)
        {
            if (Sim == null || RunIsOver) return 0f;
            var hatch = Sim.Ship.Hatch;
            if (hatch.Hp >= hatch.MaxHp - 0.01f)
            {
                TryWithdrawFromHpRequest(requestedHp);
                return 0f;
            }
            float applied = Sim.RepairHatch(requestedHp);
            if (applied > 0f && hatch.Hp >= hatch.MaxHp - 0.01f)
                Log.Event("Hatch", $"hatch fully repaired — {hatch.Hp:0}/{hatch.MaxHp:0}");
            return applied;
        }

        void TryWithdrawFromHpRequest(float requestedHp)
        {
            if (Sim == null) return;
            float rate = EquipmentCatalog.Def(EquipmentId.RepairRig).repairPerSecond;
            if (rate <= 0f) return;
            Sim.Withdraw(requestedHp / rate);
        }

        void BindProtocol(PlayerHealth local)
        {
            if (local == null || Meta == null) return;
            var proto = local.GetComponent<PlayerProtocolAbilities>();
            if (proto == null) proto = local.gameObject.AddComponent<PlayerProtocolAbilities>();
            proto.Bind(this, Meta.EquippedProtocol, Meta.EquippedPassive);
            proto.SetHosted(true);
            _protocol = proto;
            string protoName = BoarderProtocol.ProtocolName(Meta.EquippedProtocol);
            if (protoName.Length == 0) protoName = "none";
            Log.Event("Protocol", "embark " + protoName);
        }

        static void PushSlot(PlayerGun gun, MetaState meta, WeaponSlot slot, WeaponId id)
        {
            if (id == WeaponId.None || meta == null) return;
            if (!WeaponCatalog.IsAvailable(id)) return;
            gun.Configure(slot, WeaponCatalog.Def(id), meta.PointsOf(id), id);
        }

        static string LoadoutSummary(MetaState meta)
        {
            if (meta == null)
                return "no weapon (improvised cutter); no repair rig";
            string weapons = !meta.OwnsAnyWeapon
                ? "no weapon (improvised cutter)"
                : SlotSummary("Primary", meta.EquippedPrimary, meta) + "; " +
                  SlotSummary("Secondary", meta.EquippedSecondary, meta);
            string rig = meta.HasRepairRigEquipped ? "repair rig equipped" : "no repair rig";
            return weapons + "; " + rig;
        }

        static string SlotSummary(string label, WeaponId id, MetaState meta)
        {
            if (id == WeaponId.None) return label + "=—";
            return label + "=" + WeaponCatalog.Def(id).displayName + " T" + meta.TierOf(id);
        }

        static string RunBeginsFields(MetaState meta, int startWave)
        {
            string primary = "none", primaryTier = "0", secondary = "none", secondaryTier = "0";
            string rig = "none";
            long banked = 0;
            if (meta != null)
            {
                banked = meta.BankedSalvage;
                if (meta.EquippedPrimary != WeaponId.None)
                {
                    primary = WeaponCatalog.Def(meta.EquippedPrimary).displayName;
                    primaryTier = meta.TierOf(meta.EquippedPrimary).ToString();
                }
                if (meta.EquippedSecondary != WeaponId.None)
                {
                    secondary = WeaponCatalog.Def(meta.EquippedSecondary).displayName;
                    secondaryTier = meta.TierOf(meta.EquippedSecondary).ToString();
                }
                if (meta.HasRepairRigEquipped) rig = "repair";
            }
            return "startWave=" + startWave + " primary=" + primary + " primaryTier=" + primaryTier
                + " secondary=" + secondary + " secondaryTier=" + secondaryTier
                + " rig=" + rig + " banked=" + banked;
        }
        #endregion

        #region Private
        /// <summary>Move, fire, abilities, decoy ghost, interact, Prep RPCs — no Sim.Tick.
        /// Additive Ship wait keeps Bastion loaded; do not tick the hidden Bastion rig.</summary>
        void TickClientLocal(float dt)
        {
            if (NetShipWait.IsActive) return;
            if (_fpc != null) _fpc.Tick(dt);
            if (_gun != null) _gun.Tick(dt);
            if (_ability != null) _ability.Tick(dt);
            if (_protocol != null) _protocol.Tick(dt);
            if (_decoyPlacement != null) _decoyPlacement.Tick(dt);
            if (_interactor != null) _interactor.Tick(dt);
            TickClientPrepCommands();
        }

        /// <summary>Client Prep: E / +/- / T via <see cref="NetPrep"/> RPCs. Never calls
        /// StartTerminalWave locally (that refuse must stay).</summary>
        void TickClientPrepCommands()
        {
            if (Sim == null || RunIsOver) return;
            if (Sim.Phase != RunPhase.Tethered || Sim.Assault != AssaultState.Prep) return;

            var looking = _interactor != null ? _interactor.Current : null;
            var lookingTerminal = looking as BastionTerminal;
            var lookingHatch = looking as RepairInteractable;
            if (lookingTerminal == null || lookingTerminal.TerminalId != _armedTerminalId)
                _armedTerminalId = null;
            if (lookingHatch == null)
                _armedHatch = false;

            bool hatchStartable = lookingHatch != null && lookingHatch.OffersHatchBank;
            bool terminalStartable = lookingTerminal != null && CanSelectTerminal(lookingTerminal);

            NetPrep prep = LocalPrep();
            if (prep == null) return;

            if (Controls.Down(ControlId.PullDraw))
            {
                if (hatchStartable)
                {
                    if (!_armedHatch)
                    {
                        Log.Event("Hatch", "hatch-bank refused — select intensity first");
                        return;
                    }
                    prep.RequestStartHatchBank();
                    return;
                }
                if (terminalStartable)
                {
                    if (_armedTerminalId != lookingTerminal.TerminalId)
                    {
                        Log.Event("Terminal", lookingTerminal.TerminalId
                            + " refused — select intensity first");
                        return;
                    }
                    if (lookingTerminal.Kind == BastionTerminalKind.Master)
                        prep.RequestStartMaster(lookingTerminal.TerminalId);
                    else
                        prep.RequestStartTerminal(lookingTerminal.TerminalId);
                    return;
                }
                if (Sim.TotalUnbanked > 0L)
                    Log.Event("Hatch", "hatch-bank refused — look at the Hatch");
                else
                    Log.Event("Run", "draw refused — Waves start at a terminal");
                return;
            }

            if (Controls.Down(ControlId.Interact))
            {
                if (terminalStartable)
                {
                    ToggleTerminalSelect(lookingTerminal);
                    return;
                }
                if (hatchStartable)
                {
                    ToggleHatchSelect();
                    return;
                }
            }

            int delta = 0;
            if (Controls.Down(ControlId.DialUp)) delta = 1;
            else if (Controls.Down(ControlId.DialDown)) delta = -1;
            if (delta == 0) return;

            if (_armedHatch || !string.IsNullOrEmpty(_armedTerminalId))
            {
                Log.Event("Run", "juice refused — intensity selected");
                return;
            }

            bool atTrigger = lookingHatch != null
                || (lookingTerminal != null
                    && (lookingTerminal.Kind == BastionTerminalKind.Field
                        || lookingTerminal.Kind == BastionTerminalKind.Master));
            if (!atTrigger)
            {
                Log.Event("Run", "juice refused — look at a terminal");
                return;
            }
            prep.RequestJuice(delta);
        }

        NetPrep LocalPrep()
        {
            if (_net == null) return null;
            var nm = Unity.Netcode.NetworkManager.Singleton;
            if (nm == null || !nm.IsListening) return null;
            if (!nm.ConnectedClients.TryGetValue(nm.LocalClientId, out var client))
                return null;
            var po = client.PlayerObject;
            if (po == null) return null;
            var prep = po.GetComponent<NetPrep>();
            if (prep != null)
                prep.Bind(this);
            return prep;
        }
        #endregion
    }
}
