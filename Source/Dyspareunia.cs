using HarmonyLib;
using RimWorld;
using rjw;
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

            ListMethods(typeof(Dyspareunia));
            ListMethods(typeof(JobDriver_Sex));
            ListMethods(typeof(JobDriver_Lovin));
            ListMethods(typeof(JobDriver_Mate));

            HarmonyMethod postfix = new HarmonyMethod(typeof(Dyspareunia).GetMethod("MakeNewToils_Postfix"));
            if (postfix is null)
            {
                Log("MakeNewToils_Postfix is NULL!");
                return;
            }

            AddPostfixPatch(typeof(JobDriver_Lovin), postfix);
            AddPostfixPatch(typeof(JobDriver_Mate), postfix);
            AddPostfixPatch(typeof(JobDriver_Sex), postfix);
            AddPostfixPatch(typeof(JobDriver_JoinInBed), postfix);
            AddPostfixPatch(typeof(JobDriver_Rape), postfix);
            AddPostfixPatch(typeof(JobDriver_Breeding), postfix);

            Log("Dyspareunia initialization is complete.");
        }

        internal static void Log(string message) => Verse.Log.Message("[Dyspareunia] " + message);

        public static void ListMethods(System.Type type)
        {
            Log("Methods of " + type.Name + ":");
            foreach (string s in AccessTools.GetMethodNames(type))
                Log("- " + s);
        }

        static void AddPostfixPatch(System.Type jobDriverType, HarmonyMethod postfix)
        {
            MethodBase methodBase = AccessTools.Method(jobDriverType, "MakeNewToils");
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
                Dyspareunia.Log("Pawn is NULL.");
                return;
            }
            Dyspareunia.Log("Name: " + p.Name);
            Dyspareunia.Log("Gender: " + p.gender);
            Dyspareunia.Log("Sex: " + GenderHelper.GetSex(p));
            Dyspareunia.Log("Body size: " + p.BodySize);
            Hediff gen;
            if (Genital_Helper.has_penis(p) || Genital_Helper.has_multipenis(p))
            {
                gen = Genital_Helper.get_penis_all(p);
                Dyspareunia.Log("Penis: " + gen.def + " (" + gen.Severity + ") size");
                Dyspareunia.Log("Overall size: " + (p.BodySize * gen.Severity));
            }
            if (Genital_Helper.has_vagina(p))
            {
                gen = Genital_Helper.get_vagina(p);
                Dyspareunia.Log("Vagina: " + gen.def + " (" + gen.Severity + ") size");
                Dyspareunia.Log("Overall size: " + (p.BodySize * gen.Severity));
            }
            if (rjw.Genital_Helper.has_anus(p))
            {
                BodyPartRecord anus = rjw.Genital_Helper.get_anus(p);
                gen = p.health.hediffSet.hediffs.Find((Hediff hed) => hed.Part == anus && (hed is rjw.Hediff_PartBaseNatural || hed is rjw.Hediff_PartBaseArtifical));
                if (gen == null)
                    Dyspareunia.Log("Anus not found :/");
                else
                {
                    Dyspareunia.Log("Anus: " + gen.def + " (" + gen.Severity + ") size");
                    Dyspareunia.Log("Overall size: " + (p.BodySize * gen.Severity));
                }
            }
        }

        public static void MakeNewToils_Postfix(JobDriver __instance)
        {
            Dyspareunia.Log("Dyspareunia.MakeNewToils_Postfix for " + __instance.GetType().Name);
            Pawn partner = null;
            if (__instance is JobDriver_Sex)
                partner = ((JobDriver_Sex)__instance).Partner;
            else 
                partner = __instance.job.GetTarget(TargetIndex.A).Pawn;
            Log("* Initiator *");
            LogPawnData(__instance.pawn);
            if (partner != null)
            {
                Log("* Partner *");
                LogPawnData(partner);
            }
            else Log("Could not find partner.");
        }
    }
}
