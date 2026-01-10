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
        var sel = e.StartEventLoop ?? new();

        if (sel.LoopDelay < 1f)
        {
            LogError("LoopDelay must be greater than or equal to 1.0 seconds");
            return;
        }

        if (!ActiveEventLoops.TryAdd(sel.LoopIndex, null))
        {
            LogError($"EventLoop {sel.LoopIndex} is already active...");
            return;
        }

        LogDebug($"Starting EventLoop Index: {sel.LoopIndex}");
        ActiveEventLoops[sel.LoopIndex] = CoroutineManager.StartCoroutine(DoLoop(sel).WrapToIl2Cpp());
    }

    static IEnumerator DoLoop(WEE_StartEventLoop sel)
    {
        int index = sel.LoopIndex;
        int repeatNum = 0;
        int repeatMax = sel.LoopCount;
        bool repeatInf = repeatMax == -1;

        var eData = sel.EventsToActivate.ToIl2Cpp();
        int myReloadCount = CheckpointManager.CheckpointUsage;
        WaitForSeconds delay = new(sel.LoopDelay);

        while (repeatNum < repeatMax || repeatInf)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel || myReloadCount < CheckpointManager.CheckpointUsage)
            {
                ActiveEventLoops.TryRemove(index, out _);
                yield break; // not in level or checkpoint was used, exit
            }
            
            Logger.Debug("StartEventLoop", $"EventLoop {index} repeating #{repeatNum + 1}");
            WOManager.CheckAndExecuteEventsOnTrigger(eData, eWardenObjectiveEventTrigger.None, true);

            yield return delay;
            repeatNum++;
        }

        Logger.Debug("StartEventLoop", $"EventLoop {index} is now done");
        ActiveEventLoops.TryRemove(index, out _);
    }
}