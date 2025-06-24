using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;

namespace AWO;

internal static class Logger
{
    private static readonly ManualLogSource MSL;
    private const string Dev = "Dev";

    static Logger()
    {
        MSL = new ManualLogSource(VersionInfo.RootNamespace);
        BepInEx.Logging.Logger.Sources.Add(MSL);
    }

    private static string Format(string module, object msg) => $"[{module}] {msg}";
    public static void Info(BepInExInfoLogInterpolatedStringHandler handler) => MSL.LogInfo(handler);
    public static void Info(string str) => MSL.LogMessage(str);
    public static void Info(string module, object data) => MSL.LogMessage(Format(module, data));
    public static void Debug(BepInExDebugLogInterpolatedStringHandler handler) => MSL.LogDebug(handler);
    public static void Debug(string str) => MSL.LogDebug(str);
    public static void Debug(string module, object data) => MSL.LogDebug(Format(module, data));
    public static void Error(BepInExErrorLogInterpolatedStringHandler handler) => MSL.LogError(handler);
    public static void Error(string str) => MSL.LogError(str);
    public static void Error(string module, object data) => MSL.LogError(Format(module, data));
    public static void Warn(BepInExWarningLogInterpolatedStringHandler handler) => MSL.LogWarning(handler);
    public static void Warn(string str) => MSL.LogWarning(str);
    public static void Warn(string module, object data) => MSL.LogWarning(Format(module, data));
    
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
