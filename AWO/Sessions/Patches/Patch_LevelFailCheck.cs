using HarmonyLib;
using Player;

namespace AWO.Sessions.Patches;

[HarmonyPatch]
internal static class Patch_LevelFailCheck
{
    [HarmonyPatch(typeof(WOManager), nameof(WOManager.CheckExpeditionFailed))]
    [HarmonyPostfix]
    [HarmonyAfter]
    private static void Post_CheckLevelFail(ref bool __result)
    {
        if (!LevelFailUpdateState.LevelFailAllowed)
        {
            __result = false;
        }
        else if (LevelFailUpdateState.LevelFailWhenAnyPlayerDown && HasAnyDownedPlayer())
        {
            __result = true;
        }
    }

    private static bool HasAnyDownedPlayer()
    {
        foreach (PlayerAgent player in PlayerManager.PlayerAgentsInLevel)
        {
            if (!player.Alive)
            {
                return true;
            }
        }

        return false;
    }
}
