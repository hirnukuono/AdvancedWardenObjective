using Player;

namespace AWO.Modules.WEE.Events;

internal sealed class GiveResourceEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.GiveResource;
    public override bool AllowArrayableGlobalIndex => true;

    protected override void TriggerMaster(WEE_EventData e)
    {
        var data = e.GiveResource ?? new();
        var activeSlotIndices = new HashSet<int>(data.PlayerFilter.Select(filter => (int)filter));

        LevelGeneration.LG_Zone? zone = null;
        if (data.UseLocation && !TryGetZone(e, out zone)) 
            return;

        bool overflow = data.FullTeamOverflow && activeSlotIndices.Count == 4 && activeSlotIndices.Max() < 4;
        for (int i = 0; i < PlayerManager.PlayerAgentsInLevel.Count; i++)
        {
            var player = PlayerManager.PlayerAgentsInLevel[i];

            if (!overflow && !activeSlotIndices.Contains(i))
                continue; // Player not in PlayerFilter, continue
            if (data.UseLocation && player.CourseNode?.m_zone.ID != zone!.ID)
                continue; // Node is null, continue

            GiveResource(player, data);
        }
    }

    private static void GiveResource(PlayerAgent player, WEE_GiveResource data)
    {
        if (data.HasAnyAmmoGain)
        {
            float mod = data.IncludeSupplyEfficiency ? AgentModifierManager.ApplyModifier(player, AgentModifier.AmmoSupport, 1f) : 1f;
            PlayerBackpackManager.GiveAmmoToPlayer(player.Owner, data.MainAmmo * mod, data.SpecialAmmo * mod, data.ToolAmmo * mod);
        }

        if (data.Health != 0f)
        {
            float mod = data.IncludeSupplyEfficiency ? AgentModifierManager.ApplyModifier(player, AgentModifier.HealSupport, 1f) : 1f;
            player.GiveHealth(player, data.Health * mod);
        }
    }
}
