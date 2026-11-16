namespace LinkUi;

/// <summary>The visible part of an ImGui label, cached so widgets can draw it every frame without allocating.</summary>
internal static class Labels
{
    private const int MaxEntries = 1024;
    private static readonly Dictionary<string, string> Cache = new(StringComparer.Ordinal);

    /// <summary>The text before the first <c>##</c>; the same instance when there's no <c>##</c>.</summary>
    public static string Visible(string label)
    {
        var hashes = label.IndexOf("##", StringComparison.Ordinal);
        if (hashes < 0)
            return label;
        if (hashes == 0)
            return string.Empty;
        if (Cache.TryGetValue(label, out var visible))
            return visible;
        if (Cache.Count >= MaxEntries)
            Cache.Clear();
        visible = label[..hashes];
        Cache[label] = visible;
        return visible;
    }

    private static string? copyHelp;
    private static string copyTip = "Click to copy.";

    /// <summary>The tooltip of a copyable value: the help, a blank line, then "Click to copy.". One entry is cached, keyed on the help instance.</summary>
    public static string CopyTip(string? help)
    {
        if (help == null)
            return "Click to copy.";
        if (!ReferenceEquals(help, copyHelp))
        {
            copyTip = help + "\n\nClick to copy.";
            copyHelp = help;
        }
        return copyTip;
    }
}
