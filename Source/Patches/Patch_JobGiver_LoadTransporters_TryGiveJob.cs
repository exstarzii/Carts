using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Carts
{
    [HarmonyPatch(typeof(JobGiver_LoadTransporters))]
    [HarmonyPatch("TryGiveJob")]
    public class Patch_JobGiver_LoadTransporters_TryGiveJob
    {
        static readonly  List<CompTransporter> tmpTransporters = AccessTools.StaticFieldRefAccess<JobGiver_LoadTransporters, List<CompTransporter>>("tmpTransporters");
        public static bool Prefix(JobGiver_LoadTransporters __instance, Pawn pawn, ref Job __result)
        {
            TransporterUtility.GetTransportersInGroup(pawn.mindState.duty.transportersGroup, pawn.Map, tmpTransporters);
            WorkGiver_LoadTransportersByCart wg = new WorkGiver_LoadTransportersByCart();
            wg.neededTransporters = tmpTransporters;
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