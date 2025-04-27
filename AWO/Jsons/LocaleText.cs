using Localization;
using System.Text.Json.Serialization;

namespace AWO.Jsons;

[JsonConverter(typeof(LocaleTextConverter))]
public struct LocaleText
{
    public uint ID;
    public string RawText;

    public LocaleText(LocalizedText baseText)
    {
        RawText = baseText.ToText();
        ID = baseText.Id;
    }

    public LocaleText(string text)
    {
        if (EntryPoint.PartialDataIsLoaded && PartialData.TryGetGUID(text, out uint guid))
        {
            RawText = string.Empty;
            ID = guid;
        }
        else
        {
            RawText = text;
            ID = 0u;
        }
    }

    public LocaleText(uint id)
    {
        RawText = string.Empty;
        ID = id;
    }

    public readonly string TextFallback
    {
        get
        {
            return ID == 0u ? RawText : Text.Get(ID);
        }
    }

    public readonly LocalizedText ToLocalizedText()
    {
        return new LocalizedText
        {
            Id = ID,
            UntranslatedText = TextFallback
        };
    }

    public override readonly string ToString()
    {
        return TextFallback;
    }

    public static explicit operator LocaleText(LocalizedText localizedText) => new(localizedText);
    public static explicit operator LocaleText(string text) => new(text);

    public static implicit operator LocalizedText(LocaleText localeText) => localeText.ToLocalizedText();
    public static implicit operator string(LocaleText localeText) => localeText.ToString();

    public static readonly LocaleText Empty = new(string.Empty);
}
