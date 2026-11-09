using System.Numerics;

namespace LinkUi;

/// <summary>
/// Every colour the library draws with. The greys and status colours are fixed; the accent states,
/// selection and on-accent text are derived from one accent colour in OKLCH by <see cref="From"/>.
/// </summary>
public sealed record Palette
{
    public Vector4 Window { get; init; }
    public Vector4 Panel { get; init; }
    public Vector4 Frame { get; init; }
    public Vector4 FrameHovered { get; init; }
    public Vector4 FrameActive { get; init; }
    public Vector4 Header { get; init; }
    public Vector4 Popup { get; init; }
    public Vector4 Tooltip { get; init; }
    public Vector4 TooltipBorder { get; init; }
    public Vector4 Control { get; init; }
    public Vector4 ControlHovered { get; init; }
    public Vector4 ControlActive { get; init; }
    public Vector4 Border { get; init; }
    public Vector4 WindowBorder { get; init; }
    public Vector4 Hairline { get; init; }
    public Vector4 Text { get; init; }
    public Vector4 Title { get; init; }
    public Vector4 Muted { get; init; }
    public Vector4 Hint { get; init; }
    public Vector4 Disabled { get; init; }

    /// <summary>Header and ghost-button icons at rest.</summary>
    public Vector4 Glyph { get; init; }

    public Vector4 Accent { get; init; }
    public Vector4 AccentHovered { get; init; }
    public Vector4 AccentPressed { get; init; }
    public Vector4 AccentMuted { get; init; }

    /// <summary>Outline of a disabled accent button: <see cref="AccentMuted"/> a little darker.</summary>
    public Vector4 AccentMutedBorder { get; init; }

    public Vector4 Selection { get; init; }
    public Vector4 SelectionText { get; init; }

    /// <summary>Text drawn on the accent: a dark or a light candidate, whichever contrasts more.</summary>
    public Vector4 OnAccent { get; init; }

    public Vector4 Success { get; init; }
    public Vector4 SuccessTint { get; init; }
    public Vector4 Warning { get; init; }
    public Vector4 WarningTint { get; init; }
    public Vector4 Error { get; init; }
    public Vector4 ErrorTint { get; init; }
    public Vector4 Danger { get; init; }
    public Vector4 DangerHovered { get; init; }
    public Vector4 DangerActive { get; init; }
    public Vector4 DangerText { get; init; }

    /// <summary>Outline of a danger button: <see cref="Danger"/> a little darker.</summary>
    public Vector4 DangerBorder { get; init; }

    public Vector4 Info { get; init; }
    public Vector4 InfoTint { get; init; }

    /// <summary>The palette for an accent colour; the accent's alpha is forced to 1.</summary>
    public static Palette From(Vector4 accent)
    {
        accent.W = 1f;
        var (l, c, h) = Oklch.ToOklch(accent);
        var hoverL = l + 0.045f > 0.90f ? l - 0.045f : l + 0.045f;
        var pressedL = l - 0.067f < 0.25f ? l + 0.067f : l - 0.067f;
        var dark = Oklch.FromOklch(0.187f, c * 0.28f, h + 11.4f);
        var light = Oklch.FromOklch(0.985f, 0f, h);
        var danger = Oklch.Hex("#3a2222");
        var (dangerL, dangerC, dangerH) = Oklch.ToOklch(danger);
        return new Palette
        {
            Window = Oklch.Hex("#262626"),
            Panel = Oklch.Hex("#1e1e1e"),
            Frame = Oklch.Hex("#1a1a1a"),
            FrameHovered = Oklch.Hex("#222222"),
            FrameActive = Oklch.Hex("#2a2a2a"),
            Header = Oklch.Hex("#1d1d1d"),
            Popup = Oklch.Hex("#202020"),
            Tooltip = Oklch.Hex("#111111"),
            TooltipBorder = Oklch.Hex("#333333"),
            Control = Oklch.Hex("#383838"),
            ControlHovered = Oklch.Hex("#444444"),
            ControlActive = Oklch.Hex("#4f4f4f"),
            Border = Oklch.Hex("#141414"),
            WindowBorder = Oklch.Hex("#121212"),
            Hairline = Oklch.Hex("#333333"),
            Text = Oklch.Hex("#d9d9d9"),
            Title = Oklch.Hex("#e6e6e6"),
            Muted = Oklch.Hex("#909090"),
            Hint = Oklch.Hex("#7a7a7a"),
            Disabled = Oklch.Hex("#5e5e5e"),
            Glyph = Oklch.Hex("#9a9a9a"),
            Accent = accent,
            AccentHovered = Oklch.FromOklch(hoverL, c * 0.94f, h + 1.1f),
            AccentPressed = Oklch.FromOklch(pressedL, c * 0.95f, h - 1.3f),
            AccentMuted = Oklch.FromOklch(0.358f, c * 0.37f, h + 4.0f),
            AccentMutedBorder = Oklch.FromOklch(0.308f, c * 0.30f, h + 5.7f),
            Selection = Oklch.FromOklch(0.386f, c * 0.45f, h + 2.3f),
            SelectionText = Oklch.FromOklch(0.893f, c * 0.76f, h + 3.6f),
            OnAccent = Oklch.Contrast(dark, accent) >= Oklch.Contrast(light, accent) ? dark : light,
            Success = Oklch.Hex("#4dc780"),
            SuccessTint = Oklch.Hex("#12251a"),
            Warning = Oklch.Hex("#f2b33d"),
            WarningTint = Oklch.Hex("#2a2010"),
            Error = Oklch.Hex("#e65a5a"),
            ErrorTint = Oklch.Hex("#2a1414"),
            Danger = danger,
            DangerBorder = Oklch.FromOklch(dangerL - 0.058f, dangerC * 0.93f, dangerH + 0.5f),
            DangerHovered = Oklch.Hex("#4a2828"),
            DangerActive = Oklch.Hex("#5a2e2e"),
            DangerText = Oklch.Hex("#ff9a9a"),
            Info = Oklch.Hex("#5aa7e6"),
            InfoTint = Oklch.Hex("#122030"),
        };
    }
}
