using BepInEx;
using BepInEx.Configuration;
using GTFO.API.Utilities;

namespace AWO;

internal static class Configuration
{
    private static ConfigFile s_config = null!;

    public static bool VerboseEnabled => s_verboseEnabled.Value;
    private static ConfigEntry<bool> s_verboseEnabled = null!;

    public static void Init()
    {
        s_config = new(Path.Combine(Paths.ConfigPath, "AWO.cfg"), true);
        string section = "General Settings";
        string key = "Enable Verbose Debug Logging";
        string description = "Prints some additional logs to the console, which may be useful for rundown devs";
        s_verboseEnabled = s_config.Bind(section, key, false, description);
        SafeFileSystemWatcher.Create(s_config);
    }
}
