using AWO.Jsons;
using GameData;
using GTFO.API;
using LogEventType = AWO.Modules.WEE.WEE_SetTerminalLog.LogEventType;

namespace AWO.Modules.WEE.Events;

internal sealed class SetTerminalLog : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SetTerminalLog;
    public static readonly Dictionary<(uint, string), Queue<WardenObjectiveEventData>> LogEventQueue = new();

    protected override void OnSetup()
    {
        LevelAPI.OnLevelCleanup += OnLevelCleanup;
    }

    private void OnLevelCleanup()
    {
        LogEventQueue.Clear();
    }

    protected override void TriggerCommon(WEE_EventData e)
    {
        var eLog = e.SetTerminalLog;
        if (!TryGetTerminalFromZone(e, eLog.TerminalIndex, out var term)) return;

        if (eLog.Type == LogEventType.Add)
        {
            if (term.GetLocalLogs().ContainsKey(eLog.FileName.ToUpper()))
            {
                LogError($"A log file with filename {eLog.FileName.ToUpper()} is already present on terminal!");
                return;
            }
            else if (eLog.FileContent == LocaleText.Empty)
            {
                LogError("Terminal log 'FileContent' cannot be empty.");
                return;
            }
            
            term.AddLocalLog(new()
            {
                FileName = eLog.FileName,
                FileContent = eLog.FileContent,
                FileContentOriginalLanguage = eLog.FileContentOriginalLanguage,
                AttachedAudioFile = eLog.AttachedAudioFile,
                AttachedAudioByteSize = eLog.AttachedAudioByteSize,
                PlayerDialogToTriggerAfterAudio = eLog.PlayerDialogToTriggerAfterAudio
            });

            LogEventQueue.Add((term.SyncID, eLog.FileName), new(eLog.EventsOnFileRead));
        }
         else if (eLog.Type == LogEventType.Remove)
        {
            term.RemoveLocalLog(eLog.FileName);
        }
    }
}