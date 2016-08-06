﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Outfitter
{
    public class ITab_Pawn_Outfitter : ITab
    {
        private Vector2 _scrollPosition = Vector2.zero;

        public static Texture2D resetButton  = ContentFinder<Texture2D>.Get( "reset" ),
                                deleteButton = ContentFinder<Texture2D>.Get( "delete" ),
                                addButton    = ContentFinder<Texture2D>.Get( "add" );

        public ITab_Pawn_Outfitter()
        {
            size = new Vector2( 360f, 600f );
            labelKey = "OutfitterTab";
        }

        protected override void FillTab()
        {
            // main canvas
            Rect canvas = new Rect( 0f, 0f, size.x, size.y ).ContractedBy( 20f );
            GUI.BeginGroup( canvas );
            Vector2 cur = Vector2.zero;

            // header
            Rect tempHeaderRect = new Rect( cur.x, cur.y, canvas.width, 30f );
            cur.y += 30f;
            Text.Anchor = TextAnchor.LowerLeft;
            Widgets.Label( tempHeaderRect, "PreferedTemperature".Translate() );
            Text.Anchor = TextAnchor.UpperLeft;

            // line
            GUI.color = Color.grey;
            Widgets.DrawLineHorizontal( cur.x, cur.y, canvas.width );
            GUI.color = Color.white;

            // some padding
            cur.y += 10f;

            // temperature slider
            ApparelStatCache pawnStatCache = SelPawn.GetApparelStatCache();
            FloatRange targetTemps = pawnStatCache.TargetTemperatures;
            FloatRange minMaxTemps = ApparelStatsHelper.MinMaxTemperatureRange;

            Rect sliderRect = new Rect( cur.x, cur.y, canvas.width - 20f, 40f );
            Rect tempResetRect = new Rect( sliderRect.xMax + 4f, cur.y + 10f, 16f, 16f );
            cur.y += 60f; // includes padding 

            // current temperature settings
            GUI.color = pawnStatCache.targetTemperaturesOverride ? Color.white : Color.grey;
            Widgets_FloatRange.FloatRange( sliderRect, 123123123, ref targetTemps, minMaxTemps, ToStringStyle.Temperature );
            GUI.color = Color.white;

            if ( Math.Abs( targetTemps.min - SelPawn.GetApparelStatCache().TargetTemperatures.min) > 1e-4 ||
                 Math.Abs( targetTemps.max - SelPawn.GetApparelStatCache().TargetTemperatures.max) > 1e-4 )
            {
                SelPawn.GetApparelStatCache().TargetTemperatures = targetTemps;
            }

            if ( pawnStatCache.targetTemperaturesOverride )
            {
                if ( Widgets.ButtonImage( tempResetRect, resetButton ) )
                {
                    pawnStatCache.targetTemperaturesOverride = false;
                    pawnStatCache.UpdateTemperatureIfNecessary( true );
                }
                TooltipHandler.TipRegion( tempResetRect, "TemperatureRangeReset".Translate() );
            }
            

            // header
            Rect statsHeaderRect = new Rect( cur.x, cur.y, canvas.width, 30f );
            cur.y += 30f;
            Text.Anchor = TextAnchor.LowerLeft;
            Text.Font = GameFont.Small;
            Widgets.Label( statsHeaderRect, "PreferredStats".Translate() );
            Text.Anchor = TextAnchor.UpperLeft;

            // add button
            Rect addStatRect = new Rect( statsHeaderRect.xMax - 16f, statsHeaderRect.yMin + 10f, 16f, 16f );
            if( Widgets.ButtonImage( addStatRect, addButton ) )
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach( StatDef def in SelPawn.NotYetAssignedStatDefs() )
                {
                    options.Add( new FloatMenuOption( def.LabelCap, delegate
                    {
                        SelPawn.GetApparelStatCache()
                               .StatCache.Insert( 0, new ApparelStatCache.StatPriority( def, 0f, StatAssignment.Manual ) );
                    } ) );
                }
                Find.WindowStack.Add( new FloatMenu( options ) );
            }
            TooltipHandler.TipRegion( addStatRect, "StatPriorityAdd".Translate() );

            // line
            GUI.color = Color.grey; 
            Widgets.DrawLineHorizontal( cur.x, cur.y, canvas.width );
            GUI.color = Color.white;

            // some padding
            cur.y += 10f;

            // main content in scrolling view
            Rect contentRect = new Rect( cur.x, cur.y, canvas.width, canvas.height - cur.y );
            Rect viewRect = contentRect;
            viewRect.height = SelPawn.GetApparelStatCache().StatCache.Count * 30f + 10f;
            if ( viewRect.height > contentRect.height )
            {
                viewRect.width -= 20f;
            }

            Widgets.BeginScrollView( contentRect, ref _scrollPosition, viewRect );
            GUI.BeginGroup( viewRect );
            cur = Vector2.zero;

            // none label
            if( !SelPawn.GetApparelStatCache().StatCache.Any() )
            {
                Rect noneLabel = new Rect( cur.x, cur.y, viewRect.width, 30f );
                GUI.color = Color.grey;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label( noneLabel, "None".Translate() );
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                cur.y += 30f;
            }
            else
            {
                // legend kind of thingy.
                Rect legendRect = new Rect( cur.x + (viewRect.width - 24) / 2, cur.y, (viewRect.width - 24) / 2, 20f );
                Text.Font = GameFont.Tiny;
                GUI.color = Color.grey;
                Text.Anchor = TextAnchor.LowerLeft;
                Widgets.Label( legendRect, "-10" );
                Text.Anchor = TextAnchor.LowerRight;
                Widgets.Label( legendRect, "10" );
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
                cur.y += 15f;
                
                // stat weight sliders
                foreach( ApparelStatCache.StatPriority stat in SelPawn.GetApparelStatCache().StatCache )
                {
                    bool stop_UI;
                    ApparelStatCache.DrawStatRow( ref cur, viewRect.width, stat, SelPawn, out stop_UI );
                    if ( stop_UI )
                    {
                        // DrawStatRow can change the StatCache, invalidating the loop. So if it does that, stop looping - we'll redraw on the next tick.
                        break;
                    }
                }
            }

            GUI.EndGroup();
            Widgets.EndScrollView();

            GUI.EndGroup();
        }

        public override bool IsVisible
        {
            get
            {
                Pawn selectedPawn = SelPawn;

                // thing selected is a pawn
                if ( selectedPawn == null )
                {
                    return false;
                }

                // of this colony
                if ( selectedPawn.Faction != Faction.OfPlayer )
                {
                    return false;
                }

                // and has apparel (that should block everything without apparel, animals, bots, that sort of thing)
                if ( selectedPawn.apparel == null )
                {
                    return false;
                }
                return true;
            }
        }
    }
}
