global using AWO.CustomFields;
using AWO.Jsons;
using AWO.Modules.TerminalSerialLookup;
using AWO.Modules.WEE;
//using AWO.Modules.WOE;
using AWO.Sessions;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using GTFO.API;
using HarmonyLib;
using UnityEngine;

namespace AWO;

[BepInPlugin("GTFO.AWO", "AWO", VersionInfo.Version)]
[BepInDependency("GTFO.InjectLib", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
internal class EntryPoint : BasePlugin
{
    public static System.Random SessionRand { get; private set; } = new();
    public struct Coroutines
    {
        public static float CountdownStarted { get; set; }
        public static float TPFStarted { get; set; }
    }
    public struct TimerMods
    {
        public static float TimeModifier { get; set; }
        public static Color TimerColor { get; set; }
        public static float SpeedModifier { get; set; }
        public static LocaleText CountupText { get; set; }
    }

    public unsafe override void Load()
    {
        WardenEventExt.Initialize();
        //WardenObjectiveExt.Initialize();

        new Harmony("AWO.Harmony").PatchAll();

        AssetAPI.OnStartupAssetsLoaded += OnStartupAssetsLoaded;
        LevelAPI.OnBuildDone += OnBuildDone;

        WOEventDataFields.Init();
        //WODataBlockFields.Init();
        SerialLookupManager.Init();

        Logger.Info("AWO is done loading! --Amorously");
    }

    private void OnStartupAssetsLoaded()
    {
        BlackoutState.AssetLoaded();
        LevelFailUpdateState.AssetLoaded();
    }

    private void OnBuildDone()
    {
        int seed = RundownManager.GetActiveExpeditionData().sessionSeed;
        SessionRand = new(seed);
        Logger.Info($"SessionSeed {seed}");
    }
}