﻿namespace Outfitter.Window
{
    using RimWorld;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using Verse;

    public class Dialog_PawnApparelComparer : Window
    {
        private readonly Apparel _apparel;

        private readonly Pawn _pawn;

        private Vector2 scrollPosition;

        public Dialog_PawnApparelComparer(Pawn pawn, Apparel apparel)
        {
            this.doCloseX = true;
            this.closeOnEscapeKey = true;
            this.doCloseButton = true;

            this._pawn = pawn;
            this._apparel = apparel;
        }

        public override Vector2 InitialSize => new Vector2(500f, 700f);

        public override void DoWindowContents(Rect windowRect)
        {
            ApparelStatCache apparelStatCache = new ApparelStatCache(GameComponent_Outfitter.GetCache(this._pawn));
            List<Apparel> allApparels = new List<Apparel>(
                this._pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel).OfType<Apparel>());
            foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonists)
            {
                foreach (Apparel pawnApparel in pawn.apparel.WornApparel)
                {
                    if (pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(pawnApparel))
                    {
                        allApparels.Add(pawnApparel);
                    }
                }
            }

            allApparels = allApparels.Where(i => !ApparelUtility.CanWearTogether(this._apparel.def, i.def)).ToList();

            Rect groupRect = windowRect.ContractedBy(10f);
            groupRect.height -= 100;
            GUI.BeginGroup(groupRect);

            float apparelScoreWidth = 100f;
            float apparelGainWidth = 100f;
            float apparelLabelWidth = (groupRect.width - apparelScoreWidth - apparelGainWidth) / 3 - 8f - 8f;
            float apparelEquipedWidth = apparelLabelWidth;
            float apparelOwnerWidth = apparelLabelWidth;

            Rect itemRect = new Rect(groupRect.xMin + 4f, groupRect.yMin, groupRect.width - 8f, 28f);

            this.DrawLine(
                ref itemRect,
                null,
                "Apparel",
                apparelLabelWidth,
                null,
                "Equiped",
                apparelEquipedWidth,
                null,
                "Target",
                apparelOwnerWidth,
                "Score",
                apparelScoreWidth,
                "Gain",
                apparelGainWidth);

            groupRect.yMin += itemRect.height;
            Widgets.DrawLineHorizontal(groupRect.xMin, groupRect.yMin, groupRect.width);
            groupRect.yMin += 4f;
            groupRect.height -= 4f;
            groupRect.height -= Text.LineHeight * 1.2f * 3f;

            Rect viewRect = new Rect(
                groupRect.xMin,
                groupRect.yMin,
                groupRect.width - 16f,
                allApparels.Count * 28f + 16f);
            if (viewRect.height < groupRect.height)
            {
                groupRect.height = viewRect.height;
            }

            Rect listRect = viewRect.ContractedBy(4f);

            Widgets.BeginScrollView(groupRect, ref this.scrollPosition, viewRect);

            allApparels = allApparels.OrderByDescending(
                i =>
                    {
                        float g;
                        if (apparelStatCache.DIALOG_CalculateApparelScoreGain(i, out g))
                        {
                            return g;
                        }

                        return -1000f;
                    }).ToList();

            foreach (Apparel currentAppel in allApparels)
            {
                itemRect = new Rect(listRect.xMin, listRect.yMin, listRect.width, 28f);
                if (Mouse.IsOver(itemRect))
                {
                    GUI.DrawTexture(itemRect, TexUI.HighlightTex);
                    GUI.color = Color.white;
                }

                Pawn equiped = null;
                Pawn target = null;

                foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonists)
                {
                    foreach (Apparel a in pawn.apparel.WornApparel)
                    {
                        if (a == currentAppel)
                        {
                            equiped = pawn;
                            break;
                        }
                    }

                    // foreach (Apparel a in mapComponent.GetCache(pawn).targetApparel)
                    // if (a == currentAppel)
                    // {
                    // target = pawn;
                    // break;
                    // }
                    if (equiped != null && target != null)
                    {
                        break;
                    }
                }

                float gain;
                if (apparelStatCache.DIALOG_CalculateApparelScoreGain(currentAppel, out gain))
                {
                    this.DrawLine(
                        ref itemRect,
                        currentAppel,
                        currentAppel.LabelCap,
                        apparelLabelWidth,
                        equiped,
                        equiped == null ? null : equiped.LabelCap,
                        apparelEquipedWidth,
                        target,
                        target == null ? null : target.LabelCap,
                        apparelOwnerWidth,
                        apparelStatCache.ApparelScoreRaw(currentAppel, this._pawn).ToString("N5"),
                        apparelScoreWidth,
                        gain.ToString("N5"),
                        apparelGainWidth);
                }
                else
                {
                    this.DrawLine(
                        ref itemRect,
                        currentAppel,
                        currentAppel.LabelCap,
                        apparelLabelWidth,
                        equiped,
                        equiped == null ? null : equiped.LabelCap,
                        apparelEquipedWidth,
                        target,
                        target == null ? null : target.LabelCap,
                        apparelOwnerWidth,
                        apparelStatCache.ApparelScoreRaw(currentAppel, this._pawn).ToString("N5"),
                        apparelScoreWidth,
                        "No Allow",
                        apparelGainWidth);
                }

                listRect.yMin = itemRect.yMax;
            }

            Widgets.EndScrollView();

            Widgets.DrawLineHorizontal(groupRect.xMin, groupRect.yMax, groupRect.width);

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.EndGroup();
        }

        private void DrawLine(
            ref Rect itemRect,
            Apparel apparelThing,
            string apparelText,
            float textureWidth,
            Pawn apparelEquipedThing,
            string apparelEquipedText,
            float apparelEquipedWidth,
            Pawn apparelOwnerThing,
            string apparelOwnerText,
            float apparelOwnerWidth,
            string apparelScoreText,
            float apparelScoreWidth,
            string apparelGainText,
            float apparelGainWidth)
        {
            Rect fieldRect;
            if (apparelThing != null)
            {
                fieldRect = new Rect(itemRect.xMin, itemRect.yMin, itemRect.height, itemRect.height);
                if (!string.IsNullOrEmpty(apparelText))
                {
                    TooltipHandler.TipRegion(fieldRect, apparelText);
                }

                if (apparelThing.def.DrawMatSingle != null && apparelThing.def.DrawMatSingle.mainTexture != null)
                {
                    Widgets.ThingIcon(fieldRect, apparelThing);
                }

                if (Widgets.ButtonInvisible(fieldRect))
                {
                    this.Close();
                    Find.MainTabsRoot.EscapeCurrentTab();
                    if (apparelEquipedThing != null)
                    {
                        Find.CameraDriver.JumpToVisibleMapLoc(apparelEquipedThing.PositionHeld);
                        Find.Selector.ClearSelection();
                        if (apparelEquipedThing.Spawned)
                        {
                            Find.Selector.Select(apparelEquipedThing);
                        }
                    }
                    else
                    {
                        Find.CameraDriver.JumpToVisibleMapLoc(apparelThing.PositionHeld);
                        Find.Selector.ClearSelection();
                        if (apparelThing.Spawned)
                        {
                            Find.Selector.Select(apparelThing);
                        }
                    }

                    return;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(apparelText))
                {
                    fieldRect = new Rect(itemRect.xMin, itemRect.yMin, textureWidth, itemRect.height);
                    Text.Anchor = TextAnchor.UpperLeft;
                    Widgets.Label(fieldRect, apparelText);
                }
            }

            itemRect.xMin += textureWidth;

            if (apparelEquipedThing != null)
            {
                fieldRect = new Rect(itemRect.xMin, itemRect.yMin, itemRect.height, itemRect.height);
                if (!string.IsNullOrEmpty(apparelEquipedText))
                {
                    TooltipHandler.TipRegion(fieldRect, apparelEquipedText);
                }

                if (apparelEquipedThing.def.DrawMatSingle != null
                    && apparelEquipedThing.def.DrawMatSingle.mainTexture != null)
                {
                    Widgets.ThingIcon(fieldRect, apparelEquipedThing);
                }

                if (Widgets.ButtonInvisible(fieldRect))
                {
                    this.Close();
                    Find.MainTabsRoot.EscapeCurrentTab();
                    Find.CameraDriver.JumpToVisibleMapLoc(apparelEquipedThing.PositionHeld);
                    Find.Selector.ClearSelection();
                    if (apparelEquipedThing.Spawned)
                    {
                        Find.Selector.Select(apparelEquipedThing);
                    }

                    return;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(apparelEquipedText))
                {
                    fieldRect = new Rect(itemRect.xMin, itemRect.yMin, apparelEquipedWidth, itemRect.height);
                    Text.Anchor = TextAnchor.UpperLeft;
                    Widgets.Label(fieldRect, apparelText);
                }
            }

            itemRect.xMin += apparelEquipedWidth;

            if (apparelOwnerThing != null)
            {
                fieldRect = new Rect(itemRect.xMin, itemRect.yMin, itemRect.height, itemRect.height);
                if (!string.IsNullOrEmpty(apparelOwnerText))
                {
                    TooltipHandler.TipRegion(fieldRect, apparelOwnerText);
                }

                if (apparelOwnerThing.def.DrawMatSingle != null
                    && apparelOwnerThing.def.DrawMatSingle.mainTexture != null)
                {
                    Widgets.ThingIcon(fieldRect, apparelOwnerThing);
                }

                if (Widgets.ButtonInvisible(fieldRect))
                {
                    this.Close();
                    Find.MainTabsRoot.EscapeCurrentTab();
                    Find.CameraDriver.JumpToVisibleMapLoc(apparelOwnerThing.PositionHeld);
                    Find.Selector.ClearSelection();
                    if (apparelOwnerThing.Spawned)
                    {
                        Find.Selector.Select(apparelOwnerThing);
                    }

                    return;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(apparelOwnerText))
                {
                    fieldRect = new Rect(itemRect.xMin, itemRect.yMin, apparelOwnerWidth, itemRect.height);
                    Text.Anchor = TextAnchor.UpperLeft;
                    Widgets.Label(fieldRect, apparelOwnerText);
                }
            }

            itemRect.xMin += apparelOwnerWidth;

            fieldRect = new Rect(itemRect.xMin, itemRect.yMin, apparelScoreWidth, itemRect.height);
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(fieldRect, apparelScoreText);
            if (apparelThing != null)
            {
                Text.Anchor = TextAnchor.UpperLeft;
                if (Widgets.ButtonInvisible(fieldRect))
                {
                    this.Close();
                    Find.MainTabsRoot.EscapeCurrentTab();
                    Find.WindowStack.Add(new Window_Pawn_ApparelDetail(this._pawn, apparelThing));
                    return;
                }
            }

            itemRect.xMin += apparelScoreWidth;

            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, apparelGainWidth, itemRect.height), apparelGainText);
            itemRect.xMin += apparelGainWidth;
        }
    }
}