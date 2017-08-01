﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Outfitter.Helper;
using RimWorld;
using UnityEngine;
using Verse;

namespace Outfitter.Window
{
    public class Dialog_ManageOutfitsOutfitter : Verse.Window
    {
        private const float TopAreaHeight = 40f;

        private const float TopButtonHeight = 35f;

        private const float TopButtonWidth = 150f;

        private static StatDef[] _allDefs;

        private static ThingFilter _apparelGlobalFilter;

        private static readonly Regex ValidNameRegex = new Regex("^[a-zA-Z0-9 '\\-]*$");

        private Vector2 _scrollPosition;

        private Outfit _selOutfitInt;

        public Dialog_ManageOutfitsOutfitter(Outfit selectedOutfit)
        {
            forcePause = true;
            doCloseX = true;
            closeOnEscapeKey = true;
            doCloseButton = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            if (_apparelGlobalFilter == null)
            {
                _apparelGlobalFilter = new ThingFilter();
                _apparelGlobalFilter.SetAllow(ThingCategoryDefOf.Apparel, true);
            }

            SelectedOutfit = selectedOutfit;
        }

        private Outfit SelectedOutfit
        {
            get
            {
                return _selOutfitInt;
            }

            set
            {
                CheckSelectedOutfitHasName();
                _selOutfitInt = value;
            }
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(700f, 700f);
            }
        }

        private void CheckSelectedOutfitHasName()
        {
            if (SelectedOutfit != null && SelectedOutfit.label.NullOrEmpty())
            {
                SelectedOutfit.label = "Unnamed";
            }
        }

        // StorageSearch
        private string searchText = string.Empty;

        private bool isFocused;

        [Detour(typeof(Dialog_ManageOutfits), bindingFlags = BindingFlags.Instance | BindingFlags.Public)]
        public override void DoWindowContents(Rect inRect)
        {
            float num = 0f;
            Rect rect = new Rect(0f, 0f, 150f, 35f);
            num += 150f;
            if (Widgets.ButtonText(rect, "SelectOutfit".Translate(), true, false))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                foreach (Outfit current in Current.Game.outfitDatabase.AllOutfits)
                {
                    Outfit localOut = current;
                    list.Add(
                        new FloatMenuOption(
                            localOut.label,
                            delegate { SelectedOutfit = localOut; },
                            MenuOptionPriority.Medium,
                            null,
                            null));
                }

                Find.WindowStack.Add(new FloatMenu(list));
            }

            num += 10f;
            Rect rect2 = new Rect(num, 0f, 150f, 35f);
            num += 150f;
            if (Widgets.ButtonText(rect2, "NewOutfit".Translate(), true, false))
            {
                SelectedOutfit = Current.Game.outfitDatabase.MakeNewOutfit();
            }

            num += 10f;
            Rect rect3 = new Rect(num, 0f, 150f, 35f);
            num += 150f;
            if (Widgets.ButtonText(rect3, "DeleteOutfit".Translate(), true, false, true))
            {
                List<FloatMenuOption> list2 = new List<FloatMenuOption>();
                foreach (Outfit current2 in Current.Game.outfitDatabase.AllOutfits)
                {
                    Outfit localOut = current2;
                    list2.Add(
                        new FloatMenuOption(
                            localOut.label,
                            delegate
                                {
                                    AcceptanceReport acceptanceReport = Current.Game.outfitDatabase.TryDelete(localOut);
                                    if (!acceptanceReport.Accepted)
                                    {
                                        Messages.Message(acceptanceReport.Reason, MessageSound.RejectInput);
                                    }
                                    else if (localOut == SelectedOutfit)
                                    {
                                        SelectedOutfit = null;
                                    }
                                },
                            MenuOptionPriority.Medium,
                            null,
                            null,
                            0f,
                            null));
                }

                Find.WindowStack.Add(new FloatMenu(list2));
            }

            Rect rect4 = new Rect(0f, 40f, 300f, inRect.height - 40f - CloseButSize.y).ContractedBy(10f);
            if (SelectedOutfit == null)
            {
                GUI.color = Color.grey;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect4, "NoOutfitSelected".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return;
            }

            GUI.BeginGroup(rect4);
            Rect rect5 = new Rect(0f, 0f, 180f, 30f);
            DoNameInputRect(rect5, ref SelectedOutfit.label, 30);

            Rect clearSearchRect = new Rect(rect4.width - 20f, (29f - 14f) / 2f, 14f, 14f);
            bool shouldClearSearch = Widgets.ButtonImage(clearSearchRect, Widgets.CheckboxOffTex);

            Rect searchRect = new Rect(rect5.width + 10f, 0f, rect4.width - rect5.width - 10f, 29f);
            var watermark = (searchText != string.Empty || isFocused) ? searchText : "Search";

            bool escPressed = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape;
            bool clickedOutside = !Mouse.IsOver(searchRect) && Event.current.type == EventType.MouseDown;

