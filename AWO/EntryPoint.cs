using AmorLib.Dependencies;
using AmorLib.Utils.JsonElementConverters;
using AWO.Modules.TSL;
using AWO.Modules.WEE;
//using AWO.Modules.WOE;
using AWO.Sessions;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using GTFO.API;
using HarmonyLib;
using UnityEngine;

namespace AWO;

[BepInPlugin("GTFO.AWO", "AWO", "2.4.2")]
[BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("Amor.AmorLib", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(InjectLib_Wrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(PData_Wrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
internal sealed class EntryPoint : BasePlugin
{
    /* Globals */
    public static BlackoutState BlackoutState { get; private set; } = new();
    public static SessionRandReplicator SessionRand { get; private set; } = new();
    
    internal static class Coroutines
    {
        public static float CountdownStarted { get; set; }
    }
    internal static class TimerMods
    {
        public static float TimeModifier { get; set; }
        public static Color TimerColor { get; set; }
        public static float SpeedModifier { get; set; }
        public static LocaleText TimerTitleText { get; set; }
        public static LocaleText TimerBodyText { get; set; }
    }

    public unsafe override void Load()
    {
        Configuration.Init();
        WardenEventExt.Initialize();
        // WardenObjectiveExt.Initialize();

        new Harmony("AWO.Harmony").PatchAll();

        AssetAPI.OnStartupAssetsLoaded += () => LevelFailUpdateState.AssetLoaded();
        LevelAPI.OnBuildDone += OnBuildDone;
        LevelAPI.OnLevelCleanup += OnLevelCleanup;

        WOEventDataFields.Init();
        // WODataBlockFields.Init();
        SerialLookupManager.Init();

        Logger.Info("AWO is done loading!");
    }

    private void OnBuildDone()
    {
        BlackoutState.Setup();
        SessionRand.Setup(1u, RundownManager.GetActiveExpeditionData().sessionSeed);
    }

    private void OnLevelCleanup()
    {
        WOManager.m_exitEventsTriggered = false;
        BlackoutState.Cleanup();
        SessionRand.Cleanup();
    }
}