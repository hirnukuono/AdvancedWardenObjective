using BepInEx;
using BepInEx.Configuration;
using GTFO.API.Utilities;

namespace AWO;

internal static class Configuration
{
    private static readonly ConfigFile Config;

    public static bool VerboseEnabled => Config_VerboseEnabled.Value;
    private static readonly ConfigEntry<bool> Config_VerboseEnabled;

    static Configuration()
    {
        Config = new(Path.Combine(Paths.ConfigPath, "AWO.cfg"), true);
        string section = "General Settings";
        string key = "Enable Verbose Debug Logging";
        string description = "Prints some additional logs to the console, which may be useful for rundown devs";
        Config_VerboseEnabled = Config.Bind(section, key, false, description);
    }

    public static void Init()
    {
        LiveEditListener listener = LiveEdit.CreateListener(Paths.ConfigPath, "AWO.cfg", false);
        listener.FileChanged += OnFileChanged;
        Logger.Debug($"Verbose logging is enabled: {VerboseEnabled}");
    }

    private static void OnFileChanged(LiveEditEventArgs e)
    {
        Logger.Warn($"Config file changed: {e.FullPath}");
        Config.Reload();
    }
}
