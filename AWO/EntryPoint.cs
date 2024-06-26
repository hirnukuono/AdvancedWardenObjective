global using AWO.CustomFields;
using AWO.Jsons;
using AWO.Modules.WEE;
using AWO.Modules.WOE;
using AWO.Sessions;
using AWO.WEE.Events.Level;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using ChainedPuzzles;
using GameData;
using GTFO.API;
using HarmonyLib;
using LevelGeneration;
using UnityEngine;

namespace AWO;

[BepInPlugin("GTFO.AWO", "AWO", VersionInfo.Version)]
[BepInDependency("GTFO.InjectLib", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
internal class EntryPoint : BasePlugin
{
    public static HashSet<int> ActiveEventLoops { get; set; } = new();
    public static HashSet<LG_WorldEventNavMarker> NavMarkers { get; set; } = new();
    public struct Coroutines
    {
        public static float CountdownStarted { get; set; }
        public static float TPFStarted { get; set; }
        public static float IOTStarted { get; set; }
        public static float DOTStarted { get; set; }
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
        WardenObjectiveExt.Initialize();

        new Harmony("AWO.Harmony").PatchAll();

        AssetAPI.OnStartupAssetsLoaded += AssetAPI_OnStartupAssetsLoaded;

        WOEventDataFields.Init();
        WODataBlockFields.Init();
    }

    private void AssetAPI_OnStartupAssetsLoaded()
    {
        BlackoutState.AssetLoaded();
        LevelFailUpdateState.AssetLoaded();
    }
    public class AWOTerminalCommands
    {
        public WEE_EventData data;
        public bool hidden;
        public bool used;
        public AWOTerminalCommands(WEE_EventData e)
        {
            used = false;
            hidden = false;
            data = e;
        }
    }
    public static List<AWOTerminalCommands> AWOCommandList = new();

    /*
    public static System.Collections.IEnumerator DoAwoCommand(LG_ComputerTerminalCommandInterpreter __instance, AWOTerminalCommands awocmd)
    {
        Debug.Log($"AdvancedWardenObjective - command {awocmd.data.AddTerminalCommand.Command.ToUpper()} coroutine is running");

        if (awocmd.data.AddTerminalCommand.PostCommandOutputs != null)
        {
            for (int k = 0; k < awocmd.data.AddTerminalCommand.PostCommandOutputs.Length; k++)
            {
                __instance.AddOutput(awocmd.data.AddTerminalCommand.PostCommandOutputs[k].LineType,
                ((string)awocmd.data.AddTerminalCommand.PostCommandOutputs[k].Output != null) ? ((string)awocmd.data.AddTerminalCommand.PostCommandOutputs[k].Output) : "",
                awocmd.data.AddTerminalCommand.PostCommandOutputs[k].Time);
            }
        }

        if (awocmd.data.AddTerminalCommand.Command != null)
        {
            for (int j = 0; j < awocmd.data.AddTerminalCommand.CommandEvents.Length; j++)
            {
                WardenObjectiveEventData wardenObjectiveEventData = awocmd.data.AddTerminalCommand.CommandEvents[j];
                ChainedPuzzleInstance newcp = null;
                if (wardenObjectiveEventData != null)
                {
                    yield return new WaitForSeconds(awocmd.data.AddTerminalCommand.CommandEvents[j].Delay);
                    if (awocmd.data.ChainPuzzle != 0)
                    {
                        newcp = new()
                        {
                            Data = ChainedPuzzleDataBlock.GetBlock(awocmd.data.ChainPuzzle)
                        };
                    }
                    if (newcp != null) newcp.AttemptInteract(eChainedPuzzleInteraction.Activate);
                    if (newcp != null) while (!newcp.IsSolved) yield return new WaitForSeconds(0.2f);
                }
                else if (newcp == null)
                {
                    if (awocmd.data.AddTerminalCommand.CommandEvents[j].Condition.ConditionIndex != -1)
                    {
                        if (WorldEventManager.GetCondition(awocmd.data.AddTerminalCommand.CommandEvents[j].Condition.ConditionIndex) == awocmd.data.AddTerminalCommand.CommandEvents[j].Condition.IsTrue)
                        {
                            if (awocmd.data.AddTerminalCommand.CommandEvents[j].Type == eWardenObjectiveEventType.EventBreak)
                            {
                                UnityEngine.Debug.Log("AdvancedWardenObjective - Event Break 999 encountered, stopping the event chain here.");
                                yield break;
                            }
                            WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(wardenObjectiveEventData, eWardenObjectiveEventTrigger.None, ignoreTrigger: true);
                        }
                    }
                }
            }
        }
        yield return null;
    } */
}