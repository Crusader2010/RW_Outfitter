﻿// Outfitter/StatCache.cs
//
// Copyright Karel Kroeze, 2016.
//
// Created 2016-01-02 13:58

namespace Outfitter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using JetBrains.Annotations;

    using Outfitter.Textures;

    using RimWorld;

    using UnityEngine;

    using Verse;

    public class ApparelStatCache
    {
        public const float MaxValue = 2.5f;

        // public List<Apparel> recentApparel = new List<Apparel>();
        public readonly List<StatPriority> Cache;

        public Dictionary<Apparel, Pawn> ToDropList = new Dictionary<Apparel, Pawn>();

        private readonly Pawn pawn;

        private readonly SaveablePawn pawnSave;

        private int lastStatUpdate;

        private int lastTempUpdate;

        private int lastWeightUpdate;

        public ApparelStatCache(Pawn pawn)
            : this(pawn.GetSaveablePawn())
        {
        }

        // public NeededWarmth neededWarmth;
        public ApparelStatCache([NotNull] SaveablePawn saveablePawn)
        {
            this.pawn = saveablePawn.Pawn;
            this.pawnSave = this.pawn.GetSaveablePawn();
            this.Cache = new List<StatPriority>();
            this.lastStatUpdate = -5000;
            this.lastTempUpdate = -5000;
            this.lastWeightUpdate = -5000;
        }

        public delegate void ApparelScoreRawIgnored_WTHandlers(ref List<StatDef> statDef);

        public delegate void ApparelScoreRawInfusionHandlers(
            [NotNull] Apparel apparel,
            [NotNull] StatDef parentStat,
            ref HashSet<StatDef> infusedOffsets);

        public delegate void ApparelScoreRawStatsHandler(Apparel apparel, StatDef statDef, out float num);

        public static event ApparelScoreRawInfusionHandlers ApparelScoreRaw_FillInfusedStat;

        public static event ApparelScoreRawStatsHandler ApparelScoreRaw_PawnStatsHandlers;

        public static event ApparelScoreRawIgnored_WTHandlers Ignored_WTHandlers;

        [NotNull]
        public List<StatPriority> StatCache
        {
            get
            {
                // update auto stat priorities roughly between every vanilla gear check cycle
                if (Find.TickManager.TicksGame - this.lastStatUpdate
                    > JobGiver_OutfitterOptimizeApparel.ApparelStatCheck || this.pawnSave.forceStatUpdate)
                {
                    // list of auto stats
                    if (this.Cache.Count < 1 && this.pawnSave.Stats.Count > 0)
                    {
                        foreach (Saveable_Pawn_StatDef statDef in this.pawnSave.Stats)
                        {
                            this.Cache.Add(new StatPriority(statDef.Stat, statDef.Weight, statDef.Assignment));
                        }
                    }

                    this.pawnSave.Stats.Clear();

                    // clear auto priorities
                    this.Cache.RemoveAll(stat => stat.Assignment == StatAssignment.Automatic);
                    this.Cache.RemoveAll(stat => stat.Assignment == StatAssignment.Individual);

                    // loop over each (new) stat
                    // Armor only used by the Battle beacon, no relevance to jobs etc.
                    Pawn thisPawn = this.pawn;
                    if (this.pawnSave.armorOnly)
                    {
                        Dictionary<StatDef, float> updateArmorStats = thisPawn.GetWeightedApparelArmorStats();
                        foreach (KeyValuePair<StatDef, float> pair in updateArmorStats)
                        {
                            // find index of existing priority for this stat
                            int i = this.Cache.FindIndex(stat => stat.Stat == pair.Key);

                            // if index -1 it doesnt exist yet, add it
                            if (i < 0)
                            {
                                StatPriority armorStats = new StatPriority(pair.Key, pair.Value);
                                this.Cache.Add(armorStats);
                            }
                            else
                            {
                                // it exists, make sure existing is (now) of type override.
                                this.Cache[i].Weight += pair.Value;
                            }
                        }
                    }
                    else
                    {
                        Dictionary<StatDef, float> updateAutoPriorities = thisPawn.GetWeightedApparelStats();
                        Dictionary<StatDef, float> updateIndividualPriorities =
                            thisPawn.GetWeightedApparelIndividualStats();

                        // updateAutoPriorities = updateAutoPriorities.OrderBy(x => x.Key.label).ToDictionary(x => x.Key, x => x.Value);
                        updateAutoPriorities = updateAutoPriorities.OrderByDescending(x => Mathf.Abs(x.Value))
                            .ToDictionary(x => x.Key, x => x.Value);
                        updateIndividualPriorities = updateIndividualPriorities.OrderBy(x => x.Key.label)
                            .ToDictionary(x => x.Key, x => x.Value);

                        foreach (KeyValuePair<StatDef, float> pair in updateIndividualPriorities)
                        {
                            // find index of existing priority for this stat
                            int i = this.Cache.FindIndex(stat => stat.Stat == pair.Key);

                            // if index -1 it doesnt exist yet, add it
                            if (i < 0)
                            {
                                StatPriority individual =
                                    new StatPriority(pair.Key, pair.Value, StatAssignment.Individual);
                                this.Cache.Add(individual);
                            }
                            else
                            {
                                // if exists, make sure existing is (now) of type override.
                                this.Cache[i].Assignment = StatAssignment.Override;
                            }
                        }

                        foreach (KeyValuePair<StatDef, float> pair in updateAutoPriorities)
                        {
                            // find index of existing priority for this stat
                            int i = this.Cache.FindIndex(stat => stat.Stat == pair.Key);

                            // if index -1 it doesnt exist yet, add it
                            if (i < 0)
                            {
                                this.Cache.Add(new StatPriority(pair));
                            }
                            else
                            {
                                // if exists, make sure existing is (now) of type override.
                                this.Cache[i].Assignment = StatAssignment.Override;
                            }
                        }
                    }

                    // update our time check.
                    this.lastStatUpdate = Find.TickManager.TicksGame;
                    this.pawnSave.forceStatUpdate = false;

                    foreach (StatPriority statPriority in this.Cache.Where(
                        statPriority => statPriority.Assignment != StatAssignment.Automatic
                                        && statPriority.Assignment != StatAssignment.Individual))
                    {
                        bool exists = false;
                        foreach (Saveable_Pawn_StatDef stat in this.pawnSave.Stats.Where(
                            stat => stat.Stat.Equals(statPriority.Stat)))
                        {
                            stat.Weight = statPriority.Weight;
                            stat.Assignment = statPriority.Assignment;
                            exists = true;
                        }

                        if (exists)
                        {
                            continue;
                        }

                        Saveable_Pawn_StatDef stats =
                            new Saveable_Pawn_StatDef
                            {
                                Stat = statPriority.Stat,
                                Assignment = statPriority.Assignment,
                                Weight = statPriority.Weight
                            };
                        this.pawnSave.Stats.Add(stats);
                    }
                }

                return this.Cache;
            }
        }

        public FloatRange TargetTemperatures
        {
            get
            {
                this.UpdateTemperatureIfNecessary();
                return this.pawnSave.TargetTemperatures;
            }

            set
            {
                this.pawnSave.TargetTemperatures = value;
                this.pawnSave.TargetTemperaturesOverride = true;
            }
        }

        private FloatRange TemperatureWeight
        {
            get
            {
                this.UpdateTemperatureIfNecessary(false, true);
                return this.pawnSave.Temperatureweight;
            }
        }

        public static float ApparelScoreRaw_ProtectionBaseStat(Apparel ap)
        {
            float num = ap.GetStatValue(StatDefOf.ArmorRating_Sharp)
                         + ap.GetStatValue(StatDefOf.ArmorRating_Blunt);
            return num * 0.1f;
        }

        public static void DoApparelScoreRaw_PawnStatsHandlers(
            [NotNull] Apparel apparel,
            [NotNull] StatDef statDef,
            out float num)
        {
            num = 0f;
            ApparelScoreRaw_PawnStatsHandlers?.Invoke(apparel, statDef, out num);
        }

        public static void DrawStatRow(
            ref Vector2 cur,
            float width,
            [NotNull] StatPriority stat,
            Pawn pawn,
            out bool stopUI)
        {
            // sent a signal if the statlist has changed
            stopUI = false;

            // set up rects
            Rect labelRect = new Rect(cur.x, cur.y, (width - 24) / 2f, 30f);
            Rect sliderRect = new Rect(labelRect.xMax + 4f, cur.y + 5f, labelRect.width, 25f);
            Rect buttonRect = new Rect(sliderRect.xMax + 4f, cur.y + 3f, 16f, 16f);

            // draw label
            Text.Font = Text.CalcHeight(stat.Stat.LabelCap, labelRect.width) > labelRect.height
                            ? GameFont.Tiny
                            : GameFont.Small;
            switch (stat.Assignment)
            {
                case StatAssignment.Automatic:
                    GUI.color = Color.grey;
                    break;

                case StatAssignment.Individual:
                    GUI.color = Color.cyan;
                    break;

                case StatAssignment.Manual:
                    GUI.color = Color.white;
                    break;

                case StatAssignment.Override:
                    GUI.color = new Color(0.75f, 0.69f, 0.33f);
                    break;

                default:
                    GUI.color = Color.white;
                    break;
            }
            Widgets.Label(labelRect, stat.Stat.LabelCap);
            Text.Font = GameFont.Small;

            // draw button
            // if manually added, delete the priority
            string buttonTooltip = string.Empty;
            if (stat.Assignment == StatAssignment.Manual)
            {
                buttonTooltip = "StatPriorityDelete".Translate(stat.Stat.LabelCap);
                if (Widgets.ButtonImage(buttonRect, OutfitterTextures.DeleteButton))
                {
                    stat.Delete(pawn);
                    stopUI = true;
                }
            }

            // if overridden auto assignment, reset to auto
            if (stat.Assignment == StatAssignment.Override)
            {
                buttonTooltip = "StatPriorityReset".Translate(stat.Stat.LabelCap);
                if (Widgets.ButtonImage(buttonRect, OutfitterTextures.ResetButton))
                {
                    stat.Reset(pawn);
                    stopUI = true;
                }
            }

            // draw line behind slider
            GUI.color = new Color(.3f, .3f, .3f);
            for (int y = (int)cur.y; y < cur.y + 30; y += 5)
            {
                Widgets.DrawLineVertical((sliderRect.xMin + sliderRect.xMax) / 2f, y, 3f);
            }

            // draw slider
            switch (stat.Assignment)
            {
                case StatAssignment.Automatic:
                    GUI.color = Color.grey;
                    break;

                case StatAssignment.Individual:
                    GUI.color = Color.cyan;
                    break;

                case StatAssignment.Manual:
                    GUI.color = Color.white;
                    break;

                case StatAssignment.Override:
                    GUI.color = new Color(0.75f, 0.69f, 0.33f);
                    break;

                default:
                    GUI.color = Color.white;
                    break;
            }
            float weight = GUI.HorizontalSlider(sliderRect, stat.Weight, -MaxValue, MaxValue);
            if (Mathf.Abs(weight - stat.Weight) > 1e-4)
            {
                stat.Weight = weight;
                if (stat.Assignment == StatAssignment.Automatic || stat.Assignment == StatAssignment.Individual)
                {
                    stat.Assignment = StatAssignment.Override;
                }
            }

            GUI.color = Color.white;

            // tooltips
            TooltipHandler.TipRegion(labelRect, stat.Stat.LabelCap + "\n\n" + stat.Stat.description);
            if (buttonTooltip != string.Empty)
            {
                TooltipHandler.TipRegion(buttonRect, buttonTooltip);
            }

            TooltipHandler.TipRegion(sliderRect, stat.Weight.ToStringByStyle(ToStringStyle.FloatTwo));

            // advance row
            cur.y += 30f;
        }

        public static void FillIgnoredInfused_PawnStatsHandlers(ref List<StatDef> allApparelStats)
        {
            Ignored_WTHandlers?.Invoke(ref allApparelStats);
        }

        public float ApparelScoreRaw([NotNull] Apparel ap)
        {
            // only allow shields to be considered if a primary weapon is equipped and is melee
            Pawn thisPawn = this.pawn;
            if (ap.def.thingClass == typeof(ShieldBelt) && thisPawn.equipment.Primary?.def.IsRangedWeapon == true)
            {
                return -1f;
            }

            // relevant apparel stats
            ApparelEntry entry = this.GetAllOffsets(ap);

            HashSet<StatDef> equippedOffsets = entry.equippedOffsets;
            HashSet<StatDef> statBases = entry.statBases;
            HashSet<StatDef> infusedOffsets = entry.infusedOffsets;

            // start score at 1
            float score = 1;

            // add values for each statdef modified by the apparel
            List<StatPriority> stats = thisPawn.GetApparelStatCache().StatCache;

            foreach (StatPriority statPriority in stats.Where(statPriority => statPriority != null))
            {
                if (statBases.Contains(statPriority.Stat))
                {
                    float statValue = ap.GetStatValue(statPriority.Stat);

                    // add stat to base score before offsets are handled ( the pawn's apparel stat cache always has armors first as it is initialized with it).
                    score += statValue * statPriority.Weight;
                }

                // equipped offsets, e.g. movement speeds
                if (equippedOffsets.Contains(statPriority.Stat))
                {
                    float statValue = ap.GetEquippedStatValue(statPriority.Stat);

                    score += statValue * statPriority.Weight;

                    // multiply score to favour items with multiple offsets
                    // score *= adjusted;

                    // debug.AppendLine( statWeightPair.Key.LabelCap + ": " + score );
                }

                // infusions
                if (infusedOffsets.Contains(statPriority.Stat))
                {
                    // float statInfused = StatInfused(infusionSet, statPriority, ref dontcare);
                    DoApparelScoreRaw_PawnStatsHandlers(ap, statPriority.Stat, out float statInfused);

                    score += statInfused * statPriority.Weight;
                }
            }

            score += ap.GetSpecialApparelScoreOffset();

            score += ApparelScoreRaw_ProtectionBaseStat(ap);

            // offset for apparel hitpoints
            if (ap.def.useHitPoints)
            {
                float x = ap.HitPoints / (float)ap.MaxHitPoints;
                score *= ApparelStatsHelper.HitPointsPercentScoreFactorCurve.Evaluate(x);
            }

            if (ap.WornByCorpse && ThoughtUtility.CanGetThought(thisPawn, ThoughtDefOf.DeadMansApparel))
            {
                score -= 0.5f;
                if (score > 0f)
                {
                    score *= 0.1f;
                }
            }

            if (ap.Stuff == ThingDefOf.Human.race.leatherDef)
            {
                if (ThoughtUtility.CanGetThought(thisPawn, ThoughtDefOf.HumanLeatherApparelSad))
                {
                    score -= 0.5f;
                    if (score > 0f)
                    {
                        score *= 0.1f;
                    }
                }

                if (ThoughtUtility.CanGetThought(thisPawn, ThoughtDefOf.HumanLeatherApparelHappy))
                {
                    score *= 2f;
                }
            }

            score *= this.ApparelScoreRaw_Temperature(ap);

            return score;
        }

        private static readonly SimpleCurve curve =
            new SimpleCurve { new CurvePoint(-10f, 0.1f), new CurvePoint(0f, 1f), new CurvePoint(60f, 2f) };

        public float ApparelScoreRaw_Temperature([NotNull] Apparel apparel)
        {
            // float minComfyTemperature = pawnSave.RealComfyTemperatures.min;
            // float maxComfyTemperature = pawnSave.RealComfyTemperatures.max;
            Pawn thisPawn = this.pawn;
            float minComfyTemperature = thisPawn.ComfortableTemperatureRange().min;
            float maxComfyTemperature = thisPawn.ComfortableTemperatureRange().max;

            // temperature
            FloatRange targetTemperatures = this.TargetTemperatures;

            // offsets on apparel
            float insulationCold = apparel.GetStatValue(StatDefOf.Insulation_Cold);
            float insulationHeat = apparel.GetStatValue(StatDefOf.Insulation_Heat);

            insulationCold += apparel.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.Insulation_Cold);
            insulationHeat += apparel.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.Insulation_Heat);
            {
                // offsets on apparel infusions
                DoApparelScoreRaw_PawnStatsHandlers(
                    apparel,
                    StatDefOf.ComfyTemperatureMin,
                    out float infInsulationCold);
                DoApparelScoreRaw_PawnStatsHandlers(
                    apparel,
                    StatDefOf.ComfyTemperatureMax,
                    out float infInsulationHeat);
                insulationCold += infInsulationCold;
                insulationHeat += infInsulationHeat;
            }

            // string log = apparel.LabelCap + " - InsCold: " + insulationCold + " - InsHeat: " + insulationHeat + " - TargTemp: "
            // + targetTemperatures + "\nMinComfy: " + minComfyTemperature + " - MaxComfy: "
            // + maxComfyTemperature;

            // if this gear is currently worn, we need to make sure the contribution to the pawn's comfy temps is removed so the gear is properly scored
            List<Apparel> wornApparel = thisPawn.apparel.WornApparel;
            if (!wornApparel.NullOrEmpty())
            {
                if (wornApparel.Contains(apparel))
                {
                    // log += "\nPawn is wearer of this apparel.";
                    minComfyTemperature -= insulationCold;
                    maxComfyTemperature -= insulationHeat;
                }
                else
                {
                    // check if the candidate will replace existing gear
                    foreach (Apparel wornAp in wornApparel)
                    {
                        if (!ApparelUtility.CanWearTogether(wornAp.def, apparel.def, thisPawn.RaceProps.body))
                        {
                            float insulationColdWorn = wornAp.GetStatValue(StatDefOf.Insulation_Cold);
                            float insulationHeatWorn = wornAp.GetStatValue(StatDefOf.Insulation_Heat);

                            insulationColdWorn +=
                                wornAp.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.Insulation_Cold);
                            insulationHeatWorn +=
                                wornAp.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.Insulation_Heat);
                            {
                                // offsets on apparel infusions
                                DoApparelScoreRaw_PawnStatsHandlers(
                                    wornAp,
                                    StatDefOf.ComfyTemperatureMin,
                                    out float infInsulationColdWorn);
                                DoApparelScoreRaw_PawnStatsHandlers(
                                    wornAp,
                                    StatDefOf.ComfyTemperatureMax,
                                    out float infInsulationHeatWorn);
                                insulationColdWorn += infInsulationColdWorn;
                                insulationHeatWorn += infInsulationHeatWorn;
                            }

                            minComfyTemperature -= insulationColdWorn;
                            maxComfyTemperature -= insulationHeatWorn;

                            // Log.Message(apparel +"-"+ insulationColdWorn + "-" + insulationHeatWorn + "-" + minComfyTemperature + "-" + maxComfyTemperature);
                        }
                    }
                }
            }

            // log += "\nBasic stat - MinComfy: " + minComfyTemperature + " - MaxComfy: " + maxComfyTemperature;

            // now for the interesting bit.
            FloatRange temperatureScoreOffset = new FloatRange(0f, 0f);

            // isolation_cold is given as negative numbers < 0 means we're underdressed
            float neededInsulation_Cold = targetTemperatures.min - minComfyTemperature;

            // isolation_warm is given as positive numbers.
            float neededInsulation_Warmth = targetTemperatures.max - maxComfyTemperature;

            // log += "\nWeight: " + tempWeight + " - NeedInsCold: " + neededInsulation_Cold + " - NeedInsWarmth: "
            // + neededInsulation_Warmth;


            if (neededInsulation_Cold < 0)
            {
                // currently too cold
                temperatureScoreOffset.min += -insulationCold;
            }
            else
            {
                // currently warm enough

                if (insulationCold > neededInsulation_Cold)
                {
                    // this gear would make us too cold
                    temperatureScoreOffset.min += insulationCold - neededInsulation_Cold;
                }
            }


            if (neededInsulation_Warmth > 0)
            {
                // currently too warm
                temperatureScoreOffset.max += insulationHeat;
            }
            else
            {
                // currently cool enough
                if (insulationHeat < neededInsulation_Warmth)
                {
                    // this gear would make us too warm
                    temperatureScoreOffset.max += neededInsulation_Warmth - insulationHeat;
                }
            }

            // Punish bad apparel
            // temperatureScoreOffset.min *= temperatureScoreOffset.min < 0 ? 2f : 1f;
            // temperatureScoreOffset.max *= temperatureScoreOffset.max < 0 ? 2f : 1f;

            // New
            FloatRange tempWeight = this.TemperatureWeight;

            temperatureScoreOffset.min = curve.Evaluate(temperatureScoreOffset.min * tempWeight.min);
            temperatureScoreOffset.max = curve.Evaluate(temperatureScoreOffset.max * tempWeight.max);


            // log += "\nScoreOffsetMin: " + temperatureScoreOffset.min + " - ScoreOffsetMax: "
            // + temperatureScoreOffset.max + " => " + 1 + (temperatureScoreOffset.min + temperatureScoreOffset.max) / 25;
            // Log.Message(log);

            return temperatureScoreOffset.min * temperatureScoreOffset.max;
            //return 1 + (temperatureScoreOffset.min + temperatureScoreOffset.max) / 15;
        }

        public ApparelEntry GetAllOffsets([NotNull] Apparel ap)
        {
            if (Outfitter.Cache.ApparelEntries.ContainsKey(ap))
            {
                return Outfitter.Cache.ApparelEntries[ap];
            }

            ApparelEntry entry = new ApparelEntry();
            this.GetStatsOfApparel(ap, ref entry.equippedOffsets, ref entry.statBases);
            this.GetStatsOfApparelInfused(ap, ref entry.infusedOffsets);

            Outfitter.Cache.ApparelEntries.Add(ap, entry);
            return entry;
        }

        public void UpdateTemperatureIfNecessary(bool force = false, bool forceweight = false)
        {
            Pawn thisPawn = this.pawn;
            if (Find.TickManager.TicksGame - this.lastTempUpdate > JobGiver_OutfitterOptimizeApparel.ApparelStatCheck
                || force)
            {
                // get desired temperatures
                if (!this.pawnSave.TargetTemperaturesOverride)
                {
                    float temp = GenTemperature.GetTemperatureAtTile(thisPawn.Map.Tile);
                    float lowest = this.LowestTemperatureComing(thisPawn.Map);

                    float minTemp = Mathf.Min(lowest - 5f, temp - 15f);

                    this.pawnSave.TargetTemperatures = new FloatRange(minTemp, temp + 15f);

                    if (this.pawnSave.TargetTemperatures.min >= 12)
                    {
                        this.pawnSave.TargetTemperatures.min = 12;
                    }

                    if (this.pawnSave.TargetTemperatures.max <= 32)
                    {
                        this.pawnSave.TargetTemperatures.max = 32;
                    }

                    if (thisPawn.workSettings.WorkIsActive(DefDatabase<WorkTypeDef>.GetNamed("Cooking")))
                    {
                        {
                            this.pawnSave.TargetTemperatures.min = Mathf.Min(this.pawnSave.TargetTemperatures.min, -3);
                        }
                    }

                    this.lastTempUpdate = Find.TickManager.TicksGame;
                }
            }

            FloatRange RealComfyTemperatures = thisPawn.ComfortableTemperatureRange();

            if (Find.TickManager.TicksGame - this.lastWeightUpdate > JobGiver_OutfitterOptimizeApparel.ApparelStatCheck
                || forceweight)
            {
                FloatRange weight = new FloatRange(1f, 1f);

                if (this.pawnSave.TargetTemperatures.min < RealComfyTemperatures.min)
                {
                    weight.min += Math.Abs((this.pawnSave.TargetTemperatures.min - RealComfyTemperatures.min) / 100);
                }

                if (this.pawnSave.TargetTemperatures.max > RealComfyTemperatures.max)
                {
                    weight.max += Math.Abs((this.pawnSave.TargetTemperatures.max - RealComfyTemperatures.max) / 100);
                }

                this.pawnSave.Temperatureweight = weight;
                this.lastWeightUpdate = Find.TickManager.TicksGame;
            }
        }

        private static void FillInfusionHashset_PawnStatsHandlers(
            Apparel apparel,
            StatDef parentStat,
            ref HashSet<StatDef> infusedOffsets)
        {
            ApparelScoreRaw_FillInfusedStat?.Invoke(apparel, parentStat, ref infusedOffsets);
        }

        private void GetStatsOfApparel(
            [NotNull] Apparel ap,
            ref HashSet<StatDef> equippedOffsets,
            ref HashSet<StatDef> statBases)
        {
            if (ap.def.equippedStatOffsets != null)
            {
                foreach (StatModifier equippedStatOffset in ap.def.equippedStatOffsets)
                {
                    equippedOffsets.Add(equippedStatOffset.stat);
                }
            }

            if (ap.def.statBases != null)
            {
                foreach (StatModifier statBase in ap.def.statBases)
                {
                    statBases.Add(statBase.stat);
                }
            }
        }

        private void GetStatsOfApparelInfused(Apparel ap, ref HashSet<StatDef> infusedOffsets)
        {
            foreach (StatPriority statPriority in this.pawn.GetApparelStatCache().StatCache)
            {
                FillInfusionHashset_PawnStatsHandlers(ap, statPriority.Stat, ref infusedOffsets);
            }
        }

        private float GetTemperature(Twelfth twelfth, [NotNull] Map map)
        {
            return GenTemperature.AverageTemperatureAtTileForTwelfth(map.Tile, twelfth);
        }

        private float LowestTemperatureComing([NotNull] Map map)
        {
            Twelfth twelfth = GenLocalDate.Twelfth(map);
            float a = this.GetTemperature(twelfth, map);
            for (int i = 0; i < 3; i++)
            {
                twelfth = twelfth.NextTwelfth();
                a = Mathf.Min(a, this.GetTemperature(twelfth, map));
            }

            return Mathf.Min(a, map.mapTemperature.OutdoorTemp);
        }

        // ReSharper disable once CollectionNeverUpdated.Global
    }
}