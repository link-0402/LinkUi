using LinkUi.Tests.Support;

namespace LinkUi.Tests;

/// <summary>
/// The counting behind <c>Style.Override()</c>. <c>StyleOverride</c> itself can't load here (its methods take
/// ImGui types), so these drive <see cref="OverrideStack"/> the way each override shape does: every copy of an
/// override carries the mark taken by <c>Override()</c>, and dispose pops back to it.
/// </summary>
internal static class StyleScenarios
{
    public static void Run()
    {
        Scenarios.Run(ChainedAndUnchained);
        Scenarios.Run(Conditional);
        Scenarios.Run(NestedUnwindInOrder);
        Scenarios.Run(DisposedTwiceOrOutOfOrder);
    }

    static void ChainedAndUnchained()  // final review A-I2
    {
        var stack = new OverrideStack();
        var chained = stack.Mark(); stack.PushedColour(); stack.PushedVar();
        Assertions.Check(stack.Unwind(chained) == (1, 1), "chained: pops one colour and one var");
        var unchained = stack.Mark(); stack.PushedColour(); stack.PushedColour();
        Assertions.Check(stack.Unwind(unchained) == (2, 0), "unchained calls on the bound value: pops both colours");
        Assertions.Check(stack.Colours == 0 && stack.Vars == 0, "back at zero");
    }
    static void Conditional()  // final review A-I2: using var o = Style.Override(); if (x) o.Colour(...);
    {
        var stack = new OverrideStack();
        var none = stack.Mark();
        Assertions.Check(stack.Unwind(none) == (0, 0), "condition false: pops nothing");
        var some = stack.Mark(); stack.PushedColour();
        Assertions.Check(stack.Unwind(some) == (1, 0), "condition true: pops the one push");
    }
    static void NestedUnwindInOrder()
    {
        var stack = new OverrideStack();
        var outer = stack.Mark(); stack.PushedColour();
        var inner = stack.Mark(); stack.PushedColour(); stack.PushedVar();
        Assertions.Check(stack.Unwind(inner) == (1, 1), "inner pops only its own pushes");
        stack.PushedVar();
        Assertions.Check(stack.Unwind(outer) == (1, 1), "outer pops its colour and the var pushed after the inner ended");
        Assertions.Check(stack.Colours == 0 && stack.Vars == 0, "nested: back at zero");
    }
    static void DisposedTwiceOrOutOfOrder()
    {
        var stack = new OverrideStack();
        var once = stack.Mark(); stack.PushedColour();
        stack.Unwind(once);
        Assertions.Check(stack.Unwind(once) == (0, 0), "a second dispose pops nothing");
        var outer = stack.Mark(); stack.PushedColour();
        var inner = stack.Mark(); stack.PushedVar();
        Assertions.Check(stack.Unwind(outer) == (1, 1), "outer disposed first pops everything above its mark");
        Assertions.Check(stack.Unwind(inner) == (0, 0) && stack.Colours == 0 && stack.Vars == 0, "then the inner pops nothing and the counts never go negative");
    }
}
