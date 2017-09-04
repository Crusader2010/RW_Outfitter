﻿//namespace Outfitter
//{
//    public class Outfitter_ModBase : HugsLib.ModBase
//    {
//        public override string ModIdentifier { get { return "Outfitter"; } }
//    }

using System.Linq;

using Harmony;

using Outfitter;

using RimWorld;

using Verse;

using JobGiver_OptimizeApparel = Outfitter.JobGiver_OptimizeApparel;

[StaticConstructorOnStartup]
internal class HarmonyPatches
{

    #region Public Constructors

    static HarmonyPatches()
    {
        HarmonyInstance harmony = HarmonyInstance.Create("com.outfitter.rimworld.mod");

        harmony.Patch(
            AccessTools.Method(typeof(InspectPaneUtility), "DoTabs"),
            new HarmonyMethod(typeof(TabsPatch), nameof(TabsPatch.DoTabs_Prefix)),
            null);

        harmony.Patch(
            AccessTools.Method(typeof(RimWorld.JobGiver_OptimizeApparel), "TryGiveJob"),
            new HarmonyMethod(
                typeof(JobGiver_OptimizeApparel),
                nameof(JobGiver_OptimizeApparel.TryGiveJob_Prefix)),
            null);

       // harmony.Patch(
       //     AccessTools.Method(typeof(ITab_Bills), "FillTab"),
       //     new HarmonyMethod(
       //         typeof(ITab_Bills_Patch),
       //         nameof(ITab_Bills_Patch.FillTab_Prefix)),
       //     null);


        Log.Message(
        "Outfitter successfully completed " + harmony.GetPatchedMethods().Count() + " patches with harmony.");
    }

    #endregion Public Constructors

}