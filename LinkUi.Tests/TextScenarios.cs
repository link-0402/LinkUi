using LinkUi.Tests.Support;

namespace LinkUi.Tests;

/// <summary>The cached text widgets draw: visible labels and number strings.</summary>
internal static class TextScenarios
{
    public static void Run()
    {
        Scenarios.Run(LabelText);
        Scenarios.Run(NumberText);
        Scenarios.Run(WheelSteps);
    }

    static void LabelText()
    {
        Assertions.Check(Labels.Visible("Save##x") == "Save" && Labels.Visible("##only") == "", "text before ##");
        var plain = "Save"; Assertions.Check(ReferenceEquals(Labels.Visible(plain), plain), "no ## returns the same string");
        Labels.Visible("Cached##1"); var before = GC.GetAllocatedBytesForCurrentThread(); Labels.Visible("Cached##1");
        Assertions.Check(GC.GetAllocatedBytesForCurrentThread() == before, "second call allocates nothing");
    }
    static void NumberText()
    {
        Assertions.Check(Numbers.Of(0) == "0" && Numbers.Of(1023) == "1023" && Numbers.Of(-5) == "-5" && Numbers.Of(1_000_000) == "1000000", "values");
        Assertions.Check(ReferenceEquals(Numbers.Of(42), Numbers.Of(42)), "small numbers are cached");
    }
    static void WheelSteps()
    {
        Assertions.Check(ComboWheel.Step(0, 3, 1) == 1 && ComboWheel.Step(2, 3, 1) == 0 && ComboWheel.Step(0, 3, -1) == 2, "wraps both ways");
        Assertions.Check(ComboWheel.Step(-1, 3, 1) == 0 && ComboWheel.Step(7, 3, -1) == 2, "out of range starts at an end");
        Assertions.Check(ComboWheel.Step(1, 0, 1) == 1, "empty list keeps the index");
        Assertions.Check(ComboWheel.Pick(new[] { "a", "b", "c" }, "c", 1) == "a" && ComboWheel.Pick(new[] { "a", "b" }, "zz", -1) == "b", "pick by value");
        Assertions.Check(ComboWheel.Pick(Array.Empty<string>(), "x", 1) == "x", "pick by value on an empty list keeps the current entry");
        Assertions.Check(ComboWheel.Pick(Array.Empty<string>(), s => s == "x", 1) == null, "pick by predicate on an empty list gives the default");
    }
}
