using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>HOST-ONLY (partial of GameController): Dev-mode command surface. Always compiled —
    /// DevConsole compiles out of release, but these methods must remain so criterion 6 holds
    /// (no dangling #if members on GameController). Every Dev binding lands here; none poke
    /// RunSim / MetaState from the Dev layer.</summary>
    public sealed partial class GameController
    {
        /// <summary>Despawn live gatekeepers, restore hatch + player, re-draw the same wave index.</summary>
        public void ReplayWave()
        {
            GameSession.NotifyDevAction();
            if (Sim == null || Sim.Phase != RunPhase.Tethered)
            {
                Log.Warn(Tag, $"ReplayWave ignored (phase {(Sim == null ? "no-sim" : Sim.Phase.ToString())}).");
                return;
            }

            // CurrentWaveIndex is the count of draws so far. Mid-assault it is the live wave;
            // in Prep after a clear it is the wave just finished — both are the wave to re-draw.
            int wave = Sim.CurrentWaveIndex;
            if (wave <= 0)
            {
                Log.Warn(Tag, "ReplayWave ignored — no wave has been drawn yet.");
                return;
            }

            DespawnLiveGatekeepers();
            if (Sim.Assault == AssaultState.Assault)
                Sim.AbortAssault();
            else
                Sim.SeekWave(wave); // Prep after clear: point the next draw at the same index

            ClearPendingTerminal();
            RestoreHatchBankSpawns();
            RestoreHatchAndPlayer();
            Sim.TriggerDraw();
            Log.Event("Dev", $"ReplayWave — re-drew wave {wave}/{WaveDirector.MaxWaves}");
        }

        /// <summary>Jump to 1-based wave N: despawn, seek, restore, draw. Arms via F3 then digit.</summary>
        public void JumpToWave(int oneBasedWave)
        {
            GameSession.NotifyDevAction();
            if (Sim == null || Sim.Phase != RunPhase.Tethered)
            {
                Log.Warn(Tag, $"JumpToWave ignored (phase {(Sim == null ? "no-sim" : Sim.Phase.ToString())}).");
                return;
            }
            if (oneBasedWave < 1 || oneBasedWave > WaveDirector.MaxWaves)
            {
                Log.Warn(Tag, $"JumpToWave {oneBasedWave} out of range 1..{WaveDirector.MaxWaves}.");
                return;
            }

            DespawnLiveGatekeepers();
            if (Sim.Assault == AssaultState.Assault)
                Sim.AbortAssault();

            ClearPendingTerminal();
            RestoreHatchBankSpawns();
            Sim.SeekWave(oneBasedWave);
            RestoreHatchAndPlayer();
            Sim.TriggerDraw();
            Log.Event("Dev", $"JumpToWave — drew wave {oneBasedWave}/{WaveDirector.MaxWaves}");
        }

        /// <summary>Despawn every live gatekeeper without awarding kills, then clear the assault so
        /// the run returns to Prep (and can complete if that was the last wave).</summary>
        public void ClearGatekeepers()
        {
            GameSession.NotifyDevAction();
            if (Sim == null || Sim.Phase != RunPhase.Tethered)
            {
                Log.Warn(Tag, $"ClearGatekeepers ignored (phase {(Sim == null ? "no-sim" : Sim.Phase.ToString())}).");
                return;
            }

            int n = DespawnLiveGatekeepers();
            if (Sim.Assault == AssaultState.Assault)
                Sim.NotifyAssaultCleared();
            Log.Event("Dev", $"ClearGatekeepers — despawned {n}, assault {(Sim.Assault == AssaultState.Prep ? "cleared" : "unchanged")}");
        }

        /// <summary>Refill hatch HP and player HP to full (isolates the next wave from prior damage).</summary>
        public void RestoreHatch()
        {
            GameSession.NotifyDevAction();
            if (Sim == null || Sim.Phase != RunPhase.Tethered)
            {
                Log.Warn(Tag, $"RestoreHatch ignored (phase {(Sim == null ? "no-sim" : Sim.Phase.ToString())}).");
                return;
            }
            RestoreHatchAndPlayer();
            Log.Event("Dev",
                $"RestoreHatch — hatch {Sim.Ship.Hatch.Hp:0}/{Sim.Ship.Hatch.MaxHp:0}, player refilled");
        }

        /// <summary>Toggle player invulnerability so gatekeeper behaviour can be watched without a fight.</summary>
        public void TogglePlayerInvulnerability()
        {
            GameSession.NotifyDevAction();
            var player = PlayerRegistry.Local;
            if (player == null)
            {
                Log.Warn(Tag, "TogglePlayerInvulnerability ignored — no local player.");
                return;
            }
            player.Invulnerable = !player.Invulnerable;
            Log.Event("Dev", $"player invulnerability {(player.Invulnerable ? "ON" : "OFF")}");
        }

        /// <summary>Bank salvage into the session. Static so the Ship scene (no GameController
        /// instance) can still reach a shop state via F8.</summary>
        public static void GrantSalvage(long amount)
        {
            GameSession.NotifyDevAction();
            GameSession.EnsureLoaded();
            var meta = GameSession.Meta;
            if (meta == null)
            {
                Log.Warn(Tag, "GrantSalvage ignored — no MetaState.");
                return;
            }
            meta.EarnSalvage(amount);
            Log.Event("Dev", $"GrantSalvage +{amount} — banked {meta.BankedSalvage}");
        }

        /// <summary>F10: start a host session on 0.0.0.0:7777. Editor/dev LAN smoke — may
        /// skip the shared Ship lobby. Session start, not a Dev mutation — does not taint.</summary>
        public void StartCoopHost()
        {
            EnsureNetForDevSmoke();
            if (_net == null)
            {
                Log.Warn(Tag, "StartCoopHost refused — no NetSession.");
                return;
            }
            _net.StartHost();
        }

        /// <summary>F11: join 127.0.0.1:7777. Editor/dev fallback, not the friends path.
        /// Session start, not a Dev mutation — does not taint.</summary>
        public void StartCoopClient()
        {
            EnsureNetForDevSmoke();
            if (_net == null)
            {
                Log.Warn(Tag, "StartCoopClient refused — no NetSession.");
                return;
            }
            _net.StartClient("127.0.0.1", 7777);
        }

        void EnsureNetForDevSmoke()
        {
            if (_net != null && _net.Active) return;
            var live = NetSession.FindLive();
            if (live != null)
            {
                _net = live;
                live.BindGame(this);
                return;
            }
            var created = NetSession.CreateBootstrap();
            if (created == null) return;
            _net = created;
            created.BindGame(this);
        }

        /// <summary>Own, Tier, and equip a weapon into its catalog slot, then re-push the gun if
        /// a Bastion run is live. Taints the session (Save suppressed).</summary>
        public static void SetLoadout(int slot, int weaponId, int tier)
        {
            GameSession.NotifyDevAction();
            GameSession.EnsureLoaded();
            var meta = GameSession.Meta;
            if (meta == null)
            {
                Log.Warn(Tag, "SetLoadout ignored — no MetaState.");
                return;
            }

            var id = (WeaponId)weaponId;
            if (id == WeaponId.None || !System.Enum.IsDefined(typeof(WeaponId), weaponId))
            {
                Log.Event("Dev", $"SetLoadout refused — invalid weaponId={weaponId}");
                return;
            }
            if (!WeaponCatalog.IsAvailable(id))
            {
                Log.Event("Dev", $"SetLoadout refused — {WeaponCatalog.Def(id).displayName} is disabled");
                return;
            }

            if (tier < 0) tier = 0;
            if (tier > WeaponTier.MaxTier) tier = WeaponTier.MaxTier;

            meta.SetOwned(id, true);
            meta.ReplacePointsFromFlatTier(id, tier);
            meta.SetEquipped(id);

            // slot arg is advisory — equip always uses the def's own slot (same as ShipCommands).
            var def = WeaponCatalog.Def(id);
            Log.Event("Dev",
                $"SetLoadout — {def.displayName} T{tier} equipped in {def.slot} (requested slot={slot})");

            // Dev-only locator (static entry point; there is no process-global instance).
            var gc = FindFirstObjectByType<GameController>();
            if (gc != null && gc.Sim != null && !gc.RunIsOver)
                gc.ApplyMetaEffects();
        }

        void RestoreHatchAndPlayer()
        {
            if (Sim != null)
            {
                var hatch = Sim.Ship.Hatch;
                float missing = hatch.MaxHp - hatch.Hp;
                if (missing > 0f) hatch.Repair(missing);
            }

            var player = PlayerRegistry.Local;
            if (player != null)
            {
                player.HealToFull();
                var fpc = player.GetComponent<FirstPersonController>();
                if (fpc != null && Sim != null && Sim.Phase == RunPhase.Tethered)
                {
                    fpc.MovementLocked = false;
                    fpc.SetCursorLock(true);
                }
            }
        }

        int DespawnLiveGatekeepers()
        {
            if (_spawner == null) _spawner = FindFirstObjectByType<WaveSpawner>();
            if (_spawner != null) return _spawner.DespawnAllLive();

            // Fallback if the spawner is missing: destroy gatekeepers directly (no awards).
            int n = 0;
            foreach (var gatekeeper in FindObjectsByType<EnemyGatekeeper>(FindObjectsSortMode.None))
            {
                if (gatekeeper == null) continue;
                gatekeeper.enabled = false;
                Destroy(gatekeeper.gameObject);
                n++;
            }
            return n;
        }
    }
}
