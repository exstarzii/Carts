using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Carts
{
    public class JobDriver_PushCart : JobDriver
    {
        private const TargetIndex ItemInd = TargetIndex.A;
        private const TargetIndex CartInd = TargetIndex.B;
        private const TargetIndex StorageInd = TargetIndex.C;

        private LocalTargetInfo Cart => job.targetB.Thing;
        private List<LocalTargetInfo> Items => job.targetQueueA;

        public Thing Storage => (Thing)job.GetTarget(StorageInd);
        public Thing ThingToCarry => (Thing)job.GetTarget(ItemInd);

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(Cart, job, 1, -1, null, errorOnFailed))
            {
                if (errorOnFailed) JobFailReason.Is("CouldNotReserve".Translate());
                return false;
            }
           
            for (int i = 0; i < Items.Count; i++)
            {
                pawn.Reserve(Items[i], job, 1, job.countQueue[i]);
            }

            return true;
        }

       
        protected override IEnumerable<Toil> MakeNewToils()
        {
            var tryGetOpportunityLabel = Toils_General.Label();
            var startUnloadCartLabel = Toils_General.Label();
            var returnCartLabel = Toils_General.Label();

            this.FailOnDestroyedOrNull(CartInd);
            this.FailOnForbidden(CartInd);

            yield return Toils_Cart.GotoThing(CartInd, PathEndMode.Touch).FailOnSomeonePhysicallyInteracting(CartInd);
            yield return Toils_Haul.StartCarryThing(CartInd);

            Toil tryGetNextItem = Toils_Jump.JumpIf(startUnloadCartLabel, () => !Toils_Cart.TryGetNextItemFromQueue(ItemInd, CartInd, job, pawn));
            yield return tryGetNextItem;
            yield return Toils_Cart.GotoThing(ItemInd, PathEndMode.Touch, tryGetNextItem);
            yield return Toils_Cart.DepositThingInCart(ItemInd);
            yield return Toils_Jump.Jump(tryGetNextItem);

            yield return startUnloadCartLabel;
            Toil tryGetNextStorage = Toils_Jump.JumpIf(returnCartLabel, () => !Toils_Cart.TryGetNextStorageForItem(ItemInd, StorageInd, CartInd, job, pawn));
            yield return tryGetNextStorage;
            yield return Toils_Goto.GotoThing(StorageInd, PathEndMode.Touch);
            yield return Toils_Cart.WaitWithEffect(StorageInd, ItemInd);
            yield return Toils_Cart.PlaceThingFromCartInStorage(StorageInd, ItemInd);
            yield return Toils_Jump.Jump(tryGetNextStorage);

            yield return returnCartLabel;
            yield return Toils_General.DoAtomic(() =>
            {
                var cart = job.GetTarget(CartInd).Thing as Building_Cart;
                job.SetTarget(StorageInd, cart.getLastPosition());
            });
            yield return Toils_Goto.GotoCell(StorageInd, PathEndMode.Touch);
            yield return Toils_Cart.DropCart(StorageInd);
        }
    }
}
