using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Carts
{
    [HarmonyPatch(typeof(Dialog_LoadTransporters), "TryAccept")]
    public static class Patch_Dialog_LoadTransporters_TryAccept
    {
        static readonly AccessTools.FieldRef<Dialog_LoadTransporters, List<TransferableOneWay>> transferablesRef =
            AccessTools.FieldRefAccess<Dialog_LoadTransporters, List<TransferableOneWay>>("transferables");

        static readonly AccessTools.FieldRef<Dialog_LoadTransporters, List<CompTransporter>> transportersRef =
            AccessTools.FieldRefAccess<Dialog_LoadTransporters, List<CompTransporter>>("transporters");

        static readonly AccessTools.FieldRef<Dialog_LoadTransporters, Map> mapRef =
            AccessTools.FieldRefAccess<Dialog_LoadTransporters, Map>("map");

        public static bool Prefix(Dialog_LoadTransporters __instance, ref bool __result)
        {
            var transferables = transferablesRef(__instance);
            var transporters = transportersRef(__instance);
            var map = mapRef(__instance);

            List<Pawn> pawnsFromTransferables = TransferableUtility.GetPawnsFromTransferables(transferables);
            if (!CheckForErrors(__instance, pawnsFromTransferables))
            {
                __result = false;
                return false;
            }

            if (transporters[0].LoadingInProgressOrReadyToLaunch)
            {
                AssignTransferablesToRandomTransporters(__instance);
                TransporterUtility.MakeLordsAsAppropriate(pawnsFromTransferables, transporters, map);
                IReadOnlyList<Pawn> allPawnsSpawned = map.mapPawns.AllPawnsSpawned;

                for (int i = 0; i < allPawnsSpawned.Count; i++)
                {
                    var pawn = allPawnsSpawned[i];
                    if (pawn.CurJobDef == JobDefOf.HaulToTransporter &&
                        transporters.Contains(((JobDriver_HaulToTransporter)pawn.jobs.curDriver).Transporter))
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                    }
                    if (pawn.CurJobDef == DefDatabase<JobDef>.GetNamed("LoadTransportersByCart"))
                    {
                        var curDriver = pawn.jobs.curDriver as JobDriver_HaulToContainerByCart;
                        var transporter = curDriver.Container.TryGetComp<CompTransporter>();
                        if (curDriver != null && transporters.Contains(transporter)) {
                            CartUtility.ValidateHaulToContainerByCartJob(curDriver, transporter.leftToLoad);
                        }
                    }
                }
            }
            else
            {
                TransporterUtility.InitiateLoading(transporters);
                AssignTransferablesToRandomTransporters(__instance);
                TransporterUtility.MakeLordsAsAppropriate(pawnsFromTransferables, transporters, map);

                if (transporters.IsShuttle())
                {
                    Messages.Message("MessageShuttleLoadingProcessStarted".Translate(), transporters[0].parent, MessageTypeDefOf.TaskCompletion, historical: false);
                }
                else if (transporters[0].Props.max1PerGroup)
                {
                    Messages.Message("MessageTransporterSingleLoadingProcessStarted".Translate(), transporters[0].parent, MessageTypeDefOf.TaskCompletion, historical: false);
                }
                else
                {
                    Messages.Message("MessageTransportersLoadingProcessStarted".Translate(), transporters[0].parent, MessageTypeDefOf.TaskCompletion, historical: false);
                }
            }

            __result = true;
            return false;
        }

        private static bool CheckForErrors(Dialog_LoadTransporters instance, List<Pawn> pawns)
        {
            var method = AccessTools.Method(typeof(Dialog_LoadTransporters), "CheckForErrors");
            return (bool)method.Invoke(instance, new object[] { pawns });
        }
        private static void AssignTransferablesToRandomTransporters(Dialog_LoadTransporters instance)
        {
            var method = AccessTools.Method(typeof(Dialog_LoadTransporters), "AssignTransferablesToRandomTransporters");
            method.Invoke(instance, null);
        }
    }
}