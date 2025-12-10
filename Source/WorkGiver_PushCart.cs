using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace Carts
{
    public class SearchResultCache
    {
        public Thing thing;
        public Building_Cart cart;
        public int lastSearchTick;

        public SearchResultCache(Thing thing, Building_Cart cart, int lastSearchTick)
        {
            this.thing = thing;
            this.cart = cart;
            this.lastSearchTick = lastSearchTick;
        }
    }

    public class WorkGiver_PushCart : WorkGiver_Scanner
    {
        private static Dictionary<Pawn, SearchResultCache> _pawnToSearchResultCache = new Dictionary<Pawn, SearchResultCache>();
        public virtual Dictionary<Pawn, SearchResultCache> pawnToSearchResultCache => _pawnToSearchResultCache;
        public override PathEndMode PathEndMode => PathEndMode.Touch;
        public virtual string jobDefName => "PushCart";

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            for (int i = 0; i < pawn.Map.listerBuildings.allBuildingsColonist.Count; i++)
            {
                if (pawn.Map.listerBuildings.allBuildingsColonist[i] is Building_Cart)
                {
                    yield return pawn.Map.listerBuildings.allBuildingsColonist[i];
                }
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced)
        {
            if (t is Building_Cart cart)
            {
                if ((CanLoadCart(pawn, cart) || CanUnloadCart(pawn, cart)) &&
                    !cart.IsForbidden(pawn) &&
                    HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t, forced))
                {
                    return true;
                }
            }

            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced)
        {
            var cart = t as Building_Cart;
            var items = new Dictionary<Thing, int>();
            var storages = new List<LocalTargetInfo>();
            Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed(jobDefName));
            FindAndReserveThingsAndStorages(pawn, job, cart, items, storages);
            job.count = 9999;
            job.haulMode = HaulMode.ToContainer;
            job.targetQueueA = items.Keys.Select(i => (LocalTargetInfo)i).ToList();
            job.countQueue = items.Values.ToList();
            job.SetTarget(TargetIndex.B, cart);
            return job;
        }

        public void FindAndReserveThingsAndStorages(Pawn pawn,Job job, Building_Cart cart, Dictionary<Thing, int> items, List<LocalTargetInfo> storages)
        {
            var lastPositionSearch = cart.Position;
            var virtualCart = new VirtualCart((cart.def as ThingDef_Cart).cartMaxItems, cart.innerContainer);
            while (true)
            {
                Thing foundRes = GenClosest.ClosestThingReachable(lastPositionSearch, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.HaulableEverOrMinifiable), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, (Thing r) => FindThingsAndStoragesValidator(pawn, r, cart, items, storages));
                if (foundRes == null)
                {
                    break;
                }
                lastPositionSearch = foundRes.Position;
                if (!virtualCart.Add(foundRes.def, foundRes.stackCount))
                {
                    break;
                }
                if (storages.Count > 0)
                {
                    pawn.Reserve(storages.Last(), job);
                }
                items[foundRes] = foundRes.stackCount;
            }
            pawn.Map.reservationManager.ReleaseAllClaimedBy(pawn);
        }

        // calls twice so storages contains duplicates x2
        public virtual bool FindThingsAndStoragesValidator(Pawn pawn, Thing t, Building_Cart cart, Dictionary<Thing, int> items, List<LocalTargetInfo> storages)
        {
            if (t.IsForbidden(pawn))
            {
                return false;
            }
            if (cart.GetCountCanAccept(t) == 0)
            {
                return false;
            }
            if (items.ContainsKey(t))
            {
                return false;
            }
            if (!HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t, forced: false))
            {
                return false;
            }
            StoragePriority currentPriority = StoreUtility.CurrentStoragePriorityOf(t);
            if (currentPriority == StoragePriority.Unstored)
            {
                if (!t.def.alwaysHaulable && pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Haul) == null)
                {
                    return false;
                }
                return true;
            }
            if (!StoreUtility.TryFindBestBetterStorageFor(t, pawn, pawn.Map, currentPriority, pawn.Faction, out var foundCell, out var haulDestination))
            {
                return false;
            }
            if (foundCell.IsValid)
            {
                storages.Add(foundCell);
            }
            else
            {
                storages.Add(haulDestination as Thing);
            }
            return true;
        }

        public bool CanLoadCart(Pawn pawn, Building_Cart cart)
        {
            if (pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling().Count == 0)
                return false;

            if (cart.innerContainer.Count >= (cart.def as ThingDef_Cart).cartMaxItems)
                return false;

            if (pawnToSearchResultCache.TryGetValue(pawn, out var search) &&
                search.lastSearchTick == Find.TickManager.TicksGame)
            {
                if (search.thing == null && 
                    pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling().All((Thing t) => cart.GetCountCanAccept(t) == search.cart.GetCountCanAccept(t)))
                {
                    return false;
                }
                if (search.thing != null && cart.GetCountCanAccept(search.thing) > 0)
                {
                    return true;
                }
            }

            var items = new Dictionary<Thing, int>();
            var storages = new List<LocalTargetInfo>();

            Thing foundRes = GenClosest.ClosestThingReachable(cart.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.HaulableEverOrMinifiable), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, (Thing r) => FindThingsAndStoragesValidator(pawn, r, cart, items, storages));
            pawnToSearchResultCache[pawn] = new SearchResultCache(foundRes, cart, Find.TickManager.TicksGame);
           
            return foundRes != null;
        }

        public virtual bool CanUnloadCart(Pawn pawn,Building_Cart cart)
        {
            StoragePriority currentPriority = cart.GetStoreSettings().Priority;
            foreach (var t in cart.innerContainer)
            {
                if (StoreUtility.TryFindBestBetterStorageFor(t, pawn, pawn.Map, currentPriority, pawn.Faction, out var foundCell, out var haulDestination))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
