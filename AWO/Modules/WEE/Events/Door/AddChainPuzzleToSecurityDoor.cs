using ChainedPuzzles;
using GameData;
using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class AddChainPuzzleToSecurityDoor : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.AddChainPuzzleToSecurityDoor;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!TryGetZoneEntranceSecDoor(e, out var door)) return;

        uint chainPuzzle = e.SpecialNumber > 0 ? (uint)e.SpecialNumber : e.ChainPuzzle;
        var block = ChainedPuzzleDataBlock.GetBlock(chainPuzzle);
        if (block == null || !block.internalEnabled)
        {
            LogError($"Failed to find enabled ChainedPuzzleDataBlock {chainPuzzle}!");
            return;
        }

        var state = door.m_sync.GetCurrentSyncState();
        if (door.GetChainedPuzzleStartPosition(out var pos))
        {
            switch (state.status)
            {
                case eDoorStatus.Open:
                case eDoorStatus.Opening:
                    LogError("Door is already open!");
                    break;
                
                case eDoorStatus.ChainedPuzzleActivated:
                    LogError("Door already has an active chained puzzle!");
                    break;

                case eDoorStatus.Closed:
                case eDoorStatus.Closed_LockedWithBulkheadDC:
                case eDoorStatus.Closed_LockedWithChainedPuzzle:
                case eDoorStatus.Closed_LockedWithChainedPuzzle_Alarm:                
                case eDoorStatus.Closed_LockedWithKeyItem:
                case eDoorStatus.Closed_LockedWithPowerGenerator:
                case eDoorStatus.Closed_LockedWithNoKey:
                case eDoorStatus.Unlocked:
                    if (door.m_locks.ChainedPuzzleToSolve != null && !door.m_locks.ChainedPuzzleToSolve.IsSolved)
                    {
                        LogWarning($"Door has an unsolved chained puzzle {door.m_locks.ChainedPuzzleToSolve.Data.persistentID}, overriding...");
                    }
                    var puzzleInstance = ChainedPuzzleManager.CreatePuzzleInstance(block, door.Gate.ProgressionSourceArea, door.Gate.m_linksTo, pos, door.transform);
                    state.status = door.m_locks.SetupForChainedPuzzle(puzzleInstance);
                    door.m_sync.SetStateUnsynced(state);
                    if (state.status == eDoorStatus.Closed || state.status == eDoorStatus.Unlocked)
                    {
                        if (block.TriggerAlarmOnActivate)
                        {
                            door.m_graphics.OnDoorState(new pDoorState { status = eDoorStatus.Closed_LockedWithChainedPuzzle_Alarm }, false);
                            door.m_mapLookatRevealer.SetLocalGUIObjStatus(eCM_GuiObjectStatus.DoorSecureApex);
                        }
                        else
                        {
                            door.m_graphics.OnDoorState(new pDoorState { status = eDoorStatus.Closed_LockedWithChainedPuzzle }, false);
                        }
                    }
                    var anim = door.m_anim.TryCast<LG_SecurityDoor_Anim>();
                    anim?.m_animator.Play(door.m_securityDoorType switch
                    {
                        eSecurityDoorType.Security => "ClosedIdle",
                        eSecurityDoorType.Apex => "ApexDoorClosed_Idle",
                        eSecurityDoorType.Bulkhead => "ClosedIdle",
                        _ => string.Empty
                    });                    
                    LogDebug($"Door has recieved new chained puzzle {chainPuzzle}");
                    break;

                default:
                    LogError("Door is in an unsupported state!");
                    break;
            }
        }        
    }
}