using rjw;
using System;
using System.Collections.Generic;
using Verse;

namespace Dyspareunia
{
    public static class PenetrationUtility
    {
        // Returns the size of an organ; 0.5 to 1.5 for (normal) anus; 1 to 2 for other organs
        public static double GetOrganSize(Hediff organ) =>
            ((organ.def.defName.ToLower().Contains("anus") ? 0.5 : 1) + organ.Severity) * organ.pawn.BodySize;  // Assume max natural organ size is 2x bigger than the smallest

        public static void AddHediff(string damageDefName, double damage, Hediff orifice, Pawn instigator)
        {
            Dyspareunia.Log(damageDefName + " damage amount: " + damage);
            DamageDef damageDef = DefDatabase<DamageDef>.GetNamed(damageDefName);
            if (damageDef == null)
            {
                Dyspareunia.Log("No DamageDef '" + damageDefName + "' found.");
                return;
            }
            Dyspareunia.Log("Damage def: " + damageDef);
            DamageInfo damageInfo = new DamageInfo(damageDef, (float)damage, instigator: instigator, hitPart: orifice.Part);
            damageInfo.SetIgnoreArmor(true);
            Dyspareunia.Log("Abrasion damage info: " + damageInfo);
            HediffDef hediffDef = damageDef.hediff;
            if (hediffDef == null)
            {
                Dyspareunia.Log("No hediff def found.");
                return;
            }
            Dyspareunia.Log("Hediff def: " + hediffDef);
            Hediff hediff = HediffMaker.MakeHediff(hediffDef, orifice.pawn, orifice.Part);
            hediff.Severity = (float)damage;
            orifice.pawn.health.AddHediff(hediff, orifice.Part, damageInfo);
            Dyspareunia.Log("Applied hediff: " + hediff);
        }

        public static void ApplyDamage(Hediff penetratingOrgan, Hediff orifice, bool isRape)
        {
            // Checking validity of penetrator and target
            Pawn penetrator = penetratingOrgan?.pawn;
            if (penetrator is null)
            {
                Dyspareunia.Log("Penetrator not found!");
                return;
            }
            Pawn target = orifice?.pawn;
            if (target is null)
            {
                Dyspareunia.Log("Orifice/target not found!");
                return;
            }
            Dyspareunia.Log("Applying damage from " + penetrator.Label + "'s " + penetratingOrgan.def.defName + " (effective size " + GetOrganSize(penetratingOrgan) + ") penetrating " + target.Label + "'s " + orifice.def.defName + " (effective size " + GetOrganSize(orifice) + ").");

            // Calculating damage amounts
            double relativeSize = GetOrganSize(penetratingOrgan) / GetOrganSize(orifice);
            double rubbingDamage = 1;
            double stretchDamage = Math.Max(relativeSize - 1, 0);

            if (relativeSize < 1.2) rubbingDamage *= 0.5; // If penetrating organ is smaller than the orifice, rubbing damage is lower
            if (isRape) // Rape is rough
            {
                rubbingDamage *= 2;
                stretchDamage *= 1.5;
            }

            AddHediff("SexRub", rubbingDamage, orifice, penetrator);
            AddHediff("SexStretch", stretchDamage, orifice, penetrator);

            // Adding rubbing damage (abrasion)
            //Dyspareunia.Log("Rubbing damage amount: " + rubbingDamage);
            //HediffDef hediffDef = HediffDef.Named("Abrasion");
            //if (hediffDef == null)
            //{
            //    Dyspareunia.Log("No hediff def found.");
            //    return;
            //}
            //Dyspareunia.Log("Hediff def: " + hediffDef);
            //DamageDef damageDef = DefDatabase<DamageDef>.GetNamed("SexRub");
            //if (damageDef == null)
            //{
            //    Dyspareunia.Log("No DamageDef 'Rub' found.");
            //    return;
            //}
            //Dyspareunia.Log("Damage def: " + damageDef);
            //DamageInfo damageInfo = new DamageInfo(damageDef, (float)rubbingDamage, instigator: penetrator, hitPart: orifice.Part);
            //damageInfo.SetIgnoreArmor(true);
            //Dyspareunia.Log("Abrasion damage info: " + damageInfo);
            //Hediff hediff = HediffMaker.MakeHediff(hediffDef, target, orifice.Part);
            //hediff.Severity = (float)rubbingDamage;
            //target.health.AddHediff(hediff, orifice.Part, damageInfo);
            //Dyspareunia.Log("Applied hediff: " + hediff);

            //// Adding stretch damage (rupture)
            //Dyspareunia.Log("Stretch damage amount: " + stretchDamage);
            //if (stretchDamage > 0)
            //{
            //    hediffDef = HediffDef.Named("Rupture");
            //    if (hediffDef == null)
            //    {
            //        Dyspareunia.Log("No hediff def found.");
            //        return;
            //    }
            //    Dyspareunia.Log("Hediff def: " + hediffDef);
            //    damageDef = DefDatabase<DamageDef>.GetNamed("SexStretch");
            //    if (damageDef == null)
            //    {
            //        Dyspareunia.Log("No DamageDef 'Stretch' found.");
            //        return;
            //    }
            //    Dyspareunia.Log("Damage def: " + damageDef);
            //    damageInfo = new DamageInfo(damageDef, (float)stretchDamage, instigator: penetrator, hitPart: orifice.Part);
            //    damageInfo.SetIgnoreArmor(true);
            //    Dyspareunia.Log("Stretch damage info: " + damageInfo);
            //    hediff = HediffMaker.MakeHediff(hediffDef, target, orifice.Part);
            //    hediff.Severity = (float)stretchDamage;
            //    target.health.AddHediff(hediff, orifice.Part, damageInfo);
            //    Dyspareunia.Log("Applied hediff: " + hediff);
        }

