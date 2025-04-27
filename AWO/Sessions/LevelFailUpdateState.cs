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
    public static StateReplicator<LevelFailCheck>? Replicator;
    public static bool LevelFailAllowed { get; private set; } = true;
    public static bool LevelFailWhenAnyPlayerDown { get; private set; } = false;

    internal static void AssetLoaded()
    {
        Replicator = StateReplicator<LevelFailCheck>.Create(1u, new() { mode = LevelFailMode.Default }, LifeTimeType.Permanent);
        LG_Factory.add_OnFactoryBuildStart(new Action(() =>
        {
            Replicator.ClearAllRecallSnapshot();
            Replicator.SetState(new() { mode = LevelFailMode.Default });
        }));

        Replicator.OnStateChanged += OnStateChanged;
        LevelAPI.OnLevelCleanup += LevelCleanup;
    }

    private static void LevelCleanup()
    {
        SetFailAllowed(true);
    }

    public static void SetFailAllowed(bool allowed)
    {
        Replicator?.SetState(new()
        {
            mode = allowed ? LevelFailMode.Default : LevelFailMode.Never
        });
    }

    public static void SetFailWhenAnyPlayerDown(bool enabled)
    {
        Replicator?.SetState(new()
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
