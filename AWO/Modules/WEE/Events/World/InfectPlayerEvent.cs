using GTFO.API.Utilities;
using SNetwork;
using AWO.WEE.Events;
using Player;
using System.Collections;
using UnityEngine;

namespace AWO.Modules.WEE.Events.World;

internal sealed class InfectPlayerEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.InfectPlayer;

    protected override void TriggerMaster(WEE_EventData e)
    {
        var activeSlotIndices = new HashSet<int>(e.InfectPlayer.PlayerFilter.Select(filter => (int)filter));
        EntryPoint.IOTStarted = Time.realtimeSinceStartup;

        if (!TryGetZone(e, out var zone))
        {
            LogError("Cannot find zone!");
            return;
        }

        foreach (PlayerAgent player in PlayerManager.PlayerAgentsInLevel)
        {
            if (!activeSlotIndices.Contains(player.PlayerSlotIndex) || player.Owner.IsBot)
                continue; // Player not in PlayerFilter or is bot, continue
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
        float startTime = EntryPoint.IOTStarted;
        var ip = e.InfectPlayer;
        PlayerAgent p = player;
        float duration = e.Duration;
        float infectionPerSecond = ip.InfectionAmount / duration;
        float elapsed = 0.0f;

        while (elapsed <= duration)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
                yield break; // no longer in level, exit
            if (startTime < EntryPoint.IOTStarted) 
                yield break; // new InfectPlayer event started, exit
            if (CheckpointManager.Current.m_stateReplicator.State.reloadCount > reloadCount) 
                yield break; // checkpoint was used, exit

            ApplyInfection(p, infectionPerSecond, ip.UseZone, id);
            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(1.0f);
        }
    }

    private static void ApplyInfection(PlayerAgent player, float infection, bool useZone, int id)
    {
        var data = new pInfection
        {
            amount = infection / 100.0f,
            mode = pInfectionMode.Add,
            effect = pInfectionEffect.None
        };

        if (!useZone || player.CourseNode.m_zone.ID == id)
            player.Damage.ModifyInfection(data, true, true);
    }
}
