using GTFO.API;

namespace AWO.Modules.WEE.Events;

internal sealed class ClearWardenIntelQueueEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.ClearWardenIntelQueue;

    public static bool AllowWardenIntel { get; private set; } = true;

    protected override void OnSetup()
    {
        LevelAPI.OnLevelCleanup += () => AllowWardenIntel = true;
    }

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!e.SpecialBool)
            GuiManager.PlayerLayer.m_wardenIntel.ResetSubObjectiveMesssagQueue();
        
        AllowWardenIntel = e.Enabled;
    }
}