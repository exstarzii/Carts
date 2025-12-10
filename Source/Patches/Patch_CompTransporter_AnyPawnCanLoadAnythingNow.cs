using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Carts
{
    [HarmonyPatch(typeof(CompTransporter))]
    [HarmonyPatch("get_AnyPawnCanLoadAnythingNow")]
    public class Patch_CompTransporter_AnyPawnCanLoadAnythingNow
    {
        public static bool Prefix(CompTransporter __instance, ref bool __result)
        {
            __result = AnyPawnCanLoadAnythingNow(__instance);

            return false;
        }

        public static bool AnyPawnCanLoadAnythingNow(CompTransporter __instance)
        {
            if (!__instance.AnythingLeftToLoad)
            {
                return false;
            }
            if (!__instance.parent.Spawned)
            {
                return false;
            }
            IReadOnlyList<Pawn> allPawnsSpawned = __instance.parent.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawnsSpawned.Count; i++)
            {
                if (allPawnsSpawned[i].CurJobDef == DefDatabase<JobDef>.GetNamed("LoadTransportersByCart"))
                {
                    if(allPawnsSpawned[i].jobs.curDriver is JobDriver_HaulToContainerByCart curDriver)
                    {
                        CompTransporter transporter = curDriver.Container?.TryGetComp<CompTransporter>();
                        if (transporter != null && transporter.groupID == __instance.groupID)
                        {
                            return true;
                        }
                    }
                }
                if (allPawnsSpawned[i].CurJobDef == JobDefOf.HaulToTransporter)
                {
                    CompTransporter transporter = ((JobDriver_HaulToTransporter)allPawnsSpawned[i].jobs.curDriver).Transporter;
                    if (transporter != null && transporter.groupID == __instance.groupID)
                    {
                        return true;
                    }
                }
                if (allPawnsSpawned[i].CurJobDef == JobDefOf.EnterTransporter)
                {
                    CompTransporter transporter2 = ((JobDriver_EnterTransporter)allPawnsSpawned[i].jobs.curDriver).Transporter;
                    if (transporter2 != null && transporter2.groupID == __instance.groupID)
                    {
                        return true;
                    }
                }
            }
            List<CompTransporter> list = __instance.TransportersInGroup(__instance.parent.Map);
            if (list == null)
            {
                return false;
            }
            for (int j = 0; j < allPawnsSpawned.Count; j++)
            {
                if (allPawnsSpawned[j].mindState.duty != null && allPawnsSpawned[j].mindState.duty.transportersGroup == __instance.groupID)
                {
                    CompTransporter compTransporter = JobGiver_EnterTransporter.FindMyTransporter(list, allPawnsSpawned[j]);
                    if (compTransporter != null && allPawnsSpawned[j].CanReach(compTransporter.parent, PathEndMode.Touch, Danger.Deadly))
                    {
                        return true;
                    }
                }
            }
            for (int k = 0; k < allPawnsSpawned.Count; k++)
            {
                if (!allPawnsSpawned[k].IsColonist)
                {
                    continue;
                }
                for (int l = 0; l < list.Count; l++)
                {
                    if (LoadTransportersJobUtility.HasJobOnTransporter(allPawnsSpawned[k], list[l]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
