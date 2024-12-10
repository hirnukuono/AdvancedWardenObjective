namespace AWO.Modules.WEE.Events;

internal sealed class StopEventLoop : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.StopEventLoop;

    protected override void TriggerCommon(WEE_EventData e)
    {
        lock (EntryPoint.ActiveEventLoops)
        {
            if (e.Count == -1)
            {
                LogDebug("Stopping all EventLoops...");
                EntryPoint.ActiveEventLoops.Clear();
            }
            else
            {
                LogDebug($"Stopping EventLoop {e.Count}...");
                EntryPoint.ActiveEventLoops.Remove(e.Count);
            }
        }
    }
}
