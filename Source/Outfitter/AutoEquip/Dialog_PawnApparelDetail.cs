﻿using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AutoEquip
{
    public class DialogPawnApparelDetail : Window
    {
        private readonly Pawn _pawn;
        private readonly Apparel _apparel;

        public DialogPawnApparelDetail(Pawn pawn, Apparel apparel)
        {
            doCloseX = true;
            closeOnEscapeKey = true;
            doCloseButton = true;

            _pawn = pawn;
            _apparel = apparel;
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(700f, 700f);
            }
        }

        private Vector2 _scrollPosition;
#pragma warning disable 649
        private ThingDef stuff;
#pragma warning restore 649
#pragma warning disable 649
        private Def def;
#pragma warning restore 649

        private Def Def
        {
            get
            {
                if (this._apparel != null)
                    return (Def)this._apparel.def;
                return this.def;
            }
        }

        private string GetTitle()
        {
            if (this._apparel != null)
                return this._apparel.LabelCap;
            ThingDef thingDef = this.Def as ThingDef;
            if (thingDef != null)
                return GenLabel.ThingLabel((BuildableDef)thingDef, this.stuff, 1).CapitalizeFirst();
            return this.Def.LabelCap;
        }

        public override void DoWindowContents(Rect windowRect)
        {

            Rect rect1 = new Rect(windowRect);
            rect1.height = 34f;
            Text.Font = GameFont.Medium;
            Widgets.Label(rect1, this.GetTitle());
            Text.Font = GameFont.Small;

            Rect groupRect = windowRect;
            groupRect.height -= 150f;
            groupRect.yMin += 30f;
            GUI.BeginGroup(groupRect);

            float baseValue = 100f;
            float multiplierWidth = 100f;
            float finalValue = 100f;
            float labelWidth = groupRect.width - baseValue - multiplierWidth - finalValue - 48f;

            Rect itemRect = new Rect(groupRect.xMin + 4f, groupRect.yMin, groupRect.width / 2, Text.LineHeight * 1.2f); //original groupRect.width -8f

            DrawLine(ref itemRect,
                "Status", labelWidth,
                "Base", baseValue,
                "Strengh", multiplierWidth,
                "Score", finalValue);

            groupRect.yMin += itemRect.height;
            Widgets.DrawLineHorizontal(groupRect.xMin, groupRect.yMin, groupRect.width);
            groupRect.yMin += 4f;
            groupRect.height -= 4f;
            groupRect.height -= Text.LineHeight * 1.2f * 3f + 5f;

            HashSet<StatDef> equippedOffsets = new HashSet<StatDef>();
            if (_apparel.def.equippedStatOffsets != null)
            {
                foreach (StatModifier equippedStatOffset in _apparel.def.equippedStatOffsets)
                {
                    equippedOffsets.Add(equippedStatOffset.stat);
                }
            }
            HashSet<StatDef> statBases = new HashSet<StatDef>();
            if (_apparel.def.statBases != null)
            {
                foreach (StatModifier statBase in _apparel.def.statBases)
                {
                    statBases.Add(statBase.stat);
                }
            }

            Rect viewRect = new Rect(groupRect.xMin, groupRect.yMin, groupRect.width - 16f, (statBases.Count + equippedOffsets.Count) * Text.LineHeight * 1.2f + 16f);

            if (viewRect.height > groupRect.height)
                groupRect.height = viewRect.height;

            Rect listRect = viewRect.ContractedBy(4f);


            // Detail list scrollable

            Widgets.BeginScrollView(groupRect, ref _scrollPosition, viewRect);

            float sumStatsValue = 0;

            // relevant apparel stats


            // start score at 1
            float score = 1;

            //// make infusions ready
            //InfusionSet infusions;
            //bool infused = false;
            //StatMod mod;
            //InfusionDef prefix = null;
            //InfusionDef suffix = null;
            //if ( apparel.TryGetInfusions( out infusions ) )
            //{
            //    infused = true;
            //    prefix = infusions.Prefix.ToInfusionDef();
            //    suffix = infusions.Suffix.ToInfusionDef();
            //}

            // add values for each statdef modified by the apparel
            foreach (ApparelStatCache.StatPriority statPriority in _pawn.GetApparelStatCache().StatCache)
            {
                // statbases, e.g. armor
                if (statBases.Contains(statPriority.Stat))
                {
                    itemRect = new Rect(listRect.xMin, listRect.yMin, listRect.width, Text.LineHeight * 1.2f);
                    if (Mouse.IsOver(itemRect))
                    {
                        GUI.DrawTexture(itemRect, TexUI.HighlightTex);
                        GUI.color = Color.white;
                    }

                    float statscore = _apparel.GetStatValue(statPriority.Stat) * statPriority.Weight;


                    DrawLine(ref itemRect,
                        statPriority.Stat.label, labelWidth,
                        _apparel.GetStatValue(statPriority.Stat).ToString("N2"), baseValue,
                        statPriority.Weight.ToString("N2"), multiplierWidth,
                        statscore.ToString("N2"), finalValue);

                    listRect.yMin = itemRect.yMax;
                    score += statscore;

                }

                if (equippedOffsets.Contains(statPriority.Stat))
                {
                    itemRect = new Rect(listRect.xMin, listRect.yMin, listRect.width, Text.LineHeight * 1.2f);
                    if (Mouse.IsOver(itemRect))
                    {
                        GUI.DrawTexture(itemRect, TexUI.HighlightTex);
                        GUI.color = Color.white;
                    }


                    float statValue = GetEquippedStatValue(_apparel,statPriority.Stat);
                    var statStrength = statPriority.Weight;


                    if (statValue < 1) // flipped for calc + *-1
                    {
                        statValue *= -1;
                        statValue += 1;
                        statStrength *= -1;
                        //          sumStatsValue += valueDisplay;
                    }
                    //      if (value != 1)
                    score += statValue * statStrength;


                    float statscore = statValue * statStrength;

                    DrawLine(ref itemRect,
                        statPriority.Stat.label, labelWidth,
                        GetEquippedStatValue(_apparel, statPriority.Stat).ToString("N2"), baseValue,
                        statPriority.Weight.ToString("N2"), multiplierWidth,
                        statscore.ToString("N2"), finalValue);

                    listRect.yMin = itemRect.yMax;



                    // base value
                    float norm = _apparel.GetStatValue(statPriority.Stat);
                    float adjusted = norm;

                    // add offset
                    adjusted += _apparel.def.equippedStatOffsets.GetStatOffsetFromList(statPriority.Stat) *
                                statPriority.Weight;

                    // normalize
                    if (norm != 0)
                    {
                        adjusted /= norm;
                    }

                    // multiply score to favour items with multiple offsets
               //     score *= adjusted;

                    //debug.AppendLine( statWeightPair.Key.LabelCap + ": " + score );
                }
            }

            // EXPERIMENTAL
      
            Widgets.EndScrollView();
            GUI.EndGroup();

            Widgets.DrawLineHorizontal(groupRect.xMin, groupRect.yMax, groupRect.width);

            //          itemRect.yMax += 5; 

            itemRect = new Rect(listRect.xMin, groupRect.yMax, listRect.width, Text.LineHeight * 0.6f);
            DrawLine(ref itemRect,
                "", labelWidth,
                "", baseValue,
                "", multiplierWidth,
                "", finalValue);

            itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            DrawLine(ref itemRect,
                "", labelWidth,
                "", baseValue,
                "Multiplier", multiplierWidth,
                "Subtotal", finalValue);

            //       itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 0.6f);
            //       Widgets.DrawLineHorizontal(itemRect.xMin, itemRect.yMax, itemRect.width);

            float subtotal = 1;

            itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            DrawLine(ref itemRect,
                "BasicStatusOfApparel".Translate(), labelWidth,
                "1.00", baseValue,
                "", multiplierWidth,
                score.ToString("N2"), finalValue);

            score += ApparelStatsHelper.ApparelScoreRaw_Temperature(_apparel, _pawn);
            itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            DrawLine(ref itemRect, 
                "AutoEquipTemperature".Translate(), labelWidth, 
                ApparelStatsHelper.ApparelScoreRaw_Temperature(_apparel, _pawn).ToString("N2"), baseValue, 
                "", multiplierWidth, 
                score.ToString("N2"), finalValue);


            itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);

            if (sumStatsValue > 0 && _pawn.GetApparelStatCache().StatCache.Count > 0)
            {
                subtotal += ApparelStatsHelper.ApparelScoreRaw(_apparel, _pawn);

                DrawLine(ref itemRect,
                "AverageStat".Translate(), labelWidth,
                (sumStatsValue / _pawn.GetApparelStatCache().StatCache.Count).ToString("N2"), baseValue,
                ApparelStatsHelper.ApparelScoreRaw(_apparel,_pawn).ToString("N2"), multiplierWidth,
                subtotal.ToString("N2"), finalValue);

                itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            }

            float armor = ApparelStatsHelper.ApparelScoreRaw_ProtectionBaseStat(_apparel) * 0.125f;

            score += armor;

            DrawLine(ref itemRect,
                "AutoEquipArmor".Translate(), labelWidth,
                "+", baseValue,
                armor.ToString("N2"), multiplierWidth,
                score.ToString("N2"), finalValue);

            if (_apparel.def.useHitPoints)
            {
            itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
                // durability on 0-1 scale
                float x = _apparel.HitPoints / (float)_apparel.MaxHitPoints;
                score *= ApparelStatsHelper.HitPointsPercentScoreFactorCurve.Evaluate(x);

                DrawLine(ref itemRect,
                "AutoEquipHitPoints".Translate(), labelWidth,
                ApparelStatsHelper.HitPointsPercentScoreFactorCurve.Evaluate(x).ToString("N2"), baseValue,
                "", multiplierWidth,
                score.ToString("N2"), finalValue);
            }


            itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            DrawLine(ref itemRect,
                "AutoEquipTotal".Translate(), labelWidth,
                "", baseValue,
                "", multiplierWidth,
                ApparelStatsHelper.ApparelScoreRaw(_apparel, _pawn).ToString("N2"), finalValue);

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public static float GetEquippedStatValue(Apparel apparel, StatDef stat)
        {

            float baseStat = apparel.GetStatValue(stat, true);
            float currentStat = baseStat + apparel.def.equippedStatOffsets.GetStatOffsetFromList(stat);
            //            currentStat += apparel.def.equippedStatOffsets.GetStatOffsetFromList(stat.StatDef);

            //   if (stat.StatDef.defName.Equals("PsychicSensitivity"))
            //   {
            //       return apparel.def.equippedStatOffsets.GetStatOffsetFromList(stat.StatDef) - baseStat;
            //   }

            if (baseStat != 0)
            {
                currentStat = currentStat / baseStat;
            }

            return currentStat;
        }

        private void DrawLine(ref Rect itemRect,
            string statDefLabelText, float statDefLabelWidth,
            string statDefValueText, float statDefValueWidth,
            string multiplierText, float multiplierWidth,
            string finalValueText, float finalValueWidth)
        {
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, statDefLabelWidth, itemRect.height), statDefLabelText);
            itemRect.xMin += statDefLabelWidth;

            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, statDefValueWidth, itemRect.height), statDefValueText);
            itemRect.xMin += statDefValueWidth;

            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, multiplierWidth, itemRect.height), multiplierText);
            itemRect.xMin += multiplierWidth;

            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, finalValueWidth, itemRect.height), finalValueText);
            itemRect.xMin += finalValueWidth;
        }


    }
}