using Expedition;
using HarmonyLib;
using LevelGeneration;
using System.Reflection;
using UnityEngine;

namespace AWO.Sessions.Patches;

[HarmonyPatch]
internal static class Patch_LG_ObjectsTrack
{
    [HarmonyTargetMethods]
    static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(LG_ComputerTerminal), nameof(LG_ComputerTerminal.Setup));
        yield return AccessTools.Method(typeof(LG_DoorButton), nameof(LG_DoorButton.Setup));
        yield return AccessTools.Method(typeof(LG_HSUActivator_Core), nameof(LG_HSUActivator_Core.Start));
        yield return AccessTools.Method(typeof(LG_LabDisplay), nameof(LG_LabDisplay.GenerateText), new Type[] { typeof(int), typeof(SubComplex) });
        yield return AccessTools.Method(typeof(LG_WeakLock), nameof(LG_WeakLock.Setup));
    }

    [HarmonyPostfix]
    private static void TrackObject(Component __instance)
    {
        LG_Objects.AddToTrackedList(__instance);
    }
}
