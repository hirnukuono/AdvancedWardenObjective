using BepInEx;
using GTFO.API;
using Player;
using SNetwork;
using UnityEngine;
using TagType = AWO.Modules.WEE.WEE_SetPocketItem.PlayerTagType;

namespace AWO.Modules.WEE.Events;

internal sealed class SetPocketItemEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SetPocketItem;

    public readonly static Dictionary<int, WEE_SetPocketItem> PocketItemsMap = new();
    public static bool HasEmptyPockets => PocketItemsMap.Count == 0;
    public static string TopItems { get; private set; } = string.Empty;
    public static string BottomItems { get; private set; } = string.Empty;

    protected override void OnSetup()
    {
        LevelAPI.OnLevelCleanup += OnLevelCleanup;
    }

    private void OnLevelCleanup()
    {
        PocketItemsMap.Clear();
        TopItems = string.Empty;
        BottomItems = string.Empty;
    }

    protected override void TriggerCommon(WEE_EventData e)
    {
        foreach (var pItem in e.ObjectiveItems)
        {
            int count = ResolveFieldsFallback(e.Count, pItem.Count, false);
            int index = pItem.Index;

            if (!PocketItemsMap.ContainsKey(index)) // Add new item
            {
                var slots = SNet.Slots.SlottedPlayers;
                pItem.Count = count;
                pItem.Tag = pItem.TagType switch
                {
                    TagType.Custom => pItem.CustomTag,
                    TagType.Specific => slots[(int)pItem.PlayerIndex]?.GetName(),
                    TagType.Random => slots[EntryPoint.SessionRand.Next(slots.Count)]?.GetName(),
                    TagType.Closest => GetClosestPlayerName(e.Position),
                    _ => null
                };

                if (pItem.Tag.IsNullOrWhiteSpace()) continue;

                PocketItemsMap[index] = pItem;
            }
            else if (!pItem.ShouldRemove) // Update item count
            {
                PocketItemsMap[index].Count = count;
            }
            else // Remove item
            {
                PocketItemsMap.Remove(index);
            }
        }

        TopItems = string.Join("\n", PocketItemsMap.Values.Where(pItem => pItem.IsOnTop).Select(pItem => pItem.FormatString()));
        BottomItems = string.Join("\n", PocketItemsMap.Values.Where(pItem => !pItem.IsOnTop).Select(pItem => pItem.FormatString()));

        PlayerBackpackManager.UpdatePocketItemGUI();
    }

    private static string? GetClosestPlayerName(Vector3 pos)
    {
        float minDist = float.MaxValue;
        PlayerAgent? nearestPlayer = null;
        foreach (var currentPlayer in PlayerManager.PlayerAgentsInLevel)
        {
            float dist = Vector3.Distance(pos, currentPlayer.Position);
            if (dist < minDist)
            {
                minDist = dist;
                nearestPlayer = currentPlayer;
            }
        }
        return nearestPlayer?.PlayerName;
    }
}
