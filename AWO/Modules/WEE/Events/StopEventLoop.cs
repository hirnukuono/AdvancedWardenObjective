using AmorLib.Utils.Extensions;
using static AWO.Modules.WEE.Events.StartEventLoop;

namespace AWO.Modules.WEE.Events;

internal sealed class StopEventLoop : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.StopEventLoop;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (e.Count == -1) // remove all event loops
        {
            ActiveEventLoops.ForEachValue(loop => CoroutineManager.StopCoroutine(loop));
            ActiveEventLoops.Clear();
            LogDebug("Stopped all EventLoops");
        }
        else if (ActiveEventLoops.TryRemove(e.Count, out var loop)) // remove specific event loop
        {
            CoroutineManager.StopCoroutine(loop);
            LogDebug($"Stopped EventLoop {e.Count}");
        }
        else // event loop not found
        {
            LogError("No active EventLoop found!");
        }
    }
}
