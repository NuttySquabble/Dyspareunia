using HarmonyLib;
using RimWorld;
using System;
using Verse;
using rjw;
using Verse.AI;

namespace Dyspareunia
{
    static class RJWEx_Patches
    {
        public static void anal_plug_soul_on_wear_Patch(bondage_gear_soul __instance, Pawn wearer, Apparel gear)
        {
            Dyspareunia.Log("RJWEx_Patches.anal_plug_soul_on_wear_Patch(..., '" + wearer + "', '" + gear.def.defName + "')");
            try
            {
                int size = (int)AccessTools.Field(gear.def.GetType(), "plug_size").GetValue(gear.def);
                Dyspareunia.Log("The plug is size " + size);
                PenetrationUtility.ApplyDamage(null, Dyspareunia.GetPlugSize(size, Dyspareunia.GetAnus(wearer)), Dyspareunia.GetAnus(wearer), false, false);
            }
            catch (Exception e)
            {
                Dyspareunia.Log(e.Message, true);
                return;
            }
        }

        public static void JobDriver_UseFM_stopSession(JobDriver __instance)
        {
            Dyspareunia.Log("RJWEx_Patches.JobDriver_UseFM_stopSession for " + __instance.pawn);
            Hediff orifice = Dyspareunia.GetOrifice(__instance.pawn);
            if (orifice != null)
                PenetrationUtility.ApplyDamage(null, 1.5, orifice, false);
        }
    }
}
