﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommunityCoreLibrary;
using RimWorld;
using Verse;

namespace Outfitter
{
  public class InjectDetour : SpecialInjector 
  {
     public override bool Inject()
     {
         // detour apparel selection methods
         MethodInfo source = typeof (JobGiver_OptimizeApparel).GetMethod( "ApparelScoreGain",
                                                                          BindingFlags.Static | BindingFlags.Public );
         MethodInfo destination = typeof (ApparelStatsHelper).GetMethod( "ApparelScoreGain",
                                                                         BindingFlags.Static | BindingFlags.Public );
     
         Detours.TryDetourFromTo( source, destination );
            return true;

        }
    }

    public class InjectTab : SpecialInjector
    {
        public override bool Inject()
        {
            // inject ITab into all humanlikes
            foreach ( ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading.Where( td => td.category == ThingCategory.Pawn && td.race.Humanlike ) )
            {
                if( def.inspectorTabs == null || def.inspectorTabs.Count == 0 )
                {
                    def.inspectorTabs = new List<Type>();
                    def.inspectorTabsResolved = new List<ITab>();
                }
                if( def.inspectorTabs.Contains( typeof( ITab_Pawn_Outfitter ) ) )
                {
                    return false;
                }

                def.inspectorTabs.Add( typeof( ITab_Pawn_Outfitter ) );
                def.inspectorTabsResolved.Add( ITabManager.GetSharedInstance( typeof( ITab_Pawn_Outfitter ) ) );
            }

            return true;
        }
    }

    public class ITabInjector : SpecialInjector
    {
        #region Methods

        public override bool Inject()
        {
            // get reference to lists of itabs
            var itabs = ThingDefOf.Human.inspectorTabs;
            var itabsResolved = ThingDefOf.Human.inspectorTabsResolved;

#if DEBUG
            Log.Message("Inspector tab types on humans:");
            foreach (var tab in itabs)
            {
                Log.Message("\t" + tab.Name);
            }
            Log.Message("Resolved tab instances on humans:");
            foreach (var tab in itabsResolved)
            {
                Log.Message("\t" + tab.labelKey.Translate());
            }
#endif

            // Vanilla Replacement

            // replace ITab in the unresolved list
            var index = itabs.IndexOf(typeof(ITab_Pawn_Gear));
            if (index != -1)
            {
                itabs.Remove(typeof(ITab_Pawn_Gear));
                itabs.Insert(index, typeof(ITab_Pawn_GearModded));
            }

            // replace resolved ITab, if needed.
            var oldGearTab = ITabManager.GetSharedInstance(typeof(ITab_Pawn_Gear));
            var newGearTab = ITabManager.GetSharedInstance(typeof(ITab_Pawn_GearModded));
            if (!itabsResolved.NullOrEmpty() && itabsResolved.Contains(oldGearTab))
            {
                int resolvedIndex = itabsResolved.IndexOf(oldGearTab);
                itabsResolved.Insert(resolvedIndex, newGearTab);
                itabsResolved.Remove(oldGearTab);
            }

            return true;
        }

        #endregion Methods
    }
}
