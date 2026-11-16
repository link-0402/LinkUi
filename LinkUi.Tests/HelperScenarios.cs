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
    }

    static void RightAlign()
    {
        Assertions.Check(Ui.RightAlignX(10, 200, 50) == 160f, "room");
        Assertions.Check(Ui.RightAlignX(10, 40, 50) == null, "no room");
        Assertions.Check(Ui.RightAlignX(10, 50, 50) == null, "exact fit stays put");
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
