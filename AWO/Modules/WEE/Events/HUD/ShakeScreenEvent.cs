using Player;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal sealed class ShakeScreenEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.ShakeScreen;

    protected override void TriggerCommon(WEE_EventData e)
    {
        var effect = new GameObject().AddComponent<CameraShakeEffect>();
        effect.transform.parent = PlayerManager.GetLocalPlayerAgent().transform;
        effect.transform.localPosition = Vector3.zero;

        e.CameraShake ??= new();
        effect.Radius = e.CameraShake.Radius;
        effect.Duration = ResolveFieldsFallback(e.Duration, e.CameraShake.Duration);
        effect.Amplitude = e.CameraShake.Amplitude;
        effect.Frequency = e.CameraShake.Frequency;
        effect.directional = e.CameraShake.Directional;
        effect.PlayOnEnable = true;

        effect.Play();
    }
}
