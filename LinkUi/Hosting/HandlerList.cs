namespace LinkUi;

/// <summary>
/// The host's draw subscribers. <see cref="Add"/> and <see cref="Remove"/> swap in a new array, so
/// <see cref="InvokeAll"/> walks a snapshot without allocating, and a change made during a pass takes
/// effect on the next one. A throwing handler never stops the others; each distinct failure is reported once,
/// up to <see cref="MaxReports"/> per handler, then one suppressed line and nothing more.
/// </summary>
internal sealed class HandlerList
{
    /// <summary>Distinct failures reported per handler before the rest are suppressed.</summary>
    public const int MaxReports = 5;

    private readonly object gate = new();
    private readonly HashSet<(Action Handler, Type Type, string Message)> reported = [];

    /// <summary>Distinct failures seen per handler; past <see cref="MaxReports"/> the handler is suppressed.</summary>
    private readonly Dictionary<Action, int> reportCounts = new();

    private volatile Action[] handlers = [];

    public int Count => handlers.Length;

    /// <summary>The failures remembered for de-duplication, for the tests.</summary>
    internal int TrackedErrors
    {
        get { lock (gate) return reported.Count; }
    }

    public void Add(Action h)
    {
        lock (gate)
        {
            var old = handlers;
            var next = new Action[old.Length + 1];
            Array.Copy(old, next, old.Length);
            next[old.Length] = h;
            handlers = next;
        }
    }

    /// <summary>
    /// Removes the last subscription equal to <paramref name="h"/>, as a multicast delegate would. When no equal
    /// subscription is left, its remembered failures go too, so a removed handler isn't kept alive.
    /// </summary>
    public void Remove(Action h)
    {
        lock (gate)
        {
            var old = handlers;
            var index = Array.LastIndexOf(old, h);
            if (index < 0)
                return;
            var next = new Action[old.Length - 1];
            Array.Copy(old, 0, next, 0, index);
            Array.Copy(old, index + 1, next, index, old.Length - index - 1);
            handlers = next;
            if (Array.IndexOf(next, h) < 0 && reportCounts.Remove(h))
                reported.RemoveWhere(r => r.Handler.Equals(h));
        }
    }

    /// <summary>
    /// Runs every handler of the current snapshot, each in its own try/catch. <paramref name="onNewError"/>
    /// fires once per handler, exception type and message, for the first <see cref="MaxReports"/> of them;
    /// the next new one fires <paramref name="onSuppressed"/> once instead. Called from the draw thread only.
    /// </summary>
    public void InvokeAll(Action<Action, Exception> onNewError, Action<Action> onSuppressed)
    {
        var snapshot = handlers;
        for (var i = 0; i < snapshot.Length; i++)
        {
            var handler = snapshot[i];
            try
            {
                handler();
            }
            catch (Exception e)
            {
                switch (Record(handler, e))
                {
                    case Outcome.Report:
                        onNewError(handler, e);
                        break;
                    case Outcome.Suppress:
                        onSuppressed(handler);
                        break;
                }
            }
        }
    }

    /// <summary>Error path only: locked, since <see cref="Remove"/> may run on another thread.</summary>
    private Outcome Record(Action handler, Exception e)
    {
        lock (gate)
        {
            reportCounts.TryGetValue(handler, out var count);
            if (count > MaxReports)
                return Outcome.Quiet;
            var key = (handler, e.GetType(), e.Message);
            if (reported.Contains(key))
                return Outcome.Quiet;
            reportCounts[handler] = count + 1;
            if (count == MaxReports)
                return Outcome.Suppress;
            reported.Add(key);
            return Outcome.Report;
        }
    }

    private enum Outcome
    {
        Quiet,
        Report,
        Suppress,
    }
}
