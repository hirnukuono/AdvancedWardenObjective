using GTFO.API;
using SNetwork;
using System;
using System.Runtime.InteropServices;

namespace AWO.Networking;

public delegate void OnReceiveDel<S>(ulong sender, uint replicatorID, S newState) where S : struct;

public static class StatePayloads
{
    public enum Size : int
    {
        State4Byte = 4,
        State8Byte = 8,
        State16Byte = 16,
        State32Byte = 32,
        State48Byte = 48,
        State64Byte = 64,
        State80Byte = 80,
        State96Byte = 96,
        State128Byte = 128,
        State196Byte = 196,
        State256Byte = 256
    }

    public static Size GetSizeType(int size)
    {
        Size highestSizeCap = Size.State8Byte;
        foreach (var sizeType in Enum.GetValues(typeof(Size)))
        {
            if (size <= (int)sizeType && (int)highestSizeCap < (int)sizeType)
            {
                highestSizeCap = (Size)sizeType;
                break;
            }
        }

        return highestSizeCap;
    }

    public static IReplicatorEvent<S> CreateEvent<S>(Size size, string eventName, OnReceiveDel<S> onReceiveCallback) where S : struct
    {
        return size switch
        {
            Size.State4Byte => ReplicatorPayloadWrapper<S, StatePayload4Byte>.Create(eventName, onReceiveCallback),
            Size.State8Byte => ReplicatorPayloadWrapper<S, StatePayload8Byte>.Create(eventName, onReceiveCallback),
            Size.State16Byte => ReplicatorPayloadWrapper<S, StatePayload16Byte>.Create(eventName, onReceiveCallback),
            Size.State32Byte => ReplicatorPayloadWrapper<S, StatePayload32Byte>.Create(eventName, onReceiveCallback),
            Size.State48Byte => ReplicatorPayloadWrapper<S, StatePayload48Byte>.Create(eventName, onReceiveCallback),
            Size.State64Byte => ReplicatorPayloadWrapper<S, StatePayload64Byte>.Create(eventName, onReceiveCallback),
            Size.State80Byte => ReplicatorPayloadWrapper<S, StatePayload80Byte>.Create(eventName, onReceiveCallback),
            Size.State96Byte => ReplicatorPayloadWrapper<S, StatePayload96Byte>.Create(eventName, onReceiveCallback),
            Size.State128Byte => ReplicatorPayloadWrapper<S, StatePayload128Byte>.Create(eventName, onReceiveCallback),
            Size.State196Byte => ReplicatorPayloadWrapper<S, StatePayload196Byte>.Create(eventName, onReceiveCallback),
            Size.State256Byte => ReplicatorPayloadWrapper<S, StatePayload256Byte>.Create(eventName, onReceiveCallback),
            _ => null,
        };
    }

    public static S Get<S>(byte[] bytes, int bytesLength) where S : struct
    {
        int dataSize = Marshal.SizeOf(typeof(S));
        if (dataSize > bytesLength)
        {
            throw new ArgumentException($"StateData Exceed size of {bytesLength} : Unable to Deserialize", nameof(S));
        }

        IntPtr ptr = Marshal.AllocHGlobal(dataSize);
        Marshal.Copy(bytes, 0, ptr, dataSize);
        S obj = (S)Marshal.PtrToStructure(ptr, typeof(S));
        Marshal.FreeHGlobal(ptr);
        return obj;
    }

    public static void Set<S>(S stateData, int size, ref byte[] payloadBytes) where S : struct
    {
        int dataSize = Marshal.SizeOf(stateData);

        if (dataSize > size)
        {
            throw new ArgumentException($"StateData Exceed size of {size} : Unable to Serialize", nameof(S));
        }

        byte[] bytes = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(stateData, ptr, false);
        Marshal.Copy(ptr, bytes, 0, size);
        Marshal.FreeHGlobal(ptr);

        payloadBytes = bytes;
    }
}

