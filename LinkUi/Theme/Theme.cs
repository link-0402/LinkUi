namespace LinkUi;

/// <summary>The palette and the metrics at the current UI scale.</summary>
public readonly record struct ThemeState(Palette Palette, Metrics Metrics);

/// <summary>The theme every LinkUi widget draws with; set by <see cref="LinkUiHost"/>, empty without one.</summary>
public static class Theme
{
    /// <summary>The live theme; the host rebuilds the metrics when the global scale changes.</summary>
    public static ThemeState Current { get; internal set; }
}