        /// <summary>
        /// This method discovers all penetrations in the sex act and applies respective damage
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="rape"></param>
        /// <param name="sextype"></param>
        /// <returns>Empty list if no eligible penetrations found, or an element for each penetration (can be DP etc.)</returns>
        public static void ProcessPenetrations(Pawn p1, Pawn p2, bool rape, xxx.rjwSextype sextype)
        {
            Dyspareunia.Log("Checking " + sextype + (rape ? " rape" : " sex") + " between " + p1.Label + " and " + p2.Label + ".");

            // Setting up PenetrationUtility value(s) based on sex type
            switch (sextype)
            {
                case xxx.rjwSextype.Vaginal:
                    if (Dyspareunia.HasPenetratingOrgan(p1) && Genital_Helper.has_vagina(p2))
                        ApplyDamage(Genital_Helper.get_penis_all(p1), Dyspareunia.GetVagina(p2), rape);
                    else ApplyDamage(Genital_Helper.get_penis_all(p2), Dyspareunia.GetVagina(p1), rape);
                    break;

                case xxx.rjwSextype.Anal:
                    if (Dyspareunia.HasPenetratingOrgan(p1) && Genital_Helper.has_anus(p2))
                        ApplyDamage(Genital_Helper.get_penis_all(p1), Dyspareunia.GetAnus(p2), rape);
                    else ApplyDamage(Genital_Helper.get_penis_all(p2), Dyspareunia.GetAnus(p1), rape);
                    break;

                case xxx.rjwSextype.Oral:
                    // Oral penetration not supported ATM
                    break;

                case xxx.rjwSextype.DoublePenetration:
                    if (Genital_Helper.has_multipenis(p1) && Genital_Helper.has_vagina(p2) && Genital_Helper.has_anus(p2))
                    {
                        ApplyDamage(Genital_Helper.get_penis_all(p1), Dyspareunia.GetVagina(p2), rape);
                        ApplyDamage(Genital_Helper.get_penis_all(p1), Dyspareunia.GetAnus(p2), rape);
                    }
                    else
                    {
                        ApplyDamage(Genital_Helper.get_penis_all(p2), Dyspareunia.GetVagina(p1), rape);
                        ApplyDamage(Genital_Helper.get_penis_all(p2), Dyspareunia.GetAnus(p1), rape);
                    }
                    break;

                case xxx.rjwSextype.Fingering: break; // Assume no penetration
                case xxx.rjwSextype.Fisting: break; // TODO: implement fisting
                case xxx.rjwSextype.MechImplant: break;  // TODO: implemenet mech implantation
            }
        }
    }
}
