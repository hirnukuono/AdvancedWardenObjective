using Localization;
using System.Text.Json.Serialization;

namespace AWO.Jsons;

[JsonConverter(typeof(LocaleTextConverter))]
public struct LocaleText : IEquatable<LocaleText>
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

    private readonly string TextFallback
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

    public readonly bool Equals(LocaleText other)
    {
        return ID == other.ID && string.Equals(RawText, other.RawText, StringComparison.Ordinal);
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is LocaleText other && Equals(other);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(ID, RawText);
    }

    public static bool operator ==(LocaleText left, LocaleText right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LocaleText left, LocaleText right)
    {
        return !(left == right);
    }
}
