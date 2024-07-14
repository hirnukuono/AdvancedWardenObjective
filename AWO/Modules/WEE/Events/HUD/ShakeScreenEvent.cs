using AWO.WEE.Events;
using Player;
using UnityEngine;

namespace AWO.Modules.WEE.Events.HUD;
internal sealed class ShakeScreenEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.ShakeScreen;

    protected override void TriggerCommon(WEE_EventData e)
    {
        var effect = new GameObject().AddComponent<CameraShakeEffect>();
        effect.transform.parent = PlayerManager.GetLocalPlayerAgent().transform;
        effect.transform.localPosition = Vector3.zero;

        effect.Radius = e.CameraShake.Radius;
        effect.Duration = GetDuration(e);
        effect.Amplitude = e.CameraShake.Amplitude;
        effect.Frequency = e.CameraShake.Frequency;
        effect.directional = e.CameraShake.Directional;
        effect.PlayOnEnable = true;

        effect.Play();
    }

    private static float GetDuration(WEE_EventData e)
    {
        if (e.CameraShake.Duration != 0.0f)
            return e.CameraShake.Duration;
        else if (e.Duration != 0.0f)
            return e.Duration;

        return e.CameraShake.Duration;
    }
}
