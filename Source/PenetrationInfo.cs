using rjw;
using System;
using System.Collections.Generic;
using Verse;

namespace Dyspareunia
{
    class PenetrationInfo
    {
        public Hediff penetratingOrgan;  // The part (or rather, hediff) that does the actual penetration, e.g. dick
        public Hediff orifice;  // The part (or rather, hediff) that is being penetrated, e.g. vagina or anus
        public bool isRape;

        public Pawn Penetrator => penetratingOrgan.pawn;
        public Pawn Target => orifice.pawn;

        protected PenetrationInfo(Hediff penetratingOrgan, Hediff orifice, bool isRape)
        {
            this.penetratingOrgan = penetratingOrgan;
            this.orifice = orifice;
            this.isRape = isRape;
        }

        // Returns the size of an organ; 0.5 to 1.5 for (normal) anus; 1 to 2 for other organs
        public static double GetOrganSize(Hediff organ) =>
            ((organ.def.defName.ToLower().Contains("anus") ? 0.5 : 1) + organ.Severity) * organ.pawn.BodySize;  // Assume max natural organ size is 2x bigger than the smallest

        public double RelativeOrgansSize => GetOrganSize(penetratingOrgan) / GetOrganSize(orifice);

        public void ApplyDamage()
        {
            Dyspareunia.Log("Applying damage to " + Target.Label + "'s " + orifice.Label);

            // Calculating damage amounts
            double rubbingDamage = 1;
            double stretchDamage = Math.Max(RelativeOrgansSize - 1, 0);

            if (RelativeOrgansSize < 1.2) rubbingDamage *= 0.5; // If penetrating organ is smaller than the orifice, rubbing damage is lower
            if (isRape) // Rape is rough
            {
                rubbingDamage *= 2; 
                stretchDamage *= 1.5;
            }

            // Adding rubbing damage (abrasion)
            Dyspareunia.Log("Rubbing damage amount: " + rubbingDamage);
            HediffDef hediffDef = HediffDef.Named("Abrasion");
            if (hediffDef == null)
            {
                Dyspareunia.Log("No hediff def found.");
                return;
            }
            Dyspareunia.Log("Hediff def: " + hediffDef);
            DamageDef damageDef = DefDatabase<DamageDef>.GetNamed("SexRub");
            if (damageDef == null)
            {
                Dyspareunia.Log("No DamageDef 'Rub' found.");
                return;
            }
            Dyspareunia.Log("Damage def: " + damageDef);
            DamageInfo damageInfo = new DamageInfo(damageDef, (float)rubbingDamage, instigator: Penetrator, hitPart: orifice.Part);
            damageInfo.SetIgnoreArmor(true);
            Target.health.AddHediff(hediffDef, orifice.Part, damageInfo);
            Dyspareunia.Log("Abrasion hediff added.");

            // Adding stretch damage (rupture)
            Dyspareunia.Log("Stretch damage amount: " + stretchDamage);
            if (stretchDamage > 0)
            {
                hediffDef = HediffDef.Named("Rupture");
                if (hediffDef == null)
                {
                    Dyspareunia.Log("No hediff def found.");
                    return;
                }
                Dyspareunia.Log("Hediff def: " + hediffDef);
                damageDef = DefDatabase<DamageDef>.GetNamed("SexStretch");
                if (damageDef == null)
                {
                    Dyspareunia.Log("No DamageDef 'Stretch' found.");
                    return;
                }
                Dyspareunia.Log("Damage def: " + damageDef);
                damageInfo = new DamageInfo(damageDef, (float)stretchDamage, instigator: Penetrator, hitPart: orifice.Part);
                damageInfo.SetIgnoreArmor(true);
                Target.health.AddHediff(hediffDef, orifice.Part, damageInfo);
                Dyspareunia.Log("Stretch hediff added.");
            }
        }

