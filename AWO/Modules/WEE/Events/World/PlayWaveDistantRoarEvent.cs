using AK;
using AmorLib.Utils.Extensions;
using RoarSound = AWO.Modules.WEE.WEE_PlayWaveDistantRoar.WaveRoarSound;
using RoarSize = AWO.Modules.WEE.WEE_PlayWaveDistantRoar.WaveRoarSize;

namespace AWO.Modules.WEE.Events;

internal sealed class PlayWaveDistantRoarEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.PlayWaveRoarSound;

    protected override void TriggerCommon(WEE_EventData e)
    {
        CellSoundPlayer waveRoar = new();

        waveRoar.SetSwitch(SWITCHES.ENEMY_TYPE.GROUP, e.WaveRoarSound.RoarSound switch
        {
            RoarSound.Shooter => SWITCHES.ENEMY_TYPE.SWITCH.SHOOTER,
            RoarSound.Birther => SWITCHES.ENEMY_TYPE.SWITCH.BIRTHER,
            RoarSound.Shadow => SWITCHES.ENEMY_TYPE.SWITCH.SHADOW,
            RoarSound.Tank => SWITCHES.ENEMY_TYPE.SWITCH.TANK,
            RoarSound.Flyer => SWITCHES.ENEMY_TYPE.SWITCH.FLYER,
            RoarSound.Immortal => SWITCHES.ENEMY_TYPE.SWITCH.IMMORTAL,
            RoarSound.Bullrush => SWITCHES.ENEMY_TYPE.SWITCH.BULLRUSHER,
            RoarSound.Pouncer => SWITCHES.ENEMY_TYPE.SWITCH.POUNCER,
            RoarSound.Striker_Berserk => SWITCHES.ENEMY_TYPE.SWITCH.STRIKER_BERSERK,
            RoarSound.Shooter_Spread => SWITCHES.ENEMY_TYPE.SWITCH.SHOOTER_SPREAD,
            _ => SWITCHES.ENEMY_TYPE.SWITCH.STRIKER
        });

        waveRoar.SetSwitch(SWITCHES.ROAR_SIZE.GROUP, e.WaveRoarSound.RoarSize switch
        {
            RoarSize.Medium => SWITCHES.ROAR_SIZE.SWITCH.MEDIUM,
            RoarSize.Big => SWITCHES.ROAR_SIZE.SWITCH.BIG,
            _ => SWITCHES.ROAR_SIZE.SWITCH.SMALL
        });

        waveRoar.SetSwitch(SWITCHES.ENVIROMENT.GROUP, e.WaveRoarSound.IsOutside ? SWITCHES.ENVIROMENT.SWITCH.DESERT : SWITCHES.ENVIROMENT.SWITCH.COMPLEX);
        waveRoar.PostWithCleanup(EVENTS.PLAY_WAVE_DISTANT_ROAR, GetPositionFallback(e.Position, e.SpecialText));
    }
}
