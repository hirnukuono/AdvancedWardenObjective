using BepInEx.Unity.IL2CPP.Hook;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;

namespace AWO.Modules.WEE.Detours;

internal static class Detour_ExecuteEvent
{
    public const byte IL2CPP_TRUE = 1;
    public const byte IL2CPP_FALSE = 0;

    public unsafe delegate byte MoveNextDel(IntPtr _this, Il2CppMethodInfo* methodInfo);
    private static INativeDetour _Detour;
    private static MoveNextDel _Original;

    public unsafe static void Patch()
    {
        var nested = typeof(WorldEventManager).GetNestedTypes();
        Type? executeEventType = null;
        foreach (var nestType in nested)
        {
            var match = nestType?.Name?.Contains(nameof(WorldEventManager.DoExcecuteEvent), StringComparison.InvariantCulture) ?? false;
            if (match)
            {
                executeEventType = nestType;
                Logger.Debug($"Found Patch Type: {nestType?.Name}");
                break;
            }
        }

        if (executeEventType == null)
        {
            Logger.Error($"Unable to find generated IEnumerator!");
            return;
        }

        var clazz = Il2CppClassPointerStore.GetNativeClassPointer(executeEventType);
        if (clazz == IntPtr.Zero)
        {
            Logger.Error($"Unable to get Il2Cpp Clazz Ptr!");
            return;
        }

        var method = GetIl2CppMethod(clazz, "MoveNext", typeof(bool).FullName!);
        if ((nint)method == 0)
        {
            Logger.Error($"Unable to find method: MoveNext!");
            return;
        }
        if (ExecuteEventContext.TrySetup(clazz))
        {
            _Detour = INativeDetour.CreateAndApply((nint)method, Detour, out _Original);
            Logger.Debug("Detour has finished setup!");
        }
        else
        {
            Logger.Error($"Unable to setup {nameof(ExecuteEventContext)}!");
        }
    }

    private unsafe static byte Detour(IntPtr _this, Il2CppMethodInfo* methodInfo)
    {
        var context = new ExecuteEventContext(_this);
        var data = context.Data;
        var type = data.Type;
        Logger.Debug($"We got Type {(int)type} on a Warden Event");

        if (Enum.IsDefined(typeof(WEE_Type), (int)type))
        {
            Logger.Debug($"Found WardenEventExt for '{(WEE_Type)type}', aborting original call!");
            WardenEventExt.HandleEvent((WEE_Type)type, data, context.CurrentDuration);
            context.State = -1;
            return IL2CPP_FALSE;
        }
        else if (VanillaEventOvr.HasOverride(type, data))
        {
            Logger.Debug($"Found valid VanillaEventOverride for '{type}', aborting original call!");
            VanillaEventOvr.HandleEvent(type, data, context.CurrentDuration);
            context.State = -1;
            return IL2CPP_FALSE;
        }

        return _Original.Invoke(_this, methodInfo);
    }

    private static unsafe void* GetIl2CppMethod(IntPtr clazz, string methodName, string returnTypeName)
    {
        void** ppMethod = (void**)IL2CPP.GetIl2CppMethod(clazz, false, methodName, returnTypeName).ToPointer();
        return (long)ppMethod == 0 ? ppMethod : *ppMethod;
    }
}
