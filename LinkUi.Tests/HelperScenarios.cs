using LinkUi.Tests.Support;

namespace LinkUi.Tests;

/// <summary>The text behind the labeled rows.</summary>
internal static class HelperScenarios
{
    public static void Run()
    {
        Scenarios.Run(CopyTip);
        Scenarios.Run(RightAlign);
        Scenarios.Run(HoverDelayClock);
        Scenarios.Run(FallbackTheme);
    }

    static void FallbackTheme()
    {
        Assertions.Check(LinkUiHost.Current == null, "no host in the test run");
        Assertions.Check(Theme.Current.Palette != null && Theme.Current.Palette.Text.W == 1f, "without a host the theme has a palette with opaque text");
        Assertions.Check(Theme.Current.Metrics.Scale == 1f, "without a host the metrics are at 100%");
    }

    static void RightAlign()
    {
        Assertions.Check(Ui.RightAlignX(10, 200, 50, 10, 0) == 160f, "room");
        Assertions.Check(Ui.RightAlignX(10, 40, 50, 10, 0) == null, "no room");
        Assertions.Check(Ui.RightAlignX(10, 50, 50, 10, 0) == null, "exact fit stays put");
        Assertions.Check(Ui.RightAlignX(10, 200, 50, 100, 8) == 160f, "fits after the previous item");
        Assertions.Check(Ui.RightAlignX(10, 200, 50, 152, 8) == 160f, "touching the spacing still fits");
        Assertions.Check(Ui.RightAlignX(0, 350, 90, 300, 8) == null, "would overlap the previous item, so it wraps");
    }

    static void HoverDelayClock()
    {
        var c = new HoverClock();
        Assertions.Check(!c.Update("a", 0.0, 1, false, 0.3), "first frame waits");
        Assertions.Check(c.Update("a", 0.31, 2, false, 0.3), "after the delay");
        Assertions.Check(!c.Update("b", 0.32, 3, false, 0.3), "new id restarts");
        Assertions.Check(!c.Update("b", 0.70, 6, false, 0.3), "a gap of frames restarts");
        Assertions.Check(c.Update("b", 0.70, 7, true, 0.3), "Shift shows at once");
    }

    static void CopyTip()
    {
        Assertions.Check(Labels.CopyTip(null) == "Click to copy.", "no help");
        Assertions.Check(Labels.CopyTip("Path of the mod.") == "Path of the mod.\n\nClick to copy.", "with help");
        Assertions.Check(ReferenceEquals(Labels.CopyTip("a"), Labels.CopyTip("a")), "cached for the same help");
        var first = Labels.CopyTip("a"); Labels.CopyTip("b");
        Assertions.Check(ReferenceEquals(Labels.CopyTip("a"), first), "several helps stay cached");
    }
}
