global using AWO.CustomFields;
using AWO.Jsons;
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

[BepInPlugin("GTFO.AWO", "AWO", VersionInfo.Version)]
[BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("GTFO.InjectLib", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("MTFO.Extension.PartialBlocks", BepInDependency.DependencyFlags.SoftDependency)]
internal class EntryPoint : BasePlugin
{
    public static bool PartialDataIsLoaded { get; private set; } = false;
    public static BlackoutState BlackoutState { get; private set; } = new();
    public static SessionRandReplicator SessionRand { get; private set; } = new();
    public struct Coroutines
    {
        public static float CountdownStarted { get; set; }
    }
    public struct TimerMods
    {
        public static float TimeModifier { get; set; }
        public static Color TimerColor { get; set; }
        public static float SpeedModifier { get; set; }
        public static LocaleText TimerTitleText { get; set; }
        public static LocaleText TimerBodyText { get; set; }
    }

    public unsafe override void Load()
    {
        if (IL2CPPChainloader.Instance.Plugins.TryGetValue("MTFO.Extension.PartialBlocks", out var plugin) && plugin.Metadata.Version.CompareTo(new(1, 5, 2)) >= 0)
        {
            Logger.Debug("Flowaria's PartialData v1.5.2(+) support found");
            PartialDataIsLoaded = true;
        }

        Configuration.Init();
        WardenEventExt.Initialize();
        //WardenObjectiveExt.Initialize();

        new Harmony("AWO.Harmony").PatchAll();

        AssetAPI.OnStartupAssetsLoaded += () => LevelFailUpdateState.AssetLoaded();
        LevelAPI.OnBuildDone += OnBuildDone;
        LevelAPI.OnLevelCleanup += OnLevelCleanup;

        WOEventDataFields.Init();
        //WODataBlockFields.Init();
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
        WOManager.m_exitEventsTriggered &= false;
        BlackoutState.Cleanup();
        SessionRand.Cleanup();
    }
}