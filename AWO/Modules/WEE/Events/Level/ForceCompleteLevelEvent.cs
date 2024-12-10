using LevelGeneration;
using SNetwork;

namespace AWO.Modules.WEE.Events;

internal sealed class ForceCompleteLevelEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.ForceCompleteLevel;

    protected override void TriggerMaster(WEE_EventData e)
    {
        WOManager.ForceCompleteObjective(LG_LayerType.MainLayer);
        SNet.Sync.SessionCommand(eSessionCommandType.TryEndPlaying, 2);
    }
}
