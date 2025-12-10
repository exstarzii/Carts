using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Carts
{
    [HarmonyPatch(typeof(MapPortal))]
    [HarmonyPatch("get_AnyPawnCanLoadAnythingNow")]
    public class Patch_MapPortal_AnyPawnCanLoadAnythingNow
    {
        public static bool Prefix(MapPortal __instance, ref bool __result)
        {
            __result = AnyPawnCanLoadAnythingNow(__instance);

            return false;
        }

        public static bool AnyPawnCanLoadAnythingNow(MapPortal __instance)
        {
            if (!__instance.LoadInProgress)
            {
                return false;
            }
            if (!__instance.Spawned)
            {
                return false;
            }
            IReadOnlyList<Pawn> allPawnsSpawned = __instance.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawnsSpawned.Count; i++)
            {
                if (allPawnsSpawned[i].CurJobDef == JobDefOf.HaulToPortal && ((JobDriver_HaulToPortal)allPawnsSpawned[i].jobs.curDriver).MapPortal == __instance)
                {
                    return true;
                }
                if (allPawnsSpawned[i].CurJobDef == JobDefOf.EnterPortal && ((JobDriver_EnterPortal)allPawnsSpawned[i].jobs.curDriver).MapPortal == __instance)
                {
                    return true;
                }
                if (allPawnsSpawned[i].CurJobDef == DefDatabase<JobDef>.GetNamed("HaulToPortalByCart") &&
                    ((JobDriver_HaulToContainerByCart)allPawnsSpawned[i].jobs.curDriver).Container == __instance)
                {
                    return true;
                }
            }
            for (int j = 0; j < allPawnsSpawned.Count; j++)
            {
                Thing thing = allPawnsSpawned[j].mindState?.duty?.focus.Thing;
                if (thing != null && thing == __instance && allPawnsSpawned[j].CanReach(thing, PathEndMode.Touch, Danger.Deadly))
                {
                    return true;
                }
            }
            for (int k = 0; k < allPawnsSpawned.Count; k++)
            {
                if (allPawnsSpawned[k].IsColonist && EnterPortalUtility.HasJobOnPortal(allPawnsSpawned[k], __instance))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
