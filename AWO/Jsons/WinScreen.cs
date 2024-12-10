using System.Text.Json.Serialization;

namespace AWO.Jsons;

[JsonConverter(typeof(WinScreenConverter))]
public struct WinScreen
{
    public readonly static string[] VanillaPaths = new[]
    {
        "CM_PageExpeditionSuccess_Completed",
        "CM_PageExpeditionSuccess_Resources expended_CellUI 2",
        "CM_PageExpeditionSuccess_SignalLost_CellUI",
        "CM_PageExpeditionSuccess_Stack Empty_CellUI 1"
    };

    public string PagePath;

    public WinScreen(int index)
    {
        if (index >= 0 && index < VanillaPaths.Length)
            PagePath = VanillaPaths[index];
        else
            PagePath = string.Empty;
    }

    public WinScreen(string filepath)
    {
        PagePath = filepath;
    }

    public override string ToString()
    {
        return PagePath;
    }

    public static explicit operator WinScreen(string filepath) => new(filepath);
    public static explicit operator WinScreen(int index) => new(index);

    public static implicit operator string(WinScreen winScreen) => winScreen.ToString();
    public static implicit operator int(WinScreen winScreen)
    {
        int index = Array.IndexOf(VanillaPaths, winScreen.PagePath);
        return index >= 0 ? index : -1;
    }

    public static readonly WinScreen Empty = new(string.Empty);
}
