using RimWorld;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace Carts
{
    public class ThingDef_Cart : ThingDef
    {
        public int cartMaxItems;
        public GraphicData cartFullGraphicData;
    }

    public class Building_Cart : Building, IThingHolder, IStoreSettingsParent, IHaulDestination, IStorageGroupMember, INotifyHauledTo
    {
        public StorageGroup Group { get; set; }

        bool IStorageGroupMember.DrawConnectionOverlay => base.Spawned;

        Map IStorageGroupMember.Map => base.MapHeld;

        string IStorageGroupMember.StorageGroupTag => def.building.storageGroupTag;

        StorageSettings IStorageGroupMember.StoreSettings => GetStoreSettings();

        StorageSettings IStorageGroupMember.ParentStoreSettings => GetParentStoreSettings();

        StorageSettings IStorageGroupMember.ThingStoreSettings => storageSettings;

        bool IStorageGroupMember.DrawStorageTab => true;

        bool IStorageGroupMember.ShowRenameButton => base.Faction == Faction.OfPlayer;

        public ThingOwner<Thing> innerContainer;

        private StorageSettings storageSettings;

        private IntVec3 lastPosition;
        public bool StorageTabVisible => true;

        public bool HaulDestinationEnabled => true;

        public Building_Cart()
        {
            innerContainer = new ThingOwner<Thing>(this);
            innerContainer.OnContentsChanged += () =>
            {
                this.Notify_ColorChanged();
            };
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            lastPosition = Position;
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            if (mode != DestroyMode.Vanish)
            {
                innerContainer.TryDropAll(base.Position, base.Map, ThingPlaceMode.Near);
            }
            base.DeSpawn(mode);
        }

        public override void PostMake()
        {
            base.PostMake();
            storageSettings = new StorageSettings(this);
            if (def.building.defaultStorageSettings != null)
            {
                storageSettings.CopyFrom(def.building.defaultStorageSettings);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", new object[] { this });
            Scribe_Deep.Look(ref storageSettings, "storageSettings", this);
            Scribe_Values.Look(ref lastPosition, "lastPosition");
        }

        public ThingOwner GetDirectlyHeldThings() => innerContainer;

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, innerContainer);
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());
            stringBuilder.Append("Contents".Translate() + ": " + innerContainer.ContentsString.CapitalizeFirst());
            return stringBuilder.ToString();
        }

        public StorageSettings GetStoreSettings()
        {
            return storageSettings;
        }

        public StorageSettings GetParentStoreSettings()
        {
            return def.building.fixedStorageSettings;
        }

        public void Notify_SettingsChanged()
        {

        }

        public bool Accepts(Thing t)
        {
            return GetCountCanAccept(t) > 0;
        }

        public int GetCountCanAccept(Thing t)
        {
            if (innerContainer.Count > (def as ThingDef_Cart).cartMaxItems)
            {
                return 0;
            }
            if (innerContainer.Count == (def as ThingDef_Cart).cartMaxItems)
            {
                int sum = 0;
                foreach (var item in innerContainer)
                {
                    if (item != null && item.def == t.def)
                    {
                        sum += item.def.stackLimit - item.stackCount;
                    }
                }
                return sum;
            }

            return GetStoreSettings().AllowedToAccept(t) ? t.def.stackLimit : 0;
        }

        public IntVec3 getLastPosition()
        {
            return lastPosition;
        }

        public void Notify_HauledTo(Pawn hauler, Thing thing, int count)
        {
            int maxIndex = (def as ThingDef_Cart).cartMaxItems;
            if (innerContainer.Count > maxIndex)
            {
                IntVec3 position = this.Position;
                if (Map == null && ParentHolder is Pawn_CarryTracker tracker)
                {
                    position = tracker.pawn.Position;
                }

                for (int i = maxIndex; i < innerContainer.Count; i++)
                {
                    Thing thing1 = innerContainer[i];
                    innerContainer.TryDrop(thing1, position, MapHeld, ThingPlaceMode.Near, out var _);
                }
            }
        }

        public override Graphic Graphic
        {
            get
            {
                if (innerContainer.Count > 0)
                {
                    return (def as ThingDef_Cart).cartFullGraphicData.GraphicColoredFor(this);
                }
                else
                {
                    return base.Graphic;
                }
                
            }
        }
    }
}