namespace AWO.Modules.WEE.Events;

[Obsolete]
internal sealed class SolveSingleObjectiveItemEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SolveSingleObjectiveItem;

    protected override void TriggerMaster(WEE_EventData e)
    {
        if (!WOManager.HasWardenObjectiveDataForLayer(e.Layer))
        {
            LogError($"{e.Layer} Objective is Missing");
            return;
        }

        var chainIndex = WOManager.GetCurrentChainIndex(e.Layer);
        var items = WOManager.GetObjectiveItemCollection(e.Layer, chainIndex);
        if (items == null)
        {
            LogError($"{e.Layer} Objective Doesn't have ObjectiveItem Collection!");
            return;
        }

        foreach (var item in items)
        {
            if (item.ObjectiveItemSolved)
                continue;

            WOManager.OnLocalPlayerSolvedObjectiveItem(e.Layer, item, forceSolve: false);
            break;
        }
    }
}
