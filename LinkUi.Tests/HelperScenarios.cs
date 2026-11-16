using LinkUi.Tests.Support;

namespace LinkUi.Tests;

/// <summary>The text behind the labeled rows.</summary>
internal static class HelperScenarios
{
    public static void Run()
    {
        Scenarios.Run(CopyTip);
    }

    static void CopyTip()
    {
        Assertions.Check(Labels.CopyTip(null) == "Click to copy.", "no help");
        Assertions.Check(Labels.CopyTip("Path of the mod.") == "Path of the mod.\n\nClick to copy.", "with help");
        Assertions.Check(ReferenceEquals(Labels.CopyTip("a"), Labels.CopyTip("a")), "cached for the same help");
    }
}
