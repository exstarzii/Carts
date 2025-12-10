using System.Collections.Generic;
using Verse;

namespace Carts
{
    public class VirtualCart
    {
        private Dictionary<ThingDef, int> innerContainer;
        private int remainSpace;
        public bool IsLoaded => remainSpace <= 0;
        public VirtualCart(int maxCapacity, ThingOwner<Thing> cartContainer)
        {
            innerContainer = new Dictionary<ThingDef, int>();
            remainSpace = maxCapacity;
            foreach (var item in cartContainer)
            {
                Add(item);
            }
        }

        public bool Add(Thing item)
        {
            return Add(item.def, item.stackCount);
        }

        public bool Add(ThingDef def, int count)
        {
            if (remainSpace < 0)
                return false;

            int itemsWas = innerContainer.TryGetValue(def, out int existing) ? existing : 0;
            int itemsWill = itemsWas + count;

            int stackLimit = def.stackLimit;

            int stacksBefore = (itemsWas + stackLimit - 1) / stackLimit;
            int stacksAfter = (itemsWill + stackLimit - 1) / stackLimit;

            int stacksDiff = stacksAfter - stacksBefore;
            if (stacksDiff > remainSpace)
                return false;

            innerContainer[def] = itemsWill;
            remainSpace -= stacksDiff;

            return true;
        }

        public int CanAddCount(ThingDef def)
        {
            if (remainSpace < 0)
                return 0;

            int itemsWas = innerContainer.TryGetValue(def, out int existing) ? existing : 0;
            int stacksBefore = (itemsWas + def.stackLimit - 1) / def.stackLimit;
            int maxStacksAfter = stacksBefore + remainSpace;
            int maxItemsAfter = maxStacksAfter * def.stackLimit;
            int canAdd = maxItemsAfter - itemsWas;

            return canAdd;
        }
    }
}
