using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Carts
{
    [HarmonyPatch(typeof(TransporterUtility))]
    [HarmonyPatch(nameof(TransporterUtility.ThingsBeingHauledTo))]
    public static class Patch_TransporterUtility_ThingsBeingHauledTo
    {
        public static bool Prefix(List<CompTransporter> transporters, Map map, ref IEnumerable<Thing> __result)
        {
            __result = CustomThingsBeingHauledTo(transporters, map);

            return false;
        }

        private static IEnumerable<Thing> CustomThingsBeingHauledTo(List<CompTransporter> transporters, Map map)
        {
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i].CurJobDef == JobDefOf.HaulToTransporter && transporters.Contains(((JobDriver_HaulToTransporter)pawns[i].jobs.curDriver).Transporter) && pawns[i].carryTracker.CarriedThing != null)
                {
                    yield return pawns[i].carryTracker.CarriedThing;
                }
                else if (pawns[i].CurJobDef == DefDatabase<JobDef>.GetNamed("LoadTransportersByCart") &&
                    pawns[i].jobs.curDriver is JobDriver_HaulToContainerByCart cartJob)
                {
                    var cart = cartJob.Cart.Thing as Building_Cart;
                    var isContain = transporters.Contains(cartJob.Container?.TryGetComp<CompTransporter>());
                    if (isContain && cart != null)
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