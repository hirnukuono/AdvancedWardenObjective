using AK;
using AWO.WEE.Events;
using BepInEx;
using System.Collections;
using UnityEngine;

namespace AWO.Modules.WEE.Events.World;
internal sealed class PlayWaveDistantRoarEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.PlayWaveRoarSound;

    protected override void TriggerCommon(WEE_EventData e)
    {
        CellSoundPlayer csPlayer = new(Vector3.zero);
        csPlayer.UpdatePosition(GetSoundPlayerPosition(e.Position, e.SpecialText));

        switch ((byte)e.WaveRoarSound.RoarSound)
        {
            case 0:
                csPlayer.SetSwitch(SWITCHES.ENEMY_TYPE.GROUP, SWITCHES.ENEMY_TYPE.SWITCH.STRIKER);
                break;
            case 1:
                csPlayer.SetSwitch(SWITCHES.ENEMY_TYPE.GROUP, SWITCHES.ENEMY_TYPE.SWITCH.SHOOTER);
                break;
            case 2:
                csPlayer.SetSwitch(SWITCHES.ENEMY_TYPE.GROUP, SWITCHES.ENEMY_TYPE.SWITCH.BIRTHER);
                break;
            case 3:
                csPlayer.SetSwitch(SWITCHES.ENEMY_TYPE.GROUP, SWITCHES.ENEMY_TYPE.SWITCH.SHADOW);
                break;
            case 4:
                csPlayer.SetSwitch(SWITCHES.ENEMY_TYPE.GROUP, SWITCHES.ENEMY_TYPE.SWITCH.TANK);
                break;
            case 5:
                csPlayer.SetSwitch(SWITCHES.ENEMY_TYPE.GROUP, SWITCHES.ENEMY_TYPE.SWITCH.FLYER);
                break;
            case 6:
                csPlayer.SetSwitch(SWITCHES.ENEMY_TYPE.GROUP, SWITCHES.ENEMY_TYPE.SWITCH.IMMORTAL);
                break;
            case 7:
                csPlayer.SetSwitch(SWITCHES.ENEMY_TYPE.GROUP, SWITCHES.ENEMY_TYPE.SWITCH.BULLRUSHER);
                break;
            case 8:
                csPlayer.SetSwitch(SWITCHES.ENEMY_TYPE.GROUP, SWITCHES.ENEMY_TYPE.SWITCH.POUNCER);
                break;
            case 9:
                csPlayer.SetSwitch(SWITCHES.ENEMY_TYPE.GROUP, SWITCHES.ENEMY_TYPE.SWITCH.STRIKER_BERSERK);
                break;
            case 10:
                csPlayer.SetSwitch(SWITCHES.ENEMY_TYPE.GROUP, SWITCHES.ENEMY_TYPE.SWITCH.SHOOTER_SPREAD);
                break;
        }

        switch ((byte)e.WaveRoarSound.RoarSize)
        {
            case 0:
                csPlayer.SetSwitch(SWITCHES.ROAR_SIZE.GROUP, SWITCHES.ROAR_SIZE.SWITCH.SMALL);
                break;
            case 1:
                csPlayer.SetSwitch(SWITCHES.ROAR_SIZE.GROUP, SWITCHES.ROAR_SIZE.SWITCH.MEDIUM);
                break;
            case 2:
                csPlayer.SetSwitch(SWITCHES.ROAR_SIZE.GROUP, SWITCHES.ROAR_SIZE.SWITCH.BIG);
                break;
        }

        csPlayer.SetSwitch(SWITCHES.ENVIROMENT.GROUP, e.WaveRoarSound.IsOutside ? SWITCHES.ENVIROMENT.SWITCH.DESERT : SWITCHES.ENVIROMENT.SWITCH.COMPLEX);
        csPlayer.Post(EVENTS.PLAY_WAVE_DISTANT_ROAR, true);
        CoroutineManager.StartCoroutine(Cleanup(csPlayer).WrapToIl2Cpp());
    }

    private static Vector3 GetSoundPlayerPosition(Vector3 pos, string weObjectFilter)
    {
        if (pos != Vector3.zero) return pos;

        if (weObjectFilter.IsNullOrWhiteSpace()) return Vector3.zero;

        foreach (var weObject in WorldEventManager.Current.m_worldEventObjects)
            if (weObject.gameObject.name == weObjectFilter)
                return weObject.gameObject.transform.position;

        Logger.Error($"[PlayWaveDistantRoarEvent] Could not find WorldEventObjectFilter {weObjectFilter}");
        return Vector3.zero;
    }
    
    static IEnumerator Cleanup(CellSoundPlayer csPlayer)
    {
        yield return new WaitForSeconds(10f);
        csPlayer.Recycle();
    }
}
