using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Carts
{
    [HarmonyPatch(typeof(ITab_ContentsTransporter), "EndJobForEveryoneHauling")]
    public static class Patch_ITab_ContentsTransporter_EndJobForEveryoneHauling
    {
        public static bool Prefix(ITab_ContentsTransporter __instance, TransferableOneWay t)
        {
            IReadOnlyList<Pawn> allPawnsSpawned = __instance.Transporter.Map.mapPawns.AllPawnsSpawned;

            for (int i = 0; i < allPawnsSpawned.Count; i++)
            {
                Pawn pawn = allPawnsSpawned[i];
                if (allPawnsSpawned[i].CurJobDef == JobDefOf.HaulToTransporter)
                {
                    JobDriver_HaulToTransporter jobDriver_HaulToTransporter = (JobDriver_HaulToTransporter)allPawnsSpawned[i].jobs.curDriver;
                    if (jobDriver_HaulToTransporter.Transporter == __instance.Transporter && jobDriver_HaulToTransporter.ThingToCarry != null && jobDriver_HaulToTransporter.ThingToCarry.def == t.ThingDef)
                    {
                        allPawnsSpawned[i].jobs.EndCurrentJob(JobCondition.InterruptForced);
                    }
                }
                if (pawn.CurJobDef == DefDatabase<JobDef>.GetNamed("LoadTransportersByCart"))
                {
                    var curDriver = pawn.jobs.curDriver as JobDriver_HaulToContainerByCart;
                    var transporter = curDriver.Container.TryGetComp<CompTransporter>();
                    if (curDriver != null && __instance.Transporter == transporter)
                    {
                        CartUtility.ValidateHaulToContainerByCartJob(curDriver, t);
                    }
                }
            }

            return false;
        }
    }
}