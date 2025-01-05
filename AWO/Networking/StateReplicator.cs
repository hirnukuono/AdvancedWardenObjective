using SNetwork;

namespace AWO.Networking;

public enum LifeTimeType
{
    Permanent,
    Session
}

public sealed partial class StateReplicator<S> where S : struct
{
    public bool IsValid => ID != 0u;
    public bool IsInvalid => ID == 0u;
    public uint ID { get; private set; }
    public LifeTimeType LifeTime { get; private set; }
    public IStateReplicatorHolder<S> Holder { get; private set; }
    public S State { get; private set; }
    public bool ClientSendStateAllowed { get; set; } = true;
    public bool CanSendToClient => SNet.IsInLobby && SNet.IsMaster;
    public bool CanSendToHost => SNet.IsInLobby && !SNet.IsMaster && SNet.HasMaster && ClientSendStateAllowed;

    private readonly Dictionary<eBufferType, S> _RecallStateSnapshots = new();

    public event Action<S, S, bool> OnStateChanged;

    public void SetState(S state)
    {
        if (IsInvalid)
            return;

        DoSync(state);
    }

    public void SetStateUnsynced(S state)
    {
        if (IsInvalid)
            return;

        State = state;
    }

    public void Unload()
    {
        if (IsValid)
        {
            _Replicators.Remove(ID);
            _RecallStateSnapshots.Clear();
            _Handshake.UpdateDestroyed(ID);
            ID = 0u;
        }
    }

    private void DoSync(S newState)
    {
        if (IsInvalid)
            return;

        if (CanSendToClient)
        {
            _H_SetStateEvent.Invoke(ID, newState);
            Internal_ChangeState(newState, false);
        }
        else if (CanSendToHost)
        {
            _C_RequestEvent.Invoke(ID, newState, SNet.Master);
        }
    }

    private void Internal_ChangeState(S state, bool isRecall)
    {
        if (IsInvalid)
            return;

        var oldState = State;
        State = state;

        OnStateChanged?.Invoke(oldState, state, isRecall);
        Holder?.OnStateChange(oldState, state, isRecall);
    }

    private void SendDropInState(SNet_Player target)
    {
        if (IsInvalid)
            return;

        if (target == null)
        {
            Logger.Error($"{nameof(SendDropInState)}::Target was null??");
            return;
        }

        _H_SetRecallStateEvent.Invoke(ID, State, target);
    }

    public void ClearAllRecallSnapshot()
    {
        if (IsInvalid)
            return;

        _RecallStateSnapshots.Clear();
    }

    private void SaveSnapshot(eBufferType type)
    {
        if (IsInvalid)
            return;

        _RecallStateSnapshots[type] = State;
    }

    private void RestoreSnapshot(eBufferType type)
    {
        if (IsInvalid)
            return;

        if (CanSendToClient)
        {
            if (_RecallStateSnapshots.TryGetValue(type, out var savedState))
            {
                _H_SetRecallStateEvent.Invoke(ID, savedState);
                Internal_ChangeState(savedState, isRecall: true);
            }
            else
            {
                Logger.Error($"{nameof(RestoreSnapshot)}::There was no snapshot for {type}?");
                return;
            }
        }
    }
}