using LinkUi.Tests.Support;

namespace LinkUi.Tests;

/// <summary>The cached text widgets draw: visible labels and number strings.</summary>
internal static class TextScenarios
{
    public static void Run()
    {
        Scenarios.Run(LabelText);
        Scenarios.Run(NumberText);
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
}
