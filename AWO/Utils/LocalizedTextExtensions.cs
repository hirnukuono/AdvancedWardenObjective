using Localization;

namespace AWO.Utils;

public static class LocalizedTextExtensions
{
    public static string ToText(this LocalizedText text)
    {
        return text.HasTranslation ? Text.Get(text.Id) : text.UntranslatedText;
    }
}
