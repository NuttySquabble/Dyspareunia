using HarmonyLib;
using RimWorld;
using System.Reflection;
using Verse;
using Verse.AI;

namespace Dyspareunia
{
    [StaticConstructorOnStartup]
    public static class Dyspareunia
    {
        static Dyspareunia()
        {
            Log.Message("Dyspareunia is starting.");
            Harmony harmony = new Harmony("nuttysquabble.dyspareunia");
            Log.Message("Harmony instance id is " + (harmony?.Id ?? "NULL"));
            Harmony.DEBUG = true;
            harmony.PatchAll();
            Log.Message("Dyspareunia initialization is complete.");
            Log.Message("Patched methods: " + harmony.GetPatchedMethods().EnumerableCount());
        }
    }
}
