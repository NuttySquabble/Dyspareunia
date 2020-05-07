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
                return;

            Dyspareunia.Log(damageDefName + " damage amount: " + damage);
            DamageDef damageDef = DefDatabase<DamageDef>.GetNamed(damageDefName);
            if (damageDef == null)
            {
                Dyspareunia.Log("No DamageDef '" + damageDefName + "' found.");
                return;
            }
            DamageInfo damageInfo = new DamageInfo(damageDef, (float)damage, instigator: instigator, hitPart: orifice.Part);
            damageInfo.SetIgnoreArmor(true);
            HediffDef hediffDef = damageDef.hediff;
            if (hediffDef == null)
            {
                Dyspareunia.Log("No HediffDef for '" + damageDef.label + "' found.");
                return;
            }
            Hediff hediff = HediffMaker.MakeHediff(hediffDef, orifice.pawn, orifice.Part);
            hediff.Severity = (float)damage;
            orifice.pawn.health.AddHediff(hediff, orifice.Part, damageInfo);
        }

        public static double GetWetness(Hediff organ)
        {
            double amount = 0;

            // Getting wetness
            CompHediffBodyPart hediffComp = organ.TryGetComp<CompHediffBodyPart>();
            if (hediffComp != null && hediffComp.FluidAmmount > 0)
            {
                Dyspareunia.Log("Fluid: " + hediffComp.FluidType + ". Amount: " + hediffComp.FluidAmmount + ". Modifier: " + hediffComp.FluidModifier);
                amount = hediffComp.FluidAmmount * hediffComp.FluidModifier / organ.pawn.BodySize;
            }

            // Adding semen to the amount of fluids
            foreach (Hediff_Semen hediff in organ.pawn.health.hediffSet.GetHediffs<Hediff_Semen>())
                if (hediff.Part == organ.Part)
                {
                    Dyspareunia.Log("Found " + hediff.Severity + " semen in " + hediff.Part.Label);
                    amount += hediff.Severity;
                }

            return amount / organ.pawn.BodySize * 0.1;
        }

        public static float StretchFactor { get; set; } = 1;

        public static void StretchOrgan(Hediff organ, double amount)
        {
            if (amount <= 0) return;
            float stretch = (float)amount / organ.Part.def.hitPoints * StretchFactor;
            Dyspareunia.Log("Stretching " + organ.def.defName + " (" + organ.Severity + " size) by " + stretch);
            organ.Severity += stretch;
        }

        static readonly TraitDef wimpTraitDef = TraitDef.Named("Wimp");

        public static void ApplyDamage(Pawn penetrator, double penetratingOrganSize, Hediff orifice, bool isRape)
        {
            // Checking validity of penetrator and target
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
            Dyspareunia.Log("Applying damage from " + penetrator.Label + " (effective size " + penetratingOrganSize + ") penetrating " + target.Label + "'s " + orifice.def.defName + " (effective size " + GetOrganSize(orifice) + ").");

            // Calculating damage amounts
            double relativeSize = penetratingOrganSize / GetOrganSize(orifice);
            double rubbingDamage = 0.5;
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
            rubbingDamage *= Rand.Range(0.75f, 1.25f);
            stretchDamage *= Rand.Range(0.75f, 1.25f);

            Dyspareunia.Log("Rubbing damage before lubricant: " + rubbingDamage);
            Dyspareunia.Log("Stretch damage before lubricant: " + stretchDamage);

            // Stretching the orifice
            StretchOrgan(orifice, stretchDamage);

            // Applying lubrication
            double wetness = GetWetness(orifice);
            Dyspareunia.Log("Total wetness: " + wetness);
            rubbingDamage *= Math.Max(1 - wetness * 0.5, 0.25);
            stretchDamage *= Math.Max(1 - wetness * 0.4, 0.3);

            Dyspareunia.Log("Rubbing damage final: " + rubbingDamage);
            Dyspareunia.Log("Stretch damage final: " + stretchDamage);

            // Adding a single hediff based on which damage type is stronger (to reduce clutter in the Health view and save on the number of treatments)
            AddHediff(rubbingDamage > stretchDamage ? "SexRub" : "SexStretch", rubbingDamage + stretchDamage, orifice, penetrator);

            // Applying positive moodlets for a big dick
            if (relativeSize > 1.25)
            {
                if (penetrator.needs?.mood?.thoughts?.memories != null)
                    penetrator.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("TightLovin"));
                if (!isRape && target.needs?.mood?.thoughts?.memories != null)
                    target.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("BigDick"));
            }
        }

        public static void ApplyDamage(Hediff penetratingOrgan, Hediff orifice, bool isRape) 
            => ApplyDamage(penetratingOrgan.pawn, GetOrganSize(penetratingOrgan), orifice, isRape);

        /// <summary>
        /// Returns the size (calculated as coverage * body size * 10) of the pawn's biggest finger
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static double GetFingerSize(Pawn pawn)
        {
            if (pawn?.RaceProps?.body is null)
            {
                Dyspareunia.Log("The pawn has no body!");
                return 0;
            }

            double biggest = 0;
            foreach (BodyPartRecord bpr in pawn.RaceProps.body.AllParts)
                if (bpr.def.defName == "Finger")
                {
                    Dyspareunia.Log("Finger '" + bpr.Label + "' of coverage " + bpr.coverage + " found.");
                    if (!pawn.health.hediffSet.PartIsMissing(bpr)) biggest = Math.Max(bpr.coverage, biggest);
                    else Dyspareunia.Log("But it is missing :(");
                }
            Dyspareunia.Log("Finger size: " + (biggest * pawn.BodySize * 10));
            return biggest * pawn.BodySize * 10;
        }

        /// <summary>
        /// Returns the size (calculated as coverage * body size * 10) of the pawn's hand
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static double GetHandSize(Pawn pawn)
        {
            if (pawn?.RaceProps?.body is null)
            {
                Dyspareunia.Log("The pawn has no body!");
                return 0;
            }
            if (pawn?.health?.hediffSet is null)
            {
                Dyspareunia.Log("The pawn has no hediffSet.");
                return 0;
            }

            List<BodyPartRecord> parts = (List<BodyPartRecord>)pawn.RaceProps.body.GetPartsWithDef(BodyPartDefOf.Hand);
            if (parts is null)
            {
                Dyspareunia.Log(pawn + " has no hands!");
                return 0;
            }
            Dyspareunia.Log(pawn.Label + " has " + parts.Count + " hands.");
            double size = parts.NullOrEmpty<BodyPartRecord>() ? 0 : parts[0].coverage * pawn.BodySize * 10;
            Dyspareunia.Log("Hand size: " + size);
            return size;
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

            switch (sextype)
            {
                case xxx.rjwSextype.Vaginal:
                    if (Dyspareunia.HasPenetratingOrgan(p1) && Genital_Helper.has_vagina(p2))
                        ApplyDamage(Genital_Helper.get_penis_all(p1), Dyspareunia.GetVagina(p2), rape);
                    else ApplyDamage(Genital_Helper.get_penis_all(p2), Dyspareunia.GetVagina(p1), false);
                    break;

                case xxx.rjwSextype.Anal:
                    if (Dyspareunia.HasPenetratingOrgan(p1) && Genital_Helper.has_anus(p2))
                        ApplyDamage(Genital_Helper.get_penis_all(p1), Dyspareunia.GetAnus(p2), rape);
                    else ApplyDamage(Genital_Helper.get_penis_all(p2), Dyspareunia.GetAnus(p1), false);
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
                        ApplyDamage(Genital_Helper.get_penis_all(p2), Dyspareunia.GetVagina(p1), false);
                        ApplyDamage(Genital_Helper.get_penis_all(p2), Dyspareunia.GetAnus(p1), false);
                    }
                    break;

                case xxx.rjwSextype.Fingering:
                    if (Genital_Helper.has_vagina(p2) || Genital_Helper.has_anus(p2))
                        ApplyDamage(p1, GetFingerSize(p1), Dyspareunia.GetVagina(p2) ?? Dyspareunia.GetAnus(p2), rape);
                    else ApplyDamage(p2, GetFingerSize(p2), Dyspareunia.GetVagina(p1) ?? Dyspareunia.GetAnus(p1), false);
                    break;

                case xxx.rjwSextype.Fisting:
                    if (Genital_Helper.has_vagina(p2) || Genital_Helper.has_anus(p2))
                        ApplyDamage(p1, GetHandSize(p1), Dyspareunia.GetVagina(p2) ?? Dyspareunia.GetAnus(p2), rape);
                    else ApplyDamage(p2, GetHandSize(p2), Dyspareunia.GetVagina(p1) ?? Dyspareunia.GetAnus(p1), false);
                    break;

                case xxx.rjwSextype.MechImplant:
                    Dyspareunia.Log("Processing mech implant sex between " + p1.Label + " and " + p2.Label);
                    if (p1.kindDef.race.defName.ContainsAny("Mech_Centipede", "Mech_Lancer", "Mech_Scyther", "Mech_Crawler", "Mech_Skullywag", "Mech_Flamebot", "Mech_Mammoth", "Mech_Assaulter"))
                        ApplyDamage(p1, p1.BodySize, Dyspareunia.GetVagina(p2) ?? Dyspareunia.GetAnus(p2), rape);
                    else ApplyDamage(p2, p2.BodySize, Dyspareunia.GetVagina(p1) ?? Dyspareunia.GetAnus(p1), false);
                    break;
            }
        }
    }
}
