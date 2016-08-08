﻿// AutoEquip/ApparelStatsHelper.cs
// 
// Copyright Karel Kroeze, 2016.
// 
// Created 2015-12-31 14:34

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AutoEquip
{
    public static class ApparelStatsHelper
    {
   //     private static readonly Dictionary<Pawn, ApparelStatCache> PawnApparelStatCaches = new Dictionary<Pawn, ApparelStatCache>();
        private static readonly List<string> IgnoredWorktypeDefs = new List<string>();

        public static FloatRange MinMaxTemperatureRange => new FloatRange(-100, 100);

        // exact copy of vanilla - couldn't be bothered with reflection
        public static readonly SimpleCurve HitPointsPercentScoreFactorCurve = new SimpleCurve
        {

                        new CurvePoint( 0.0f, 0.05f ),
            new CurvePoint( 0.4f, 0.3f ),
            new CurvePoint( 0.6f, 0.75f ),
            new CurvePoint( 1f, 1f )
        //  new CurvePoint( 0.0f, 0.0f ),
        //  new CurvePoint( 0.25f, 0.15f ),
        //  new CurvePoint( 0.5f, 0.7f ),
        //  new CurvePoint( 1f, 1f )
        };


        public static Dictionary<StatDef, float> GetWeightedApparelStats(this Pawn pawn)
        {
            Dictionary<StatDef, float> dict = new Dictionary<StatDef, float>();
   //       dict.Add(StatDefOf.ArmorRating_Blunt, 0.25f);
   //       dict.Add(StatDefOf.ArmorRating_Sharp, 0.25f);

            // Adds manual prioritiy adjustments 


            // add weights for all worktypes, multiplied by job priority
            foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading.Where(def => pawn.workSettings.WorkIsActive(def)))
            {
                foreach (KeyValuePair<StatDef, float> stat in GetStatsOfWorkType(workType))
                {
                    int priority = pawn.workSettings.GetPriority(workType);

                    float priorityAdjust;
                    switch (priority)
                    {
                        case 1:
                            priorityAdjust = 1f;
                            break;
                        case 2:
                            priorityAdjust = 0.5f;
                            break;
                        case 3:
                            priorityAdjust = 0.25f;
                            break;
                        case 4:
                            priorityAdjust = 0.1f;
                            break;
                        default:
                            priorityAdjust = 0.1f;
                            break;
                    }

                    float weight = stat.Value * priorityAdjust;

                    if (dict.ContainsKey(stat.Key))
                    {
                        dict[stat.Key] += weight;
                    }
                    else
                    {
                        dict.Add(stat.Key, weight);
                    }
                }
            }

            // normalize weights
            float max = dict.Values.Select(Math.Abs).Max();
            foreach (StatDef key in new List<StatDef>(dict.Keys))
            {
                // normalize max of absolute weigths to be 1
                dict[key] /= max / 1f;
            }

            return dict;
        }

        public static float ApparelScoreGain(Pawn pawn, Apparel ap)
        {
            // only allow shields to be considered if a primary weapon is equipped and is melee
            if (ap.def == ThingDefOf.Apparel_PersonalShield &&
                 pawn.equipment.Primary != null &&
                 !pawn.equipment.Primary.def.Verbs[0].MeleeRange)
            {
                return -1000f;
            }
            ApparelStatCache conf = new ApparelStatCache(pawn);

            // get the score of the considered apparel
            float candidateScore = conf.ApparelScoreRaw(ap, pawn);
        //    float candidateScore = ApparelStatCache.ApparelScoreRaw(ap, pawn);

            // get the current list of worn apparel
            List<Apparel> wornApparel = pawn.apparel.WornApparel;

            // check if the candidate will replace existing gear
            bool willReplace = false;
            for (int i = 0; i < wornApparel.Count; i++)
            {
                if (!ApparelUtility.CanWearTogether(wornApparel[i].def, ap.def))
                {
                    // can't drop forced gear
                    if (!pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(wornApparel[i]))
                    {
                        return -1000f;
                    }

                    // if replaces, score is difference of the two pieces of gear
                    candidateScore -= conf.ApparelScoreRaw(wornApparel[i], pawn);
                    willReplace = true;
                }
            }

            // increase score if this piece can be worn without replacing existing gear.
            if (!willReplace)
            {
                candidateScore *= 10f;
            }

            return candidateScore;
        }

        private static List<StatDef> _allApparelStats;

        public static List<StatDef> AllStatDefsModifiedByAnyApparel
        {
            get
            {
                if (_allApparelStats == null)
                {
                    _allApparelStats = new List<StatDef>();

                    // add all stat modifiers from all apparels
                    foreach (ThingDef apparel in DefDatabase<ThingDef>.AllDefsListForReading.Where(td => td.IsApparel))
                    {
                        if (apparel.equippedStatOffsets != null &&
                             apparel.equippedStatOffsets.Count > 0)
                        {
                            foreach (StatModifier modifier in apparel.equippedStatOffsets)
                            {
                                if (!_allApparelStats.Contains(modifier.stat))
                                {
                                    _allApparelStats.Add(modifier.stat);
                                }
                            }
                        }
                    }

                    //// add all stat modifiers from all infusions
                    //foreach ( InfusionDef infusion in DefDatabase<InfusionDef>.AllDefsListForReading )
                    //{
                    //    foreach ( KeyValuePair<StatDef, StatMod> mod in infusion.stats )
                    //    {
                    //        if ( !_allApparelStats.Contains( mod.Key ) )
                    //        {
                    //            _allApparelStats.Add( mod.Key );
                    //        }
                    //    }
                    //}
                }
                return _allApparelStats;
            }
        }

        public static List<StatDef> NotYetAssignedStatDefs(this Pawn pawn)
        {
            SaveablePawn newPawnSaveable = MapComponent_AutoEquip.Get.GetCache(pawn);
            return
                AllStatDefsModifiedByAnyApparel
                    .Except(newPawnSaveable.Stats.Select(prio => prio.Stat))
                    .ToList();
        }




        public static IEnumerable<KeyValuePair<StatDef, float>> GetStatsOfWorkType(WorkTypeDef worktype)
        {
            switch (worktype.defName)
            {
                case "Research":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ResearchSpeed"), 1f);
                    yield break;
                case "Cleaning":
                    //            yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 1f);
                    yield break;
                case "Hauling":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 0.25f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryingCapacity"), 0.25f);
                    yield break;
                case "Crafting":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.125f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("StonecuttingSpeed"), 0.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SmeltingSpeed"), 0.5f);
                    yield break;
                case "Art":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.125f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SculptingSpeed"), 1f);
                    yield break;
                case "Tailoring":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.125f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("TailoringSpeed"), 1f);
                    yield break;
                case "Smithing":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.125f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SmithingSpeed"), 1f);
                    yield break;
                case "PlantCutting":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("HarvestFailChance"), -0.25f);
                    yield break;
                case "Growing":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.125f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 0.125f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("PlantWorkSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("HarvestFailChance"), -0.75f);
                    yield break;
                case "Mining":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.125f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 0.0625f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MiningSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryingCapacity"), 0.25f);
                    yield break;
                case "Repair":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("FixBrokenDownBuildingFailChance"), -1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 0.0625f);
                    yield break;
                case "Construction":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 0.0625f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ConstructionSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SmoothingSpeed"), 0.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryingCapacity"), 0.25f);
                    yield break;
                case "Hunting":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 0.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("AimingDelayFactor"), -0.75f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ShootingAccuracy"), 1f);
                    //   yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("AimingAccuracy"), 1f); // CR
                    //   yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ReloadSpeed"), 0.25f); // CR
                    //       yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 0.0625f);
                    //       yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 0.0625f);
                    yield break;
                case "Cooking":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 0.0625f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CookSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("FoodPoisonChance"), -0.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("BrewingSpeed"), 0.75f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ButcheryFleshSpeed"), 0.75f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ButcheryFleshEfficiency"), 0.5f);
                    yield break;
                case "Handling":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 0.25f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryingCapacity"), 0.125f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("TameAnimalChance"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("TrainAnimalChance"), 1f);
                    //      yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MeleeDPS"), 1.0f);
                    //        yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MeleeHitChance"), 0.25f);
                    //               yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 0.0625f);
                    //                yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 0.0625f);
                    //         yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryWeight"), 0.25f); // CR
                    //         yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryBulk"), 0.25f); // CR
                    yield break;
                case "Warden":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SocialImpact"), 0.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("RecruitPrisonerChance"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("GiftImpact"), 0.25f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("TradePriceImprovement"), 0.5f);
                    yield break;
                case "Flicker":
                    yield break;
                case "Patient":
                    yield break;
                case "Firefighter":
                    yield break;
                case "Doctor":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MedicalOperationSpeed"), 0.75f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SurgerySuccessChance"), 0.75f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("BaseHealingQuality"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("HealingSpeed"), 0.5f);
                    yield break;
                default:
                    if (!IgnoredWorktypeDefs.Contains(worktype.defName))
                    {
                        Log.Warning("WorkTypeDef " + worktype.defName + " not handled.");
                        IgnoredWorktypeDefs.Add(worktype.defName);
                    }
                    yield break;
            }
        }
    }
}