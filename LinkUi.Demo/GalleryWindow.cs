using System.Numerics;
using System.Reflection;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Plugin.Services;

namespace LinkUi.Demo;

/// <summary>
/// Every LinkUi look in one window: the style self-test, the allocation probe, the palette tokens,
/// the fonts, the header sample's controls and the built-in ImGui widgets under the LinkUi style.
/// </summary>
internal sealed partial class GalleryWindow : ToolWindow
{
    /// <summary>One call per custom widget; the allocation probe times this batch every frame.</summary>
    internal static readonly Action[] Batch =
    [
        BatchPrimary, BatchDanger, BatchIcon, BatchGhost, BatchToggle, BatchTooltip, BatchBadge, BatchStatusDot,
        BatchBanner, BatchEmptyState, BatchSpinner, BatchHint, BatchHintWrapped, BatchMuted, BatchSectionHeader, BatchPropertyRow,
        BatchTabStrip, BatchSlider, BatchChip, BatchSearchBox, BatchCombo, BatchSplitter,
        BatchLabel, BatchLabeledValue, BatchLabeledInput, BatchLabeledCombo,
        BatchIconTextButton, BatchIconGlyph, BatchHelpTip,
        BatchDisabled, BatchAlignRight, BatchHoverDelay, BatchSizes,
    ];

    /// <summary>The bare-button baseline: as many buttons as <see cref="Batch"/> has calls. Declared after it.</summary>
    private static readonly string[] BaselineLabels = MakeBaselineLabels(Batch.Length);

    private const int LogEvery = 300;

    private readonly IPluginLog log;
    private readonly HeaderSampleWindow headerSample;
    private readonly AllocationProbe widgetsProbe = new();
    private readonly AllocationProbe baselineProbe = new();

    private string selfTestLine = "Style table: not run";
    private string? selfTestProblem;
    private bool selfTestPassed;

    private long? shownWidgets;
    private long? shownBaseline;
    private string widgetsText = "Widgets: measuring";
    private string baselineText = "Bare buttons: measuring";
    private long? shownHeader;
    private bool headerShownOpen;
    private string headerText = HeaderClosed;
    private int framesSinceLog;

    private const string HeaderClosed = "Header sample window: open it to measure";

    private Palette? tokensPalette;
    private Token[] tokens = [];

    public GalleryWindow(IPluginLog log, HeaderSampleWindow headerSample)
        : base("LinkUi Gallery###linkui-gallery")
    {
        this.log = log;
        this.headerSample = headerSample;
        Size = new Vector2(720, 640);
        SizeCondition = ImGuiCond.FirstUseEver;
        HeaderActions.Add(new HeaderAction(FontAwesomeIcon.Heart, "Sample: heart", () => log.Information("action {N}", 1)));
        HeaderActions.Add(new HeaderAction(FontAwesomeIcon.Comments, "Sample: comments", () => log.Information("action {N}", 2)));
        HeaderActions.Add(new HeaderAction(FontAwesomeIcon.BookOpen, "Sample: book", () => log.Information("action {N}", 3)));
        HeaderActions.Add(new HeaderAction(FontAwesomeIcon.Cog, "Sample: settings", () => log.Information("action {N}", 4)));
    }

    public override void OnOpen()
    {
        widgetsProbe.Reset();
        baselineProbe.Reset();
        framesSinceLog = 0;
        selfTestProblem = Style.SelfTest();
        selfTestPassed = selfTestProblem == null;
        selfTestLine = selfTestPassed ? "Style table: PASS" : "Style table: FAIL";
        if (selfTestPassed)
            log.Information("style self-test PASS");
        else
            log.Information("style self-test FAIL: {Problem:l}", selfTestProblem!);
    }

    protected override void DrawContent()
    {
        DrawSelfTest();
        DrawAllocations();
        DrawTokens();
        DrawFonts();
        DrawHeaderSampleSection();
        DrawWidgetsSection();
        DrawWidgetsPart2Section();
        DrawLayoutSection();
        DrawBuiltinsSection();
    }

    private static void SectionTitle(string title)
    {
        ImGui.Spacing();
        using (Fonts.Heading())
            ImGui.TextUnformatted(title);
        ImGui.Separator();
    }

    private void DrawSelfTest()
    {
        SectionTitle("Self-test");
        var p = Theme.Current.Palette;
        using (var _ = Style.Override().Colour(ImGuiCol.Text, p == null ? Vector4.One : selfTestPassed ? p.Success : p.Error))
            ImGui.TextUnformatted(selfTestLine);
        if (selfTestProblem != null)
            ImGui.TextWrapped(selfTestProblem);
    }

    private void DrawAllocations()
    {
        SectionTitle("Allocations");

        var start = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < Batch.Length; i++)
            Batch[i]();
        var end = GC.GetAllocatedBytesForCurrentThread();
        widgetsProbe.Record(end - start);

