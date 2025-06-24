﻿using AWO.Jsons;
using AWO.Modules.TSL;

namespace AWO.Modules.WEE.Events;

internal sealed class SetDoorInteractionEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SetDoorInteraction;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!TryGetZoneEntranceSecDoor(e, out var door)) return;

        var intMessage = door.gameObject.GetComponentInChildren<Interact_MessageOnScreen>();
        if (intMessage != null && e.SpecialText != LocaleText.Empty)
        {
            intMessage.m_message = SerialLookupManager.ParseTextFragments(e.SpecialText);
        }
    }
}
