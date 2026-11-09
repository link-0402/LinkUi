using System.Globalization;

namespace LinkUi;

/// <summary>Number text for widgets: 0 to 1023 made once, anything else formatted on each call.</summary>
internal static class Numbers
{
    private const int Cached = 1024;
    private static readonly string?[] Small = new string?[Cached];

    public static string Of(int n)
    {
        if ((uint)n >= Cached)
            return n.ToString(CultureInfo.InvariantCulture);
        return Small[n] ??= n.ToString(CultureInfo.InvariantCulture);
    }
}
