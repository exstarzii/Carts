using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace Carts
{
    public class WorkGiver_HaulToContainerByCart : WorkGiver_Scanner
    {
        public virtual string jobDefName => "HaulToContainerByCart";

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

        public virtual bool OnlyOneStorage => false;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced)
        {
            if (t is Building_Cart cart)
            {
                var enumerator = FindContainersToHaul(pawn, cart).GetEnumerator();
                if ((cart.innerContainer.Count < (cart.def as ThingDef_Cart).cartMaxItems) && enumerator.MoveNext() &&
                    !cart.IsForbidden(pawn) &&
                    HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t, forced))
                    return true;
            }

            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced)
        {
            var cart = t as Building_Cart;
            var virtualCart = new VirtualCart((cart.def as ThingDef_Cart).cartMaxItems, cart.innerContainer);
            var items = new Dictionary<Thing, int>();
            List<Thing> containers = new List<Thing>();

            var enumerator = FindContainersToHaul(pawn, cart).GetEnumerator();
            while (enumerator.MoveNext())
            {
                var c = enumerator.Current;
                if (TryFindItemsForContainer(cart, virtualCart, c, pawn, items))
                {
                    containers.Add(c);
                }
                if (OnlyOneStorage)
                {
                    break;
                }
            }

            Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed(jobDefName));
            job.count = 9999;
            job.haulMode = HaulMode.ToContainer;
            job.targetQueueA = items.Keys.Select(i => (LocalTargetInfo)i).ToList();
            job.countQueue = items.Values.ToList();
            job.targetQueueB = containers.Select(i => (LocalTargetInfo)i).ToList();
            job.SetTarget(TargetIndex.B, cart);
            return job;
        }

        public virtual bool TryFindItemsForContainer(Building_Cart cart, VirtualCart virtualCart, Thing c, Pawn pawn, Dictionary<Thing, int> items)
        {
            var construct = c as IConstructible;
            bool itemsWasAdded = false;
            foreach (ThingDefCountClass need in construct.TotalMaterialCost())
            {
                int canAddCount = virtualCart.CanAddCount(need.thingDef);
                if (canAddCount <= 0 || !cart.GetStoreSettings().AllowedToAccept(need.thingDef))
                {
                    continue;
                }
                int needFound = !(c is IHaulEnroute enroute) ? construct.ThingCountNeeded(need.thingDef) : enroute.GetSpaceRemainingWithEnroute(need.thingDef, pawn);
                needFound = Math.Min(needFound, canAddCount);
                var lastPositionSearch = cart.Position;
                while (needFound > 0)
                {
                    Thing foundRes = GenClosest.ClosestThingReachable(lastPositionSearch, pawn.Map, ThingRequest.ForDef(need.thingDef), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, (Thing r) => CartUtility.ResourceValidator(pawn, need, r, cart, items));
                    if (foundRes == null)
                    {
                        break;
                    }
                    int taken = items.ContainsKey(foundRes) ? items[foundRes] : 0;
                    int count = Math.Min(needFound, foundRes.stackCount - taken);
                    lastPositionSearch = foundRes.Position;
                    if (!virtualCart.Add(foundRes.def, count))
                    {
                        break;
                    }
                    items[foundRes] = count + taken;
                    itemsWasAdded = true;
                    needFound -= count;
                }
            }
            return itemsWasAdded;
        }

        public virtual IEnumerable<Thing> FindContainersToHaul(Pawn pawn, Building_Cart cart)
        {
            var list = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Construction);
            foreach (var t in list)
            {
                if (IsBlueprintAndHasJob(t, pawn, cart))
                {
                    yield return t;
                }
                if (IsFrameAndHasJob(t, pawn, cart))
                {
                    yield return t;
                }
            }
        }

        public bool IsFrameAndHasJob(Thing t, Pawn pawn, Building_Cart cart)
        {
            if (t.Faction != pawn.Faction)
            {
                return false;
            }
            if (!(t is Frame frame))
            {
                return false;
            }
            if (!GenConstruct.CanTouchTargetFromValidCell(frame, pawn))
            {
                return false;
            }
            if (GenConstruct.FirstBlockingThing(frame, pawn) != null)
            {
                return false;
            }
            if (!GenConstruct.CanConstruct(frame, pawn, def.workType, false, JobDefOf.HaulToContainer))
            {
                return false;
            }
            return CanDeliverResourceFor(pawn, frame,cart);
        }

        public bool IsBlueprintAndHasJob(Thing t, Pawn pawn,Building_Cart cart)
        {
            if (t.Faction != pawn.Faction)
            {
                return false;
            }
            if (!(t is Blueprint blueprint) || (t is Blueprint_Install) || (blueprint.def.entityDefToBuild is ThingDef thingDef && thingDef.plant != null))
            {
                return false;
            }
            if (!GenConstruct.CanTouchTargetFromValidCell(blueprint, pawn))
            {
                return false;
            }
            if (GenConstruct.FirstBlockingThing(blueprint, pawn) != null)
            {
                return false;
            }
            if (!GenConstruct.CanConstruct(blueprint, pawn, def.workType, false, JobDefOf.HaulToContainer))
            {
                return false;
            }
            if (ShouldRemoveExistingFloorFirst(pawn, blueprint))
            {
                return false;
            }
            return CanDeliverResourceFor(pawn, blueprint,cart);
        }

        protected static bool ShouldRemoveExistingFloorFirst(Pawn pawn, Blueprint blue)
        {
            if (blue.def.entityDefToBuild is TerrainDef)
            {
                return pawn.Map.terrainGrid.CanRemoveTopLayerAt(blue.Position);
            }
            return false;
        }

        protected bool CanDeliverResourceFor(Pawn pawn, IConstructible c, Building_Cart cart)
        {
            if (c is Blueprint_Install install)
            {
                return CanInstall(pawn, install);
            }
            var items = new Dictionary<Thing, int>();
            foreach (ThingDefCountClass need in c.TotalMaterialCost())
            {
                int num = !(c is IHaulEnroute enroute) ? c.ThingCountNeeded(need.thingDef) : enroute.GetSpaceRemainingWithEnroute(need.thingDef, pawn);
                if (num <= 0)
                {
                    continue;
                }
                var foundRes = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(need.thingDef), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, (Thing r) => CartUtility.ResourceValidator(pawn, need, r,cart, items));
                if (foundRes == null)
                {
                    continue;
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        private bool CanInstall(Pawn pawn, Blueprint_Install install)
        {
            Thing miniToInstallOrBuildingToReinstall = install.MiniToInstallOrBuildingToReinstall;
            IThingHolder parentHolder = miniToInstallOrBuildingToReinstall.ParentHolder;
            if (parentHolder != null && parentHolder is Pawn_CarryTracker pawn_CarryTracker)
            {
                return false;
            }
            if (miniToInstallOrBuildingToReinstall.IsForbidden(pawn))
            {
                return false;
            }
            if (!pawn.CanReach(miniToInstallOrBuildingToReinstall, PathEndMode.ClosestTouch, pawn.NormalMaxDanger()))
            {
                return false;
            }
            if (!pawn.CanReserve(miniToInstallOrBuildingToReinstall))
            {
                return false;
            }
            return true;
        }

        
    }
}
