using GameData;
using Il2CppInterop.Runtime;
using System.Runtime.InteropServices;

namespace AWO.Modules.WEE.Detours;

internal sealed class ExecuteEventContext
{
    public static IntPtr ClazzPtr { get; private set; }
    public static IntPtr Field_eData_Ptr { get; private set; } = IntPtr.Zero;
    public static IntPtr Field_currentDuration_Ptr { get; private set; } = IntPtr.Zero;
    public static IntPtr Field_state_Ptr { get; private set; } = IntPtr.Zero;

    public static int StateFieldOffset => (int)IL2CPP.il2cpp_field_get_offset(Field_state_Ptr);
    public static int CurrentDurationFieldOffset => (int)IL2CPP.il2cpp_field_get_offset(Field_currentDuration_Ptr);
    public static int DataFieldOffset => (int)IL2CPP.il2cpp_field_get_offset(Field_eData_Ptr);

    public unsafe WardenObjectiveEventData Data
    {
        get
        {
            var ptr = _BasePtr + DataFieldOffset;
            nint objPtr = *(nint*)ptr.ToPointer();
            return objPtr != 0 ? new WardenObjectiveEventData(objPtr) : null!;
        }
        set
        {
            var fieldPtr = _BasePtr + DataFieldOffset;
            IL2CPP.il2cpp_gc_wbarrier_set_field(_BasePtr, fieldPtr, IL2CPP.Il2CppObjectBaseToPtr(value));
        }
    }

    public unsafe float CurrentDuration
    {
        get
        {
            var ptr = _BasePtr + CurrentDurationFieldOffset;
            return *(float*)ptr;
        }
        set
        {
            *(float*)((nint)_BasePtr + CurrentDurationFieldOffset) = value;
        }
    }

    public unsafe int State
    {
        get
        {
            var ptr = _BasePtr + StateFieldOffset;
            return *(int*)ptr;
        }
        set
        {
            *(int*)((nint)_BasePtr + StateFieldOffset) = value;
        }
    }

    private readonly IntPtr _BasePtr;

    public static bool TrySetup(IntPtr clazz)
    {
        ClazzPtr = clazz;
        Field_eData_Ptr = IL2CPP.GetIl2CppField(ClazzPtr, "eData");
        if (Field_eData_Ptr == IntPtr.Zero)
        {
            Logger.Error("Unable to find 'eData' Field!");
            return false;
        }

        Field_currentDuration_Ptr = IL2CPP.GetIl2CppField(ClazzPtr, "currentDuration");
        if (Field_currentDuration_Ptr == IntPtr.Zero)
        {
            Logger.Error("Unable to find 'currentDuration' Field!");
            return false;
        }

        FindStateField();
        return true;
    }

    private static void FindStateField()
    {
        var iter = IntPtr.Zero;
        IntPtr field;
        while ((field = IL2CPP.il2cpp_class_get_fields(ClazzPtr, ref iter)) != IntPtr.Zero)
        {
            var fieldName = Marshal.PtrToStringAnsi(IL2CPP.il2cpp_field_get_name(field));
            if (fieldName is not null && fieldName.Contains("_state", StringComparison.InvariantCultureIgnoreCase))
            {
                Logger.Debug($"Found State Field: '{fieldName}'");
                Field_state_Ptr = field;
                break;
            }
        }
    }

    public ExecuteEventContext(IntPtr ptr)
    {
        _BasePtr = ptr;
    }
}
