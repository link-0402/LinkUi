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

    public int ActionCount => HeaderActions.Count;

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
    }

    protected override void OnPostDraw()
    {
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
