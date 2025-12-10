using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace Carts
{
    public class WorkGiver_UnloadCarriersByCart : WorkGiver_Scanner
    {
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

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            IReadOnlyList<Pawn> allPawnsSpawned = pawn.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawnsSpawned.Count; i++)
            {
                if (allPawnsSpawned[i].inventory.UnloadEverything)
                {
                    return false;
                }
            }
            return true;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced)
        {
            if (t is Building_Cart cart)
            {
                var enumerator = FindPawnToUnload(pawn, cart).GetEnumerator();
                bool condition = ((cart.innerContainer.Count < (cart.def as ThingDef_Cart).cartMaxItems) && enumerator.MoveNext()) &&
                HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t, forced);
                if (condition) return true;
            }

            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced)
        {
            var cart = t as Building_Cart;
            var virtualCart = new VirtualCart((cart.def as ThingDef_Cart).cartMaxItems, cart.innerContainer);
            List<Thing> otherPawns = new List<Thing>();
            
            var enumerator = FindPawnToUnload(pawn, cart).GetEnumerator();
            while (!virtualCart.IsLoaded && enumerator.MoveNext())
            {
                var c = enumerator.Current;
                if (TryUnloadPawn(cart, virtualCart, c))
                {
                    otherPawns.Add(c);
                }
            }

            Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("UnloadCarriersByCart"));
            job.count = 9999;
            job.haulMode = HaulMode.ToCellStorage;
            job.targetQueueA = otherPawns.Select(i => (LocalTargetInfo)i).ToList();
            job.countQueue = otherPawns.Select(i => i.stackCount).ToList();
            job.SetTarget(TargetIndex.B, cart);
            return job;
        }

        public IEnumerable<Thing> FindPawnToUnload(Pawn pawn, Building_Cart cart)
        {
            foreach (var t in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (UnloadCarriersJobGiverUtility.HasJobOnThing(pawn, t, false))
                {
                    bool hasItemToUnload = false;
                    foreach(var item in t.inventory.innerContainer)
                    {
                        if(cart.GetCountCanAccept(item) > 0)
                        {
                            hasItemToUnload = true;
                            break;
                        }
                    }
                    if (hasItemToUnload)
                    {
                        yield return t;
                    }
                }
            }
        }

        public virtual bool TryUnloadPawn(Building_Cart cart, VirtualCart virtualCart, Thing c)
        {
            var otherPawn = c as Pawn; 
            bool itemsWasAdded = false;
            foreach (Thing need in otherPawn.inventory.innerContainer)
            {
                if (!cart.GetStoreSettings().AllowedToAccept(need))
                {
                    continue;
                }
                if (!virtualCart.Add(need))
                {
                    break;
                }
                itemsWasAdded = true;
            }
            return itemsWasAdded;
        }
    }
}
