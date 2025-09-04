using AK;
using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class SetActiveEnemyWaveEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SetActiveEnemyWave;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!TryGetZoneEntranceSecDoor(e, out var door)) return;

        var waveData = e.ActiveEnemyWave ?? new();
        var state = door.m_sync.GetCurrentSyncState();
        switch (state.status)
        {
            case eDoorStatus.Open:
            case eDoorStatus.Opening:
                LogError("Door is already open!");
                break;

            default:
                if (door.ActiveEnemyWaveData?.HasActiveEnemyWave == true)
                    door.m_sound.Post(EVENTS.MONSTER_RUCKUS_FROM_BEHIND_SECURITY_DOOR_LOOP_STOP);

                door.SetupActiveEnemyWaveData(waveData);
                if (!waveData.HasActiveEnemyWave)
                {
                    door.m_graphics.SetActiveEnemyWaveEnabled(enabled: false);
                    door.m_locks.SetActiveEnemyWaveEnabled(enabled: false);
                    door.m_anim.SetActiveEnemyWaveEnabled(enabled: false);
                }
                LogDebug($"Set enemy wave - Active: {waveData.HasActiveEnemyWave}, GroupInFront: {waveData.EnemyGroupInfrontOfDoor}, GroupInArea: {waveData.EnemyGroupInArea}");
                break;
        }
    }
}