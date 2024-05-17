using AWO.Modules.WEE;
using LevelGeneration;
using SNetwork;

namespace AWO.WEE.Events.Level;

internal sealed class AddTerminalCommand : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.ForceCompleteLevel;

    protected override void TriggerMaster(WEE_EventData e)
    {
        WOManager.ForceCompleteObjective(LG_LayerType.MainLayer);
        SNet.Sync.SessionCommand(eSessionCommandType.TryEndPlaying, 2);
    }
}
