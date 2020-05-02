using RimWorld;
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
            // If damage is too low (or negative), skipping it
            if (damage < 0.5)
            {
                Dyspareunia.Log(damageDefName + " is too low to apply.");
                return;
            }

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

        public static float StretchFactor { get; set; } = 1;

        public static void StretchOrgan(Hediff organ, double amount)
        {
            if (amount <= 0) return;
            Dyspareunia.Log("Organ type is " + organ.GetType());
            float stretch = (float)amount / organ.Part.def.hitPoints * StretchFactor;
            Dyspareunia.Log("Stretching " + organ.def.defName + " (" + organ.Severity + " size) by " + stretch);
            organ.Severity += stretch;
        }

        static TraitDef wimpTraitDef = TraitDef.Named("Wimp");

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
            double rubbingDamage = 0.3;
            double stretchDamage = Math.Max(relativeSize - 1, 0);

            if (relativeSize > 1.25) rubbingDamage *= 1.5; // If penetrating organ is much bigger than the orifice, rubbing damage is higher
            
            if (isRape) // Rape is rough
            {
                rubbingDamage *= 1.5;
                stretchDamage *= 1.5;
            }

            if (penetrator.story?.traits != null)
            {
                if (penetrator.story.traits.HasTrait(TraitDefOf.Bloodlust))
                {
                    rubbingDamage *= 1.25;
                    stretchDamage *= 1.125;
                }

                if (penetrator.story.traits.HasTrait(xxx.rapist))
                {
                    rubbingDamage *= 1.25;
                    stretchDamage *= 1.125;
                }

                if (penetrator.story.traits.HasTrait(TraitDefOf.Psychopath))
                {
                    rubbingDamage *= 1.2;
                    stretchDamage *= 1.1;
                }

                if ((penetrator.story.traits.HasTrait(TraitDefOf.DislikesMen) && target.gender == Gender.Male) || (penetrator.story.traits.HasTrait(TraitDefOf.DislikesWomen) && target.gender == Gender.Female))
                {
                    rubbingDamage *= 1.1;
                    stretchDamage *= 1.05;
                }

                if (penetrator.story.traits.HasTrait(TraitDefOf.Kind))
                {
                    rubbingDamage *= 0.8;
                    stretchDamage *= 0.9;
                }

                if (penetrator.story.traits.HasTrait(wimpTraitDef))
                {
                    rubbingDamage *= 0.8;
                    stretchDamage *= 0.9;
                }
            }

            if (target.story?.traits != null)
            {
                if (target.story.traits.HasTrait(wimpTraitDef))
                {
                    rubbingDamage *= 1.1;
                    stretchDamage *= 1.1;
                }

                if (target.story.traits.HasTrait(xxx.masochist))
                {
                    rubbingDamage *= 1.1;
                    stretchDamage *= 1.05;
                }

                if (target.story.traits.HasTrait(xxx.masochist))
                {
                    rubbingDamage *= 1.1;
                    stretchDamage *= 1.05;
                }

                if (target.story.traits.HasTrait(TraitDefOf.Tough))
                {
                    rubbingDamage *= 0.9;
                    stretchDamage *= 0.9;
                }
            }

            Dyspareunia.Log("Rubbing damage before randomization: " + rubbingDamage);
            Dyspareunia.Log("Stretch damage before randomization: " + stretchDamage);

            // Applying randomness
            float damageFactor = Rand.Range(0.5f, 1.5f);
            Dyspareunia.Log("damageFactor = " + damageFactor);
            rubbingDamage *= damageFactor * Rand.Range(0.75f, 1.25f);
            stretchDamage *= damageFactor;

            // Adding a single hediff based on which damage type is stronger (to reduce clutter in the Health view and save on the number of treatments)
            AddHediff(rubbingDamage > stretchDamage ? "SexRub" : "SexStretch", rubbingDamage + stretchDamage, orifice, penetrator);

            // Stretching the orifice
            StretchOrgan(orifice, stretchDamage);
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
