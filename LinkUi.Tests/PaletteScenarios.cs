using System.Numerics;
using LinkUi.Tests.Support;

namespace LinkUi.Tests;

/// <summary>OKLCH colour math and the palette derived from one accent.</summary>
internal static class PaletteScenarios
{
    public static void Run()
    {
        Scenarios.Run(ColourMath);
        Scenarios.Run(AmberMatchesSpec);
        Scenarios.Run(OnAccentContrast);
        Scenarios.Run(OnAccentGrid);
        Scenarios.Run(AccentOutOfRange);
        Scenarios.Run(StatesStandApart);
        Scenarios.Run(TextContrast);
        Scenarios.Run(AchromaticAccents);
    }

    static Vector4 Hex(string hex) => Oklch.Hex(hex);

    static readonly string[] OtherAccents = ["#3a8ee0", "#4dc780", "#e685e0", "#f0f0f0", "#202020"];  // blue, green, pink, near-white, near-black
    static void ColourMath()
    {
        Assertions.Check(Oklch.Hex("#e0a43a") == new Vector4(224 / 255f, 164 / 255f, 58 / 255f, 1), "hex parse");
        foreach (var h in new[] { "#e0a43a", "#262626", "#ffffff", "#000000", "#5aa7e6", "#ff9a9a" })
        { var c = Oklch.Hex(h); var (l, ch, hue) = Oklch.ToOklch(c); Assertions.Check(Vector4.Distance(Oklch.FromOklch(l, ch, hue), c) < 1e-3f, $"OKLCH round trip {h}"); }
        Assertions.Check(MathF.Abs(Oklch.Contrast(Hex("#ffffff"), Hex("#000000")) - 21f) < 0.01f, "white on black is 21:1");
        Assertions.Check(MathF.Abs(Oklch.Contrast(Hex("#909090"), Hex("#262626")) - 4.74f) < 0.02f, "Muted on Window is about 4.74:1");
    }
    static void AmberMatchesSpec()
    {
        var p = Palette.From(Hex("#e0a43a"));
        (Vector4 Got, string Want, string Name)[] derived = [(p.AccentHovered, "#ebb455", "hover"), (p.AccentPressed, "#c98f2a", "pressed"),
            (p.AccentMuted, "#4a3a1c", "muted"), (p.Selection, "#55401a", "selection"), (p.SelectionText, "#ffd58a", "selection text"), (p.OnAccent, "#1a1200", "on accent"),
            (p.AccentMutedBorder, "#3a2e16", "muted border"), (p.DangerBorder, "#2a1515", "danger border"), (p.AccentBorder, "#a87a26", "accent border")];
        foreach (var (got, want, name) in derived)
            Assertions.Check(Oklch.DeltaE(got, Hex(want)) <= 0.03f, $"amber {name} within dE 0.03 of {want}");
        Assertions.Check(p.Window == Hex("#262626") && p.Muted == Hex("#909090") && p.TabText == Hex("#bdbdbd") && p.Accent == Hex("#e0a43a"), "fixed tokens and the accent are exact");
        // The sheet's tab strip and fill slider greys that reuse an existing token.
        (Vector4 Got, string Want, string Name)[] reused = [(p.Panel, "#1f1f1f", "tab rest"), (p.FrameActive, "#2e2e2e", "tab hover"),
            (p.Control, "#3a3a3a", "tab active"), (p.FrameActive, "#2a2a2a", "slider disabled fill"), (p.Selection, "#56431f", "slider hover fill")];
        foreach (var (got, want, name) in reused)
            Assertions.Check(Oklch.DeltaE(got, Hex(want)) <= 0.03f, $"{name} token within dE 0.03 of {want}");
    }
    static void OnAccentContrast()   // spec's five accents, plus amber
    {
        foreach (var a in OtherAccents.Append("#e0a43a"))
        { var p = Palette.From(Hex(a)); Assertions.Check(Oklch.Contrast(p.OnAccent, p.Accent) >= 4.5f, $"on-accent text on {a} at least 4.5:1"); }
    }
    static void OnAccentGrid()  // final review A-I1: every accent, not six picked ones
    {
        var below = 0; var worst = float.MaxValue; var worstAt = "";
        for (var r = 0; r < 256; r += 17)
            for (var g = 0; g < 256; g += 17)
                for (var b = 0; b < 256; b += 17)
                {
                    var p = Palette.From(new Vector4(r / 255f, g / 255f, b / 255f, 1));
                    var contrast = Oklch.Contrast(p.OnAccent, p.Accent);
                    if (contrast < 4.5f)
                        below++;
                    if (contrast < worst)
                        (worst, worstAt) = (contrast, $"#{r:x2}{g:x2}{b:x2}");
                }
        Assertions.Check(below == 0, $"on-accent text at least 4.5:1 on every step-17 grid accent ({below} below, worst {worst:0.00} at {worstAt})");
        var amber = Palette.From(Hex("#e0a43a")).OnAccent;
        Assertions.Check(ToHex(amber) == "#1a1200", $"amber keeps its on-accent colour (got {ToHex(amber)})");
    }
    static void AccentOutOfRange()  // final review A-M1
    {
        Assertions.Reject(() => Palette.From(new Vector4(float.NaN, 0.5f, 0.5f, 1)), "a NaN accent component is rejected");
        Assertions.Reject(() => Palette.From(new Vector4(0.5f, float.PositiveInfinity, 0.5f, 1)), "an infinite accent component is rejected");
        Assertions.Reject(() => Palette.From(new Vector4(224, 164, 58, 1)), "an accent in bytes instead of 0 to 1 is rejected");
        Assertions.Reject(() => Palette.From(new Vector4(0.5f, 0.5f, -0.01f, 1)), "a negative accent component is rejected");
        Assertions.Reject(() => Palette.From(new Vector4(0.5f, 0.5f, 0.5f, 255)), "an alpha outside 0 to 1 is rejected");
        Assertions.Check(Palette.From(new Vector4(0, 1, 0, 0)).Accent.W == 1f, "the ends of the range are accepted");
    }
    static string ToHex(Vector4 c) => $"#{(int)MathF.Round(c.X * 255):x2}{(int)MathF.Round(c.Y * 255):x2}{(int)MathF.Round(c.Z * 255):x2}";
    static void StatesStandApart()
    {
        foreach (var a in OtherAccents.Append("#e0a43a"))
        {
            var p = Palette.From(Hex(a));
            Assertions.Check(Oklch.DeltaE(p.AccentHovered, p.Accent) >= 0.04f, $"{a} hover differs");
            Assertions.Check(Oklch.DeltaE(p.AccentPressed, p.Accent) >= 0.04f, $"{a} pressed differs");
            Assertions.Check(Oklch.DeltaE(p.Selection, p.Accent) >= 0.04f, $"{a} selection differs");
        }
    }
    static void TextContrast()
    {
        var p = Palette.From(Hex("#e0a43a"));
        foreach (var (bg, name) in new[] { (p.Window, "Window"), (p.Panel, "Panel"), (p.Frame, "Frame") })
        {
            Assertions.Check(Oklch.Contrast(p.Text, bg) >= 7f, $"Text on {name} at least 7:1");
            Assertions.Check(Oklch.Contrast(p.Muted, bg) >= 4.5f, $"Muted on {name} at least 4.5:1");
            Assertions.Check(Oklch.Contrast(p.Hint, bg) >= 3f, $"Hint on {name} at least 3:1");
        }
    }
    static void AchromaticAccents()  // Review Focus 1
    {
        foreach (var a in new[] { "#808080", "#000000", "#ffffff" })
        {
            var p = Palette.From(Hex(a));
            var all = new[] { p.AccentHovered, p.AccentPressed, p.AccentMuted, p.AccentBorder, p.Selection, p.SelectionText, p.OnAccent };
            Assertions.Check(all.All(v => float.IsFinite(v.X + v.Y + v.Z) && v.X is >= 0 and <= 1 && v.Y is >= 0 and <= 1 && v.Z is >= 0 and <= 1), $"{a} derives finite colours in range");
        }
        var half = Hex("#e0a43a"); half.W = 0.5f;
        Assertions.Check(Palette.From(half).Accent.W == 1f, "accent alpha is forced to 1");
    }
}
