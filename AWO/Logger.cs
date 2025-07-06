using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;

namespace AWO;

internal static class Logger
{
    private static readonly ManualLogSource MLS;
    private const string Dev = "Dev";

    static Logger()
    {
        MLS = new ManualLogSource(VersionInfo.RootNamespace);
        BepInEx.Logging.Logger.Sources.Add(MLS);
    }

    private static string Format(string module, object msg) => $"[{module}] {msg}";
    public static void Info(BepInExInfoLogInterpolatedStringHandler handler) => MLS.LogInfo(handler);
    public static void Info(string str) => MLS.LogMessage(str);
    public static void Info(string module, object data) => MLS.LogMessage(Format(module, data));
    public static void Debug(BepInExDebugLogInterpolatedStringHandler handler) => MLS.LogDebug(handler);
    public static void Debug(string str) => MLS.LogDebug(str);
    public static void Debug(string module, object data) => MLS.LogDebug(Format(module, data));
    public static void Error(BepInExErrorLogInterpolatedStringHandler handler) => MLS.LogError(handler);
    public static void Error(string str) => MLS.LogError(str);
    public static void Error(string module, object data) => MLS.LogError(Format(module, data));
    public static void Warn(BepInExWarningLogInterpolatedStringHandler handler) => MLS.LogWarning(handler);
    public static void Warn(string str) => MLS.LogWarning(str);
    public static void Warn(string module, object data) => MLS.LogWarning(Format(module, data));
    
    public static void Verbose(LogLevel level, string data)
    {
        if (Configuration.VerboseEnabled)
        {
            switch (level)
            {
                case LogLevel.Info:
                    Info(Dev, data);
                    return;
                case LogLevel.Debug:
                    Debug(Dev, data);
                    return;
                case LogLevel.Error:
                    Debug(Dev, data);
                    return;
                case LogLevel.Warning:
                    Warn(Dev, data);
                    return;
            }
        }
    }
}
