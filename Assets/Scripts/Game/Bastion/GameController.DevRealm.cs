using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY (partial of GameController): dev shortcuts that fill in a realm's
    /// record and a Boarder's kit, so a slice can be looked at without playing for it.
    /// Split so GameController.DevCheckpoints.cs stays under cap.</summary>
    /// <remarks>
    /// The other checkpoints build a whole MetaState and load a scene. These deliberately do
    /// not: they mutate the session standing in front of you, so you can be on the Ship
    /// looking at a bare wall, press the key, and watch it fill. Reloading would throw away
    /// the very thing being examined.
    ///
    /// Both taint the session, so nothing here can reach a real campaign save.
    /// </remarks>
    public sealed partial class GameController
    {
        /// <summary>Runs survived to hand a realm that has never been played, chosen to be
        /// more than the marks wall holds so the overflow case is visible too.</summary>
        const int DevCleanExits = 14;
        const int DevRunsLost = 3;
        const int DevDeepestWave = 4;
        const int DevHatchArmor = 2;

        /// <summary>Fill this realm's record and this Boarder's kit, in place. The display
        /// polls, so the wall, the rack and the plate all catch up within half a second
        /// without anything here reaching into them.</summary>
        public static void StockRealmAndBoarder()
        {
            GameSession.NotifyDevAction();
            GameSession.EnsureLoaded();
            var meta = GameSession.Meta;
            if (meta == null)
            {
                Log.Warn(Tag, "stock ignored — no MetaState.");
                return;
            }

            StockRealm(meta.Realm);
            StockBoarder(meta.Boarder);
            RealmRecordPublisher.Publish("dev stock");

            Log.Event("Dev", "stocked realm \"" + meta.Realm.Name + "\" — "
                + DevCleanExits + " survived, " + DevRunsLost + " lost, wave "
                + meta.DeepestWaveReached + ", hatch +" + meta.HatchArmorLevel
                + "; Boarder carries every catalogued weapon");
        }

        /// <summary>A realm with a record worth displaying. Salvage goes through Earn so the
        /// ledger still balances afterwards — a dev shortcut that leaves the save internally
        /// inconsistent would send the next reader hunting a bug that is not there.</summary>
        static void StockRealm(RealmState realm)
        {
            if (realm == null) return;
            realm.CleanExits = DevCleanExits;
            realm.PodEjections = DevRunsLost;
            realm.TotalRuns = DevCleanExits + DevRunsLost;
            if (realm.DeepestWaveReached < DevDeepestWave)
                realm.DeepestWaveReached = DevDeepestWave;
            realm.HatchArmorLevel = DevHatchArmor;
            realm.EarnSalvage(5000L);
        }

        /// <summary>Every weapon this build sells, each with points on its ring, plus the
        /// default protocol and a repair rig. Enough to fill the rack and drive the loadout
        /// without buying anything.</summary>
        static void StockBoarder(BoarderState boarder)
        {
            if (boarder == null) return;
            for (int i = 0; i < WeaponCatalog.ArraySize; i++)
            {
                var id = (WeaponId)i;
                if (!WeaponCatalog.IsAvailable(id)) continue;
                boarder.SetOwned(id, true);
                boarder.ReplacePointsFromFlatTier(id, 2);
                boarder.SetEquipped(id);
            }
            boarder.EquipmentSlotPurchased = true;
            boarder.SetEquipmentOwned(EquipmentId.RepairRig, true);
            boarder.SetEquippedEquipment(EquipmentId.RepairRig);
            if (boarder.EquippedProtocol == BoarderProtocolId.None)
                BoarderProtocol.TryClaim(GameSession.Meta, BoarderProtocolId.Default);
        }
    }
}
