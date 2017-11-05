﻿namespace Outfitter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using JetBrains.Annotations;

    using Verse;

    public class GameComponent_Outfitter : GameComponent
    {
        [NotNull]
        public static List<SaveablePawn> _pawnCache = new List<SaveablePawn>();

        public static bool updated;

        public GameComponent_Outfitter()
        {
        }

        // ReSharper disable once UnusedMember.Global
        public GameComponent_Outfitter(Game game)
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading.Where(
                td => td.category == ThingCategory.Pawn && td.race.Humanlike))
            {
                if (def.inspectorTabs == null || def.inspectorTabs.Count == 0)
                {
                    def.inspectorTabs = new List<Type>();
                    def.inspectorTabsResolved = new List<InspectTabBase>();
                }

                if (def.inspectorTabs.Contains(typeof(ITab_Pawn_Outfitter)))
                {
                    return;
                }

                def.inspectorTabs.Add(typeof(ITab_Pawn_Outfitter));
                def.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(ITab_Pawn_Outfitter)));
            }
        }

        public static SaveablePawn GetCache(Pawn pawn)
        {
            foreach (SaveablePawn c in _pawnCache.Where(c => c.Pawn == pawn))
            {
                return c;
            }

            SaveablePawn n = new SaveablePawn { Pawn = pawn };
            _pawnCache.Add(n);
            return n;

            // if (!PawnApparelStatCaches.ContainsKey(pawn))
            // {
            // PawnApparelStatCaches.Add(pawn, new StatCache(pawn));
            // }
            // return PawnApparelStatCaches[pawn];
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref _pawnCache, "Pawns", LookMode.Deep);
        }
    }
}