using GameData;

namespace AWO.Modules.WOE.Objectives.Uplinks;

[Obsolete]
internal sealed class WOE_UplinkData : WOE_DataBase
{
    public UplinkCodeBehaviour[] CodeBehaviours { get; set; } = Array.Empty<UplinkCodeBehaviour>();
}

internal sealed class UplinkCodeBehaviour
{
    public bool ShowCodesOnTerminal { get; set; } = false;
    public bool ShowCodesOnHUD { get; set; } = true;
    public bool ShowCodeToOtherTerminal { get; set; } = true;
    public TerminalZoneSelectionData TerminalZone { get; set; } = new();
    public TerminalOutput[] StartOutputs { get; set; } = Array.Empty<TerminalOutput>();
    public TerminalOutput[] EndOutputs { get; set; } = Array.Empty<TerminalOutput>();
    public WardenObjectiveEventData[] EventsOnStart { get; set; } = Array.Empty<WardenObjectiveEventData>();
    public WardenObjectiveEventData[] EventsOnEnd { get; set; } = Array.Empty<WardenObjectiveEventData>();
}
