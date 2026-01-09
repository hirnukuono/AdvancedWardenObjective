using Player;
using System.Collections;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal sealed class DamagePlayerEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.DamagePlayer;
    public override bool AllowArrayableGlobalIndex => true;

    protected override void TriggerMaster(WEE_EventData e)
    {
        e.DamagePlayer ??= new();
        var activeSlotIndices = new HashSet<int>(e.DamagePlayer.PlayerFilter.Select(filter => (int)filter));

        if (!TryGetZone(e, out var zone)) return;

        for (int i = 0; i < PlayerManager.PlayerAgentsInLevel.Count; i++)
        {
            bool overflow = i >= 4 && e.DamagePlayer.FullTeamOverflow && activeSlotIndices.Count == 4 && activeSlotIndices.Max() < 4;
            PlayerAgent player = PlayerManager.PlayerAgentsInLevel[i];
            if (!overflow && !activeSlotIndices.Contains(i))
                continue; // Player not in PlayerFilter, continue
            if (player.CourseNode?.m_zone == null)
                continue; // Node is null, continue

            if (e.DamagePlayer.DamageOverTime && e.Duration > 0.0f)
                CoroutineManager.StartCoroutine(DamageOverTime(e, player, zone.ID).WrapToIl2Cpp());
            else
                ApplyDamage(player, e.DamagePlayer.DamageAmount, e.DamagePlayer.UseZone, zone.ID);
        }
    }

    private static IEnumerator DamageOverTime(WEE_EventData e, PlayerAgent player, int id)
    {
        int reloadCount = CheckpointManager.CheckpointUsage;
        float damagePerSecond = e.DamagePlayer!.DamageAmount / e.Duration;
        float elapsed = 0.0f;
        WaitForSeconds delay = new(e.DamagePlayer.Interval);

        while (elapsed < e.Duration)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel || reloadCount < CheckpointManager.CheckpointUsage)
            {
                yield break; // checkpoint was used or not in level, exit
            }

            ApplyDamage(player, damagePerSecond, e.DamagePlayer.UseZone, id);
            elapsed += Time.deltaTime;
            yield return delay;
        }
    }

    private static void ApplyDamage(PlayerAgent player, float damage, bool useZone, int id)
    {
        if (!useZone || player.CourseNode.m_zone.ID == id)
            player.Damage.OnIncomingDamage((damage / 4.0f), default);
    }
}
