using GTFO.API;
using System.Collections;
using UnityEngine;
using SNetwork;
using AWO.WEE.Events;

namespace AWO.Modules.WEE.Events;

internal sealed class StartEventLoop : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.StartEventLoop;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (e.StartEventLoop.LoopDelay < 1.0f)
        {
            Logger.Error($"AdvancedWardenObjective - EventLoop LoopDelay must be > 1.0");
            return;
        }

        lock (EntryPoint.ActiveEventLoops)
        {
            if (EntryPoint.ActiveEventLoops.Contains(e.StartEventLoop.LoopIndex))
            {
                Logger.Error($"AdvancedWardenObjective - EventLoop {e.StartEventLoop.LoopIndex} is already active...");
                return;
            }

            EntryPoint.ActiveEventLoops.Add(e.StartEventLoop.LoopIndex);
            Logger.Debug($"AdvancedWardenObjective - Starting EventLoop Index: {e.StartEventLoop.LoopIndex}");
            CoroutineManager.StartCoroutine(DoLoop(e).WrapToIl2Cpp());
            LevelAPI.OnLevelCleanup += OnLevelCleanup;
        }
    }

    private static void OnLevelCleanup()
    {
        Logger.Debug($"AdvancedWardenObjective - Cleaned up active EventLoops...");
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
                    Logger.Debug($"AdvancedWardenObjective - EventLoop {index} done");
                    yield break; // StopEventLoop used, exit
                }
            }
            
            Logger.Debug($"AdvancedWardenObjective - EventLoop {index} repeating #{repeatNum}");
            foreach (var eventData in sel.EventsToActivate)
                if (SNet.IsMaster)
                    WorldEventManager.ExecuteEvent(eventData);

            yield return new WaitForSeconds(sel.LoopDelay);
            repeatNum++;
        }
        Logger.Debug($"AdvancedWardenObjective - EventLoop {index} done");
    }
}