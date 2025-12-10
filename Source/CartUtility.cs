using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace Carts
{
    public static class CartUtility
    {
        public static Thing GetBuildingAtCell(Map map, IntVec3 c)
        {
            foreach (var t in map.thingGrid.ThingsAt(c))
            {
                if (t is IConstructible || t.TryGetInnerInteractableThingOwner() != null)
                {
                    return t;
                }
            }
            return null;
        }

        public static int GetPawnReservedCount(Pawn p, LocalTargetInfo target)
        {
            var reservations = p.Map.reservationManager.ReservationsReadOnly;
            for (int i = reservations.Count - 1; i >= 0; i--)
            {
                if (reservations[i].Target == target && reservations[i].Claimant == p)
                {
                    if (reservations[i].StackCount == -1)
                    {
                        return target.Thing.def.stackLimit;
                    }
                    else
                    {
                        return reservations[i].StackCount;
                    }
                }
            }

            return 0;
        }

        public static LocalTargetInfo ClosestThingReachable(IntVec3 center, Map map, IEnumerable<LocalTargetInfo> searchSet, PathEndMode peMode, TraverseParms traverseParams, float maxDistance = 9999f, Predicate<LocalTargetInfo> validator = null)
        {
            if (searchSet == null)
            {
                return null;
            }

            LocalTargetInfo bestThing = LocalTargetInfo.Invalid;
            float maxDistanceSquared = maxDistance * maxDistance;
            float closestDistSquared = 2.14748365E+09f;

            foreach (LocalTargetInfo t in searchSet)
            {
                if (t != null && t.IsValid)
                {
                    float num2 = (center - t.Thing.Position).LengthHorizontalSquared;
                    if (num2 < closestDistSquared && num2 < maxDistanceSquared &&
                       (validator == null || validator(t)))
                    {
                        bestThing = t;
                        closestDistSquared = num2;
                    }
                }
            }

            return bestThing;
        }

        public static bool ResourceValidator(Pawn pawn, ThingDefCountClass need, Thing t, Building_Cart cart, Dictionary<Thing, int> items)
        {
            if (t.def != need.thingDef)
            {
                return false;
            }
            if (t.IsForbidden(pawn))
            {
                return false;
            }
            if (cart.GetCountCanAccept(t) == 0)
            {
                return false;
            }
            if (items.ContainsKey(t) && items[t] >= t.stackCount) 
            {
                return false;
            }
            if (!HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t, forced: false))
            {
                return false;
            }
            return true;
        }

        public static bool ContainerLoadValidator(Pawn pawn, Thing t, Building_Cart cart, Dictionary<Thing, int> items)
        {
            if (!t.Spawned)
            {
                return false;
            }
            if (t.stackCount <= 0)
            {
                return false;
            }
            if (items.ContainsKey(t))
            {
                return false;
            }
            if (t.IsForbidden(pawn))
            {
                return false;
            }
            if (!pawn.CanReserve(t))
            {
                return false;
            }
            if (cart.GetCountCanAccept(t) == 0)
            {
                return false;
            }
            return true;
        }

        public static int ThingCountToLoadToContainer(Thing thingToCheck, List<TransferableOneWay> leftToLoad)
        {
            var count = 0;
            TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatchingDesperate(thingToCheck, leftToLoad, TransferAsOneMode.PodsOrCaravanPacking);
            if (transferableOneWay != null)
            {
                count = Math.Min(thingToCheck.stackCount, transferableOneWay.CountToTransfer);
            }
            return count;
        }

        public static void ValidateHaulToContainerByCartJob(JobDriver_HaulToContainerByCart jobDriver, List<TransferableOneWay> leftToLoad)
        {
            jobDriver.Items.RemoveAll(t => t == null || !t.HasThing || !LeftToLoadContains(leftToLoad,t.Thing));
            ThingOwner owner = jobDriver.Cart.HasThing ? jobDriver.Cart.Thing.TryGetInnerInteractableThingOwner() : null;
            if (jobDriver.Items.Count == 0 && owner != null && !owner.Any(t => LeftToLoadContains(leftToLoad, t)))
            {
                jobDriver.markToInterruptForced = true;
            }
        }

        public static void ValidateHaulToContainerByCartJob(JobDriver_HaulToContainerByCart jobDriver, TransferableOneWay tran)
        {
            jobDriver.Items.RemoveAll(t => t == null || !t.HasThing || !tran.things.Contains(t.Thing));
            ThingOwner owner = jobDriver.Cart.HasThing ? jobDriver.Cart.Thing.TryGetInnerInteractableThingOwner() : null;
            if (jobDriver.Items.Count == 0 && owner != null && !owner.Any(t => tran.things.Contains(t)))
            {
                jobDriver.markToInterruptForced = true;
            }
        }

        public static bool LeftToLoadContains(List<TransferableOneWay> leftToLoad, Thing thing)
        {
            if (leftToLoad == null)
            {
                return false;
            }

            for (int i = 0; i < leftToLoad.Count; i++)
            {
                for (int j = 0; j < leftToLoad[i].things.Count; j++)
                {
                    if (leftToLoad[i].things[j] == thing)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
