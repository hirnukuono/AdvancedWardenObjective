namespace AWO.Modules.WEE.Events;

internal sealed class PlaySubtitlesEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.PlaySubtitles;

    protected override void TriggerCommon(WEE_EventData e)
    {
        GuiManager.PlayerLayer.m_subtitles.ShowMultiLineSubtitle(e.SoundSubtitle, e.Duration);
    }
}
