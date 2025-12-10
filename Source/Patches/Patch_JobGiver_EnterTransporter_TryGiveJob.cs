using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Carts
{
    [HarmonyPatch(typeof(JobGiver_EnterTransporter))]
    [HarmonyPatch("TryGiveJob")]
    public class Patch_JobGiver_EnterTransporter_TryGiveJob
    {
        public static bool Prefix(JobGiver_EnterTransporter __instance, Pawn pawn, ref Job __result)
        {
            int transportersGroup = pawn.mindState.duty.transportersGroup;
            if (transportersGroup != -1)
            {
                IReadOnlyList<Pawn> allPawnsSpawned = pawn.Map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < allPawnsSpawned.Count; i++)
                {
                    if (allPawnsSpawned[i] != pawn && allPawnsSpawned[i].CurJobDef == DefDatabase<JobDef>.GetNamed("LoadTransportersByCart"))
                    {
                        CompTransporter transporter = ((JobDriver_HaulToContainerByCart)allPawnsSpawned[i].jobs.curDriver).Container?.TryGetComp<CompTransporter>();
                        if (transporter != null && transporter.groupID == transportersGroup)
                        {
                            __result = null;
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
}
