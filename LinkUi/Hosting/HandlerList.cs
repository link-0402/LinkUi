namespace LinkUi;

/// <summary>
/// The host's draw subscribers. <see cref="Add"/> and <see cref="Remove"/> swap in a new array, so
/// <see cref="InvokeAll"/> walks a snapshot without allocating, and a change made during a pass takes
/// effect on the next one. A throwing handler never stops the others; each distinct failure is reported once.
/// </summary>
internal sealed class HandlerList
{
    private readonly object gate = new();
    private readonly HashSet<(Action Handler, Type Type, string Message)> reported = [];
    private volatile Action[] handlers = [];

    public int Count => handlers.Length;

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

    /// <summary>Removes the last subscription equal to <paramref name="h"/>, as a multicast delegate would.</summary>
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
        }
    }

    /// <summary>
    /// Runs every handler of the current snapshot, each in its own try/catch. <paramref name="onNewError"/>
    /// fires once per handler, exception type and message. Called from the draw thread only.
    /// </summary>
    public void InvokeAll(Action<Action, Exception> onNewError)
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
                if (reported.Add((handler, e.GetType(), e.Message)))
                    onNewError(handler, e);
            }
        }
    }
}
