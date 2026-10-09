using AK;
using HarmonyLib;
using LevelGeneration;

namespace AWO.Modules.WEE.Patches;

[HarmonyPatch]
internal static class Patch_LG_SecurityDoor
{
    private static bool s_inRecall = false;
    [HarmonyPatch(typeof(LG_SecurityDoor_Anim), nameof(LG_SecurityDoor_Anim.OnDoorState))]
    [HarmonyPrefix]
    private static void PreStateChange(bool isRecall)
    {
        s_inRecall = isRecall;
    }

    [HarmonyPatch(typeof(LG_SecurityDoor_Anim), nameof(LG_SecurityDoor_Anim.OnDoorState))]
    [HarmonyPostfix]
    private static void PostStateChange()
    {
        s_inRecall = false;
    }

    [HarmonyPatch(typeof(LG_SecurityDoor_Anim), nameof(LG_SecurityDoor_Anim.SetAsClosed))]
    [HarmonyPostfix]
    private static void OnClosed(LG_SecurityDoor_Anim __instance)
    {
        if (s_inRecall) return;

        // Restore blood door sound, but only on non-recall (SetAsClosed is called on recall, but vanilla already starts the audio)
        var door = __instance.m_gate.SpawnedDoor.Cast<LG_SecurityDoor>();
        if (door.ActiveEnemyWaveData != null && door.ActiveEnemyWaveData.HasActiveEnemyWave)
            __instance.m_sound.Post(EVENTS.MONSTER_RUCKUS_FROM_BEHIND_SECURITY_DOOR_LOOP_START);
    }
}
