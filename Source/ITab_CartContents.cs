using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Carts
{
    public class ITab_CartContents : ITab_ContentsBase
    {
        public override IList<Thing> container => Cart.innerContainer;

        public Building_Cart Cart => (Building_Cart)SelThing;

        public ITab_CartContents()
        {
            labelKey = "TabCasketContents";
            containedItemsKey = "ContainedItems";
        }

        public override bool IsVisible
        {
            get
            {
                if (SelThing!= null &&
                    SelThing is Building_Cart &&
                    SelThing.Faction == Faction.OfPlayer)
                {
                    return Cart.innerContainer.Any;
                }
                return false;
            }
        }

        protected override void OnDropThing(Thing t, int count)
        {
            IntVec3 position = base.SelThing.Position;
            if (base.SelThing.Map == null && base.SelThing.ParentHolder is Pawn_CarryTracker tracker)
            {
                position = tracker.pawn.Position;
            }
            GenDrop.TryDropSpawn(t.SplitOff(count), position + DropOffset, base.SelThing.MapHeld, ThingPlaceMode.Near, out var _);
        }
    }
}
