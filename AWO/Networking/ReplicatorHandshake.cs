using GTFO.API;
using SNetwork;

namespace AWO.Networking;

public sealed class ReplicatorHandshake
{
    public delegate void ClientRequestedSyncDel(SNet_Player requestedPlayer);

    public event ClientRequestedSyncDel? OnClientSyncRequested;
    public string EventName { get; private set; }
    public bool IsReadyToSync { get; private set; }
    private readonly Dictionary<uint, Data> _Lookup = new();

    public static ReplicatorHandshake? Create(string guid)
    {
        if (string.IsNullOrWhiteSpace(guid))
            return null;

        var eventName = $"RHs{guid}";
        return NetworkAPI.IsEventRegistered(eventName) ? null : (new(eventName));
    }

    private ReplicatorHandshake(string eventName)
    {
        EventName = eventName;
        NetworkAPI.RegisterEvent<Packet>(eventName, OnSyncAction);
    }

    public void Reset()
    {
        _Lookup.Clear();
    }

    private void OnSyncAction(ulong sender, Packet packet)
    {
        if (!SNet.IsMaster && sender == SNet.Master.Lookup)
        {
            if (packet.action == PacketAction.Created)
                SetHostState(packet.replicatorID, isSetup: true);
            else if (packet.action == PacketAction.Destroyed)
                SetHostState(packet.replicatorID, isSetup: false);
        }
        else if (SNet.IsMaster)
        {
            if (packet.action == PacketAction.Created)
                SetClientState(packet.replicatorID, isSetup: true);
            else if (packet.action == PacketAction.Destroyed)
                SetClientState(packet.replicatorID, isSetup: false);
            else if (packet.action == PacketAction.SyncRequest)
            {
                if (!SNet.TryGetPlayer(sender, out var player))
                {
                    Logger.Error($"Cannot find player from sender: {sender}");
                    return;
                }
                OnClientSyncRequested?.Invoke(player);
            }
        }
    }

    public void UpdateCreated(uint id)
    {
        if (SNet.IsInLobby)
        {
            if (SNet.IsMaster)
            {
                SetHostState(id, isSetup: true);
                NetworkAPI.InvokeEvent(EventName, new Packet()
                {
                    replicatorID = id,
                    action = PacketAction.Created
                });
            }
            else if (SNet.HasMaster)
            {
                SetClientState(id, isSetup: true);
                NetworkAPI.InvokeEvent(EventName, new Packet()
                {
                    replicatorID = id,
                    action = PacketAction.Created
                }, SNet.Master);
            }
            else
            {
                Logger.Error("Handshake::MASTER is NULL in lobby; This should NOT happen!!!!!!!!!!!!");
            }
        }
        else
        {
            Logger.Error("Handshake::Session Type StateReplicator cannot be created without lobby!");
        }
    }

    public void UpdateDestroyed(uint id)
    {
        if (SNet.IsInLobby)
        {
            if (SNet.IsMaster)
            {
                SetHostState(id, isSetup: true);
                NetworkAPI.InvokeEvent(EventName, new Packet()
                {
                    replicatorID = id,
                    action = PacketAction.Destroyed
                });
            }
            else if (SNet.HasMaster)
            {
                SetClientState(id, isSetup: true);
                NetworkAPI.InvokeEvent(EventName, new Packet()
                {
                    replicatorID = id,
                    action = PacketAction.Destroyed
                }, SNet.Master);
            }
            else
            {
                Logger.Error("Handshake::MASTER is NULL in lobby; This should NOT happen!!!!!!!!!!!!");
            }
        }
        else
        {
            Logger.Error("Handshake::Session Type StateReplicator cannot be created without lobby!");
        }
    }

    private void SetHostState(uint id, bool isSetup)
    {
        if (_Lookup.TryGetValue(id, out var data))
        {
            data.SetupOnHost = isSetup;
        }
        else
        {
            _Lookup[id] = new() { SetupOnHost = isSetup };
        }

        UpdateSyncState(id);
    }

    private void SetClientState(uint id, bool isSetup)
    {
        if (_Lookup.TryGetValue(id, out var data))
        {
            data.SetupOnClient = isSetup;
        }
        else
        {
            _Lookup[id] = new() { SetupOnClient = isSetup };
        }

        UpdateSyncState(id);
    }

    private void UpdateSyncState(uint id)
    {
        var isReadyOld = IsReadyToSync;

        if (_Lookup.TryGetValue(id, out var data))
        {
            IsReadyToSync = data.SetupOnHost && data.SetupOnClient;
        }
        else
        {
            IsReadyToSync = false;
        }

        if (IsReadyToSync && isReadyOld != IsReadyToSync)
        {
            if (!SNet.HasMaster || SNet.IsMaster)
                return;

            NetworkAPI.InvokeEvent(EventName, new Packet()
            {
                replicatorID = id,
                action = PacketAction.SyncRequest
            }, SNet.Master);
        }
    }

    public struct Packet
    {
        public uint replicatorID;
        public PacketAction action;
    }

    public enum PacketAction : byte
    {
        Created,
        Destroyed,
        SyncRequest
    }

    public sealed class Data
    {
        public bool SetupOnHost = false;
        public bool SetupOnClient = false;
    }
}
