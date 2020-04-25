using HarmonyLib;
using RimWorld;
using rjw;
using System;
using System.Reflection;
using Verse;
using Verse.AI;

namespace Dyspareunia
{
    [StaticConstructorOnStartup]
    public static class Dyspareunia
    {
        static Harmony harmony;

        static Dyspareunia()
        {
            Log("Dyspareunia is starting.");
            harmony = new Harmony("nuttysquabble.dyspareunia");
            Log("Harmony instance id is " + (harmony?.Id ?? "NULL"));
            Harmony.DEBUG = true;

            HarmonyMethod postfix = new HarmonyMethod(typeof(Dyspareunia).GetMethod("JobDriver_Postfix"));
            if (postfix is null)
            {
                Log("Postfix is NULL!");
                return;
            }

            AddPostfixPatch(typeof(JobDriver), postfix);
            harmony.Patch(typeof(SexUtility).GetMethod("ProcessSex"), postfix: new HarmonyMethod(typeof(Dyspareunia).GetMethod("SexUtility_Postfix")));

            //AddPostfixPatch(typeof(JobDriver_Lovin), postfix);
            //AddPostfixPatch(typeof(JobDriver_Mate), postfix);
            //AddPostfixPatch(typeof(JobDriver_Sex), postfix);
            //AddPostfixPatch(typeof(JobDriver_JoinInBed), postfix);
            //AddPostfixPatch(typeof(JobDriver_Rape), postfix);
            //AddPostfixPatch(typeof(JobDriver_Breeding), postfix);

            Log("Dyspareunia initialization is complete. " + harmony.GetPatchedMethods().EnumerableCount() + " patches applied.");
        }

        internal static void Log(string message) => Verse.Log.Message("[Dyspareunia] " + DateTime.UtcNow + ": " + message);

        public static void ListMethods(System.Type type)
        {
            Log("Methods of " + type.Name + ":");
            foreach (string s in AccessTools.GetMethodNames(type))
                Log("- " + s);
        }

        static void AddPostfixPatch(System.Type jobDriverType, HarmonyMethod postfix)
        {
            MethodBase methodBase = AccessTools.Method(jobDriverType, "Notify_Starting");
            if (methodBase is null)
                Log("MethodBase for " + jobDriverType.Name + " is NULL!");
            else
            {
                Log("MethodBase: " + jobDriverType.Name + "." + methodBase.Name);
                harmony.Patch(methodBase, postfix: postfix);
            }
        }

        public static void LogPawnData(Pawn p)
        {
            if (p is null)
            {
                Log("Pawn is NULL.");
                return;
            }
            Log("Name: " + p.Name);
            Log("Gender: " + p.gender);
            Log("Sex: " + GenderHelper.GetSex(p));
            Log("Body size: " + p.BodySize);
            Hediff gen;
            if (Genital_Helper.has_penis(p) || Genital_Helper.has_multipenis(p))
            {
                gen = Genital_Helper.get_penis_all(p);
                Log("Penis: " + gen.def + " (" + gen.Severity + ") size");
                Log("Overall size: " + (p.BodySize * gen.Severity));
            }
            if (Genital_Helper.has_vagina(p))
            {
                gen = Genital_Helper.get_vagina(p);
                Log("Vagina: " + gen.def + " (" + gen.Severity + ") size");
                Log("Overall size: " + (p.BodySize * gen.Severity));
            }
            if (rjw.Genital_Helper.has_anus(p))
            {
                BodyPartRecord anus = rjw.Genital_Helper.get_anus(p);
                gen = p.health.hediffSet.hediffs.Find((Hediff hed) => hed.Part == anus && (hed is rjw.Hediff_PartBaseNatural || hed is rjw.Hediff_PartBaseArtifical));
                if (gen == null)
                    Log("Anus not found :/");
                else
                {
                    Log("Anus: " + gen.def + " (" + gen.Severity + ") size");
                    Log("Overall size: " + (p.BodySize * gen.Severity));
                }
            }
        }

        public static void JobDriver_Postfix(JobDriver __instance)
        {
            if (!(__instance is JobDriver_Sex || __instance is JobDriver_Lovin || __instance is JobDriver_Mate))
                return;
            Log("Dyspareunia.Postfix for " + __instance.GetType().Name);
            Pawn partner = null;
            if (__instance is JobDriver_Sex)
                partner = ((JobDriver_Sex)__instance).Partner;
            else
                partner = __instance.job.GetTarget(TargetIndex.A).Pawn;
            if (partner == null)
            {
                Log("No partner found for this JobDriver. Dyspareunia doesn't apply to solo jobs.");
                return;
            }
            Log("* Initiator *");
            LogPawnData(__instance.pawn);
            Log("* Partner *");
            LogPawnData(partner);
        }

        public static void SexUtility_Postfix(Pawn pawn, Pawn partner, xxx.rjwSextype sextype)
        {
            Log("SexUtility_Postfix");
            Log("Sex type: " + sextype);
            Log("* Initiator *");
            LogPawnData(pawn);
            if (partner == null)
            {
                Log("No partner found for this JobDriver. Dyspareunia doesn't apply to solo jobs.");
                return;
            }
            Log("* Partner *");
            LogPawnData(partner);
        }
    }
}
