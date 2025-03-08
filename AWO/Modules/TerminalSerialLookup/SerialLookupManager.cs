using BepInEx;
using GTFO.API;
using LevelGeneration;
using System.Text;
using System.Text.RegularExpressions;

namespace AWO.Modules.TerminalSerialLookup;

public static class SerialLookupManager
{
    public readonly static Dictionary<string, Dictionary<(int, int, int), List<String>>> SerialMap = new();
    private const string Pattern = @"\[(?<ItemName>[^\[]+?)_(?<Dimension>\d+)_(?<Layer>\d+)_(?<Zone>\d+)(?:_(?<InstanceIndex>\d+))?\]";
    private const string Terminal = "TERMINAL";
    private const string Zone = "ZONE";

    internal static void Init()
    {
        LevelAPI.OnBuildDone += BuildSerialMap;
        LevelAPI.OnLevelCleanup += Cleanup;
    }

    private static void BuildSerialMap()
    {
        int count = 0;
        
        // collect all general terminal items
        foreach (var serial in LG_LevelInteractionManager.GetAllTerminalInterfaces())
        {
            if (serial?.Value.SpawnNode == null) continue;
            int split = serial.Key.LastIndexOf('_');
            if (split == -1) continue;
            string itemName = serial.Key.Substring(0, split);
            string serialNumber = serial.Key.Substring(split + 1);

            if (itemName == Terminal && serial.Value.SpawnNode.m_dimension.DimensionData.IsStaticDimension) continue; // skip static terminals here

            int dimension = (int)serial.Value.SpawnNode.m_dimension.DimensionIndex;
            int layer = (int)serial.Value.SpawnNode.LayerType;
            int zone = (int)serial.Value.SpawnNode.m_zone.LocalIndex;
            var globalIndex = (dimension, layer, zone);

            SerialMap.GetOrAddNew(itemName).GetOrAddNew(globalIndex).Add(serialNumber);
            count++;
        }

        // collect all static terminals
        foreach (var dimension in Builder.CurrentFloor.m_dimensions)
        {
            if (dimension.DimensionData.IsStaticDimension)
            {
                var node = dimension.GetStartCourseNode();
                int dimensionIndex = (int)dimension.DimensionIndex;
                int layer = (int)node.LayerType;
                var globalIndex = (dimensionIndex, layer, 0);

                foreach (var staticTerm in node.m_zone.TerminalsSpawnedInZone)
                {
                    int split = staticTerm.m_terminalItem.TerminalItemKey.LastIndexOf('_');
                    if (split == -1) continue;
                    string serialNumber = staticTerm.m_terminalItem.TerminalItemKey.Substring(split + 1);

                    SerialMap.GetOrAddNew(Terminal).GetOrAddNew(globalIndex).Add(serialNumber);
                    count++;
                }
            }
        }

        // collect all zone alias numbers
        foreach (var zone in Builder.CurrentFloor.allZones)
        {
            var globalIndex = ((int)zone.DimensionIndex, (int)zone.Layer.m_type, (int)zone.LocalIndex);
            SerialMap.GetOrAddNew(Zone).GetOrAddNew(globalIndex).Add(zone.Alias.ToString());
            count++;
        }
        
        // parse door interaction text
        foreach (var locks in UnityEngine.Object.FindObjectsOfType<LG_SecurityDoor_Locks>())
        {            
            locks.m_intCustomMessage.m_message = ParseTextFragments(locks.m_intCustomMessage.m_message);
            locks.m_intOpenDoor.InteractionMessage = ParseTextFragments(locks.m_intOpenDoor.InteractionMessage);
        }

        Logger.Info($"[SerialLookupManager] On build done, collected {count} serial numbers");
    }

    private static void Cleanup()
    {
        SerialMap.Clear();
    }

    public static string ParseTextFragments(string input)
    {
        if (input.IsNullOrWhiteSpace()) return input;

        StringBuilder result = new(input);
        foreach (Match match in Regex.Matches(input, Pattern))
        {
            if (match.Success && TryFindSerialNumber(match, out string serialStr))
            {
                result.Replace(match.Value, serialStr);
            }
        }

        return result.ToString();
    }

    public static bool TryFindSerialNumber(Match match, out string serialStr)
    {
        string itemName = match.Groups["ItemName"].Value;
        int dimension = int.Parse(match.Groups["Dimension"].Value);
        int layer = int.Parse(match.Groups["Layer"].Value);
        int zone = int.Parse(match.Groups["Zone"].Value);
        int instanceIndex = match.Groups["InstanceIndex"].Success ? int.Parse(match.Groups["InstanceIndex"].Value) : 0;

        if (SerialMap.TryGetValue(itemName, out var localSerialMap) && localSerialMap.TryGetValue((dimension, layer, zone), out var serialList))
        {
            if (instanceIndex < serialList.Count)
            {
                string serialNumber = serialList[instanceIndex];
                serialStr = $"<color=orange>{itemName}{(itemName != Zone ? "_" : " ")}{serialNumber}</color>";
                return true;
            }
        }

        serialStr = match.Value;
        Logger.Error($"[SerialLookupManager] No match found for TerminalItem: '{itemName}' in (D{dimension}, L{layer}, Z{zone}) at instance #{instanceIndex}");
        return false;
    }
}
