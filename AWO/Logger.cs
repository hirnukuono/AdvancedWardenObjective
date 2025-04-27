using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;

namespace AWO;

internal static class Logger
{
    private static readonly ManualLogSource _Logger;

    static Logger()
    {
        _Logger = new ManualLogSource(VersionInfo.RootNamespace);
        BepInEx.Logging.Logger.Sources.Add(_Logger);
    }

    private static string? Format(object msg) => msg.ToString();
    public static void Info(BepInExInfoLogInterpolatedStringHandler handler) => _Logger.LogInfo(handler);
    public static void Info(string str) => _Logger.LogMessage(str);
    public static void Info(object data) => _Logger.LogMessage(Format(data));
    public static void Debug(BepInExDebugLogInterpolatedStringHandler handler) => _Logger.LogDebug(handler);
    public static void Debug(string str) => _Logger.LogDebug(str);
    public static void Debug(object data) => _Logger.LogDebug(Format(data));
    public static void Error(BepInExErrorLogInterpolatedStringHandler handler) => _Logger.LogError(handler);
    public static void Error(string str) => _Logger.LogError(str);
    public static void Error(object data) => _Logger.LogError(Format(data));
    public static void Warn(BepInExWarningLogInterpolatedStringHandler handler) => _Logger.LogWarning(handler);
    public static void Warn(string str) => _Logger.LogWarning(str);
    public static void Warn(object data) => _Logger.LogWarning(Format(data));
    
    public static void Dev(LogLevel level, string str)
    {
        if (Configuration.DevDebug)
        {
            switch (level)
            {
                case LogLevel.Info:
                    Info($"[Dev] {str}");
                    return;
                case LogLevel.Debug:
                    Debug($"[Dev] {str}");
                    return;
                case LogLevel.Error:
                    Debug($"[Dev] {str}");
                    return;
                case LogLevel.Warning:
                    Warn($"[Dev] {str}");
                    return;
            }
        }
    }
}
