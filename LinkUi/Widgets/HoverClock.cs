namespace LinkUi;

/// <summary>The timer behind <see cref="Ui.HoverDelay"/>: one item hovered for a delay, Shift skips the wait.</summary>
internal struct HoverClock
{
    private string? id;
    private double since;
    private int frame;

    /// <summary>
    /// True once the same item has been hovered for the delay. The timer restarts when the id changes
    /// or more than one frame passed since the last call.
    /// </summary>
    public bool Update(string id, double now, int frame, bool shift, double delaySeconds)
    {
        if (!string.Equals(id, this.id, StringComparison.Ordinal) || frame - this.frame > 1)
        {
            this.id = id;
            since = now;
        }
        this.frame = frame;
        return shift || now - since >= delaySeconds;
    }
}
