using GTFO.API;
using LevelGeneration;

namespace AWO.Modules.WOE;

[Obsolete]
public static class WardenObjectiveExt
{
    private static readonly Dictionary<eWardenObjectiveType, Type> _DTOTypes = new();
    private static readonly List<WOE_ContextBase> _ActiveContexts = new();

    static WardenObjectiveExt()
    {
        var contextTypes = typeof(WOE_ContextBase).Assembly.GetTypes()
            .Where(x => !x.IsAbstract)
            .Where(x => x.IsAssignableTo(typeof(WOE_ContextBase)));

        foreach (var type in contextTypes)
        {
            var instance = (WOE_ContextBase)Activator.CreateInstance(type)!;
            if (_DTOTypes.TryGetValue(instance.TargetType, out var existing))
            {
                Logger.Error($"Duplicate {nameof(WOE_ContextBase.TargetType)} Detected!");
                Logger.Error($"With '{type.Name}' and '{instance.GetType().Name}'");
                continue;
            }

            if (!instance.DataType.IsAssignableTo(typeof(WOE_DataBase)))
            {
                Logger.Error($"{type.Name} does not have valid {nameof(WOE_ContextBase.DataType)} (not derived from {nameof(WOE_DataBase)})");
                continue;
            }

            _DTOTypes[instance.TargetType] = type;
        }

        WOEvents.OnSetup += ObjectiveSetup;
        LevelAPI.OnLevelCleanup += LevelCleanup;
    }

    internal static void Initialize()
    {

    }

    private static void ObjectiveSetup(LG_LayerType layer, int chainIndex)
    {
        if (!WOManager.TryGetWardenObjectiveDataForLayer(layer, chainIndex, out var data))
        {
            Logger.Error($"{layer} Layer (CI: {chainIndex}) does not have ObjectiveData!!!");
            return;
        }
        
        if (!_DTOTypes.TryGetValue(data.Type, out var type))
        {
            return;
        }

        var context = (WOE_ContextBase)Activator.CreateInstance(type)!;
        context.Setup(layer, chainIndex);
        _ActiveContexts.Add(context);
    }

    private static void LevelCleanup()
    {
        foreach(var context in _ActiveContexts)
        {
            context.OnLevelCleanup();
        }
        _ActiveContexts.Clear();
    }
}
