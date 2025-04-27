namespace AWO.Networking;

public interface IStateReplicatorHolder<S> where S : struct
{
    void OnStateChange(S oldState, S state, bool isRecall);
    public StateReplicator<S>? Replicator { get; }
}
