using AmorLib.Utils.JsonElementConverters;
using GTFO.API;
using LevelGeneration;

namespace AWO.Modules.WEE.Events;

internal sealed class ClearWardenIntelQueueEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.ClearWardenIntelQueue;

    public static bool AllowWardenIntel { get; private set; } = true;

    protected override void OnSetup()
    {
        LevelAPI.OnLevelCleanup += () => AllowWardenIntel = true;
    }

    protected override void TriggerCommon(WEE_EventData e)
    {
        GuiManager.PlayerLayer.m_wardenIntel.ResetSubObjectiveMesssagQueue();        
        AllowWardenIntel = e.Enabled;

        if (e.WardenIntel != LocaleText.Empty && e.SpecialBool)
        {
            ShowSubObjectiveMessage(e.Layer, e.WardenIntel);
        }
    }

    internal static void ShowSubObjectiveMessage(LG_LayerType layer, LocaleText newSubObj)
    {
        string wardenIntel = WOManager.ReplaceFragmentsInString(layer, WOManager.GetCurrentChainIndex(layer), newSubObj);
        var puiWardenIntel = GuiManager.PlayerLayer.m_wardenIntel;
        puiWardenIntel.m_subObjectiveMessageQueue.AddLast(puiWardenIntel.NewObjectiveSequence("", wardenIntel, false, 200f, 8f, null));
    }
}