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
}
