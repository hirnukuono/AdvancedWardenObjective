﻿using GTFO.API;
using SNetwork;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using static AWO.Networking.Patches.Patch_SNet_Capture;

namespace AWO.Networking;

public sealed partial class StateReplicator<S> where S : struct
{
    public static readonly string Name;
    public static readonly string HashName;
    public static readonly string ClientRequestEventName;
    public static readonly string HostSetStateEventName;
    public static readonly string HostSetRecallStateEventName;
    public static readonly int StateSize;
    public static readonly StatePayloads.Size StateSizeType;

    private static readonly IReplicatorEvent<S> _C_RequestEvent;
    private static readonly IReplicatorEvent<S> _H_SetStateEvent;
    private static readonly IReplicatorEvent<S> _H_SetRecallStateEvent;
    private static readonly ReplicatorHandshake _Handshake;

    private static readonly Dictionary<uint, StateReplicator<S>> _Replicators = new();

    static StateReplicator()
    {
        Name = typeof(S).Name;

        StateSize = Marshal.SizeOf(typeof(S));
        StateSizeType = StatePayloads.GetSizeType(StateSize);

        using var md5 = MD5.Create();
        byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(typeof(S).FullName));
        HashName = Convert.ToBase64String(bytes);
        ClientRequestEventName = $"SRs{Name}-{HashName}";
        HostSetStateEventName = $"SRr{Name}-{HashName}";
        HostSetRecallStateEventName = $"SRre{Name}-{HashName}";

        _C_RequestEvent = StatePayloads.CreateEvent<S>(StateSizeType, ClientRequestEventName, ClientRequestEventCallback);
        _H_SetStateEvent = StatePayloads.CreateEvent<S>(StateSizeType, HostSetStateEventName, HostSetStateEventCallback);
        _H_SetRecallStateEvent = StatePayloads.CreateEvent<S>(StateSizeType, HostSetRecallStateEventName, HostSetRecallStateEventCallback);
        _Handshake = ReplicatorHandshake.Create($"{Name}-{HashName}");
        _Handshake.OnClientSyncRequested += ClientSyncRequested;

        OnBufferCapture += BufferStored;
        OnBufferRecalled += BufferRecalled;
        LevelAPI.OnLevelCleanup += LevelCleanedUp;
    }

    private static void ClientSyncRequested(SNet_Player requestedPlayer)
    {
        foreach (var replicator in _Replicators.Values)
        {
            if (replicator.IsValid)
                replicator.SendDropInState(requestedPlayer);
        }
    }

    private static void BufferStored(eBufferType type)
    {
        foreach (var replicator in _Replicators.Values)
        {
            if (replicator.IsValid)
                replicator.SaveSnapshot(type);
        }
    }

    private static void BufferRecalled(eBufferType type)
    {
        foreach (var replicator in _Replicators.Values)
        {
            if (replicator.IsValid)
            {
                replicator.RestoreSnapshot(type);
            }
        }
    }

    private static void LevelCleanedUp()
    {
        UnloadSessionReplicator();
    }

    private StateReplicator() { }

    public static StateReplicator<S> Create(uint replicatorID, S startState, LifeTimeType lifeTime, IStateReplicatorHolder<S> holder = null)
    {
        if (replicatorID == 0u)
        {
            Logger.Error("Replicator ID 0 is reserved for empty!");
            return null;
        }

        if (_Replicators.ContainsKey(replicatorID))
        {
            Logger.Error("Replicator ID has already assigned!");
            return null;
        }

        var replicator = new StateReplicator<S>
        {
            ID = replicatorID,
            LifeTime = lifeTime,
            Holder = holder,
            State = startState
        };

        if (lifeTime == LifeTimeType.Permanent)
        {
            Logger.Debug($"LifeTime is {nameof(LifeTimeType.Permanent)} :: Handshaking is disabled!");
        }
        else if (lifeTime == LifeTimeType.Session)
        {
            _Handshake.UpdateCreated(replicatorID);
        }
        else
        {
            Logger.Error($"LifeTime is invalid!: {lifeTime}");
            return null;
        }


        _Replicators[replicatorID] = replicator;
        return replicator;
    }

    public static void UnloadSessionReplicator()
    {
        List<uint> idsToRemove = new();
        foreach (var replicator in _Replicators.Values)
        {
            if (replicator.LifeTime == LifeTimeType.Session)
            {
                idsToRemove.Add(replicator.ID);
                replicator.Unload();
            }
        }

        foreach (var id in idsToRemove)
        {
            _Replicators.Remove(id);
        }

        _Handshake.Reset();
    }

    private static void ClientRequestEventCallback(ulong sender, uint replicatorID, S newState)
    {
        if (!SNet.IsMaster)
            return;

        if (_Replicators.TryGetValue(replicatorID, out var replicator))
        {
            replicator.SetState(newState);
        }
    }

    private static void HostSetStateEventCallback(ulong sender, uint replicatorID, S newState)
    {
        if (!SNet.HasMaster)
            return;

        if (SNet.Master.Lookup != sender)
            return;

        if (_Replicators.TryGetValue(replicatorID, out var replicator))
        {
            replicator.Internal_ChangeState(newState, isRecall: false);
        }
    }

    private static void HostSetRecallStateEventCallback(ulong sender, uint replicatorID, S newState)
    {
        if (!SNet.HasMaster)
            return;

        if (SNet.Master.Lookup != sender)
            return;

        if (_Replicators.TryGetValue(replicatorID, out var replicator))
        {
            replicator.Internal_ChangeState(newState, isRecall: true);
        }
    }
}