﻿using System.Collections.Generic;
using Verse;

namespace Outfitter
{
    public class SaveablePawn : IExposable
    {
        // Exposed members
        public Pawn Pawn;

        public bool TargetTemperaturesOverride;
        public bool AddWorkStats = true;
        public bool AddIndividualStats= true;

        public FloatRange TargetTemperatures;

        public List<Saveable_Pawn_StatDef> Stats = new List<Saveable_Pawn_StatDef>();

        //  public SaveablePawn(Pawn pawn)
        //    {
        //        Pawn = pawn;
        //        Stats = new List<Saveable_Pawn_StatDef>();
        //        _lastStatUpdate = -5000;
        //        _lastTempUpdate = -5000;
        //    }


        public void ExposeData()
        {
            Scribe_References.LookReference(ref Pawn, "Pawn");
          Scribe_Values.LookValue(ref TargetTemperaturesOverride, "targetTemperaturesOverride");
            Scribe_Values.LookValue(ref TargetTemperatures, "TargetTemperatures");
            Scribe_Collections.LookList(ref Stats, "Stats", LookMode.Deep);
            Scribe_Values.LookValue(ref AddWorkStats, "AddWorkStats", true);
            Scribe_Values.LookValue(ref AddIndividualStats, "AddIndividualStats", true);
        }
    }
}