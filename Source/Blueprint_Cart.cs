using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Carts
{
    public class Blueprint_Cart : Blueprint_Build, IStoreSettingsParent
    {
        public StorageSettings settings;

        bool IStoreSettingsParent.StorageTabVisible => true;

        public StorageSettings GetParentStoreSettings()
        {
            return base.BuildDef.building.fixedStorageSettings;
        }

        public StorageSettings GetStoreSettings()
        {
            return settings;
        }

        void IStoreSettingsParent.Notify_SettingsChanged()
        {
        }

        public override void PostMake()
        {
            base.PostMake();
            settings = new StorageSettings(this);
            if (base.BuildDef.building.defaultStorageSettings != null)
            {
                settings.CopyFrom(base.BuildDef.building.defaultStorageSettings);
            }
        }

        protected override Thing MakeSolidThing(out bool shouldSelect)
        {
            Frame obj = (Frame)base.MakeSolidThing(out shouldSelect);
            obj.storageSettings = new StorageSettings();
            obj.storageSettings.CopyFrom(GetStoreSettings());
            return obj;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            foreach (Gizmo item in StorageSettingsClipboard.CopyPasteGizmosFor(GetStoreSettings()))
            {
                yield return item;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref settings, "settings", this);
        }
    }
}
