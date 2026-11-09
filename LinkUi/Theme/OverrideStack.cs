namespace LinkUi;

/// <summary>
/// Counts the colours and style vars pushed through <see cref="StyleOverride"/>s. An override takes a
/// <see cref="Mark"/> when it starts and unwinds back to it on dispose, so every copy of it pops the same
/// pushes, however its calls were chained, and nested overrides unwind in order. Draw thread only.
/// </summary>
internal sealed class OverrideStack
{
    public int Colours { get; private set; }

    public int Vars { get; private set; }

    public (int Colours, int Vars) Mark() => (Colours, Vars);

    public void PushedColour() => Colours++;

    public void PushedVar() => Vars++;

    /// <summary>
    /// How many colours and vars to pop to get back to <paramref name="mark"/>, and forgets them. Nothing
    /// for a mark already unwound past (a second dispose, or an inner override disposed after its outer one).
    /// </summary>
    public (int Colours, int Vars) Unwind((int Colours, int Vars) mark)
    {
        var colours = Math.Max(0, Colours - mark.Colours);
        var vars = Math.Max(0, Vars - mark.Vars);
        Colours -= colours;
        Vars -= vars;
        return (colours, vars);
    }
}