public interface IReplicatorEvent<S> where S : struct
{
    public string Name { get; }
    public bool IsRegistered { get; }
    public void Invoke(uint replicatorID, S data);
    public void Invoke(uint replicatorID, S data, SNet_ChannelType channelType);
    public void Invoke(uint replicatorID, S data, SNet_Player target);
    public void Invoke(uint replicatorID, S data, SNet_Player target, SNet_ChannelType channelType);
}

public class ReplicatorPayloadWrapper<S, P> : IReplicatorEvent<S> where S : struct where P : struct, IStatePayload
{
    public string Name { get; private set; }
    public bool IsRegistered { get; private set; } = false;

    public static IReplicatorEvent<S> Create(string eventName, OnReceiveDel<S> onReceiveCallback)
    {
        var wrapper = new ReplicatorPayloadWrapper<S, P>();
        wrapper.Register(eventName, onReceiveCallback);
        return wrapper.IsRegistered ? wrapper : (IReplicatorEvent<S>)null;
    }

    public void Register(string eventName, OnReceiveDel<S> onReceiveCallback)
    {
        if (IsRegistered)
            return;

        if (NetworkAPI.IsEventRegistered(eventName))
            return;

        NetworkAPI.RegisterEvent(eventName, (ulong sender, P payload) => { onReceiveCallback?.Invoke(sender, payload.ID, payload.Get<S>()); });
        IsRegistered = true;
        Name = eventName;
    }

    public void Invoke(uint replicatorID, S data)
    {


        var payload = new P()
        {
            ID = replicatorID
        };
        payload.Set(data);

        NetworkAPI.InvokeEvent(Name, payload);
    }

    public void Invoke(uint replicatorID, S data, SNet_ChannelType channelType)
    {
        var payload = new P()
        {
            ID = replicatorID
        };
        payload.Set(data);

        NetworkAPI.InvokeEvent(Name, payload, channelType);
    }

    public void Invoke(uint replicatorID, S data, SNet_Player target)
    {
        var payload = new P()
        {
            ID = replicatorID
        };
        payload.Set(data);

        NetworkAPI.InvokeEvent(Name, payload, target);
    }

    public void Invoke(uint replicatorID, S data, SNet_Player target, SNet_ChannelType channelType)
    {
        var payload = new P()
        {
            ID = replicatorID
        };
        payload.Set(data);

        NetworkAPI.InvokeEvent(Name, payload, target, channelType);
    }
}

public interface IStatePayload
{
    public uint ID { get; set; }
    public S Get<S>() where S : struct;
    public void Set<S>(S stateData) where S : struct;
}

public struct StatePayload4Byte : IStatePayload
{
    public const int Size = (int)StatePayloads.Size.State4Byte;
    public uint ID { get => id; set => id = value; }
    private uint id;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Size)]
    public byte[] PayloadBytes;

    public S Get<S>() where S : struct => StatePayloads.Get<S>(PayloadBytes, Size);
    public void Set<S>(S stateData) where S : struct => StatePayloads.Set(stateData, Size, ref PayloadBytes);
}

public struct StatePayload8Byte : IStatePayload
{
    public const int Size = (int)StatePayloads.Size.State8Byte;

    [field: MarshalAs(UnmanagedType.U4)]
    public uint ID { get; set; }

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Size)]
    public byte[] PayloadBytes;

    public S Get<S>() where S : struct => StatePayloads.Get<S>(PayloadBytes, Size);
    public void Set<S>(S stateData) where S : struct => StatePayloads.Set(stateData, Size, ref PayloadBytes);
}

public struct StatePayload16Byte : IStatePayload
{
    public const int Size = (int)StatePayloads.Size.State16Byte;

    [field: MarshalAs(UnmanagedType.U4)]
    public uint ID { get; set; }

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Size)]
    public byte[] PayloadBytes;

    public S Get<S>() where S : struct => StatePayloads.Get<S>(PayloadBytes, Size);
    public void Set<S>(S stateData) where S : struct => StatePayloads.Set(stateData, Size, ref PayloadBytes);
}

