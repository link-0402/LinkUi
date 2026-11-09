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
            (p.AccentMuted, "#4a3a1c", "muted"), (p.Selection, "#55401a", "selection"), (p.SelectionText, "#ffd58a", "selection text"), (p.OnAccent, "#1a1200", "on accent")];
        foreach (var (got, want, name) in derived)
            Assertions.Check(Oklch.DeltaE(got, Hex(want)) <= 0.03f, $"amber {name} within dE 0.03 of {want}");
        Assertions.Check(p.Window == Hex("#262626") && p.Muted == Hex("#909090") && p.Accent == Hex("#e0a43a"), "fixed tokens and the accent are exact");
    }
    static void OnAccentContrast()   // spec's five accents, plus amber
    {
        foreach (var a in OtherAccents.Append("#e0a43a"))
        { var p = Palette.From(Hex(a)); Assertions.Check(Oklch.Contrast(p.OnAccent, p.Accent) >= 4.5f, $"on-accent text on {a} at least 4.5:1"); }
    }
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
            var all = new[] { p.AccentHovered, p.AccentPressed, p.AccentMuted, p.Selection, p.SelectionText, p.OnAccent };
            Assertions.Check(all.All(v => float.IsFinite(v.X + v.Y + v.Z) && v.X is >= 0 and <= 1 && v.Y is >= 0 and <= 1 && v.Z is >= 0 and <= 1), $"{a} derives finite colours in range");
        }
        var half = Hex("#e0a43a"); half.W = 0.5f;
        Assertions.Check(Palette.From(half).Accent.W == 1f, "accent alpha is forced to 1");
    }
}
