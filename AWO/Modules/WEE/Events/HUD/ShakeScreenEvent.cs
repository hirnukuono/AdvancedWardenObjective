using Player;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal sealed class ShakeScreenEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.ShakeScreen;

    protected override void TriggerCommon(WEE_EventData e)
    {
        var effect = new GameObject().AddComponent<CameraShakeEffect>();
        var pos = GetPositionFallback(e.Position, e.SpecialText);
        if (pos != Vector3.zero)
        {
            effect.transform.position = pos;
        }
        else
        {
            effect.transform.SetParent(PlayerManager.GetLocalPlayerAgent().transform);
        }

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
