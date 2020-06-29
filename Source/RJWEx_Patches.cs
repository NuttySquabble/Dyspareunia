using HarmonyLib;
using RimWorld;
using System;
using Verse;
using rjw;

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
    }
}
