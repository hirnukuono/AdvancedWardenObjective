using AWO.WEE.Events;
using Agents;
using Player;

namespace AWO.Modules.WEE.Events.World;

internal sealed class RevivePlayerEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.RevivePlayer;

    protected override void TriggerMaster(WEE_EventData e)
    {
        var activeSlotIndices = new HashSet<int>(e.RevivePlayer.PlayerFilter.Select(filter => (int)filter));

        foreach (PlayerAgent player in PlayerManager.PlayerAgentsInLevel)
            if (activeSlotIndices.Contains(player.PlayerSlotIndex) && !player.Alive)
                AgentReplicatedActions.PlayerReviveAction(player, player, player.Position);
    }
}