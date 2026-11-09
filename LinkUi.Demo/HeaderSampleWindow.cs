using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Plugin.Services;

namespace LinkUi.Demo;

/// <summary>A ToolWindow the Gallery drives, to check the header's subtitle, folding and clipping.</summary>
internal sealed class HeaderSampleWindow : ToolWindow
{
    private const string SampleSubtitle = "material_a.mtrl · Body";

    private static readonly Vector2 NarrowSize = new(300, 200);
    private static readonly Vector2 WideSize = new(560, 300);

    private readonly HeaderAction[] actions;
    private bool resizePending;
    private float fill = 0.5f;

    /// <summary>Allocation start of this frame's draw, read at the end of PreDraw.</summary>
    private long drawStart;

    public HeaderSampleWindow(IPluginLog log)
        : base("Header sample###linkui-header-sample")
    {
        actions =
        [
            new HeaderAction(FontAwesomeIcon.Heart, "Sample: heart", () => log.Information("sample action {N}", 1)),
            new HeaderAction(FontAwesomeIcon.Comments, "Sample: comments", () => log.Information("sample action {N}", 2)),
            new HeaderAction(FontAwesomeIcon.BookOpen, "Sample: book", () => log.Information("sample action {N}", 3)),
            new HeaderAction(FontAwesomeIcon.Cog, "Sample: settings", () => log.Information("sample action {N}", 4)),
        ];
        Size = WideSize;
        SizeCondition = ImGuiCond.FirstUseEver;
        SetActionCount(actions.Length);
    }

    public bool ShowSubtitle
    {
        get => Subtitle != null;
        set => Subtitle = value ? SampleSubtitle : null;
    }

    /// <summary>The window sizes itself to its content and header.</summary>
    public bool AutoSize
    {
        get => (Flags & ImGuiWindowFlags.AlwaysAutoResize) != 0;
        set => Flags = value ? Flags | ImGuiWindowFlags.AlwaysAutoResize : Flags & ~ImGuiWindowFlags.AlwaysAutoResize;
    }

    public int ActionCount => HeaderActions.Count;

    /// <summary>Bytes allocated per frame from the end of PreDraw to PostDraw: Begin, the header, the options menu, the content and End.</summary>
    public AllocationProbe DrawProbe { get; } = new();

    public override void OnOpen() => DrawProbe.Reset();

    /// <summary>Shows the first <paramref name="count"/> sample actions.</summary>
    public void SetActionCount(int count)
    {
        HeaderActions.Clear();
        for (var i = 0; i < Math.Min(count, actions.Length); i++)
            HeaderActions.Add(actions[i]);
    }

    public void MakeNarrow() => ResizeOnce(NarrowSize);

    public void MakeWide() => ResizeOnce(WideSize);

    protected override void DrawContent()
    {
        Ui.Muted("Resize the window to fold the actions into the options menu.");
        ImGui.TextUnformatted(ShowSubtitle ? "Subtitle: on" : "Subtitle: off");
        // A row that fills the width: with Auto size on, the window must hold its width, not creep wider.
        Ui.PropertyRow("Fill row");
        Ui.Slider("##hs-fill", ref fill, 0, 1);
    }

    protected override void OnPreDraw() => drawStart = GC.GetAllocatedBytesForCurrentThread();

    protected override void OnPostDraw()
    {
        DrawProbe.Record(GC.GetAllocatedBytesForCurrentThread() - drawStart);
        // Dalamud applied the size during this frame's Begin: from now on the user's size stays.
        if (!resizePending)
            return;
        resizePending = false;
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    private void ResizeOnce(Vector2 size)
    {
        Size = size;
        SizeCondition = ImGuiCond.Always;
        resizePending = true;
    }
}
