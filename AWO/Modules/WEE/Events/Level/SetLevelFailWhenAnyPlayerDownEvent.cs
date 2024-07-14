using AWO.Sessions;
using AWO.WEE.Events;

namespace AWO.Modules.WEE.Events.Level;
internal sealed class SetLevelFailWhenAnyPlayerDownEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SetLevelFailWhenAnyPlayerDowned;

    protected override void TriggerMaster(WEE_EventData e)
    {
        LevelFailUpdateState.SetFailWhenAnyPlayerDown(e.Enabled);
    }
}
