using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Carts
{
    [HarmonyPatch(typeof(EnterPortalUtility))]
    [HarmonyPatch(nameof(EnterPortalUtility.FindThingToLoad))]
    public static class Patch_EnterPortalUtility_FindThingToLoad
    {
        private static HashSet<Thing> neededThings = new HashSet<Thing>();

        private static Dictionary<TransferableOneWay, int> tmpAlreadyLoading = new Dictionary<TransferableOneWay, int>();

        public static bool Prefix(Pawn p, MapPortal portal, ref ThingCount __result)
        {
            Func<Thing, bool> func = thing => true;
            __result = FindThingToLoad(p, portal, func);

            return false;
        }

        public static ThingCount FindThingToLoad(Pawn p, MapPortal portal, Func<Thing, bool> validator)
        {
            neededThings.Clear();
            List<TransferableOneWay> leftToLoad = portal.leftToLoad;
            tmpAlreadyLoading.Clear();

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

            if (leftToLoad != null)
            {
                IReadOnlyList<Pawn> allPawnsSpawned = portal.Map.mapPawns.PawnsInFaction(Faction.OfPlayer);
                for (int i = 0; i < allPawnsSpawned.Count; i++)
                {
                    if (allPawnsSpawned[i] == p) continue;

                    var jobDef = allPawnsSpawned[i].CurJobDef;
                    if (jobDef == JobDefOf.HaulToPortal)
                    {
                        var job = allPawnsSpawned[i].jobs.curDriver as JobDriver_HaulToPortal;
                        if (job?.Container == portal)
                            RegisterLoading(job.ThingToCarry, job.initialCount);
                    }
                    else if (jobDef == DefDatabase<JobDef>.GetNamed("HaulToPortalByCart"))
                    {
                        var job = allPawnsSpawned[i].jobs.curDriver as JobDriver_HaulToContainerByCart;
                        if (job?.Container != portal) continue;

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
            if (!neededThings.Any())
            {
                tmpAlreadyLoading.Clear();
                return default(ThingCount);
            }
            Thing thing = GenClosest.ClosestThingReachable(p.Position, p.Map, ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), PathEndMode.Touch, TraverseParms.For(p), 9999f, (Thing x) => neededThings.Contains(x) && validator(x) && p.CanReserve(x) && !x.IsForbidden(p) && p.carryTracker.AvailableStackSpace(x.def) > 0, null, 0, -1, forceAllowGlobalSearch: false, RegionType.Set_Passable, ignoreEntirelyForbiddenRegions: false, lookInHaulSources: true);
            if (thing == null)
            {
                foreach (Thing neededThing in neededThings)
                {
                    if (neededThing is Pawn pawn && ((!pawn.IsFreeColonist && !pawn.IsColonyMech) || pawn.Downed || pawn.IsSelfShutdown()) && !pawn.inventory.UnloadEverything && p.CanReserveAndReach(pawn, PathEndMode.Touch, Danger.Deadly))
                    {
                        neededThings.Clear();
                        tmpAlreadyLoading.Clear();
                        return new ThingCount(pawn, 1);
                    }
                }
            }
            neededThings.Clear();
            if (thing != null)
            {
                TransferableOneWay transferableOneWay3 = null;
                for (int num = 0; num < leftToLoad.Count; num++)
                {
                    if (leftToLoad[num].things.Contains(thing))
                    {
                        transferableOneWay3 = leftToLoad[num];
                        break;
                    }
                }
                if (!tmpAlreadyLoading.TryGetValue(transferableOneWay3, out var value3))
                {
                    value3 = 0;
                }
                tmpAlreadyLoading.Clear();
                return new ThingCount(thing, Mathf.Min(transferableOneWay3.CountToTransfer - value3, thing.stackCount));
            }
            tmpAlreadyLoading.Clear();
            return default(ThingCount);
        }
    }
}
