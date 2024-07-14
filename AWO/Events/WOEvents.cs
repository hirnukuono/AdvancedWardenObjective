using LevelGeneration;

namespace AWO.Events;

public delegate void SetupObjectiveDel(LG_LayerType layer, int chainIndex);

internal static class WOEvents
{
    public static event SetupObjectiveDel OnSetup;

    internal static void Invoke_OnSetup(LG_LayerType layer, int chainIndex) => OnSetup?.Invoke(layer, chainIndex);
}
