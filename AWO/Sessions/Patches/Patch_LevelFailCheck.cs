using HarmonyLib;
using Player;

namespace AWO.Sessions.Patches;

[HarmonyPatch]
internal static class Patch_LevelFailCheck
{
    [HarmonyPatch(typeof(WOManager), nameof(WOManager.CheckExpeditionFailed))]
    [HarmonyAfter]
    private static void Postfix(ref bool __result)
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
