namespace LinkUi;

/// <summary>Splits an ImGui window name into the title the header shows and the id the store keys on.</summary>
internal static class WindowText
{
    /// <summary>The text before the first <c>##</c>, or the whole name when there's none.</summary>
    public static string Title(string windowName)
    {
        var hashes = windowName.IndexOf("##", StringComparison.Ordinal);
        return hashes < 0 ? windowName : windowName[..hashes];
    }

    /// <summary>The text after <c>###</c>; the whole name when there's no <c>###</c> or nothing follows it.</summary>
    public static string Id(string windowName)
    {
        var hashes = windowName.IndexOf("###", StringComparison.Ordinal);
        return hashes < 0 || hashes + 3 == windowName.Length ? windowName : windowName[(hashes + 3)..];
    }
}
