using AWO.Networking;
using GTFO.API;
using LevelGeneration;

namespace AWO.Sessions;

internal enum LevelFailMode
{
    Default,
    Never,
    AnyPlayerDown
}

internal struct LevelFailCheck
{
    public LevelFailMode mode;
}

internal sealed class LevelFailUpdateState
{
    private static StateReplicator<LevelFailCheck>? _Replicator;
    public static bool LevelFailAllowed { get; private set; } = true;
    public static bool LevelFailWhenAnyPlayerDown { get; private set; } = false;

    internal static void AssetLoaded()
    {
        if (_Replicator != null) return;

        /*if (!StateReplicator<LevelFailCheck>.TryCreate(1u, new() { mode = LevelFailMode.Default }, LifeTimeType.Permanent, out var replicator))
        {
            Logger.Error("Failed to create LevelFailUpdateState Replicator!");
            return;
        }

        _Replicator = replicator;*/

        _Replicator = StateReplicator<LevelFailCheck>.Create(1u, new() { mode = LevelFailMode.Default }, LifeTimeType.Permanent);
        LG_Factory.add_OnFactoryBuildStart(new Action(() =>
        {
            _Replicator.ClearAllRecallSnapshot();
            _Replicator.SetState(new() { mode = LevelFailMode.Default });
        }));

        _Replicator.OnStateChanged += OnStateChanged;
        LevelAPI.OnLevelCleanup += LevelCleanup;
    }

    private static void LevelCleanup()
    {
        SetFailAllowed(true);
    }

    public static void SetFailAllowed(bool allowed)
    {
        _Replicator?.SetState(new()
        {
            mode = allowed ? LevelFailMode.Default : LevelFailMode.Never
        });
    }

    public static void SetFailWhenAnyPlayerDown(bool enabled)
    {
        _Replicator?.SetState(new()
        {
            mode = enabled ? LevelFailMode.AnyPlayerDown : LevelFailMode.Default
        });
    }

    private static void OnStateChanged(LevelFailCheck _, LevelFailCheck state, bool __)
    {
        switch (state.mode)
        {
            case LevelFailMode.Default:
                LevelFailAllowed = true;
                LevelFailWhenAnyPlayerDown = false;
                break;

            case LevelFailMode.Never:
                LevelFailAllowed = false;
                LevelFailWhenAnyPlayerDown = false;
                break;

            case LevelFailMode.AnyPlayerDown:
                LevelFailAllowed = true;
                LevelFailWhenAnyPlayerDown = true;
                break;
        }
    }
}
