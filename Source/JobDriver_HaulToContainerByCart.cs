using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace Carts
{
    [Flags]
    public enum HaulToContainerStatus
    {
        Ongoing = 0,
        ReturnCart = 1,
    }
    public class JobDriver_HaulToContainerByCart : JobDriver, IBuildableDriver
    {
        private const TargetIndex ItemInd = TargetIndex.A;
        private const TargetIndex CartInd = TargetIndex.B;
        private const TargetIndex StorageInd = TargetIndex.C;
        public bool markToInterruptForced;
        public HaulToContainerStatus jobStatus = HaulToContainerStatus.Ongoing;

        public LocalTargetInfo Cart => job.targetB;
        public List<LocalTargetInfo> Items => job.targetQueueA;
        public List<LocalTargetInfo> Storages => job.targetQueueB;
        public Thing Container
        {
            get
            {
                if (job.targetQueueB.Any() && job.targetQueueB[0].IsValid)
                {
                    return job.targetQueueB[0].Thing;
                }
                else if (job.targetC.IsValid)
                {
                    return job.targetC.Thing;
                }
                else
                {
                    return null;
                }
            }
        }

        public Thing ThingToCarry => (Thing)job.GetTarget(TargetIndex.A);

        public bool TryGetBuildableRect(out CellRect rect)
        {
            if (Container is Blueprint)
            {
                rect = Container.OccupiedRect();
                return true;
            }
            rect = default(CellRect);
            return false;
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(Cart, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            for (int i = 0; i < Items.Count; i++)
            {
                pawn.Reserve(Items[i], job, 1, job.countQueue[i]);
            }

            pawn.ReserveAsManyAsPossible(Storages, job);

            var allItems = new Dictionary<ThingDef, int>();
            foreach (var item in Items)
            {
                allItems.TryGetValue(item.Thing.def, out int currentCount);
                allItems[item.Thing.def] = currentCount + item.Thing.stackCount;
            }
            for(int i=0;i<Storages.Count;i++)
            {
                var building = Storages[i].Thing;
                if (building is IHaulEnroute container && !building.DestroyedOrNull())
                {
                    foreach (ThingDefCountClass need in (building as IConstructible).TotalMaterialCost())
                    {
                        if (allItems.ContainsKey(need.thingDef))
                        {
                            int count = Math.Min(need.count, allItems[need.thingDef]);
                            container.Map.enrouteManager.AddEnroute(container, pawn, need.thingDef, count);
                            allItems[need.thingDef] -= count;
                            if (allItems[need.thingDef] <= 0)
                            {
                                allItems.Remove(need.thingDef);
                            }
                        }
                    }
                }
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var startUnloadCartLabel = Toils_General.Label();
            var returnCartLabel = Toils_General.Label();

            this.FailOnDestroyedOrNull(CartInd);
            this.FailOnForbidden(CartInd);
            this.FailJump(returnCartLabel, () => EnterPortalUtility.WasLoadingCanceled(Container));
            this.FailJump(returnCartLabel, () => TransporterUtility.WasLoadingCanceled(Container));
            this.FailJump(returnCartLabel, () => markToInterruptForced);

            yield return Toils_Cart.GotoThing(CartInd, PathEndMode.Touch).FailOnSomeonePhysicallyInteracting(CartInd);
            yield return Toils_Haul.StartCarryThing(CartInd);

            Toil tryGetNextItem = Toils_Jump.JumpIf(startUnloadCartLabel, () => !Toils_Cart.TryGetNextItemFromQueue(ItemInd, CartInd, job, pawn));
            yield return tryGetNextItem;
            yield return Toils_Cart.GotoThing(ItemInd, PathEndMode.Touch, tryGetNextItem);
            yield return Toils_Cart.DepositThingInCart(ItemInd,true);
            yield return Toils_Jump.Jump(tryGetNextItem);

            yield return startUnloadCartLabel;
            Toil tryGetNextStorage = Toils_Jump.JumpIf(returnCartLabel, () => !Toils_Cart.TryGetNextContainerFromQueue(TargetIndex.B, StorageInd, CartInd, job, pawn));
            yield return tryGetNextStorage;
            Toil goToBuilding = Toils_Cart.GotoBuilding(StorageInd, PathEndMode.Touch, tryGetNextStorage);
            yield return goToBuilding;
            yield return Toils_Goto.MoveOffTargetBlueprint(StorageInd);
            yield return Toils_Cart.WaitWithEffect(StorageInd, ItemInd);
            yield return Toils_Construct.MakeSolidThingFromBlueprintIfNecessary(StorageInd);
            yield return Toils_Cart.PlaceThingFromCartInStorage(StorageInd, ItemInd);
            yield return Toils_Jump.Jump(tryGetNextStorage);

            yield return returnCartLabel;
            yield return Toils_General.DoAtomic(() =>
            {
                var cart = pawn.carryTracker.CarriedThing as Building_Cart;
                if(cart == null)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                    return;
                }
                job.SetTarget(ItemInd, cart.getLastPosition());
            });
            yield return Toils_Goto.GotoCell(ItemInd, PathEndMode.Touch);
            yield return Toils_Cart.DropCart(ItemInd);
        }
    }
}
