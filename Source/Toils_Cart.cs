using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Carts
{
    public static class Toils_Cart
    {
        public static Toil DepositThingInCart(TargetIndex targetItemOrContainerInd, bool onlyReservedCount=false)
        {
            Toil toil = ToilMaker.MakeToil("DepositThingInCart");
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                Thing haulableThing = curJob.GetTarget(targetItemOrContainerInd).Thing;
                Building_Cart cart = actor.carryTracker.CarriedThing as Building_Cart;
                if (cart == null)
                {
                    Log.Error(actor?.ToString() + " tried to place hauled thing in cart but cart is not exist.");
                    return;
                }
                if (haulableThing is Pawn otherPawn)
                {
                    TryDepositAllFromPawnInCart(actor, otherPawn, cart);
                    return;
                }

                DepositThingInCartFromCell(curJob.GetTarget(targetItemOrContainerInd), actor, cart, onlyReservedCount);
            };
            return toil;
        }

        private static void DepositThingInCartFromCell(LocalTargetInfo haulableThingInfo,Pawn actor, Building_Cart cart, bool onlyReservedCount)
        {
            var haulableThing = haulableThingInfo.Thing;
            int count = Math.Min(haulableThing.stackCount, cart.GetCountCanAccept(haulableThing));
            if (onlyReservedCount)
            {
                count = Math.Min(count, CartUtility.GetPawnReservedCount(actor, haulableThingInfo));
            }
            if (cart.innerContainer != null && count > 0)
            {
                Thing thing = haulableThing.SplitOff(count);
                int num2 = cart.innerContainer.TryAdd(thing, count);
                if (num2 != 0)
                {
                    foreach (ThingComp allComp in cart.AllComps)
                    {
                        if (allComp is INotifyHauledTo notifyHauledTo2)
                        {
                            notifyHauledTo2.Notify_HauledTo(actor, haulableThing, num2);
                        }
                    }
                }
            }
            else
            {
                Log.Error($"Could not deposit hauled thing in container: {cart} innerContainer {cart.innerContainer} count {count}");
            }
        }

        private static void TryDepositAllFromPawnInCart(Pawn actor, Pawn otherPawn, Building_Cart cart)
        {
            var index = otherPawn.inventory.innerContainer.Count - 1;
            while (index >= 0)
            {
                var itemInPawn = otherPawn.inventory.innerContainer[index];
                index--;
                int count = Math.Min(itemInPawn.stackCount, cart.GetCountCanAccept(itemInPawn));
                if (count <= 0)
                {
                    continue;
                }
                int num2 = otherPawn.inventory.innerContainer.TryTransferToContainer(itemInPawn, cart.innerContainer, count);
                if (num2 != 0)
                {
                    foreach (ThingComp allComp in cart.AllComps)
                    {
                        if (allComp is INotifyHauledTo notifyHauledTo2)
                        {
                            notifyHauledTo2.Notify_HauledTo(actor, itemInPawn, num2);
                        }
                    }
                }
            }
            if (otherPawn.RaceProps.packAnimal && otherPawn.inventory.innerContainer.Count == 0)
            {
                otherPawn.Drawer.renderer.SetAllGraphicsDirty();
            }
        }

        public static Toil PlaceThingFromCartInStorage(TargetIndex storageInd, TargetIndex thingInd)
        {
            Toil toil = ToilMaker.MakeToil("PlaceThingFromCartInStorage");
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                Thing cart = actor.carryTracker.CarriedThing;
                Thing thingToBePlaced = curJob.GetTarget(thingInd).Thing;
                var storage = curJob.GetTarget(storageInd);
                if (cart == null)
                {
                    Log.Error(actor?.ToString() + " tried to place thing from hauled Cart but is not hauling anything.");
                    return;
                }
                ThingOwner cartContainer = cart.TryGetInnerInteractableThingOwner();
                if (cartContainer == null)
                {
                    Log.Error(actor?.ToString() + " tried to place thing from hauled Cart but Cart has not ThingOwner.");
                    return;
                }

                if (storage.Thing == null)
                {
                    PlaceThingFromCartInCell(actor, cartContainer, thingToBePlaced, storage.Cell);
                    return;
                }
                ThingOwner storageOwner = storage.Thing.TryGetInnerInteractableThingOwner();  
                if (storageOwner == null)
                {
                    Log.Error($"{actor?.ToString()} tried to place thing from hauled Cart in Storage {storage} but It has not ThingOwner.");
                    return;
                }

                if (storage.Thing is MapPortal portal)
                {
                    Func<Thing, int> countToLoad = (Thing thingToCheck) => CartUtility.ThingCountToLoadToContainer(thingToCheck, portal.leftToLoad);
                    TryDepositAllFromCartInContainer(actor, cartContainer, storageOwner, storage.Thing, countToLoad);
                    curJob.SetTarget(storageInd, LocalTargetInfo.Invalid);
                }
                else if (storage.Thing.TryGetComp<CompTransporter>() != null)
                {
                    var transporter = storage.Thing.TryGetComp<CompTransporter>();
                    Func<Thing, int> countToLoad = (Thing thingToCheck) => CartUtility.ThingCountToLoadToContainer(thingToCheck, transporter.leftToLoad);
                    TryDepositAllFromCartInContainer(actor, cartContainer, storageOwner, storage.Thing, countToLoad);
                    curJob.SetTarget(storageInd, LocalTargetInfo.Invalid);
                }
                else if (storage.Thing is IHaulEnroute haulEnroute)
                {
                    Func<Thing, int> countToLoad = (Thing thingToCheck) => haulEnroute.GetSpaceRemainingWithEnroute(thingToCheck.def, actor);
                    TryDepositAllFromCartInContainer(actor, cartContainer, storageOwner, storage.Thing, countToLoad);
                    curJob.SetTarget(storageInd, LocalTargetInfo.Invalid);
                }
                else
                {
                    PlaceThingFromCartInContainer(actor, cartContainer, storageOwner, thingToBePlaced, storage.Thing);
                }
            };
            return toil;
        }

        private static void PlaceThingFromCartInCell(Pawn actor, ThingOwner cartContainer, Thing thingToBePlaced, IntVec3 cell)
        {
            SlotGroup slotGroup = actor.Map.haulDestinationManager.SlotGroupAt(cell);
            if (slotGroup != null && slotGroup.Settings.AllowedToAccept(thingToBePlaced))
            {
                actor.Map.designationManager.TryRemoveDesignationOn(thingToBePlaced, DesignationDefOf.Haul);
            }

            if (cartContainer.TryDrop(thingToBePlaced, cell, actor.MapHeld, ThingPlaceMode.Direct, out var resultingThing, null))
            {
                if (resultingThing != null && actor.Faction.HostileTo(Faction.OfPlayer))
                {
                    resultingThing.SetForbidden(value: true, warnOnFail: false);
                }

                actor.MapHeld.resourceCounter.UpdateResourceCounts();
            }
        }

        private static void PlaceThingFromCartInContainer(Pawn actor, ThingOwner cartContainer, ThingOwner storageOwner, Thing thingToBePlaced, Thing storage)
        {
            int num2 = cartContainer.TryTransferToContainer(thingToBePlaced, storageOwner, thingToBePlaced.stackCount);
            if (num2 != 0)
            {
                NotifyHauledTo(actor, thingToBePlaced, storage, num2);

                actor.MapHeld.resourceCounter.UpdateResourceCounts();
            }
        }
        private static void TryDepositAllFromCartInContainer(Pawn actor, ThingOwner cartContainer, ThingOwner containerOwner, Thing container, Func<Thing, int> getCountToLoad)
        {
            var index = cartContainer.Count - 1;
            while (index >= 0)
            {
                var itemInCart = cartContainer[index];
                index--;
                int num = itemInCart.stackCount;
                num = Mathf.Min(getCountToLoad(itemInCart), num);
                if (num == 0)
                {
                    continue;
                }
                int num2 = cartContainer.TryTransferToContainer(itemInCart, containerOwner, num);
                if (num2 != 0)
                {
                    NotifyHauledTo(actor, itemInCart, container, num2);
                }
            }
        }

        private static void NotifyHauledTo (Pawn actor, Thing thingToBePlaced, Thing container, int num2)
        {
            if (container is IHaulEnroute haulEnroute2)
            {
                container.Map.enrouteManager.ReleaseFor(haulEnroute2, actor);
            }
            if (container is INotifyHauledTo notifyHauledTo)
            {
                notifyHauledTo.Notify_HauledTo(actor, thingToBePlaced, num2);
            }
            if (container is ThingWithComps thingWithComps)
            {
                foreach (ThingComp allComp in thingWithComps.AllComps)
                {
                    if (allComp is INotifyHauledTo notifyHauledTo2)
                    {
                        notifyHauledTo2.Notify_HauledTo(actor, thingToBePlaced, num2);
                    }
                }
            }
        }

        public static Toil GotoThing(TargetIndex ind, PathEndMode peMode, Toil jumpOnIncompletable=null)
        {
            Toil toil = ToilMaker.MakeToil("GotoThing");
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                LocalTargetInfo dest = actor.jobs.curJob.GetTarget(ind);
                actor.pather.StartPath(dest, peMode);
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            toil.JumpOnDestroyedNullOrForbidden(ind, jumpOnIncompletable);
            return toil;
        }

        public static Toil JumpOnDestroyedNullOrForbidden(this Toil toil, TargetIndex ind, Toil jumpOnIncompletable)
        {
            if (jumpOnIncompletable == null)
            {
                toil.FailOnDestroyedNullOrForbidden(ind);
            }
            else
            {
                toil.AddEndCondition(() =>
                {
                    Pawn actor = toil.actor;
                    LocalTargetInfo dest = actor.jobs.curJob.GetTarget(ind);
                    if (dest.Thing.DestroyedOrNull() || dest.Thing.IsForbidden(actor))
                    {
                        toil.actor.jobs.curDriver.JumpToToil(jumpOnIncompletable);
                    }
                    return JobCondition.Ongoing;
                });
            }
            return toil;
        }

        public static Toil JumpOn(this Toil toil, Func<bool> condition, Toil jumpTarget)
        {
            toil.AddEndCondition(() =>
            {
                if (condition())
                {
                    toil.actor.jobs.curDriver.JumpToToil(jumpTarget);
                }
                return JobCondition.Ongoing;
            });
            return toil;
        }

        public static Toil GotoBuilding(TargetIndex ind, PathEndMode peMode, Toil jumpOnIncompletable)
        {
            IntVec3 buildingCell = IntVec3.Invalid;

            Toil toil = ToilMaker.MakeToil("GotoThing");
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                LocalTargetInfo dest = actor.jobs.curJob.GetTarget(ind);
                buildingCell = dest.Thing.Position;
                actor.pather.StartPath(dest, peMode);
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            toil.JumpOn(() =>
            {
                var building = toil.actor.jobs.curJob.GetTarget(ind);
                if (building.Thing.DestroyedOrNull())
                {
                    if (!buildingCell.IsValid)
                    {
                        return true;
                    }

                    var newbuilding = CartUtility.GetBuildingAtCell(toil.actor.Map, buildingCell);
                    if (newbuilding == null)
                        return true;

                    toil.actor.jobs.curJob.SetTarget(ind, newbuilding);
                }
                else if (building.Thing.IsForbidden(toil.actor))
                {
                    return true;
                }
                return false;
            }, jumpOnIncompletable);

            return toil;
        }

        public static bool TryGetNextItemFromQueue(TargetIndex ItemsInd, TargetIndex CartInd, Job job, Pawn actor)
        {
            var items = job.GetTargetQueue(ItemsInd);
            var cart = job.GetTarget(CartInd).Thing as Building_Cart;

            if (cart.innerContainer.Count >= (cart.def as ThingDef_Cart).cartMaxItems)
            {
                return false;
            }

            if (items != null && items.Count != 0)
            {
                int count = 0;
                while(count < items.Count)
                {
                    if (Validator(items[count].Thing))
                    {
                        job.SetTarget(ItemsInd, items[count]);
                        items.Remove(items[count]);
                        return true;
                    }
                    count++;
                }

                bool Validator(Thing item)
                {
                    if (!item.Spawned ||
                        item.stackCount <= 0 ||
                        item.IsForbidden(actor))
                    {
                        return false;
                    }

                    return true;
                }
            }

            return false;
        }

        public static bool TryGetNextContainerFromQueue(TargetIndex ContainersInd, TargetIndex TargetInd, TargetIndex CartInd, Job job, Pawn actor)
        {
            var containers = job.GetTargetQueue(ContainersInd);
            var cart = job.GetTarget(CartInd).Thing as Building_Cart;

            if (cart.innerContainer.Count == 0 || containers == null || containers.Count == 0)
            {
                return false;
            }
            var target = CartUtility.ClosestThingReachable(actor.Position, actor.Map, containers, PathEndMode.Touch, TraverseParms.For(actor), 99999f, Validator);
            if (target.IsValid)
            {
                containers.Remove(target);
                job.SetTarget(TargetInd, target);
                return true;
            }

            bool Validator(LocalTargetInfo item)
            {
                if (item == null ||
                    !item.HasThing ||
                    !item.Thing.Spawned ||
                    item.Thing.IsForbidden(actor) ||
                    !actor.Map.reachability.CanReach(actor.Position, item, PathEndMode.Touch, TraverseParms.For(actor)))
                {
                    return false;
                }

                return true;
            }
            return false;
        }

        public static bool TryGetNextStorageForItem(TargetIndex ItemsInd, TargetIndex StorageInd, TargetIndex CartInd, Job job, Pawn pawn)
        {
            var cart = job.GetTarget(CartInd).Thing as Building_Cart;

            if (job.targetQueueB == null || job.targetQueueB.Count == 0)
            {
                RecalculateStorages(pawn, job, cart);
            }

            int index = 0;
            while (index < cart.innerContainer.Count && index < job.targetQueueB.Count)
            {
                var item = cart.innerContainer[index];
                var storage = job.targetQueueB[index];

                if (storage.IsValid)
                {
                    job.targetQueueB.Remove(storage);
                    job.SetTarget(StorageInd, storage);
                    job.SetTarget(ItemsInd, item);
                    return true;
                }
                index++;
            }
            return false;
        }

        private static void RecalculateStorages(Pawn pawn,Job job, Building_Cart cart)
        {
            job.targetQueueB = new List<LocalTargetInfo>();
            StoragePriority currentPriority = cart.GetStoreSettings().Priority;
            foreach (var item in cart.innerContainer)
            {
                if (StoreUtility.TryFindBestBetterStorageFor(item, pawn, pawn.Map, currentPriority, pawn.Faction, out var foundCell, out var haulDestination))
                {
                    if (haulDestination is ISlotGroupParent)
                    {
                        pawn.Reserve(foundCell, job);
                        job.targetQueueB.Add(foundCell);
                    }
                    else if (haulDestination is Thing thing && thing.TryGetInnerInteractableThingOwner() != null)
                    {
                        pawn.Reserve(thing, job);
                        job.targetQueueB.Add(thing);
                    }
                }
                else
                {
                    job.targetQueueB.Add(IntVec3.Invalid);
                }
            }
        }

        public static Toil DropCart(TargetIndex CellInd)
        {
            
            Toil toil = ToilMaker.MakeToil("DropCart");
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Thing cart = actor.carryTracker.CarriedThing;
                IntVec3 position = actor.jobs.curJob.GetTarget(CellInd).Cell;
                Thing resultingThing;
                if (cart != null && !actor.carryTracker.TryDropCarriedThing(position, ThingPlaceMode.Direct, out resultingThing))
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                }
            };
            return toil;
        }

        public static Toil WaitWithEffect(TargetIndex StorageInd, TargetIndex ItemInd)
        {
            Effecter graveDigEffect = null;
            Toil toil = Toils_General.Wait(0, StorageInd);
            toil.initAction = delegate
            {
                toil.actor.pather.StopDead();
                int duration = 0;
                Thing Storage = toil.actor.jobs.curJob.GetTarget(StorageInd).Thing;
                Thing ThingToCarry = toil.actor.jobs.curJob.GetTarget(ItemInd).Thing;
                if (ThingToCarry != null && Storage != null && Storage is Building building)
                {
                    duration = building.HaulToContainerDuration(ThingToCarry);
                    if (building is MapPortal)
                    {
                        duration = 90;
                    }
                }
                toil.actor.jobs.curDriver.ticksLeftThisToil = duration;
                toil.defaultDuration = duration;
                if (duration != 0)
                {
                    toil.WithProgressBarToilDelay(StorageInd);
                }
            };
            toil.tickIntervalAction = delegate (int delta)
            {
                Thing destThing = toil.actor.jobs.curJob.GetTarget(StorageInd).Thing;
                if (toil.actor.IsHashIntervalTick(80, delta) && destThing is Building_Grave && graveDigEffect == null)
                {
                    graveDigEffect = EffecterDefOf.BuryPawn.Spawn();
                    graveDigEffect.Trigger(destThing, destThing);
                }
            };
            toil.tickAction = delegate
            {
                Thing destThing = toil.actor.jobs.curJob.GetTarget(StorageInd).Thing;
                graveDigEffect?.EffectTick(destThing, destThing);
            };
            return toil;
        }

        public static JobDriver_HaulToContainerByCart FailJump(this JobDriver_HaulToContainerByCart jobDriver, Toil jumpTarget, Func<bool> condition)
        {
            jobDriver.AddEndCondition(() =>
            {
                if (jobDriver.jobStatus == HaulToContainerStatus.Ongoing && condition())
                {
                    jobDriver.job.targetQueueA = new List<LocalTargetInfo>();
                    jobDriver.job.targetQueueB = new List<LocalTargetInfo>();
                    jobDriver.jobStatus = HaulToContainerStatus.ReturnCart;
                    jobDriver.JumpToToil(jumpTarget);
                }
                return JobCondition.Ongoing;
            });
            return jobDriver;
        }
    }
}
