using SNetwork;
using AWO.WEE.Events;

namespace AWO.Modules.WEE.Events;

internal sealed class StopEventLoop : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.StopEventLoop;

    protected override void TriggerMaster(WEE_EventData e)
    {
        lock (EntryPoint.ActiveEventLoops)
        {
            if (e.Count == -1)
            {
                Logger.Debug($"AdvancedWardenObjective - Stopping all EventLoops...");
                EntryPoint.ActiveEventLoops.Clear();
            }
            else
            {
                Logger.Debug($"AdvancedWardenObjective - Stopping EventLoop {e.Count}...");
                EntryPoint.ActiveEventLoops.Remove(e.Count);
            }
        }
    }
}