            if (!isFocused)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.6f);
            }

            GUI.SetNextControlName("StorageSearchInput");
            string searchInput = Widgets.TextField(searchRect, watermark);
            GUI.color = Color.white;

            if (isFocused)
            {
                searchText = searchInput;
            }

            if ((GUI.GetNameOfFocusedControl() == "StorageSearchInput" || isFocused) && (escPressed || clickedOutside))
            {
                GUIUtility.keyboardControl = 0;
                isFocused = false;
            }
            else if (GUI.GetNameOfFocusedControl() == "StorageSearchInput" && !isFocused)
            {
                isFocused = true;
            }

            if (shouldClearSearch)
            {
                searchText = string.Empty;
            }

            UIHighlighter.HighlightOpportunity(rect, "StoragePriority");

            // if (_apparelGlobalFilter != null)
            // {
            // parentFilter = _apparelGlobalFilter;
            // }
            // Rect rect5a = new Rect(0f, 35f, rect4.width, rect4.height - 35f);
            // HelperThingFilterUI.DoThingFilterConfigWindow(rect5a, ref this._scrollPosition, SelectedOutfit.filter, parentFilter, 8, searchText);
            Rect rect6 = new Rect(0f, 40f, rect4.width, rect4.height - 45f - 10f);

            // fix for the filter
            if (_apparelGlobalFilter == null)
            {
                _apparelGlobalFilter = new ThingFilter();
                _apparelGlobalFilter.SetAllow(ThingCategoryDefOf.Apparel, true);
            }

            var parentFilter = _apparelGlobalFilter;

            HelperThingFilterUI.DoThingFilterConfigWindow(
                rect6,
                ref _scrollPosition,
                SelectedOutfit.filter,
                parentFilter,
                8,
                searchText);

            // ThingFilterUI.DoThingFilterConfigWindow(rect6, ref _scrollPosition, SelectedOutfit.filter, _apparelGlobalFilter, 16);
            GUI.EndGroup();

            rect4 = new Rect(300f, 40f, inRect.width - 300f, inRect.height - 40f - CloseButSize.y).ContractedBy(10f);
            GUI.BeginGroup(rect4);

            rect6 = new Rect(0f, 40f, rect4.width, rect4.height - 45f - 10f);

            // DoStatsInput(rect6, ref _scrollPositionStats, saveout.Stats);
            GUI.EndGroup();
        }

        public override void PreClose()
        {
            base.PreClose();
            CheckSelectedOutfitHasName();
        }

        private static void DoNameInputRect(Rect rect, ref string name, int maxLength)
        {
            string text = Widgets.TextField(rect, name);
            if (text.Length <= maxLength && ValidNameRegex.IsMatch(text))
            {
                name = text;
            }
        }

        public static void DoStatsInput(Rect rect, ref Vector2 scrollPosition, List<Saveable_Pawn_StatDef> stats)
        {
            Widgets.DrawMenuSection(rect, true);
            Text.Font = GameFont.Tiny;
            float num = rect.width - 2f;
            Rect rect2 = new Rect(rect.x + 1f, rect.y + 1f, num / 2f, 24f);
            if (Widgets.ButtonText(rect2, "ClearAll".Translate(), true, false)) stats.Clear();

            rect.yMin = rect2.yMax;
            rect2 = new Rect(rect.x + 5f, rect.y + 1f, rect.width - 2f - 16f - 8f, 20f);

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(rect2, "-100%");

            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(rect2, "0%");

            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(rect2, "100%");

            rect.yMin = rect2.yMax;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            Rect position = new Rect(rect2.xMin + rect2.width / 2, rect.yMin + 5f, 1f, rect.height - 10f);
            GUI.DrawTexture(position, BaseContent.GreyTex);

            rect.width -= 2;
            rect.height -= 2;

            List<StatDef> sortedDefs = new List<StatDef>();

            _allDefs = DefDatabase<StatDef>.AllDefs.OrderBy(i => i.label.ToString()).ThenBy(i => i.category.defName)
                .ToArray();

            // _allDefs = DefDatabase<StatDef>.AllDefs.OrderBy(i => i.category.defName).ThenBy(i => i.defName).ToArray();
            foreach (StatDef statDef in _allDefs)
            {
                if (!statDef.defName.Equals("LeatherAmount") && !statDef.defName.Equals("MeatAmount")
                    && !statDef.defName.Equals("EatingSpeed") && !statDef.defName.Equals("MinimumHandlingSkill"))
                {
                    if (statDef.category.defName.Equals("Basics") || statDef.category.defName.Equals("BasicsPawn")
                        || statDef.category.defName.Equals("Apparel") || statDef.category.defName.Equals("Weapon")
                        || statDef.category.defName.Equals("PawnCombat")
                        || statDef.category.defName.Equals("PawnSocial") || statDef.category.defName.Equals("PawnMisc")
                        || statDef.category.defName.Equals("PawnWork") // check
                    ) sortedDefs.Add(statDef);
                }
            }

            Rect viewRect = new Rect(
                rect.xMin,
                rect.yMin,
                rect.width - 16f,
                sortedDefs.Count * Text.LineHeight * 1.2f + stats.Count * 60);

            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            Rect rect6 = viewRect.ContractedBy(4f);

            rect6.yMin += 12f;

            Listing_Standard listingStandard = new Listing_Standard(rect6);
            listingStandard.ColumnWidth = rect6.width;

            foreach (StatDef stat in sortedDefs) DrawStat(stats, listingStandard, stat);

            listingStandard.End();

            Widgets.EndScrollView();
        }

        private static void DrawStat(List<Saveable_Pawn_StatDef> stats, Listing_Standard listingStandard, StatDef stat)
        {
            Saveable_Pawn_StatDef outfitStat = stats.FirstOrDefault(i => i.Stat == stat);
            bool active = outfitStat != null;
            listingStandard.CheckboxLabeled(stat.label, ref active);

            if (active)
            {
                if (outfitStat == null)
                {
                    outfitStat = new Saveable_Pawn_StatDef();
                    outfitStat.Stat = stat;
                    outfitStat.Weight = 0;
                }

                if (!stats.Contains(outfitStat)) stats.Add(outfitStat);

                outfitStat.Weight = listingStandard.Slider(outfitStat.Weight, -1f, 1f);
            }
            else
            {
                if (stats.Contains(outfitStat)) stats.Remove(outfitStat);
                outfitStat = null;
            }
        }
    }
}