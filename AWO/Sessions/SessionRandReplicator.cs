using AmorLib.Networking.StateReplicators;
using BepInEx.Logging;

namespace AWO.Sessions;

public struct SessionRandState
{
    public uint currentStep;
}

public sealed class SessionRandReplicator : IStateReplicatorHolder<SessionRandState>
{
    public StateReplicator<SessionRandState>? Replicator { get; private set; }
    public int Seed { get; private set; }
    public uint Step { get; private set; }
    public uint State { get; private set; }

    public void Setup(uint id, int seed) // reserved id: 1u
    {
        Seed = seed;
        Step = 0u;
        State = (uint)Seed;
        Logger.Info($"SessionSeed {Seed}");
        Replicator = StateReplicator<SessionRandState>.Create(id, new() { currentStep = Step }, LifeTimeType.Session, this);
    }

    public void Cleanup()
    {
        Step = 0u;
        Replicator?.Unload();
    }
    
    public void SyncStep()
    {
        Replicator?.SetState(new() { currentStep = Step });
    }
    
    public void OnStateChange(SessionRandState oldState, SessionRandState state, bool isRecall)
    {
        if (state.currentStep == Step)
        {
            Logger.Verbose(LogLevel.Debug, $"No change in SessionRand step from received state, isRecall: {isRecall}");
            return;
        }
        else if (state.currentStep < Step)
        {
            Logger.Verbose(LogLevel.Warning, $"SessionRand step cannot be lowered by received state, isRecall: {isRecall}");
            Replicator?.SetStateUnsynced(new() { currentStep = Step });
            return;
        }

        Logger.Debug($"Jumping ahead from local step {Step} to session step {state.currentStep}, isRecall: {isRecall}");
        Step = state.currentStep;
        Jump(Step);        
    }

    // Adapted from https://rosettacode.org/wiki/Pseudo-random_numbers/Splitmix64 + TommyYettinger and bryc
    public uint Next() // SplitMix32 PRNG
    {
        Replicator?.SetStateUnsynced(new() { currentStep = ++Step });
        uint z = State += 0x9e3779b9;
        z ^= z >> 16;
        z *= 0x21f0aaad;
        z ^= z >> 15;
        z *= 0x735a2d97;
        z ^= z >> 15;
        return z;
    }

    public void Jump(ulong steps) // jump client ahead to current step
    {
        if (steps > 0)
        {
            unchecked
            {
                State += (uint)(steps * 0x9e3779b9UL);
            }
        }
    }

    /* random methods */
    public int NextInt()
    {
        return (int)(Next() & 0x7FFFFFFF);
    }

    public int NextInt(int max)
    {
        if (max <= 0)
            throw new ArgumentOutOfRangeException(nameof(max), "max must be positive.");

        return (int)(NextFloat() * max);
    }

    public int NextInt(int min, int max)
    {
        if (min > max)
            throw new ArgumentOutOfRangeException(nameof(min), "min must be less than or equal to max.");

        return min + NextInt(max - min);
    }

    public float NextFloat()
    {
        return Next() * (1.0f / 4294967296.0f);
    }
}
