using Player;
using System.Collections;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal sealed class InfectPlayerEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.InfectPlayer;
    public override bool WhitelistArrayableGlobalIndex => true;

    protected override void TriggerMaster(WEE_EventData e)
    {
        var activeSlotIndices = new HashSet<int>(e.InfectPlayer.PlayerFilter.Select(filter => (int)filter));

        if (!TryGetZone(e, out var zone)) return;

        for (int i = 0; i < PlayerManager.PlayerAgentsInLevel.Count; i++)
        {
            bool overflow = i >= 4 && e.InfectPlayer.FullTeamOverflow && activeSlotIndices.Count == 4 && activeSlotIndices.Max() < 4;
            PlayerAgent player = PlayerManager.PlayerAgentsInLevel[i];
            if ((!overflow && !activeSlotIndices.Contains(i)) || player.Owner.IsBot)
                continue; // Player is neither in PlayerFilter nor is bot, continue
            if (player.CourseNode?.m_zone == null)
                continue; // Node is null, continue

            if (e.InfectPlayer.InfectOverTime && e.Duration > 0.0f)
                CoroutineManager.StartCoroutine(InfectOverTime(e, player, zone.ID).WrapToIl2Cpp());
            else
                ApplyInfection(player, e.InfectPlayer.InfectionAmount, e.InfectPlayer.UseZone, zone.ID);
        }
    }

    private static IEnumerator InfectOverTime(WEE_EventData e, PlayerAgent player, int id)
    {
        int reloadCount = CheckpointManager.Current.m_stateReplicator.State.reloadCount;
        float infectionPerSecond = e.InfectPlayer.InfectionAmount / e.Duration;
        float elapsed = 0.0f;
        WaitForSeconds delay = new(e.InfectPlayer.Interval);

        while (elapsed < e.Duration)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel || CheckpointManager.Current.m_stateReplicator.State.reloadCount > reloadCount)
            {
                yield break; // checkpoint was used or not in level, exit
            }

            ApplyInfection(player, infectionPerSecond, e.InfectPlayer.UseZone, id);
            elapsed += Time.deltaTime;
            yield return delay;
        }
    }

    private static void ApplyInfection(PlayerAgent player, float infection, bool useZone, int id)
    {
        pInfection data = new()
        {
            amount = infection / 100.0f,
            mode = pInfectionMode.Add,
            effect = pInfectionEffect.None
        };

        if (!useZone || player.CourseNode.m_zone.ID == id)
            player.Damage.ModifyInfection(data, true, true);
    }
}
