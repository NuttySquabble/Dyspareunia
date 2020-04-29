using HarmonyLib;
using RimWorld;
using rjw;
using System;
using System.Collections.Generic;
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
            Harmony.DEBUG = true;

            harmony.Patch(typeof(SexUtility).GetMethod("ProcessSex"), postfix: new HarmonyMethod(typeof(Dyspareunia).GetMethod("SexUtility_Postfix")));

            Log("Dyspareunia initialization is complete. " + harmony.GetPatchedMethods().EnumerableCount() + " patches applied.");
        }

        internal static void Log(string message) => Verse.Log.Message("[Dyspareunia] " + DateTime.UtcNow + ": " + message);

        public static bool HasPenetratingOrgan(Pawn pawn) => (Genital_Helper.has_penis(pawn) || Genital_Helper.has_penis_infertile(pawn) || Genital_Helper.has_ovipositorM(pawn)) && !Genital_Helper.penis_blocked(pawn);

        public static Hediff GetVagina(Pawn pawn) => pawn.health.hediffSet.hediffs.Find((Hediff hed) => (hed is Hediff_PartBaseNatural || hed is Hediff_PartBaseArtifical) && hed.def.defName.ToLower().Contains("vagina"));

        public static Hediff GetMouth(Pawn pawn) => pawn.health.hediffSet.hediffs.Find((Hediff hed) => hed.Part == Genital_Helper.get_mouth(pawn) && (hed is Hediff_PartBaseNatural || hed is Hediff_PartBaseArtifical));

        public static Hediff GetAnus(Pawn pawn) => pawn.health.hediffSet.hediffs.Find((Hediff hed) => hed.Part == Genital_Helper.get_anus(pawn) && (hed is Hediff_PartBaseNatural || hed is Hediff_PartBaseArtifical));

        public static void LogPawnData(Pawn p)
        {
            if (p is null)
            {
                Log("Pawn is NULL.");
                return;
            }
            Log("Pawn: " + p.Label);
            Log("Gender is " + p.gender + " and RJW sex is " + GenderHelper.GetSex(p));
            Log("Body size: " + p.BodySize);
            Hediff gen;
            if (HasPenetratingOrgan(p))
            {
                gen = Genital_Helper.get_penis_all(p);
                if (gen == null) Log("There is a penetrating organ, but no penis.");
                else
                {
                    Log("Penis: " + gen.def + " (" + gen.Severity + " size)");
                    Log("Overall size: " + PenetrationUtility.GetOrganSize(gen));
                }
            }
            if (Genital_Helper.has_vagina(p))
            {
                gen = GetVagina(p);
                if (gen is null) Log("Vagina is NULL.");
                else
                {
                    Log("Vagina: " + gen.def + " (" + gen.Severity + " size)");
                    Log("Overall size: " + PenetrationUtility.GetOrganSize(gen));
                }
            }
            if (rjw.Genital_Helper.has_anus(p))
            {
                gen = GetAnus(p);
                if (gen is null) Log("Anus not found :/");
                else
                {
                    Log("Anus: " + gen.def + " (" + gen.Severity + " size)");
                    Log("Overall size: " + PenetrationUtility.GetOrganSize(gen));
                }
            }
        }

        public static void SexUtility_Postfix(Pawn pawn, Pawn partner, bool rape, xxx.rjwSextype sextype)
        {
            Log("SexUtility_Postfix");
            Log("Sex type: " + sextype);
            Log("* Initiator *");
            LogPawnData(pawn);
            Log("* Partner *");
            LogPawnData(partner);

            PenetrationUtility.ProcessPenetrations(pawn, partner, rape, sextype);

            // The code below is just a test for an alternative way of getting sex types. It can safely be deleted if the current method works
            List<LogEntry> entries = Find.PlayLog.AllEntries;
            for (int i = 0; i < entries.Count; i++)
                if ((entries[i] is PlayLogEntry_Interaction logEntry) && (logEntry.Concerns(pawn) && logEntry.Concerns(partner)))
                {
                    Log("Log entry #" + (i + 1) + "/" + entries.Count + " (" + logEntry.Age + " ticks ago): " + logEntry);
                    break;
                }
        }
    }
}
