using AK;
using AWO.Jsons;
using GameData;
using Localization;
using Player;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using AkEventCallback = AkCallbackManager.EventCallback;
using DialogueType = AWO.Modules.WEE.WEE_ForcePlayerDialogue.DialogueType;
using IntensityState = AWO.Modules.WEE.WEE_ForcePlayerDialogue.PlayerIntensityState;

namespace AWO.Modules.WEE.Events;

internal sealed class ForcePlayerDialogueEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.ForcePlayPlayerDialogue;

    protected override void TriggerMaster(WEE_EventData e)
    {
        EntryPoint.SessionRand.SyncStep(); // runs after TriggerCommon!
    }

    protected override void TriggerCommon(WEE_EventData e)
    {
        var block = PlayerDialogDataBlock.GetBlock(e.DialogueID);
        if (block == null || !block.internalEnabled)
        {
            LogError("Failed to find enabled PlayerDialogDataBlock!");
            return;
        }

        if (!TryGetPlayerCharacter(e.PlayerDialogue, GetPositionFallback(e.Position, e.SpecialText, false), out var player, out var charFilterList))
        {
            LogError("Failed to find character!");
            return;
        }
        
        CellSoundPlayer dialogue = new();

        dialogue.SetSwitch(SWITCHES.CHARACTER.GROUP, player.CharacterID switch
        {
            0 => SWITCHES.CHARACTER.SWITCH.CH_01,
            1 => SWITCHES.CHARACTER.SWITCH.CH_02,
            2 => SWITCHES.CHARACTER.SWITCH.CH_03,
            3 => SWITCHES.CHARACTER.SWITCH.CH_04,
            _ => throw new NotImplementedException($"[{Name}] only supports default lobby size. Unknown CharacterID {player.CharacterID}")
        });

        dialogue.SetSwitch(SWITCHES.INTENSITY_STATE.GROUP, e.PlayerDialogue.IntensityState switch
        {
            IntensityState.Exploration => SWITCHES.INTENSITY_STATE.SWITCH.INTENSITY_1_EXPLORATION,
            IntensityState.Stealth => SWITCHES.INTENSITY_STATE.SWITCH.INTENSITY_2_STEALTH,
            IntensityState.Encounter => SWITCHES.INTENSITY_STATE.SWITCH.INTENSITY_3_ENCOUNTER,
            IntensityState.Combat => SWITCHES.INTENSITY_STATE.SWITCH.INTENSITY_4_COMBAT,
            _ => SWITCHES.INTENSITY_STATE.SWITCH.INTENSITY_1_EXPLORATION
        });

        if (!player.IsLocallyOwned) // idk if this does anything anymore
        {
            dialogue.SetRTPCValue(GAME_PARAMETERS.FIRST_PERSON_MIX, 0.0f);
            float magnitude = (LocalPlayer.Position - player.Position).magnitude;
            if (magnitude > player.PlayerData.radioEnabledDefaultDistance)
            {
                float quality = 1.0f - (magnitude - player.PlayerData.radioEnabledDefaultDistance) / (player.PlayerData.radioQualityLowestAtDistance - player.PlayerData.radioEnabledDefaultDistance);
                quality *= 100.0f;
                dialogue.SetRTPCValue(GAME_PARAMETERS.RADIO_QUALITY_DISTANCE, quality);
                dialogue.SetRTPCValue(GAME_PARAMETERS.RADIO_DISTORTION_ON_OFF, 1.0f);
            }
            else
            {
                dialogue.SetRTPCValue(GAME_PARAMETERS.RADIO_QUALITY_DISTANCE, 100.0f);
                dialogue.SetRTPCValue(GAME_PARAMETERS.RADIO_DISTORTION_ON_OFF, 0.0f);
            }
        }
        else
        {
            dialogue.SetRTPCValue(GAME_PARAMETERS.FIRST_PERSON_MIX, 1.0f);
        }

        DialogCharFilter playerCharFilter = player.PlayerCharacterFilter;
        if (playerCharFilter != DialogCharFilter.None)
        {
            charFilterList.Remove(playerCharFilter);
        }
        DialogAlternativeWithCast? dialogueVariation = PlayerDialogManager.Current.m_dialogCastingDirector.GetDialogAlternativeWithCast(e.DialogueID, charFilterList.ToArray(), playerCharFilter);
        if (dialogueVariation == null)
        {
            LogError($"PlayerDialogDataBlock {block.persistentID} has no dialogAlternatives?");
            return;
        }

        int index = EntryPoint.SessionRand.NextInt(dialogueVariation.m_data.lineEventIDs.Count);
        uint lineEvent = dialogueVariation.m_data.lineEventIDs[index];
        uint subtitle = dialogueVariation.m_data.SubtitleIDs[index];

        AkSoundEngine.SetRandomSeed(EntryPoint.SessionRand.Next());
        dialogue.Post(lineEvent, player.Position, 1u, (AkEventCallback)VoiceDoneCallback, dialogue);

        WOManager.Current.m_sound.Post(e.SoundID, true);
        if (e.SoundSubtitle != LocaleText.Empty)
        {
            LogWarning("Skipping this event's SoundSubtitle since player dialogue is active");
        }
        GuiManager.PlayerLayer.m_subtitles.ShowMultiLineSubtitle(Text.Get(subtitle), ResolveFieldsFallback(4.0f, e.Duration));
    }

    private static bool TryGetPlayerCharacter(WEE_ForcePlayerDialogue dialog, Vector3 pos, [NotNullWhen(true)] out PlayerAgent? player, out List<DialogCharFilter> charFilterList)
    {
        charFilterList = new();
        player = null;

        var charFiltersInLevel = PlayerDialogManager.GetAllRegistredPlayerCharacterFilters();
        float minDist = float.MaxValue;
        bool flag = false;
        foreach (DialogCharFilter charFilter in charFiltersInLevel)
        {
            charFilterList.Add(charFilter);
            if (flag) continue;
            PlayerAgent? currentPlayer = PlayerDialogManager.GetPlayerAgentForCharacter(charFilter);

            if (dialog.Type == DialogueType.Random)
            {
                player = PlayerManager.PlayerAgentsInLevel[EntryPoint.SessionRand.NextInt(charFiltersInLevel.Count)];
                flag = true;
                continue;
            }
            else if (dialog.Type == DialogueType.Specific && currentPlayer.CharacterID == (int)dialog.CharacterID)
            {
                player = currentPlayer;
                flag = true;
                continue;
            }
            else if (dialog.Type == DialogueType.Closest)
            {
                float dist = Vector3.Distance(pos, currentPlayer.Position);
                if (dist < minDist)
                {
                    minDist = dist;
                    player = currentPlayer;
                }
            }
        }

        return player != null;
    }

    private static void VoiceDoneCallback(Il2CppSystem.Object in_cookie, AkCallbackType in_type, AkCallbackInfo callbackInfo)
    {
        var callbackPlayer = in_cookie.Cast<CellSoundPlayer>();
        callbackPlayer?.Recycle();
    }
}
