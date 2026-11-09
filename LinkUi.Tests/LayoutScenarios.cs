using System.Numerics;
using System.Reflection;
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
        Scenarios.Run(HeaderNaturalWithActions);
        Scenarios.Run(HeaderNaturalWithoutActions);
        Scenarios.Run(HeaderNaturalSubtitle);
        Scenarios.Run(HeaderNaturalClickThrough);
        Scenarios.Run(WindowNames);
        Scenarios.Run(WidthCache);
    }

    static HeaderInput Input(float width, float title = 120, float sub = 0, int actions = 4, bool ct = false, float scale = 1) =>
        new(width * scale, 23 * scale, 16 * scale, true, title * scale, sub * scale, actions, true, true, ct, 200 * scale, Metrics.At(scale));
    static void MetricsScale()
    {
        var m1 = Metrics.At(1f);
        Assertions.Check(m1.WindowRounding == 5 && m1.FramePadding == new Vector2(6, 3) && m1.HeaderExtra == 8 && m1.MinTitleWidth == 80, "base values");
        // Every float and Vector2 size but Scale and BorderSize, by reflection, so a new one can't miss its scale.
        var sizes = typeof(Metrics).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => (p.PropertyType == typeof(float) || p.PropertyType == typeof(Vector2)) && p.Name is not ("Scale" or "BorderSize"))
            .ToArray();
        Assertions.Check(sizes.Length >= 28, $"reflection finds the sizes ({sizes.Length})");
        foreach (var s in new[] { 1.25f, 1.5f, 2f })
        {
            var m = Metrics.At(s);
            object at1 = Metrics.At(1f), atS = m;
            var wrong = sizes.Where(p => p.PropertyType == typeof(float)
                ? (float)p.GetValue(atS)! != (float)p.GetValue(at1)! * s
                : (Vector2)p.GetValue(atS)! != (Vector2)p.GetValue(at1)! * s).Select(p => p.Name).ToArray();
            Assertions.Check(wrong.Length == 0, $"every size scales at {s}" + (wrong.Length == 0 ? "" : ": not " + string.Join(", ", wrong)));
            Assertions.Check(m.BorderSize == 1 && m.Scale == s, $"border stays 1 at {s}");
        }
    }
    static void HeaderWide()   { var r = HeaderLayout.Compute(Input(560)); Assertions.Check(r.ActionsInHeader && !r.ActionsInMenu && r.TitleShown == 120 && r.ShowButtons, "wide: everything shows"); }
    static void HeaderNarrow() { var r = HeaderLayout.Compute(Input(300)); Assertions.Check(!r.ActionsInHeader && r.ActionsInMenu && r.TitleShown == 120, "300 px: actions fold into the menu"); }
    static void HeaderLongTitle() { var r = HeaderLayout.Compute(Input(300, title: 400)); Assertions.Check(r.TitleShown == 185 && r.ShowButtons, "long title clips to 185, pin/options/close stay"); }
    static void HeaderSubtitleClipsFirst() { var r = HeaderLayout.Compute(Input(400, sub: 200, actions: 0)); Assertions.Check(r.TitleShown == 120 && r.SubtitleShown == 159, "subtitle clips before the title"); }
    static void HeaderClickThrough() { var r = HeaderLayout.Compute(Input(560, ct: true)); Assertions.Check(!r.ShowButtons && r.ShowPill && !r.ActionsInHeader && !r.ActionsInMenu, "click-through: only the pill"); }
    static void HeaderTiny() { var r = HeaderLayout.Compute(Input(50, sub: 80)); Assertions.Check(r.TitleShown >= 0 && r.SubtitleShown >= 0 && r.PillShown >= 0, "50 px: no negative widths"); }
    static void HeaderScaleInvariant() { var a = HeaderLayout.Compute(Input(300)); var b = HeaderLayout.Compute(Input(300, scale: 2)); Assertions.Check(a.ActionsInHeader == b.ActionsInHeader && b.TitleShown == a.TitleShown * 2, "same decisions at 200%"); }
    static void HeaderNaturalWithActions()
    {
        var input = Input(0);
        var natural = HeaderLayout.NaturalWidth(input);
        Assertions.Check(natural == 342, "natural width with 4 actions is 342");
        Assertions.Check(Full(input with { Width = natural }, input), "4 actions: nothing folds or clips at the natural width");
        Assertions.Check(!Full(input with { Width = natural - 1 }, input), "4 actions: one pixel less folds or clips");
        Assertions.Check(HeaderLayout.NaturalWidth(Input(0, scale: 2)) == natural * 2 - 1, "natural width scales, all but the 1 px separator");
    }
    static void HeaderNaturalWithoutActions()
    {
        var input = Input(0, actions: 0);
        var natural = HeaderLayout.NaturalWidth(input);
        Assertions.Check(natural == 235, "natural width without actions is 235");
        Assertions.Check(Full(input with { Width = natural }, input), "no actions: nothing clips at the natural width");
        Assertions.Check(!Full(input with { Width = natural - 1 }, input), "no actions: one pixel less clips");
    }
    static void HeaderNaturalSubtitle()
    {
        var input = Input(0, sub: 150);
        var natural = HeaderLayout.NaturalWidth(input);
        Assertions.Check(Full(input with { Width = natural }, input), "subtitle: nothing folds or clips at the natural width");
        Assertions.Check(!Full(input with { Width = natural - 1 }, input), "subtitle: one pixel less clips");
    }
    static void HeaderNaturalClickThrough()
    {
        var input = Input(0, sub: 150, ct: true);
        var natural = HeaderLayout.NaturalWidth(input);
        Assertions.Check(HeaderLayout.Compute(input with { Width = natural }).PillShown == 200 && Full(input with { Width = natural }, input), "click-through: the whole pill, title and subtitle fit");
        Assertions.Check(!Full(input with { Width = natural - 1 }, input), "click-through: one pixel less clips");
    }
    /// <summary>Title and subtitle show in full and, when there are actions and no pill, they stay in the header.</summary>
    static bool Full(in HeaderInput sized, in HeaderInput input)
    {
        var r = HeaderLayout.Compute(sized);
        return r.TitleShown == input.TitleWidth && r.SubtitleShown == input.SubtitleWidth
            && (input.ClickThrough || input.PluginActions == 0 || r.ActionsInHeader);
    }
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
        var slot = new WidthSlot(); nint font = 0x1000, other = 0x2000;
        slot.Get("Gallery", font, 17, measure); slot.Get("Gallery", font, 17, measure); slot.Get(new string("Gallery".ToCharArray()), font, 17, measure);
        Assertions.Check(calls == 1, "same text, font and size measure once");
        slot.Get("Gallery", font, 25.5f, measure); Assertions.Check(calls == 2, "a new font size re-measures");
        slot.Get("Other", font, 25.5f, measure); Assertions.Check(calls == 3, "new text re-measures");
        slot.Get("Other", other, 25.5f, measure); Assertions.Check(calls == 4, "another font at the same size re-measures");  // final review A-M6
        slot.Get("Other", other, 25.5f, measure); Assertions.Check(calls == 4, "and then holds");
    }
}
