using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Carts
{
    public class WorkGiver_LoadTransportersByCart : WorkGiver_HaulToContainerByCart
    {
        private static HashSet<Thing> neededThings = new HashSet<Thing>();

        private static Dictionary<TransferableOneWay, int> tmpAlreadyLoading = new Dictionary<TransferableOneWay, int>();
        public override bool OnlyOneStorage => true;
        public override string jobDefName => "LoadTransportersByCart";
        public List<CompTransporter> neededTransporters;
        private static void FillLoadingAndNeededThings(CompTransporter transporter, Pawn p)
        {
            neededThings.Clear();
            tmpAlreadyLoading.Clear();

            List<TransferableOneWay> leftToLoad = transporter.leftToLoad;
            if (leftToLoad == null)
            {
                return;
            }

            void RegisterLoading(Thing regThing, int count)
            {
                if (regThing == null || count <= 0) return;
                var transferable = TransferableUtility.TransferableMatchingDesperate(
                    regThing, leftToLoad, TransferAsOneMode.PodsOrCaravanPacking);
                if (transferable == null) return;

                tmpAlreadyLoading[transferable] = tmpAlreadyLoading.TryGetValue(transferable, out var current)
                    ? current + count
                    : count;
            }

            IReadOnlyList<Pawn> allPawnsSpawned = transporter.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawnsSpawned.Count; i++)
            {
                if (allPawnsSpawned[i] == p) continue;

                var jobDef = allPawnsSpawned[i].CurJobDef;
                if (jobDef == JobDefOf.HaulToTransporter)
                {
                    var job = allPawnsSpawned[i].jobs.curDriver as JobDriver_HaulToTransporter;
                    if (job?.Container == transporter.parent)
                        RegisterLoading(job.ThingToCarry, job.initialCount);
                }
                else if (jobDef == DefDatabase<JobDef>.GetNamed("LoadTransportersByCart"))
                {
                    var job = allPawnsSpawned[i].jobs.curDriver as JobDriver_HaulToContainerByCart;
                    if (job?.Container != transporter.parent) continue;

                    var cart = job.Cart.Thing as Building_Cart;
                    if (cart != null)
                    {
                        foreach (var item in cart.innerContainer)
                            RegisterLoading(item, item.stackCount);
                    }

                    if (job.Items != null)
                    {
                        foreach (var item in job.Items)
                            RegisterLoading(item.Thing, CartUtility.GetPawnReservedCount(allPawnsSpawned[i], item));
                    }
                }
            }
            for (int j = 0; j < leftToLoad.Count; j++)
            {
                TransferableOneWay transferableOneWay2 = leftToLoad[j];
                if (!tmpAlreadyLoading.TryGetValue(leftToLoad[j], out var value2))
                {
                    value2 = 0;
                }
                if (transferableOneWay2.CountToTransfer - value2 > 0)
                {
                    for (int k = 0; k < transferableOneWay2.things.Count; k++)
                    {
                        neededThings.Add(transferableOneWay2.things[k]);
                    }
                }
            }
        }

        public override bool TryFindItemsForContainer(Building_Cart cart, VirtualCart virtualCart, Thing c, Pawn p, Dictionary<Thing, int> items)
        {
            var transporter = c.TryGetComp<CompTransporter>();
            FillLoadingAndNeededThings(transporter, p);

            List<TransferableOneWay> leftToLoad = transporter.leftToLoad;

            if (!neededThings.Any())
            {
                tmpAlreadyLoading.Clear();
                return false;
            }
            var lastPositionSearch = p.Position;
            bool itemsWasAdded = false;
            while (neededThings.Count > 0)
            {
                Thing thing = GenClosest.ClosestThing_Global_Reachable(lastPositionSearch, p.Map, neededThings, PathEndMode.Touch, TraverseParms.For(p), 99999f, (Thing x) => CartUtility.ContainerLoadValidator(p, x, cart, items));

                if (thing == null)
                {
                    break;
                }
                TransferableOneWay transferableOneWay3 = leftToLoad.First(t => t.things.Contains(thing));
                if (!tmpAlreadyLoading.TryGetValue(transferableOneWay3, out var value3))
                {
                    value3 = 0;
                }
                int count = Mathf.Min(transferableOneWay3.CountToTransfer - value3, thing.stackCount);
                lastPositionSearch = thing.Position;
                if (!virtualCart.Add(thing.def, count))
                {
                    break;
                }
                items[thing] = count;
                itemsWasAdded = true;
                neededThings.Remove(thing);
            }

            return itemsWasAdded;
        }

        public override IEnumerable<Thing> FindContainersToHaul(Pawn pawn, Building_Cart cart)
        {
            if (neededTransporters != null)
            {
                foreach (var transporter in neededTransporters)
                {
                    if (transporter != null && HasJobOnTransporter(pawn, transporter, cart))
                    {
                        yield return transporter.parent;
                    }
                }
            }
            else
            {
                var list = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Transporter);
                foreach (var t in list)
                {
                    CompTransporter transporter = t.TryGetComp<CompTransporter>();
                    if (transporter != null && HasJobOnTransporter(pawn, transporter, cart))
                    {
                        yield return t;
                    }
                }
            }
        }

        public static bool HasJobOnTransporter(Pawn pawn, CompTransporter transporter, Building_Cart cart)
        {
            if (!transporter.AnythingLeftToLoad)
            {
                return false;
            }

            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return false;
            }

            if (!pawn.CanReach(transporter.parent, PathEndMode.Touch, pawn.NormalMaxDanger()))
            {
                return false;
            }

            var thingToLoad = Patch_LoadTransportersJobUtility_FindThingToLoad.FindThingToLoad(pawn, transporter, cart.GetStoreSettings().AllowedToAccept).Thing;
            if (thingToLoad == null || thingToLoad is Pawn)
            {
                return false;
            }

            return true;
        }
    }
}
