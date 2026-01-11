using AmorLib.Utils;
using AmorLib.Utils.Extensions;
using AmorLib.Utils.JsonElementConverters;
using BepInEx;
using BepInEx.Logging;
using GTFO.API;
using LevelGeneration;
using System.Text;
using System.Text.RegularExpressions;

namespace AWO.Modules.TSL;

public static class SerialLookupManager
{
    public static readonly Dictionary<string, Dictionary<(int, int, int), List<string>>> SerialMap = new();
    private static readonly Queue<LG_SecurityDoor_Locks> LocksQueue = new();

    private const string Pattern = @"\[(?<ItemName>.+?)_(?:(?:[^\d_]*)(?<Dimension>\d+))_(?:(?:[^\d_]*)(?<Layer>\d+))_(?:(?:[^\d_]*)(?<Zone>\d+))(?:_(?<InstanceIndex>\d+))?\]";
    private const string Terminal = "TERMINAL";
    private const string Zone = "ZONE";
    private static readonly string Module = nameof(SerialLookupManager);

    internal static void Init()
    {
        LevelAPI.OnBuildDone += BuildSerialMap;
        LevelAPI.OnEnterLevel += OnEnterLevel;
        LevelAPI.OnLevelCleanup += Cleanup;

        InteropAPI.RegisterCall("TSL-ParseTextFragments", args =>
        {
            if (args?.Length > 0 && args[0] is string input)
            {
                return ParseTextFragments(input);
            }

            return null;
        });
    }

    private static void BuildSerialMap()
    {
        Logger.Verbose(LogLevel.Debug, "Building serial map...");
        int count = 0;

        // collect all general terminal items
        foreach (var serial in LG_LevelInteractionManager.GetAllTerminalInterfaces())
        {
            try
            {
                if (serial.Key == null || serial.Value?.SpawnNode == null) continue;
                int split = serial.Key.LastIndexOf('_');
                if (split == -1) continue;
                string itemName = serial.Key.Substring(0, split);
                string serialNumber = serial.Key.Substring(split + 1);
                
                if (serial.Value.FloorItemType == eFloorInventoryObjectType.Terminal) continue; // skip terminals here
                else if (!int.TryParse(serialNumber, out _))
                {
                    Logger.Warn(Module, $"{serial.Key} does not have a serial number");
                    continue;
                }

                var globalIndex = serial.Value.SpawnNode.m_zone.ToIntTuple();
                SerialMap.GetOrAddNew(itemName).GetOrAddNew(globalIndex).Add(serialNumber);
                count++;
            }
            catch (Exception ex)
            {
                Logger.Verbose(LogLevel.Error, $"We encountered an exception iterating through LG_LevelInteractionManager.GetAllTerminalInterfaces [{count + 1}]:\n{ex}");
                continue;
            }
        }

        // collect all terminals and zone alias numbers
        foreach (var zone in Builder.CurrentFloor.allZones)
        {
            try
            {
                if (zone == null) continue;
                var globalIndex = zone.ToIntTuple();
                SerialMap.GetOrAddNew(Zone).GetOrAddNew(globalIndex).Add(zone.Alias.ToString());
                count++;

                if (zone.TerminalsSpawnedInZone == null) continue;

                foreach (var term in zone.TerminalsSpawnedInZone)
                {
                    if (term == null) continue;
                    int split = term.m_terminalItem.TerminalItemKey.LastIndexOf('_');
                    if (split == -1) continue;
                    string serialNumber = term.m_terminalItem.TerminalItemKey.Substring(split + 1);

                    SerialMap.GetOrAddNew(Terminal).GetOrAddNew(globalIndex).Add(serialNumber);
                    count++;
                } 

                var locks = zone.m_sourceGate?.SpawnedDoor?.TryCast<LG_SecurityDoor>()?.m_locks?.TryCast<LG_SecurityDoor_Locks>();
                if (locks == null) continue;
                LocksQueue.Enqueue(locks);
            }
            catch (Exception ex)
            {
                Logger.Verbose(LogLevel.Error, $"We encountered an exception iterating through ({zone.DimensionIndex}, {zone.Layer.m_type}, {zone.LocalIndex})'s TerminalsSpawnedInZone and LG_SecurityDoor_Locks:\n{ex}");
                continue;
            }
        }

        Logger.Verbose(LogLevel.Debug, PrintSerialMap());
        Logger.Info(Module, $"On build done, collected {count} serial numbers");
    }

    private static void OnEnterLevel()
    {
        while (LocksQueue.Count > 0)
        {
            var locks = LocksQueue.Dequeue();
            locks.m_intCustomMessage.m_message = ParseTextFragments(locks.m_intCustomMessage.m_message);
            locks.m_intOpenDoor.InteractionMessage = ParseTextFragments(locks.m_intOpenDoor.InteractionMessage);
            locks.m_intUseKeyItem.m_msgNeedItemHeader = ParseTextFragments(locks.m_intUseKeyItem.m_msgNeedItemHeader);
        }
    }

    private static void Cleanup()
    {
        LocksQueue.Clear();
        SerialMap.Clear();        
    }

    public static LocaleText ParseLocaleText(LocaleText input)
    {
        if (input == LocaleText.Empty) return input;
        return new(ParseTextFragments(input));
    }

    public static string ParseTextFragments(string input)
    {
        if (input.IsNullOrWhiteSpace()) 
            return input;
        
        var spans = new List<(int start, int end)>();
        var stack = new Stack<int>();
        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '[')
            {
                stack.Push(i);
            }
            else if (input[i] == ']' && stack.Count > 0)
            {
                int top = stack.Pop();
                spans.Add((top, i));
            }
        }
        
        StringBuilder result = new(input);
        var tagIndicesPairs = spans.Where(s => !spans.Any(t => t.start > s.start && t.end < s.end)).ToList();
        foreach (var (start, end) in tagIndicesPairs)
        {
            string spanText = input.Substring(start, end - start + 1);
            var match = Regex.Match(spanText, Pattern);

            if (match.Success && TryFindSerialNumber(match, out var serialStr))
            {
                result.Replace(spanText, serialStr);
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
        var globalIndex = (dimension, layer, zone);

        if (SerialMap.TryGetValue(itemName, out var localSerialMap) && localSerialMap.TryGetValue(globalIndex, out var serialList))
        {
            if (instanceIndex < serialList.Count)
            {
                string serialNumber = serialList[instanceIndex];
                serialStr = $"<color=orange>{itemName}{(itemName != Zone ? "_" : " ")}{serialNumber}</color>";
                return true;
            }
        }

        serialStr = match.Value;
        Logger.Error(Module, $"No match found for TerminalItem: '{itemName}' in (D{dimension}, L{layer}, Z{zone}) at instance #{instanceIndex}");
        return false;
    }

    public static string PrintSerialMap()
    {
        StringBuilder sb = new();
        foreach (var outer in SerialMap)
        {
            sb.AppendLine($"Item: {outer.Key}");
            foreach (var inner in outer.Value)
            {
                sb.Append($"\t(D{inner.Key.Item1}, L{inner.Key.Item2}, Z{inner.Key.Item3}): ");
                if (inner.Value.Count > 0)
                {
                    sb.AppendLine(string.Join(", ", inner.Value));
                }
            }
        }
        return $"TERMINAL SERIAL MAP\n{sb}";
    }
}
