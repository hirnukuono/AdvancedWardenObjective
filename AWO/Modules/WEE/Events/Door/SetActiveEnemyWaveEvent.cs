using AK;
using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class SetActiveEnemyWaveEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SetActiveEnemyWave;
    public override bool AllowArrayableGlobalIndex => true;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!TryGetZoneEntranceSecDoor(e, out var door)) 
            return;

        var waveData = e.ActiveEnemyWave ?? new();
        var state = door.m_sync.GetCurrentSyncState();

        if (door.ActiveEnemyWaveData?.HasActiveEnemyWave == true)
            door.m_sound.Post(EVENTS.MONSTER_RUCKUS_FROM_BEHIND_SECURITY_DOOR_LOOP_STOP);

        var hasWave = waveData.HasActiveEnemyWave;
        door.ActiveEnemyWaveData = waveData;
        door.m_graphics.SetActiveEnemyWaveEnabled(hasWave);
        door.m_locks.SetActiveEnemyWaveEnabled(hasWave);
        door.m_anim.SetActiveEnemyWaveEnabled(hasWave);
        if (hasWave)
        {
            switch (state.status)
            {
                case eDoorStatus.Open:
                case eDoorStatus.Opening:
                    break;
                case eDoorStatus.Closed:
                    if (door.m_anim.InAnimation)
                        break;
                    door.m_sound.Post(EVENTS.MONSTER_RUCKUS_FROM_BEHIND_SECURITY_DOOR_LOOP_START);
                    break;
                default:
                    door.m_sound.Post(EVENTS.MONSTER_RUCKUS_FROM_BEHIND_SECURITY_DOOR_LOOP_START);
                    break;
            }
        }
        LogDebug($"Set enemy wave - Active: {waveData.HasActiveEnemyWave}, GroupInFront: {waveData.EnemyGroupInfrontOfDoor}, GroupInArea: {waveData.EnemyGroupInArea}");
    }
}