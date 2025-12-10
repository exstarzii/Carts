using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Carts
{
    [HarmonyPatch(typeof(EnterPortalUtility))]
    [HarmonyPatch(nameof(EnterPortalUtility.ThingsBeingHauledTo))]
    public static class Patch_EnterPortalUtility_ThingsBeingHauledTo
    {
        public static bool Prefix(MapPortal portal, ref IEnumerable<Thing> __result)
        {
            __result = CustomThingsBeingHauledTo(portal);

            return false;
        }

        private static IEnumerable<Thing> CustomThingsBeingHauledTo(MapPortal portal)
        {
            IReadOnlyList<Pawn> pawns = portal.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i].CurJobDef == JobDefOf.HaulToPortal && ((JobDriver_HaulToPortal)pawns[i].jobs.curDriver).MapPortal == portal && pawns[i].carryTracker.CarriedThing != null)
                {
                    yield return pawns[i].carryTracker.CarriedThing;
                }
                else if (pawns[i].CurJobDef == DefDatabase<JobDef>.GetNamed("HaulToPortalByCart") &&
                    pawns[i].jobs.curDriver is JobDriver_HaulToContainerByCart cartJob)
                {
                    var cart = cartJob.Cart.Thing as Building_Cart;
                    if (cartJob.Container == portal && cart != null)
                    {
                        foreach (var item in cart.innerContainer)
                        {
                            if (!item.DestroyedOrNull())
                            {
                                yield return item;
                            }
                        }
                    }
                }
            }
        }
    }
}