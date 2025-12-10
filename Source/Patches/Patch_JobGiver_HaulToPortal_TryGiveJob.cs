using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Carts
{
    [HarmonyPatch(typeof(JobGiver_HaulToPortal))]
    [HarmonyPatch("TryGiveJob")]
    public class Patch_JobGiver_HaulToPortal_TryGiveJob
    {
        public static bool Prefix(JobGiver_HaulToPortal __instance, Pawn pawn, ref Job __result)
        {
            MapPortal portal = pawn.mindState.duty.focus.Thing as MapPortal;
            WorkGiver_HaulToPortalByCart wg = new WorkGiver_HaulToPortalByCart();
            wg.neededPortal = portal;
            foreach (var cart in wg.PotentialWorkThingsGlobal(pawn))
            {
                if (wg.HasJobOnThing(pawn, cart, false))
                {
                    __result = wg.JobOnThing(pawn, cart, false);
                    return false;
                }
            }
            return true;
        }
    }
}