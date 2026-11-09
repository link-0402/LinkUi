namespace LinkUi.Demo;

/// <summary>
/// Bytes allocated per frame by one batch of draw calls: skips the first <c>warmup</c> frames, then
/// reports the largest of the last <c>window</c> frames.
/// </summary>
internal sealed class AllocationProbe
{
    private readonly int warmup;
    private readonly long[] recent;
    private int next;
    private long recorded;

    public AllocationProbe(int warmup = 120, int window = 60)
    {
        this.warmup = warmup;
        recent = new long[window];
    }

    public void Record(long bytes)
    {
        recent[next] = bytes;
        next = (next + 1) % recent.Length;
        recorded++;
    }

    /// <summary>Forgets every frame, so the next reading waits out the warmup again.</summary>
    public void Reset()
    {
        Array.Clear(recent);
        next = 0;
        recorded = 0;
    }

    /// <summary>Null until <c>warmup + window</c> frames are recorded; then the largest of the last <c>window</c>.</summary>
    public long? Max
    {
        get
        {
            if (recorded < warmup + recent.Length)
                return null;
            var max = recent[0];
            for (var i = 1; i < recent.Length; i++)
                max = Math.Max(max, recent[i]);
            return max;
        }
    }
}
