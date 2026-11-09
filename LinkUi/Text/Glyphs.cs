using Dalamud.Interface;

namespace LinkUi;

/// <summary>Icon-font strings, made once per icon: <c>ToIconString</c> allocates on every call.</summary>
internal static class Glyphs
{
    private static readonly Dictionary<FontAwesomeIcon, string> Cache = new();

    public static string Of(FontAwesomeIcon icon)
    {
        if (!Cache.TryGetValue(icon, out var glyph))
            Cache[icon] = glyph = icon.ToIconString();
        return glyph;
    }
}
