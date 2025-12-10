using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Carts
{
    public class WorkGiver_HaulCorpsesByCart : WorkGiver_PushCart
    {
        private static Dictionary<Pawn, SearchResultCache> _pawnToSearchResultCache = new Dictionary<Pawn, SearchResultCache>();
        public override Dictionary<Pawn, SearchResultCache> pawnToSearchResultCache => _pawnToSearchResultCache;
        public override string jobDefName => "HaulCorpsesByCart";

        public override bool FindThingsAndStoragesValidator(Pawn pawn, Thing t, Building_Cart cart, Dictionary<Thing, int> items, List<LocalTargetInfo> storages)
        {
            if (!(t is Corpse))
            {
                return false;
            }
            Pawn pawn2 = pawn.Map.physicalInteractionReservationManager.FirstReserverOf(t);
            if (pawn2 != null && pawn2.RaceProps.Animal && pawn2.Faction != Faction.OfPlayer)
            {
                return false;
            }
            return base.FindThingsAndStoragesValidator(pawn, t, cart, items, storages);
        }

        public override bool CanUnloadCart(Pawn pawn, Building_Cart cart)
        {
            StoragePriority currentPriority = cart.GetStoreSettings().Priority;
            foreach (var t in cart.innerContainer)
            {
                if (!(t is Corpse))
                {
                    continue;
                }
                if (StoreUtility.TryFindBestBetterStorageFor(t, pawn, pawn.Map, currentPriority, pawn.Faction, out var foundCell, out var haulDestination))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