        /// <summary>
        /// This method creates a list of penetrations
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="rape"></param>
        /// <param name="sextype"></param>
        /// <returns>Empty list if no eligible penetrations found, or an element for each penetration (can be DP etc.)</returns>
        public static List<PenetrationInfo> GetPenetrationInfo(Pawn p1, Pawn p2, bool rape, xxx.rjwSextype sextype)
        {
            Dyspareunia.Log("Checking " + sextype + (rape ? " rape" : " sex") + " between " + p1.Label + " and " + p2.Label + ".");
            List<PenetrationInfo> res = new List<PenetrationInfo>();

            // Setting up PenetrationInfo value(s) based on sex type
            switch (sextype)
            {
                case xxx.rjwSextype.Vaginal:
                    if (Dyspareunia.HasPenetratingOrgan(p1) && Genital_Helper.has_vagina(p2))
                        res.Add(new PenetrationInfo(Genital_Helper.get_penis_all(p1), Dyspareunia.GetVagina(p2), rape));
                    else res.Add(new PenetrationInfo(Genital_Helper.get_penis_all(p2), Dyspareunia.GetVagina(p1), rape));
                    break;
                case xxx.rjwSextype.Anal:
                    if (Dyspareunia.HasPenetratingOrgan(p1) && Genital_Helper.has_anus(p2))
                        res.Add(new PenetrationInfo(Genital_Helper.get_penis_all(p1), Dyspareunia.GetAnus(p2), rape));
                    else res.Add(new PenetrationInfo(Genital_Helper.get_penis_all(p2), Dyspareunia.GetAnus(p1), rape));
                    break;
                case xxx.rjwSextype.Oral:
                    // Oral penetration not supported ATM
                    //if (Dyspareunia.HasPenetratingOrgan(p1) && Genital_Helper.has_mouth(p2))
                    //    res.Add(new PenetrationInfo(Genital_Helper.get_penis_all(p1), Dyspareunia.GetMouth(p2), rape));
                    //else if (Dyspareunia.HasPenetratingOrgan(p1) && Genital_Helper.has_mouth(p1))
                    //    res.Add(new PenetrationInfo(Genital_Helper.get_penis_all(p2), Dyspareunia.GetMouth(p1), rape));
                    break;
                case xxx.rjwSextype.DoublePenetration:
                    if (Genital_Helper.has_multipenis(p1) && Genital_Helper.has_vagina(p2) && Genital_Helper.has_anus(p2))
                    {
                        res.Add(new PenetrationInfo(Genital_Helper.get_penis_all(p1), Dyspareunia.GetVagina(p2), rape));
                        res.Add(new PenetrationInfo(Genital_Helper.get_penis_all(p1), Dyspareunia.GetAnus(p2), rape));
                    }
                    else
                    {
                        res.Add(new PenetrationInfo(Genital_Helper.get_penis_all(p2), Dyspareunia.GetVagina(p1), rape));
                        res.Add(new PenetrationInfo(Genital_Helper.get_penis_all(p2), Dyspareunia.GetAnus(p1), rape));
                    }
                    break;
                case xxx.rjwSextype.Fingering: break; // Assume no penetration
                case xxx.rjwSextype.Fisting: break; // TODO: implement fisting
                case xxx.rjwSextype.MechImplant: break;  // TODO: implemenet mech implantation
            }

            // Now checking all found penetrations' validity
            for (int i = res.Count - 1; i >= 0; i--)
            {
                Dyspareunia.Log("Penetration #" + (i + 1) + "/" + res.Count);
                if (res[i].penetratingOrgan != null)
                    Dyspareunia.Log("Penetrating organ: " + res[i].penetratingOrgan.def.defName + " (size " + res[i].penetratingOrgan.Severity + ")");
                else Dyspareunia.Log("No penetrating organ found!");

                if (res[i].orifice != null)
                    Dyspareunia.Log("Orifice: " + res[i].orifice.def.defName + " (size " + res[i].orifice.Severity + ")");
                else Dyspareunia.Log("No orifice found!");

                if (res[i].penetratingOrgan == null || res[i].orifice == null)
                {
                    Dyspareunia.Log("This penetration is invalid.");
                    res.RemoveAt(i);
                }
                else
                    Dyspareunia.Log(res[i].Penetrator.Label + "'s " + res[i].penetratingOrgan.Label + " (overall size " + GetOrganSize(res[i].penetratingOrgan) + ") penetrates " + res[i].Target.Label + "'s " + res[i].orifice.Label + " (overall size " + GetOrganSize(res[i].orifice) + ").");
            }

            return res;
        }
    }
}
