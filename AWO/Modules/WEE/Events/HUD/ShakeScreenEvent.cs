using AmorLib.Utils;
using FluffyUnderware.DevTools.Extensions;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal sealed class ShakeScreenEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.ShakeScreen;

    protected override void TriggerCommon(WEE_EventData e)
    {
        CameraShakeEffect effect;
        var pos = GetPositionFallback(e.Position, e.SpecialText, false);
        if (pos != Vector3.zero)
        {
            var node = CourseNodeUtil.GetCourseNode(pos);
            effect = node?.m_area.AddChildGameObject<CameraShakeEffect>("AWO_ShakeScreenEvent") ?? new();
            effect.transform.position = pos;
        }
        else
        {
            effect = LocalPlayer.AddChildGameObject<CameraShakeEffect>("AWO_ShakeScreenEvent");
        }

        var camData = e.CameraShake ??= new();
        effect.Radius = camData.Radius;
        effect.Duration = ResolveFieldsFallback(e.Duration, camData.Duration);
        effect.Amplitude = camData.Amplitude;
        effect.Frequency = camData.Frequency;
        effect.directional = camData.Directional;
        effect.PlayOnEnable = true;
        effect.Play();
    }
}
