using AmorLib.Utils.Extensions;
using GameData;
using GTFO.API;
using GTFO.API.Extensions;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal sealed class StartEventLoop : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.StartEventLoop;
    public static readonly ConcurrentDictionary<int, Coroutine?> ActiveEventLoops = new();

    protected override void OnSetup()
    {
        LevelAPI.OnLevelCleanup += OnLevelCleanup;
    }

    private void OnLevelCleanup()
    {
        ActiveEventLoops.ForEachValue(loop => CoroutineManager.StopCoroutine(loop));
        ActiveEventLoops.Clear();
    }

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (e.StartEventLoop.LoopDelay < 1.0f)
        {
            LogError("LoopDelay must be greater than or equal to 1.0 seconds");
            return;
        }

        if (!ActiveEventLoops.TryAdd(e.StartEventLoop.LoopIndex, null))
        {
            LogError($"EventLoop {e.StartEventLoop.LoopIndex} is already active...");
            return;
        }

        LogDebug($"Starting EventLoop Index: {e.StartEventLoop.LoopIndex}");
        ActiveEventLoops[e.StartEventLoop.LoopIndex] = CoroutineManager.StartCoroutine(DoLoop(e).WrapToIl2Cpp());
    }

    static IEnumerator DoLoop(WEE_EventData e)
    {
        var sel = e.StartEventLoop;
        int index = sel.LoopIndex;
        int repeatNum = 0;
        int repeatMax = sel.LoopCount;
        bool repeatInf = repeatMax == -1;

        var eData = sel.EventsToActivate.ToIl2Cpp();
        int myReloadCount = CheckpointManager.Current.m_stateReplicator.State.reloadCount;
        WaitForSeconds delay = new(sel.LoopDelay);

        while (repeatNum < repeatMax || repeatInf)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel || CheckpointManager.Current.m_stateReplicator.State.reloadCount > myReloadCount)
            {
                ActiveEventLoops.TryRemove(index, out _);
                yield break; // not in level or checkpoint was used, exit
            }
            
            Logger.Debug("StartEventLoop", $"EventLoop {index} repeating #{repeatNum + 1}");
            WOManager.CheckAndExecuteEventsOnTrigger(eData, eWardenObjectiveEventTrigger.None, ignoreTrigger: true);

            yield return delay;
            repeatNum++;
        }

        Logger.Debug("StartEventLoop", $"EventLoop {index} is now done");
        ActiveEventLoops.TryRemove(index, out _);
    }
}