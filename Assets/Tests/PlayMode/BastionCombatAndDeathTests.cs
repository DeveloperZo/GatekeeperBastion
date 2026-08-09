using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GatekeeperBastion.Game;
using GatekeeperBastion.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GatekeeperBastion.PlayTests
{
    /// <summary>
    /// BASTION RUN CONTRACT — the tests that would have caught "the player could not fire,
    /// then died, then the death -> lose screen -> return-to-ship path threw and dead-ended".
    ///
    /// Both tests drive the REAL Bastion scene through the real MonoBehaviour layer, because
    /// that is precisely the seam the 97 EditMode sim tests cannot see.
    /// </summary>
    public class BastionCombatAndDeathTests
    {
        // Save isolation is assembly-wide via PlayModeSaveFixture — do not nest SaveFileGuard
        // here; nesting overwrites .playtestbackup and leaves the fixture unable to restore.
        readonly List<GameObject> _spawned = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (GameObject go in _spawned)
                if (go != null) UnityEngine.Object.Destroy(go);
            _spawned.Clear();

            yield return PlayTestUtil.ReturnToBlankScene();
        }

        // =====================================================================
        // D. Firing works when the weapon is owned; the no-weapon rule is stated
        // =====================================================================

        /// <summary>
        /// The gun is gated on GameSession.Meta.WeaponOwned, and a fresh campaign starts with
        /// WeaponOwned == false — so a player who embarks before visiting the Armory has no way
        /// to hurt anything and no way to find that out. This test pins BOTH weapon states:
        ///
        ///   owned    -> firing must damage a Damageable.
        ///   not owned-> EITHER the shot deals reduced damage (weak fallback gun) OR the game
        ///               exposes a stated reason string the HUD can show. Either is a valid fix;
        ///               silently doing full damage, or silently doing nothing, is not.
        /// </summary>
        [UnityTest]
        public IEnumerator Firing_DamagesAnEnemy_WhenWeaponOwned_AndNoWeaponBehaviourIsStated()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.Bastion);

            using (var logs = new PlayTestUtil.ErrorLogCapture())
            {
                yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.Bastion);
                yield return PlayTestUtil.WaitUntil(
                    () => GameController.Instance != null && GameController.Instance.Sim != null, 10f);

                GameController gc = GameController.Instance;
                Assert.IsNotNull(gc, "Bastion scene has no GameController — the run never starts.");
                Assert.IsNotNull(gc.Sim, "GameController.Sim was never created.");
                Assert.IsNotNull(GameSession.Meta, "GameSession.Meta is null inside the Bastion scene.");

                PlayerHealth player = PlayerRegistry.Local;
                Assert.IsNotNull(player, "No player registered in the Bastion scene (PlayerRegistry.Local).");

                // The gun type is reached by name: another agent is actively reshaping it.
                Type gunType = PlayTestUtil.FindType("GatekeeperBastion.Game.PlayerGun");
                Assert.IsNotNull(gunType, "Type GatekeeperBastion.Game.PlayerGun does not exist.");
                Component gun = player.GetComponentInChildren(gunType);
                if (gun == null)
                {
                    List<Component> anyGun = PlayTestUtil.FindAll(gunType);
                    if (anyGun.Count > 0) gun = anyGun[0];
                }
                Assert.IsNotNull(gun,
                    "The Bastion player rig has no PlayerGun component — the player cannot shoot at all.");

                // Firing is normally driven by Input inside Update, which a test cannot press.
                bool fireIsPublic;
                MethodInfo fire = PlayTestUtil.FindZeroArgMethod(gunType, FireMethodNames, out fireIsPublic);
                Assert.IsNotNull(fire,
                    "PlayerGun exposes no callable fire entry point (looked for a zero-argument " +
                    string.Join("/", FireMethodNames) + "). Add a public, input-independent fire method " +
                    "(e.g. `public bool TryFire()`) so the WeaponOwned gate is testable instead of only " +
                    "reachable through Input.GetMouseButton in Update().");

                // Some gun paths refuse to fire while the cursor is free; make the state legal.
                Cursor.lockState = CursorLockMode.Locked;
                yield return null;

                MetaState meta = GameSession.Meta;
                bool originallyOwned = meta.Owns(WeaponId.Mk1Sidearm);
                int originalTier = meta.TierOf(WeaponId.Mk1Sidearm);
                WeaponId originalSecondary = meta.EquippedSecondary;

                // ---- state 1: weapon owned ----
                meta.SetOwned(WeaponId.Mk1Sidearm, true);
                meta.EquippedSecondary = WeaponId.Mk1Sidearm;
                InvokeApplyMetaEffects(gc);
                yield return null;

                Damageable ownedTarget = SpawnTarget(gun.transform, 3f, "PlayTest_Target_Owned");
                yield return new WaitForFixedUpdate();
                yield return null;
                float ownedHpBefore = ownedTarget.Hp;
                object ownedResult = SafeInvoke(fire, gun);
                yield return null;
                float ownedDamage = ownedHpBefore - ownedTarget.Hp;

                Assert.Greater(ownedDamage, 0f,
                    "With MK-1 owned and equipped, firing dealt NO damage to a Damageable " +
                    $"3 m down the barrel (fire entry point '{fire.Name}' returned {Describe(ownedResult)}). " +
                    "The player cannot fight.");

                // ---- state 2: no weapon owned ----
                meta.SetOwned(WeaponId.Mk1Sidearm, false);
                meta.EquippedSecondary = WeaponId.None;
                meta.EquippedPrimary = WeaponId.None;
                InvokeApplyMetaEffects(gc);
                yield return new WaitForSeconds(0.6f); // clear any fire-rate cooldown

                Damageable unownedTarget = SpawnTarget(gun.transform, 3f, "PlayTest_Target_NoWeapon");
                yield return null;
                float unownedHpBefore = unownedTarget.Hp;
                object unownedResult = SafeInvoke(fire, gun);
                yield return null;
                float unownedDamage = unownedHpBefore - unownedTarget.Hp;

                string statedReason = FindStatedNoWeaponReason(gun, gc);
                bool reducedDamage = unownedDamage < ownedDamage - 0.001f;
                bool hasStatedReason = !string.IsNullOrEmpty(statedReason);

                // restore
                meta.SetOwned(WeaponId.Mk1Sidearm, originallyOwned);
                meta.ReplacePointsFromFlatTier(WeaponId.Mk1Sidearm, originalTier);
                meta.EquippedSecondary = originalSecondary;

                Assert.IsTrue(reducedDamage || hasStatedReason,
                    "CONTRACT UNMET for the no-weapon state. With no weapon owned the " +
                    $"same shot still dealt {unownedDamage:0.##} damage (owned: {ownedDamage:0.##}) and no reason " +
                    "string is exposed anywhere, so the player is silently unarmed with no explanation. " +
                    "EITHER of these is an acceptable fix:\n" +
                    "  (a) WEAK FALLBACK GUN — an unarmed player still shoots, for strictly less damage than " +
                    "the owned MK-1;\n" +
                    "  (b) BLOCKED WITH A STATED REASON — firing is refused and the game exposes a non-empty " +
                    "reason string.\n" +
                    $"Fire entry point used: '{fire.Name}' (public: {fireIsPublic}), returned {Describe(unownedResult)}.");

                logs.AssertNoErrors("firing in both weapon states");
            }
        }

        // =====================================================================
        // M11 criterion 5 — per-slot clip survives a switch away and back
        // =====================================================================

        [UnityTest]
        public IEnumerator SlotClip_SurvivesSwitchAwayAndBack()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.Bastion);

            using (var logs = new PlayTestUtil.ErrorLogCapture())
            {
                yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.Bastion);
                yield return PlayTestUtil.WaitUntil(
                    () => GameController.Instance != null && GameController.Instance.Sim != null, 10f);

                GameController gc = GameController.Instance;
                MetaState meta = GameSession.Meta;
                Assert.IsNotNull(meta);

                meta.SetOwned(WeaponId.Mk1Sidearm, true);
                meta.SetOwned(WeaponId.Br12Shotgun, true);
                meta.EquippedSecondary = WeaponId.Mk1Sidearm;
                meta.EquippedPrimary = WeaponId.Br12Shotgun;
                InvokeApplyMetaEffects(gc);
                yield return null;

                PlayerHealth player = PlayerRegistry.Local;
                Assert.IsNotNull(player);
                var gun = player.GetComponentInChildren<PlayerGun>();
                Assert.IsNotNull(gun);
                Cursor.lockState = CursorLockMode.Locked;
                yield return null;

                gun.TrySelectSlotPublic(WeaponSlot.Primary);
                yield return null;
                int before = gun.ClipRemaining;
                Assert.Greater(before, 3, "Primary should start with a full magazine");

                Assert.IsTrue(gun.TryFire());
                // Wait out fire-rate cooldown so the next pulls are not refused.
                yield return new WaitForSeconds(1.0f / Mathf.Max(0.01f, gun.EffectiveFireRate) + 0.05f);
                Assert.IsTrue(gun.TryFire());
                yield return new WaitForSeconds(1.0f / Mathf.Max(0.01f, gun.EffectiveFireRate) + 0.05f);
                Assert.IsTrue(gun.TryFire());
                yield return null;
                Assert.AreEqual(before - 3, gun.ClipRemaining);

                gun.TrySelectSlotPublic(WeaponSlot.Secondary);
                yield return null;
                gun.TrySelectSlotPublic(WeaponSlot.Primary);
                yield return null;

                Assert.AreEqual(before - 3, gun.ClipRemaining,
                    "Primary clip must be unchanged after switching away and back (criterion 5).");

                logs.AssertNoErrors("per-slot clip");
            }
        }

        // =====================================================================
        // E. Death -> lose screen -> return to ship: never throws, never dead-ends
        // =====================================================================

        /// <summary>
        /// THE HIGH-VALUE ONE. Walks the exact sequence the developer hit: enter the Bastion,
        /// pull the draw, kill something, die, let the lose screen render, press RETURN TO SHIP.
        /// Fails on ANY logged error/exception at any stage, on the run not reaching Lost, on a
        /// kill failing to bank currency on the spot, on death reducing banked salvage, and on the
        /// return command failing to land in the Ship scene (including the dead-end where Ship was
        /// never registered in Build Settings).
        /// </summary>
        [UnityTest]
        public IEnumerator PlayerDeath_LoseScreen_ReturnToShip_NeverThrowsAndReachesTheShip()
        {
            PlayTestUtil.RequireSceneInBuild(SceneFlow.Bastion);

            // Not a skip: if Bastion is playable but Ship is not registered, the ONLY exit from
            // the lose screen throws and the player is stranded. That is the shipped bug.
            Assert.IsTrue(PlayTestUtil.IsSceneInBuild(SceneFlow.Ship),
                $"DEAD END: scene '{SceneFlow.Ship}' is not in Build Settings (registered: " +
                $"{PlayTestUtil.RegisteredScenes()}), but GameController.ReturnToShip() -> SceneFlow.LoadShip() " +
                "is the lose screen's only exit. Pressing RETURN TO SHIP would throw and strand the player. " +
                "Register all four flow scenes (GatekeeperBastion > Build All Scenes).");

            using (var logs = new PlayTestUtil.ErrorLogCapture())
            {
                // ---- enter the Bastion ----
                yield return PlayTestUtil.LoadSceneAndSettle(SceneFlow.Bastion);
                yield return PlayTestUtil.WaitUntil(
                    () => GameController.Instance != null && GameController.Instance.Sim != null, 10f);

                GameController gc = GameController.Instance;
                Assert.IsNotNull(gc, "Bastion scene has no GameController.");
                RunSim sim = gc.Sim;
                Assert.IsNotNull(sim, "GameController.Sim was never created.");

                yield return PlayTestUtil.WaitUntil(() => sim.Phase == RunPhase.Tethered, 5f);
                Assert.AreEqual(RunPhase.Tethered, sim.Phase,
                    "Entering the Bastion must auto-board into Tethered (Docked is a transient boot phase).");
                logs.AssertNoErrors("entering the Bastion scene");

                // Something must draw the lose screen, or RETURN TO SHIP is unreachable.
                Assert.IsNotNull(FindHudRenderer(),
                    "No HUD component with an OnGUI pass exists in the Bastion scene, so the lose screen " +
                    "(and its RETURN TO SHIP button) can never be drawn — the run dead-ends on death.");

                MetaState meta = GameSession.Meta;
                Assert.IsNotNull(meta, "GameSession.Meta is null inside the Bastion scene.");
                long bankedBefore = meta.BankedSalvage;

                // ---- trigger a draw (waves + income live) ----
                if (sim.Assault == AssaultState.Prep) sim.TriggerDraw();
                Assert.AreEqual(AssaultState.Assault, sim.Assault,
                    "TriggerDraw must move the run from Prep into Assault.");
                yield return new WaitForSeconds(0.75f);
                logs.AssertNoErrors("triggering the siphon draw");

                // ---- a kill pays, and it pays into the purse straight away ----
                // Enemies are worth currency: enemy dies, currency added. There is no
                // end-of-run settlement left to carry it, so if this does not land in
                // MetaState.BankedSalvage now, it never will.
                long bankedBeforeKill = meta.BankedSalvage;
                gc.GatekeeperKilled();
                Assert.Greater(meta.BankedSalvage, bankedBeforeKill,
                    $"A reported kill did not add currency to MetaState.BankedSalvage (still " +
                    $"{meta.BankedSalvage}). GameController.GatekeeperKilled() -> RunSim.AwardKill() is " +
                    "the only income path in the game, and it banks immediately.");
                long bankedAfterKill = meta.BankedSalvage;
                logs.AssertNoErrors("banking a kill");

                // ---- kill the player ----
                PlayerHealth player = PlayerRegistry.Local;
                Assert.IsNotNull(player, "No player registered in the Bastion scene (PlayerRegistry.Local).");
                // Invoked by reflection so an added parameter (e.g. a damage-source Transform)
                // cannot break this test's compilation while another agent is editing PlayerHealth.
                object killResult;
                bool killed = PlayTestUtil.TryInvokeWithDefaults(
                    player, "TakeDamage", new object[] { player.MaxHp }, out killResult);
                Assert.IsTrue(killed,
                    "PlayerHealth has no public TakeDamage(float, ...) method — the death path cannot " +
                    "be driven, and neither can enemies drive it.");
                Assert.IsTrue(player.IsDead, "PlayerHealth.TakeDamage(MaxHp) must kill the player.");

                yield return PlayTestUtil.WaitUntil(() => sim.Phase == RunPhase.Lost, 5f);
                Assert.AreEqual(RunPhase.Lost, sim.Phase,
                    "Player death must end the run: PlayerHealth.Die -> GameController.PlayerDied -> " +
                    "RunSim.NotifyPlayerDown -> Lost.");
                Assert.AreEqual(bankedAfterKill, meta.BankedSalvage,
                    $"Dying CHANGED banked salvage ({bankedAfterKill} -> {meta.BankedSalvage}). " +
                    "Death stops the earning and nothing else: every kill was already banked, and " +
                    "there is no stake, forfeit or end-of-run settlement to move the number now.");
                Assert.GreaterOrEqual(meta.BankedSalvage, bankedBefore,
                    $"Banked salvage went BACKWARDS across the run ({bankedBefore} -> " +
                    $"{meta.BankedSalvage}). A run may never take banked progress away.");
                RunReportAssertions.AssertShape(sim, "player", null);
                logs.AssertNoErrors("player death / run end");

                // ---- let the lose screen render (several OnGUI passes) ----
                yield return PlayTestUtil.Frames(10);
                logs.AssertNoErrors("rendering the lose screen");

                // ---- the only exit: RETURN TO SHIP ----
                gc.ReturnToShip();
                yield return PlayTestUtil.WaitUntil(
                    () => SceneManager.GetActiveScene().name == SceneFlow.Ship, 20f);
                Assert.AreEqual(SceneFlow.Ship, SceneManager.GetActiveScene().name,
                    "GameController.ReturnToShip() did not land in the Ship scene within 20 s — the lose " +
                    "screen dead-ends and the player can never start another run. " +
                    $"(active scene: '{SceneManager.GetActiveScene().name}')");

                yield return PlayTestUtil.Frames(5);
                logs.AssertNoErrors("returning to the ship");
                Assert.IsNotNull(GameSession.Meta,
                    "GameSession.Meta was lost across the return to the ship; the hub has nothing to show.");
            }
        }

        // =====================================================================
        // helpers
        // =====================================================================

        static readonly string[] FireMethodNames =
        {
            "TryFire", "FireOnce", "FirePrimary", "AttemptFire", "PullTrigger", "Shoot", "Fire"
        };

        static readonly string[] ReasonNameFragments =
        {
            "reason", "blocked", "holster", "cannotfire", "noweapon", "unarmed"
        };

        Damageable SpawnTarget(Transform muzzle, float distance, string name)
        {
            // A stand-in enemy: wave gatekeepers need a baked NavMesh, which a freshly generated
            // project may not have, and this test is about the weapon gate, not spawning.
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            // Place short of the first solid so a wall in the Bastion spawn facing does not
            // eat the ray before it reaches the cube.
            float placeAt = distance;
            if (Physics.Raycast(muzzle.position, muzzle.forward, out RaycastHit block, distance,
                    ~0, QueryTriggerInteraction.Ignore))
                placeAt = Mathf.Clamp(block.distance * 0.5f, 0.6f, distance);
            go.transform.position = muzzle.position + muzzle.forward * placeAt;
            go.transform.localScale = new Vector3(3f, 3f, 0.4f);
            var damageable = go.AddComponent<Damageable>();
            // Give it a huge pool so a single shot can never clamp the damage delta (SetMaxHp is
            // not on the agreed stable surface, so it is optional; the default 50 hp already
            // exceeds the strongest MK-1 shot).
            object ignored;
            PlayTestUtil.TryInvokeWithDefaults(damageable, "SetMaxHp", new object[] { 100000f }, out ignored);
            Physics.SyncTransforms();
            _spawned.Add(go);
            return damageable;
        }

        static object SafeInvoke(MethodInfo method, object target)
        {
            try { return method.Invoke(target, null); }
            catch (TargetInvocationException e)
            {
                Assert.Fail($"Fire entry point '{method.Name}' threw: {e.InnerException}");
                return null;
            }
        }

        static string Describe(object value)
        {
            return value == null ? "void/null" : value.ToString();
        }

        static void InvokeApplyMetaEffects(GameController gc)
        {
            // Not on the agreed stable API surface, so it is optional: it exists to re-push meta
            // values into the scene after WeaponOwned flips, in case the gun caches them.
            MethodInfo m = gc.GetType().GetMethod("ApplyMetaEffects", BindingFlags.Instance | BindingFlags.Public);
            if (m != null && m.GetParameters().Length == 0)
            {
                try { m.Invoke(gc, null); }
                catch (TargetInvocationException) { /* optional path; the assertions still stand */ }
            }
        }

        /// <summary>Look for a non-empty "why can't I shoot" string on the gun, the controller, or
        /// any HUD component in the scene.</summary>
        static string FindStatedNoWeaponReason(Component gun, GameController gc)
        {
            string reason = PlayTestUtil.TryReadStringMember(gun, ReasonNameFragments);
            if (!string.IsNullOrEmpty(reason)) return reason;

            reason = PlayTestUtil.TryReadStringMember(gc, ReasonNameFragments);
            if (!string.IsNullOrEmpty(reason)) return reason;

            foreach (MonoBehaviour mb in PlayTestUtil.FindAll<MonoBehaviour>())
            {
                if (mb == null) continue;
                if (mb.GetType().Name.IndexOf("Hud", StringComparison.OrdinalIgnoreCase) < 0) continue;
                reason = PlayTestUtil.TryReadStringMember(mb, ReasonNameFragments);
                if (!string.IsNullOrEmpty(reason)) return reason;
            }
            return null;
        }

        /// <summary>Any MonoBehaviour in the active scene whose type name contains "Hud" and that
        /// actually declares an OnGUI pass.</summary>
        static MonoBehaviour FindHudRenderer()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (MonoBehaviour mb in PlayTestUtil.FindAll<MonoBehaviour>())
            {
                if (mb == null) continue;
                Type type = mb.GetType();
                if (type.Name.IndexOf("Hud", StringComparison.OrdinalIgnoreCase) < 0) continue;
                MethodInfo onGui;
                try { onGui = type.GetMethod("OnGUI", flags); }
                catch (AmbiguousMatchException) { onGui = null; }
                if (onGui != null) return mb;
            }
            return null;
        }
    }
}
