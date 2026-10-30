using BepInEx;
using BepInEx.Configuration;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using GTFO.API.Attributes.ConfigModel;
using GTFO.API.Utilities;
using System.ComponentModel;

namespace AWO;

internal static class Logger
{
    internal static ManualLogSource MLS = null!;
    private const string Dev = "Dev";

    [Section("Debug")]
    [ConfigEntry("Enable verbose logging", false)]
    [Description("Toggles additional debug logging of information to the console")]
    public static ConfigEntry<bool> VerboseEnabled { get; private set; }

    internal static void Setup(ManualLogSource log)
    {
        MLS = log;
        ConfigFile cfg = new(Path.Combine(Paths.ConfigPath, "AWO.cfg"), true);
        cfg.ConfigReloaded += (o, args) => Warn("Config file changed: " + cfg.ConfigFilePath);
        ConfigBinder.BindToType(cfg, typeof(Logger));
        SafeFileSystemWatcher.Create(cfg);
    }

    private static string Format(string module, object msg) => $"[{module}] {msg}";
    public static void Error(BepInExErrorLogInterpolatedStringHandler handler) => MLS.LogError(handler);
    public static void Error(string str) => MLS.LogError(str);
    public static void Error(string module, object data) => MLS.LogError(Format(module, data));
    public static void Warn(BepInExWarningLogInterpolatedStringHandler handler) => MLS.LogWarning(handler);
    public static void Warn(string str) => MLS.LogWarning(str);
    public static void Warn(string module, object data) => MLS.LogWarning(Format(module, data));
    public static void Message(BepInExMessageLogInterpolatedStringHandler handler) => MLS.LogMessage(handler);
    public static void Message(string str) => MLS.LogMessage(str);
    public static void Message(string module, object data) => MLS.LogMessage(Format(module, data));
    public static void Info(BepInExInfoLogInterpolatedStringHandler handler) => MLS.LogInfo(handler);
    public static void Info(string str) => MLS.LogInfo(str);
    public static void Info(string module, object data) => MLS.LogInfo(Format(module, data));
    public static void Debug(BepInExDebugLogInterpolatedStringHandler handler) => MLS.LogDebug(handler);
    public static void Debug(string str) => MLS.LogDebug(str);
    public static void Debug(string module, object data) => MLS.LogDebug(Format(module, data));
    
    public static void Verbose(string str, LogLevel level = LogLevel.Debug)
    {
        if (VerboseEnabled.Value)
        {
            switch (level)
            {
                case LogLevel.Error:
                    Error(Dev, str);
                    return;
                case LogLevel.Warning:
                    Warn(Dev, str);
                    return;
                case LogLevel.Message:
                    Message(Dev, str);
                    return;
                case LogLevel.Info:
                    Info(Dev, str);
                    return;
                case LogLevel.Debug:
                    Debug(Dev, str);
                    return;                
            }
        }
    }
}
