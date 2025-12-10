using HarmonyLib;
using Verse;

namespace Carts
{
    [StaticConstructorOnStartup]
    public static class CartsMod
    {
        static CartsMod()
        {
            var harmony = new Harmony("Exstarzii.Carts");
            harmony.PatchAll();
        }
    }
}
