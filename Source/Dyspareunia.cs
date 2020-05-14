using HarmonyLib;
using RimWorld;
using rjw;
using System;
using System.Collections.Generic;
using Verse;
using HugsLib;
using HugsLib.Settings;

namespace Dyspareunia
{
    //[StaticConstructorOnStartup]
    public class Dyspareunia : ModBase
    {
        static Harmony harmony;

        Dyspareunia()
        {
            harmony = new Harmony("nuttysquabble.dyspareunia");

            harmony.Patch(typeof(SexUtility).GetMethod("Aftersex", new Type[] { typeof(Pawn), typeof(Pawn), typeof(bool), typeof(bool), typeof(bool), typeof(xxx.rjwSextype) }), prefix: new HarmonyMethod(typeof(Dyspareunia).GetMethod("SexUtility_Prefix")));
            harmony.Patch(typeof(Hediff_PartBaseNatural).GetMethod("Tick"), postfix: new HarmonyMethod(typeof(Dyspareunia).GetMethod("PartBase_Tick_Postfix")));
            harmony.Patch(typeof(Hediff_PartBaseArtifical).GetMethod("Tick"), postfix: new HarmonyMethod(typeof(Dyspareunia).GetMethod("PartBase_Tick_Postfix")));
            harmony.Patch(typeof(Hediff_BasePregnancy).GetMethod("PostBirth"), postfix: new HarmonyMethod(typeof(Dyspareunia).GetMethod("Hediff_BasePregnancy_Patch")));

            Log("Dyspareunia initialization is complete. " + harmony.GetPatchedMethods().EnumerableCount() + " patches applied.");
        }

        internal static void Log(string message, bool important = false)
        {
            if (important || (DebugLogging is null) || DebugLogging)
                Verse.Log.Message("[Dyspareunia] " + DateTime.UtcNow + ": " + message);
        }

        // Settings
        internal static SettingHandle<int> DamageFactor;
        internal static SettingHandle<int> StretchFactor;
        internal static SettingHandle<int> ContractionTime;
        internal static SettingHandle<bool> DebugLogging;

        public override void DefsLoaded()
        {
            DamageFactor = Settings.GetHandle<int>("DamageFactor", "Damage Factor", "Percentage of damage taken from rubbing and stretch, compared to default values", 100);
            StretchFactor = Settings.GetHandle<int>("StretchFactor", "Stretch Factor", "Percentage of organ stretch from sex and childbirth", 100);
            ContractionTime = Settings.GetHandle<int>("ContractionTime", "Contraction Time", "How many days it takes for organs to naturally contract from maximum looseness to normal state", 30);
            DebugLogging = Settings.GetHandle<bool>("DebugLogging", "Debug Logging", "Enable verbose logging, use to report bugs");
        }

        public static bool HasPenetratingOrgan(Pawn pawn) => (Genital_Helper.has_penis(pawn) || Genital_Helper.has_penis_infertile(pawn) || Genital_Helper.has_ovipositorM(pawn)) && !Genital_Helper.penis_blocked(pawn);

        public static Hediff GetVagina(Pawn pawn) => pawn.health.hediffSet.hediffs.Find((Hediff hed) => (hed is Hediff_PartBaseNatural || hed is Hediff_PartBaseArtifical) && hed.def.defName.ToLower().Contains("vagina"));

        public static Hediff GetAnus(Pawn pawn) => pawn.health.hediffSet.hediffs.Find((Hediff hed) => hed.Part == Genital_Helper.get_anus(pawn) && (hed is Hediff_PartBaseNatural || hed is Hediff_PartBaseArtifical));

        public static bool IsOrifice(Hediff hediff) => (hediff is Hediff_PartBaseNatural || hediff is Hediff_PartBaseArtifical) && (hediff.def.defName.ToLower().Contains("vagina") || hediff.def.defName.ToLower().Contains("anus"));

        public static void LogPawnData(Pawn p)
        {
            if (p is null)
            {
                Log("Pawn is NULL.", true);
                return;
            }
            Log("Pawn: " + p.Label);
            Log("Body size: " + p.BodySize);
        }

