using System.Numerics;

namespace LinkUi;

/// <summary>
/// Sizes, roundings and paddings in pixels at one UI scale. <see cref="At"/> multiplies the 100% values
/// by the scale; <see cref="BorderSize"/> stays 1 at every scale.
/// </summary>
public readonly record struct Metrics
{
    public float Scale { get; init; }
    public float WindowRounding { get; init; }
    public float ChildRounding { get; init; }
    public float FrameRounding { get; init; }
    public float PopupRounding { get; init; }
    public float ScrollbarRounding { get; init; }
    public float GrabRounding { get; init; }
    public float TabRounding { get; init; }

    /// <summary>Always 1: a scaled border looks blurry.</summary>
    public float BorderSize { get; init; }

    public float IndentSpacing { get; init; }
    public float ScrollbarSize { get; init; }
    public float GrabMinSize { get; init; }

    /// <summary>Height the header adds above the default title bar.</summary>
    public float HeaderExtra { get; init; }

    public float HeaderPadLeft { get; init; }
    public float HeaderPadRight { get; init; }

    /// <summary>Gap between the icon, the title, the subtitle and the buttons.</summary>
    public float HeaderGap { get; init; }

    public float HeaderButtonGap { get; init; }

    /// <summary>Space on each side of the separator before the plugin actions.</summary>
    public float SeparatorMargin { get; init; }

    /// <summary>Title width the plugin actions must leave, or they fold into the options menu.</summary>
    public float MinTitleWidth { get; init; }

    public float TitleFade { get; init; }
    public float FocusLine { get; init; }
    public float PillPadX { get; init; }
    public float PropertyGutter { get; init; }
    public float SplitterLine { get; init; }
    public float SplitterHit { get; init; }
    public Vector2 WindowPadding { get; init; }
    public Vector2 FramePadding { get; init; }
    public Vector2 ItemSpacing { get; init; }
    public Vector2 ItemInnerSpacing { get; init; }
    public Vector2 CellPadding { get; init; }

    /// <summary>The metrics at a UI scale (1 is 100%).</summary>
    public static Metrics At(float scale) => new()
    {
        Scale = scale,
        WindowRounding = 5 * scale,
        ChildRounding = 3 * scale,
        FrameRounding = 3 * scale,
        PopupRounding = 4 * scale,
        ScrollbarRounding = 3 * scale,
        GrabRounding = 2 * scale,
        TabRounding = 3 * scale,
        BorderSize = 1,
        IndentSpacing = 16 * scale,
        ScrollbarSize = 12 * scale,
        GrabMinSize = 10 * scale,
        HeaderExtra = 8 * scale,
        HeaderPadLeft = 10 * scale,
        HeaderPadRight = 4 * scale,
        HeaderGap = 6 * scale,
        HeaderButtonGap = 2 * scale,
        SeparatorMargin = 3 * scale,
        MinTitleWidth = 80 * scale,
        TitleFade = 24 * scale,
        FocusLine = 2 * scale,
        PillPadX = 8 * scale,
        PropertyGutter = 96 * scale,
        SplitterLine = 2 * scale,
        SplitterHit = 6 * scale,
        WindowPadding = new Vector2(8, 8) * scale,
        FramePadding = new Vector2(6, 3) * scale,
        ItemSpacing = new Vector2(6, 4) * scale,
        ItemInnerSpacing = new Vector2(4, 4) * scale,
        CellPadding = new Vector2(6, 3) * scale,
    };
}
