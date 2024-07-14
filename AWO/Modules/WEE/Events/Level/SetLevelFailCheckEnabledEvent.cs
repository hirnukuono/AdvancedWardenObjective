using AWO.Sessions;
using AWO.WEE.Events;

namespace AWO.Modules.WEE.Events.Level;

internal sealed class SetLevelFailCheckEnabledEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SetLevelFailCheckEnabled;

    protected override void TriggerMaster(WEE_EventData e)
    {
        LevelFailUpdateState.SetFailAllowed(e.Enabled);
    }
}
