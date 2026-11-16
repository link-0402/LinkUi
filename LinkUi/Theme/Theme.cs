namespace LinkUi;

/// <summary>The palette and the metrics at the current UI scale.</summary>
public readonly record struct ThemeState(Palette Palette, Metrics Metrics);

/// <summary>The theme every LinkUi widget draws with; set by <see cref="LinkUiHost"/>, a fallback without one.</summary>
public static class Theme
{
    /// <summary>
    /// The theme before a host exists and after it is disposed: the default amber accent at 100%, built once,
    /// so colours read outside a host are valid instead of null.
    /// </summary>
    internal static readonly ThemeState Fallback = new(Palette.From(Oklch.Hex("#e0a43a")), Metrics.At(1f));

    /// <summary>The live theme; the host rebuilds the metrics when the global scale changes.</summary>
    public static ThemeState Current { get; internal set; } = Fallback;
}