        /// <summary>
        /// Harmony postfix method for SexUtility.ProcessSex. It calculates and applies damage and other effects
        /// </summary>
        /// <param name="pawn">Pawn 1 (rapist, whore etc.)</param>
        /// <param name="partner">Pawn 2 (victim, client etc.)</param>
        /// <param name="rape">True if it's a non-consensual sex</param>
        /// <param name="sextype">Sex type (only Vaginal, Anal and Double Penetration are supported ATM)</param>
        public static void SexUtility_Prefix(Pawn pawn, Pawn partner, bool rape, xxx.rjwSextype sextype)
        {
            Log("* Initiator *");
            LogPawnData(pawn);
            Log("* Partner *");
            LogPawnData(partner);

            PenetrationUtility.ProcessPenetrations(pawn, partner, rape, sextype);
        }

        /// <summary>
        /// Harmony postfix method for Hediff_PartBaseNatural.Tick and Hediff_PartBaseNatural.Tick. Applies organ contraction
        /// </summary>
        /// <param name="__instance"></param>
        public static void PartBase_Tick_Postfix(HediffWithComps __instance)
        {
            // Only runs once per 14 hours 40 minutes (to contract by 50% in 30 days)
            if (__instance.ageTicks % 36000 != 0)
                return;

            // Skip unspawned pawns
            if (!__instance.pawn.Spawned)
                return;

            // Only works for orifices
            if (!IsOrifice(__instance))
                return;

            // Only contracts organs more than 0.5 in size
            if (__instance.Severity <= 0.5)
                return;

            // Contract the part by 1%
            __instance.Heal(0.3f / ContractionTime);
        }

        static int lastBirthTick;
        static List<Pawn> gaveBirthThisTick = new List<Pawn>();

        public override string ModIdentifier => "Dyspareunia";

        /// <summary>
        /// Harmony patch for childbirth damage
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="baby"></param>
        public static void Hediff_BasePregnancy_Patch(Pawn mother, Pawn baby)
        {
            if (mother?.health?.hediffSet is null)
            {
                Log("No hediffSet found for the mother!", true);
                if (mother != null) Log("Mother: " + mother.Label, true);
                return;
            }

            Log("Hediff_BasePregnancy_Patch for " + mother?.Label);

            // Checking if this mother has already given birth in current tick (damage applies only once)
            if ((lastBirthTick = Find.TickManager.TicksGame) != lastBirthTick)
                gaveBirthThisTick.Clear();
            else if (gaveBirthThisTick.Contains(mother))
            {
                Log("This mother has already given birth this tick. No more damage is applied.");
                return;
            }

            // Remember this mother, so as not to apply damage again if she has several babies
            gaveBirthThisTick.Add(mother);

            Hediff vagina = GetVagina(mother);
            if (vagina?.Part is null)
            {
                Log("No vagina found for " + mother.Label + "!", true);
                return;
            }

            Log("Vagina original size: " + vagina.Severity + "; effective size: " + PenetrationUtility.GetOrganSize(vagina) + "; HP: " + vagina.Part.def.hitPoints);

            double babySize;
            if (baby is null)
            {
                Log("Baby not found for " + mother.Label + "! Assuming it is 25% the size of the mother.", true);
                babySize = mother.BodySize * 0.25;
            }
            else babySize = baby.BodySize;
            Log("Baby size: " + babySize);

            double damage = (babySize / PenetrationUtility.GetOrganSize(vagina) * 30 - 1) * vagina.Part.def.hitPoints / 12 * Rand.Range(0.75f, 1.25f);
            Log("Childbirth damage: " + damage + " HP");
            if (damage > 0)
            {
                PenetrationUtility.StretchOrgan(vagina, damage);
                damage *= Math.Max(1 - PenetrationUtility.GetWetness(vagina) * 0.5, 0.4);
                PenetrationUtility.AddHediff("SexStretch", damage, vagina, null);
            }
        }
    }
}
