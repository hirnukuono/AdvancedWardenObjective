using CellMenu;
using FluffyUnderware.DevTools.Extensions;
using LevelGeneration;
using UnityEngine;

namespace AWO.Modules.WEE.Events.HUD;

internal sealed class FadeScreenEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.FadeScreenInOut;

    protected override void TriggerCommon(WEE_EventData e)
    {
        var effect = Builder.GetElevatorArea().AddChildGameObject<CM_PreSuccessScreen_FadeOut>("AWO_FadeScreenEvent");
        var data = e.FadeScreen ?? new();
        effect.m_fadeColor = data.FadeColor;
        effect.m_curve = AnimationCurve.EaseInOut(data.CurveTimeStart, data.CurveValueStart, data.CurveTimeEnd, data.CurveValueEnd);
        effect.m_fadeSpeed = data.FadeSpeed;
    }
}
