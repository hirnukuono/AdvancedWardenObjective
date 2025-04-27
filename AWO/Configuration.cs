using BepInEx;
using BepInEx.Configuration;

namespace AWO;

internal static class Configuration
{
    public static bool DevDebug { get; private set; } = false;

    public static void Init()
    {
        BindAll(new ConfigFile(Path.Combine(Paths.ConfigPath, "AWO" + ".cfg"), true));
        Logger.Debug($"Dev logging is enabled: {DevDebug}");
    }

    private static void BindAll(ConfigFile config)
    {
        string section = "General Settings";
        string key = "Enable Dev Debug Logging";
        string description = "Prints some additional logs to the console, which may be useful for rundown devs";
        DevDebug = config.Bind(new ConfigDefinition(section, key), DevDebug, new ConfigDescription(description, null)).Value;
    }
}
