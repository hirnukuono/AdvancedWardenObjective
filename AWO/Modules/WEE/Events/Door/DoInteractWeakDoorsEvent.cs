using AmorLib.Utils.Extensions;
using AWO.Modules.WEE.Replicators;

namespace AWO.Modules.WEE.Events;

internal sealed class DoInteractWeakDoorsEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.DoInteractWeakDoorsInZone;
    public override bool AllowArrayableGlobalIndex => true;

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!TryGetZone(e, out var zone) || !zone.gameObject.TryAndGetComponent<WeakDoorInfoReplicator>(out var replicator))
        {
            LogError("Unable to find WeakDoorInfoReplicator component in zone?");
            return;
        }        
        
        replicator.PlayButtonAnimation(e.Enabled);
        replicator.DoInteract(e.Enabled);
    }
}