using System.Numerics;
using LinkUi.Tests.Support;

namespace LinkUi.Tests;

/// <summary>Scaled metrics, the header's fold and clip rules, window name parsing and the text width cache.</summary>
internal static class LayoutScenarios
{
    public static void Run()
    {
        Scenarios.Run(MetricsScale);
        Scenarios.Run(HeaderWide);
        Scenarios.Run(HeaderNarrow);
        Scenarios.Run(HeaderLongTitle);
        Scenarios.Run(HeaderSubtitleClipsFirst);
        Scenarios.Run(HeaderClickThrough);
        Scenarios.Run(HeaderTiny);
        Scenarios.Run(HeaderScaleInvariant);
        Scenarios.Run(WindowNames);
        Scenarios.Run(WidthCache);
    }

    static HeaderInput Input(float width, float title = 120, float sub = 0, int actions = 4, bool ct = false, float scale = 1) =>
        new(width * scale, 23 * scale, 16 * scale, true, title * scale, sub * scale, actions, true, true, ct, 200 * scale, Metrics.At(scale));
    static void MetricsScale()
    {
        var m1 = Metrics.At(1f);
        Assertions.Check(m1.WindowRounding == 5 && m1.FramePadding == new Vector2(6, 3) && m1.HeaderExtra == 8 && m1.MinTitleWidth == 80, "base values");
        foreach (var s in new[] { 1.5f, 2f })
        {
            var m = Metrics.At(s);
            Assertions.Check(m.WindowRounding == 5 * s && m.ItemSpacing == new Vector2(6, 4) * s && m.ScrollbarSize == 12 * s && m.TitleFade == 24 * s, $"sizes scale at {s}");
            Assertions.Check(m.BorderSize == 1, $"border stays 1 at {s}");
        }
    }
    static void HeaderWide()   { var r = HeaderLayout.Compute(Input(560)); Assertions.Check(r.ActionsInHeader && !r.ActionsInMenu && r.TitleShown == 120 && r.ShowButtons, "wide: everything shows"); }
    static void HeaderNarrow() { var r = HeaderLayout.Compute(Input(300)); Assertions.Check(!r.ActionsInHeader && r.ActionsInMenu && r.TitleShown == 120, "300 px: actions fold into the menu"); }
    static void HeaderLongTitle() { var r = HeaderLayout.Compute(Input(300, title: 400)); Assertions.Check(r.TitleShown == 185 && r.ShowButtons, "long title clips to 185, pin/options/close stay"); }
    static void HeaderSubtitleClipsFirst() { var r = HeaderLayout.Compute(Input(400, sub: 200, actions: 0)); Assertions.Check(r.TitleShown == 120 && r.SubtitleShown == 159, "subtitle clips before the title"); }
    static void HeaderClickThrough() { var r = HeaderLayout.Compute(Input(560, ct: true)); Assertions.Check(!r.ShowButtons && r.ShowPill && !r.ActionsInHeader && !r.ActionsInMenu, "click-through: only the pill"); }
    static void HeaderTiny() { var r = HeaderLayout.Compute(Input(50, sub: 80)); Assertions.Check(r.TitleShown >= 0 && r.SubtitleShown >= 0 && r.PillShown >= 0, "50 px: no negative widths"); }
    static void HeaderScaleInvariant() { var a = HeaderLayout.Compute(Input(300)); var b = HeaderLayout.Compute(Input(300, scale: 2)); Assertions.Check(a.ActionsInHeader == b.ActionsInHeader && b.TitleShown == a.TitleShown * 2, "same decisions at 200%"); }
    static void WindowNames()  // Review Focus 2
    {
        Assertions.Check(WindowText.Title("Instant Edit###ie-main") == "Instant Edit" && WindowText.Id("Instant Edit###ie-main") == "ie-main", "### name");
        Assertions.Check(WindowText.Title("Material##3") == "Material" && WindowText.Id("Material##3") == "Material##3", "## name");
        Assertions.Check(WindowText.Title("Plain") == "Plain" && WindowText.Id("Plain") == "Plain", "plain name");
        Assertions.Check(WindowText.Id("A###") == "A###" && WindowText.Title("A###") == "A", "empty ### id falls back to the whole name");
    }
    static void WidthCache()  // Review Focus 4
    {
        var calls = 0; Func<string, float> measure = s => { calls++; return s.Length * 7f; };
        var slot = new WidthSlot();
        slot.Get("Gallery", 17, measure); slot.Get("Gallery", 17, measure); slot.Get(new string("Gallery".ToCharArray()), 17, measure);
        Assertions.Check(calls == 1, "same text and size measure once");
        slot.Get("Gallery", 25.5f, measure); Assertions.Check(calls == 2, "a new font size re-measures");
        slot.Get("Other", 25.5f, measure); Assertions.Check(calls == 3, "new text re-measures");
    }
}
