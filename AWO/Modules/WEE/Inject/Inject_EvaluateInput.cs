/* using LevelGeneration;
using HarmonyLib;
using AK;
using SNetwork;
using GameData;

namespace AWO.Modules.WEE.Inject;

[HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.EvaluateInput))]
internal class Inject_EvaluateInput
{

    [HarmonyPrefix]
    static bool Prefix(LG_ComputerTerminalCommandInterpreter __instance, ref string inputString)
    {
        UnityEngine.Debug.Log($"AdvancedWardenObjective - we are in evaluate, on {__instance.m_terminal.m_terminalItem.SpawnNode.m_zone} ..");

        if (EntryPoint.AWOCommandList == null) return true;

        UnityEngine.Debug.Log("AdvancedWardenObjective - awocommandlist is not null ..");

        foreach (var awocmd in EntryPoint.AWOCommandList)
        {
            UnityEngine.Debug.Log($"AdvancedWardenObjective - checking awocmd with {awocmd.data.AddTerminalCommand.TerminalIndex} {awocmd.data.AddTerminalCommand.CommandNumber} {awocmd.data.AddTerminalCommand.Command} ..");
            LG_Zone gaa;
            var zone = __instance.m_terminal.m_terminalItem.SpawnNode.m_zone;
            Builder.Current.m_currentFloor.TryGetZoneByLocalIndex(awocmd.data.DimensionIndex, awocmd.data.Layer, awocmd.data.LocalIndex, out gaa);
            UnityEngine.Debug.Log($"AdvancedWardenObjective - terminal in {zone.NavInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number_With_Underscore)} awocmd in {gaa.NavInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number_With_Underscore)} ..");

            if (gaa == zone && gaa.TerminalsSpawnedInZone[awocmd.data.AddTerminalCommand.TerminalIndex] == __instance.m_terminal)
            {
                if (inputString.ToUpper() == awocmd.data.AddTerminalCommand.Command.ToUpper() && !awocmd.hidden)
                {
                    UnityEngine.Debug.Log($"AdvancedWardenObjective - custom command {awocmd.data.AddTerminalCommand.Command} has been run!");
                    __instance.m_terminal.m_sound.Post(EVENTS.BUTTONGENERICPRESS);
                    __instance.m_inputBuffer.Add(inputString);
                    __instance.m_terminal.m_caretBlinkOffsetFromEnd = 0;
                    __instance.m_inputBufferStep = 0;
                    if (__instance.m_inputBuffer.Count > 10)
                    {
                        __instance.m_inputBuffer.RemoveAt(0);
                    }
                    __instance.AddOutput(TerminalLineType.ProgressWait, "Executing command", 3f);

                    if (awocmd.used && awocmd.data.AddTerminalCommand.SpecialCommandRule == TERM_CommandRule.OnlyOnce)
                    {
                        __instance.AddOutput(TerminalLineType.Fail, "<color=red>ERROR://</color> This command only allows one use. Command has already been called.", 1f);
                        __instance.m_terminal.m_sound.Post(EVENTS.TERMINAL_EXECUTE_NEGATIVE);
                        return false;
                    }
                    if (awocmd.data.AddTerminalCommand.PostCommandOutputs != null)
                    {
                        for (int k = 0; k < awocmd.data.AddTerminalCommand.PostCommandOutputs.Length; k++)
                        {
                            __instance.AddOutput(awocmd.data.AddTerminalCommand.PostCommandOutputs[k].LineType,
                            ((string)awocmd.data.AddTerminalCommand.PostCommandOutputs[k].Output != null) ? ((string)awocmd.data.AddTerminalCommand.PostCommandOutputs[k].Output) : "",
                            awocmd.data.AddTerminalCommand.PostCommandOutputs[k].Time);
                        }
                    }

                    if (awocmd.data.AddTerminalCommand.SpecialCommandRule == TERM_CommandRule.OnlyOnce) awocmd.used = true;
                    foreach (var eventData in awocmd.data.AddTerminalCommand.CommandEvents)
                    {
                        eventData.Delay += 3f;
                        if (SNet.IsMaster) WorldEventManager.ExecuteEvent(eventData);
                    }
                    __instance.AddOutput("");
                    return false;
                }
            }
        }
        return true;
    }
}
*/