public struct StatePayload32Byte : IStatePayload
{
    public const int Size = (int)StatePayloads.Size.State32Byte;

    [field: MarshalAs(UnmanagedType.U4)]
    public uint ID { get; set; }

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Size)]
    public byte[] PayloadBytes;

    public S Get<S>() where S : struct => StatePayloads.Get<S>(PayloadBytes, Size);
    public void Set<S>(S stateData) where S : struct => StatePayloads.Set(stateData, Size, ref PayloadBytes);
}

public struct StatePayload48Byte : IStatePayload
{
    public const int Size = (int)StatePayloads.Size.State48Byte;

    [field: MarshalAs(UnmanagedType.U4)]
    public uint ID { get; set; }

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Size)]
    public byte[] PayloadBytes;

    public S Get<S>() where S : struct => StatePayloads.Get<S>(PayloadBytes, Size);
    public void Set<S>(S stateData) where S : struct => StatePayloads.Set(stateData, Size, ref PayloadBytes);
}

public struct StatePayload64Byte : IStatePayload
{
    public const int Size = (int)StatePayloads.Size.State64Byte;

    [field: MarshalAs(UnmanagedType.U4)]
    public uint ID { get; set; }

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Size)]
    public byte[] PayloadBytes;

    public S Get<S>() where S : struct => StatePayloads.Get<S>(PayloadBytes, Size);
    public void Set<S>(S stateData) where S : struct => StatePayloads.Set(stateData, Size, ref PayloadBytes);
}

public struct StatePayload80Byte : IStatePayload
{
    public const int Size = (int)StatePayloads.Size.State80Byte;

    [field: MarshalAs(UnmanagedType.U4)]
    public uint ID { get; set; }

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Size)]
    public byte[] PayloadBytes;

    public S Get<S>() where S : struct => StatePayloads.Get<S>(PayloadBytes, Size);
    public void Set<S>(S stateData) where S : struct => StatePayloads.Set(stateData, Size, ref PayloadBytes);
}

public struct StatePayload96Byte : IStatePayload
{
    public const int Size = (int)StatePayloads.Size.State96Byte;

    [field: MarshalAs(UnmanagedType.U4)]
    public uint ID { get; set; }

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Size)]
    public byte[] PayloadBytes;

    public S Get<S>() where S : struct => StatePayloads.Get<S>(PayloadBytes, Size);
    public void Set<S>(S stateData) where S : struct => StatePayloads.Set(stateData, Size, ref PayloadBytes);
}

public struct StatePayload128Byte : IStatePayload
{
    public const int Size = (int)StatePayloads.Size.State128Byte;

    [field: MarshalAs(UnmanagedType.U4)]
    public uint ID { get; set; }

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Size)]
    public byte[] PayloadBytes;

    public S Get<S>() where S : struct => StatePayloads.Get<S>(PayloadBytes, Size);
    public void Set<S>(S stateData) where S : struct => StatePayloads.Set(stateData, Size, ref PayloadBytes);
}

public struct StatePayload196Byte : IStatePayload
{
    public const int Size = (int)StatePayloads.Size.State196Byte;

    [field: MarshalAs(UnmanagedType.U4)]
    public uint ID { get; set; }

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Size)]
    public byte[] PayloadBytes;

    public S Get<S>() where S : struct => StatePayloads.Get<S>(PayloadBytes, Size);
    public void Set<S>(S stateData) where S : struct => StatePayloads.Set(stateData, Size, ref PayloadBytes);
}

public struct StatePayload256Byte : IStatePayload
{
    public const int Size = (int)StatePayloads.Size.State256Byte;

    [field: MarshalAs(UnmanagedType.U4)]
    public uint ID { get; set; }

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Size)]
    public byte[] PayloadBytes;

    public S Get<S>() where S : struct => StatePayloads.Get<S>(PayloadBytes, Size);
    public void Set<S>(S stateData) where S : struct => StatePayloads.Set(stateData, Size, ref PayloadBytes);
}