using BepInEx;
using BepInEx.Configuration;

namespace AWO;

internal static class Configuration
{
    public static bool VerboseEnabled { get; private set; } = false;

    public static void Init()
    {
        BindAll(new ConfigFile(Path.Combine(Paths.ConfigPath, "AWO" + ".cfg"), true));
        Logger.Debug($"Verbose logging is enabled: {VerboseEnabled}");
    }

    private static void BindAll(ConfigFile config)
    {
        string section = "General Settings";
        string key = "Enable Verbose Debug Logging";
        string description = "Prints some additional logs to the console, which may be useful for rundown devs";
        VerboseEnabled = config.Bind(section, key, false, description).Value;
    }
}