        start = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < BaselineLabels.Length; i++)
            ImGui.Button(BaselineLabels[i]);
        end = GC.GetAllocatedBytesForCurrentThread();
        baselineProbe.Record(end - start);

        var widgets = widgetsProbe.Max;
        if (widgets != shownWidgets)
        {
            shownWidgets = widgets;
            widgetsText = widgets is { } w ? $"Widgets: {w} bytes/frame" : "Widgets: measuring";
        }
        var baseline = baselineProbe.Max;
        if (baseline != shownBaseline)
        {
            shownBaseline = baseline;
            baselineText = baseline is { } b ? $"Bare buttons: {b} bytes/frame" : "Bare buttons: measuring";
        }
        // The header sample's whole window draw, measured by the sample itself; null while it's closed or warming up.
        var header = headerSample.IsOpen ? headerSample.DrawProbe.Max : null;
        if (header != shownHeader || (header == null && headerSample.IsOpen != headerShownOpen))
        {
            shownHeader = header;
            headerShownOpen = headerSample.IsOpen;
            headerText = header is { } h ? $"Header sample window: {h} bytes/frame" : headerShownOpen ? "Header sample window: measuring" : HeaderClosed;
        }
        ImGui.TextUnformatted(widgetsText);
        ImGui.TextUnformatted(baselineText);
        ImGui.TextUnformatted(headerText);

        if (widgets is { } wm && baseline is { } bm && ++framesSinceLog >= LogEvery)
        {
            framesSinceLog = 0;
            var widgetsValue = Batch.Length == 0 ? "none" : wm.ToString();
            var headerValue = header is { } hm ? hm.ToString() : headerSample.IsOpen ? "measuring" : "closed";
            log.Information("alloc widgets={Widgets:l} baseline={Baseline} header={Header:l} bytes/frame", widgetsValue, bm, headerValue);
        }
    }

    private void DrawTokens()
    {
        SectionTitle("Tokens");
        var palette = Theme.Current.Palette;
        if (!ReferenceEquals(palette, tokensPalette))
        {
            tokensPalette = palette;
            tokens = palette == null ? [] : ReadTokens(palette);
        }
        if (tokens.Length == 0)
        {
            ImGui.TextUnformatted("No palette: LinkUi has no host.");
            return;
        }
        if (!ImGui.BeginTable("tokens", 4, ImGuiTableFlags.SizingStretchSame))
            return;
        try
        {
            for (var i = 0; i < tokens.Length; i++)
            {
                ImGui.TableNextColumn();
                ImGui.ColorButton(tokens[i].Id, tokens[i].Value, ImGuiColorEditFlags.AlphaPreviewHalf);
                ImGui.SameLine();
                ImGui.TextUnformatted(tokens[i].Name);
            }
        }
        finally
        {
            ImGui.EndTable();
        }
    }

    private static void DrawFonts()
    {
        SectionTitle("Fonts");
        using (Fonts.Title())
            ImGui.TextUnformatted("Title: the quick brown fox jumps over the lazy dog");
        using (Fonts.Heading())
            ImGui.TextUnformatted("Heading: the quick brown fox jumps over the lazy dog");
        ImGui.TextUnformatted("Body: the quick brown fox jumps over the lazy dog");
    }

    private void DrawHeaderSampleSection()
    {
        SectionTitle("Header sample");
        var open = headerSample.IsOpen;
        if (ImGui.Checkbox("Show the header sample", ref open))
            headerSample.IsOpen = open;
        var subtitle = headerSample.ShowSubtitle;
        if (ImGui.Checkbox("Subtitle", ref subtitle))
            headerSample.ShowSubtitle = subtitle;
        var autoSize = headerSample.AutoSize;
        if (ImGui.Checkbox("Auto size", ref autoSize))
            headerSample.AutoSize = autoSize;
        var count = headerSample.ActionCount;
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Actions");
        var picked = false;
        ImGui.SameLine();
        picked |= ImGui.RadioButton("0##hs-actions", ref count, 0);
        ImGui.SameLine();
        picked |= ImGui.RadioButton("2##hs-actions", ref count, 2);
        ImGui.SameLine();
        picked |= ImGui.RadioButton("4##hs-actions", ref count, 4);
        if (picked)
            headerSample.SetActionCount(count);
        if (ImGui.Button("Make narrow"))
            headerSample.MakeNarrow();
        ImGui.SameLine();
        if (ImGui.Button("Make wide"))
            headerSample.MakeWide();
    }

    private static Token[] ReadTokens(Palette palette)
    {
        var list = new List<Token>();
        foreach (var property in typeof(Palette).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.PropertyType == typeof(Vector4))
                list.Add(new Token(property.Name, "##token-" + property.Name, (Vector4)property.GetValue(palette)!));
        }
        return list.ToArray();
    }

    private static string[] MakeBaselineLabels(int count)
    {
        var labels = new string[count];
        for (var i = 0; i < count; i++)
            labels[i] = "Button " + (i + 1) + "##baseline-" + i;
        return labels;
    }

    private readonly record struct Token(string Name, string Id, Vector4 Value);
}
