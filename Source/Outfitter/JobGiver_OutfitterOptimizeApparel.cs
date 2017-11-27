﻿namespace Outfitter
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using JetBrains.Annotations;

    using RimWorld;

    using UnityEngine;

    using Verse;
    using Verse.AI;

    public static class JobGiver_OutfitterOptimizeApparel
    {
        public const int ApparelStatCheck = 3750;

        private const int ApparelOptimizeCheckIntervalMin = 6000;
        private const int ApparelOptimizeCheckIntervalMax = 9000;

      //  private const int ApparelOptimizeCheckIntervalMin = 9000;
      //  private const int ApparelOptimizeCheckIntervalMax = 12000;


        private const float MinScoreGainToCare = 0.09f;
      //  private const float MinScoreGainToCare = 0.15f;

        private static StringBuilder debugSb;

       // private static Apparel lastItem;

        public static void SetNextOptimizeTick([NotNull] Pawn pawn)
        {
            pawn.mindState.nextApparelOptimizeTick = Find.TickManager.TicksGame
                                                     + Random.Range(
                                                         ApparelOptimizeCheckIntervalMin,
                                                         ApparelOptimizeCheckIntervalMax);

            // pawn.GetApparelStatCache().recentApparel.Clear();
        }

        // private static NeededWarmth neededWarmth;
        public static bool TryGiveJob_Prefix(ref Job __result, Pawn pawn)
        {
            __result = null;
            if (pawn.outfits == null)
            {
                Log.ErrorOnce(
                    pawn + " tried to run JobGiver_OutfitterOptimizeApparel without an OutfitTracker",
                    5643897);
                return false;
            }

            if (pawn.Faction != Faction.OfPlayer)
            {
                Log.ErrorOnce("Non-colonist " + pawn + " tried to optimize apparel.", 764323);
                return false;
            }

            if (!DebugViewSettings.debugApparelOptimize)
            {
                if (Find.TickManager.TicksGame < pawn.mindState.nextApparelOptimizeTick)
                {
                    return false;
                }
            }
            else
            {
                debugSb = new StringBuilder();
                debugSb.AppendLine(string.Concat("Scanning for ", pawn, " at ", pawn.Position));
            }

            Outfit currentOutfit = pawn.outfits.CurrentOutfit;
            List<Apparel> wornApparel = pawn.apparel.WornApparel;

            foreach (Apparel ap in wornApparel)
            {
                ApparelStatCache conf = pawn.GetApparelStatCache();

                bool notAllowed = !currentOutfit.filter.Allows(ap)
                                  && pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(ap);

                bool shouldDrop = conf.ApparelScoreRaw(ap) < 0f
                                  && pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(ap);

                bool someoneWantsIt = pawn.GetApparelStatCache().ToDropList.ContainsKey(ap);

                if (notAllowed || shouldDrop || someoneWantsIt)
                {
                    __result = new Job(JobDefOf.RemoveApparel, ap) { haulDroppedApparel = true };
                    if (someoneWantsIt)
                    {
                        pawn.GetApparelStatCache().ToDropList[ap].mindState.nextApparelOptimizeTick = -5000;
                        pawn.GetApparelStatCache().ToDropList[ap].mindState.Notify_OutfitChanged();
                        pawn.GetApparelStatCache().ToDropList.Remove(ap);
                    }

                    return false;
                }
            }

            Thing thing = null;
            float score = 0f;
            List<Thing> list = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel);

            if (list.Count == 0)
            {
                SetNextOptimizeTick(pawn);
                return false;
            }

            foreach (Thing t in list)
            {
                Apparel apparel = (Apparel)t;
                if (!currentOutfit.filter.Allows(apparel))
                {
                    continue;
                }

                if (apparel.Map.slotGroupManager.SlotGroupAt(apparel.Position) == null)
                {
                    continue;
                }

                if (apparel.IsForbidden(pawn))
                {
                    continue;
                }

                float gain = pawn.ApparelScoreGain(apparel);

                // this blocks pawns constantly switching between the recent apparel, due to shifting calculations
                // not very elegant but working
                // if (pawn.GetApparelStatCache().recentApparel.Contains(apparel))
                // {
                // gain *= 0.01f;
                // }
                if (DebugViewSettings.debugApparelOptimize)
                {
                    debugSb.AppendLine(apparel.LabelCap + ": " + gain.ToString("F2"));
                }

                if (gain >= MinScoreGainToCare && gain >= score)
                {
                    if (ApparelUtility.HasPartsToWear(pawn, apparel.def))
                    {
                        if (pawn.CanReserveAndReach(apparel, PathEndMode.OnCell, pawn.NormalMaxDanger(), 1))
                        {
                            thing = apparel;
                            score = gain;
                        }
                    }
                }
            }

            if (DebugViewSettings.debugApparelOptimize)
            {
                debugSb.AppendLine("BEST: " + thing);
                Log.Message(debugSb.ToString());
                debugSb = null;
            }

            // New stuff
            if (false)
            {
                IEnumerable<Pawn> list2 = pawn.Map.mapPawns.FreeColonistsSpawned.Where(x => x.IsColonistPlayerControlled);
                foreach (Apparel ap in wornApparel)
                {
                    foreach (Pawn otherPawn in list2)
                    {
                        foreach (Apparel otherAp in otherPawn.apparel.WornApparel.Where(
                            x => !ApparelUtility.CanWearTogether(ap.def, x.def, pawn.RaceProps.body)))
                        {
                            float gain = pawn.ApparelScoreGain(otherAp);
                            float otherGain = otherPawn.ApparelScoreGain(ap);
                            if (gain > MinScoreGainToCare && gain >= score && otherGain > MinScoreGainToCare)
                            {
                                score = gain;
                                Log.Message(
                                    "OUTFITTER: " + pawn + " wants " + otherAp + " currently worn by " + otherPawn
                                    + ", scores: " + gain + " - " + otherGain + " - " + score);

                                if (!otherPawn.GetApparelStatCache().ToDropList.ContainsKey(ap))
                                {
                                    otherPawn.GetApparelStatCache().ToDropList.Add(otherAp, otherPawn);
                                    otherPawn.mindState.nextApparelOptimizeTick = -5000;
                                    otherPawn.mindState.Notify_OutfitChanged();
                                }
                            }
                        }
                    }
                }
            }

            if (thing == null)
            {
                SetNextOptimizeTick(pawn);
                return false;
            }

            // foreach (Apparel apparel in wornApparel)
            // {
            // pawn.GetApparelStatCache().recentApparel.Add(apparel);
            // }
            __result = new Job(JobDefOf.Wear, thing);
            return false;
        }
    }
}