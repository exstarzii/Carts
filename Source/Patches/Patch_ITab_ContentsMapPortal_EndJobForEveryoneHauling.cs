using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Carts
{
    [HarmonyPatch(typeof(ITab_ContentsMapPortal), "EndJobForEveryoneHauling")]
    public static class Patch_ITab_ContentsMapPortal_EndJobForEveryoneHauling
    {
        public static bool Prefix(ITab_ContentsMapPortal __instance, TransferableOneWay t)
        {
            IReadOnlyList<Pawn> allPawnsSpawned = __instance.Portal.Map.mapPawns.AllPawnsSpawned;

            for (int i = 0; i < allPawnsSpawned.Count; i++)
            {
                Pawn pawn = allPawnsSpawned[i];
                if (pawn.CurJobDef == JobDefOf.HaulToTransporter)
                {
                    JobDriver_HaulToPortal jobDriver_HaulToPortal = pawn.jobs.curDriver as JobDriver_HaulToPortal;

                    if (jobDriver_HaulToPortal != null &&
                        jobDriver_HaulToPortal.MapPortal == __instance.Portal &&
                        jobDriver_HaulToPortal.ThingToCarry != null &&
                        jobDriver_HaulToPortal.ThingToCarry.def == t.ThingDef)
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                    }
                }
                if (pawn.CurJobDef == DefDatabase<JobDef>.GetNamed("HaulToPortalByCart"))
                {
                    JobDriver_HaulToContainerByCart jobDriver_HaulToPortalByCart = pawn.jobs.curDriver as JobDriver_HaulToContainerByCart;
                    if (jobDriver_HaulToPortalByCart != null && 
                        jobDriver_HaulToPortalByCart.Container == __instance.Portal)
                    {
                        CartUtility.ValidateHaulToContainerByCartJob(jobDriver_HaulToPortalByCart, t);
                    }
                }
            }

            return false;
        }
    }
}