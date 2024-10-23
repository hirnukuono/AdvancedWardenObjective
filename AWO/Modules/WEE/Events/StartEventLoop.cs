using AWO.WEE.Events;
using GTFO.API;
using System.Collections;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal sealed class StartEventLoop : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.StartEventLoop;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (e.StartEventLoop.LoopDelay < 1.0f)
        {
            LogError("LoopDelay must be > 1.0 seconds");
            return;
        }

        lock (EntryPoint.ActiveEventLoops)
        {
            if (EntryPoint.ActiveEventLoops.Contains(e.StartEventLoop.LoopIndex))
            {
                LogError($"EventLoop {e.StartEventLoop.LoopIndex} is already active...");
                return;
            }

            EntryPoint.ActiveEventLoops.Add(e.StartEventLoop.LoopIndex);
            LogDebug($"Starting EventLoop Index: {e.StartEventLoop.LoopIndex}");
            CoroutineManager.StartCoroutine(DoLoop(e).WrapToIl2Cpp());
            LevelAPI.OnLevelCleanup += OnLevelCleanup;
        }
    }

    private void OnLevelCleanup()
    {
        Logger.Debug("[StartEventLoop] Cleaning up active EventLoops...");
        EntryPoint.ActiveEventLoops.Clear();
    }

    static IEnumerator DoLoop(WEE_EventData e)
    {
        var sel = e.StartEventLoop;
        int repeatNum = 0;
        int repeatMax = sel.LoopCount;
        bool repeatInf = repeatMax == -1;
        int index = sel.LoopIndex;
        int myReloadCount = CheckpointManager.Current.m_stateReplicator.State.reloadCount;
        WaitForSeconds delay = new(sel.LoopDelay);

        while (repeatNum < repeatMax || repeatInf)
        {
            lock (EntryPoint.ActiveEventLoops)
            {
                if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
                {
                    EntryPoint.ActiveEventLoops.Remove(index);
                    yield break; // no longer in level, exit
                }
                if (CheckpointManager.Current.m_stateReplicator.State.reloadCount > myReloadCount)
                {
                    EntryPoint.ActiveEventLoops.Remove(index);
                    yield break; // checkpoint was used, exit
                }
                if (!EntryPoint.ActiveEventLoops.Contains(index))
                {
                    Logger.Debug($"[StartEventLoop] EventLoop {index} is now done");
                    yield break; // StopEventLoop used, exit
                }
            }
            
            Logger.Debug($"[StartEventLoop] EventLoop {index} repeating #{repeatNum}");
            foreach (var eventData in sel.EventsToActivate)
            {
                WorldEventManager.ExecuteEvent(eventData);
            }

            yield return delay;
            repeatNum++;
        }
        Logger.Debug($"[StartEventLoop] EventLoop {index} is now done");
    }
}