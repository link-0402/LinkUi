using System.Numerics;
using System.Reflection;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;

namespace LinkUi.Demo;

/// <summary>
/// Every LinkUi look in one window: the style self-test, the allocation probe, the palette tokens,
/// the fonts and the built-in ImGui widgets under the LinkUi style.
/// </summary>
internal sealed partial class GalleryWindow : Window
{
    /// <summary>One call per custom widget; the allocation probe times this batch every frame.</summary>
    internal static readonly Action[] Batch = [];

    /// <summary>The bare-button baseline: as many buttons as <see cref="Batch"/> has calls. Declared after it.</summary>
    private static readonly string[] BaselineLabels = MakeBaselineLabels(Batch.Length);

    private const int LogEvery = 300;

    private readonly IPluginLog log;
    private readonly AllocationProbe widgetsProbe = new();
    private readonly AllocationProbe baselineProbe = new();

    private string selfTestLine = "Style table: not run";
    private string? selfTestProblem;
    private bool selfTestPassed;

    private long? shownWidgets;
    private long? shownBaseline;
    private string widgetsText = "Widgets: measuring";
    private string baselineText = "Bare buttons: measuring";
    private int framesSinceLog;

    private Palette? tokensPalette;
    private Token[] tokens = [];

    public GalleryWindow(IPluginLog log)
        : base("LinkUi Gallery###linkui-gallery")
    {
        this.log = log;
        Size = new Vector2(720, 640);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void OnOpen()
    {
        selfTestProblem = Style.SelfTest();
        selfTestPassed = selfTestProblem == null;
        selfTestLine = selfTestPassed ? "Style table: PASS" : "Style table: FAIL";
        if (selfTestPassed)
            log.Information("[LinkUi.Demo] style self-test PASS");
        else
            log.Information("[LinkUi.Demo] style self-test FAIL: {Problem:l}", selfTestProblem!);
    }

    public override void Draw()
    {
        DrawSelfTest();
        DrawAllocations();
        DrawTokens();
        DrawFonts();
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
        ImGui.TextUnformatted(widgetsText);
        ImGui.TextUnformatted(baselineText);

        if (widgets is { } wm && baseline is { } bm && ++framesSinceLog >= LogEvery)
        {
            framesSinceLog = 0;
            var widgetsValue = Batch.Length == 0 ? "none" : wm.ToString();
            log.Information($"[LinkUi.Demo] alloc widgets={widgetsValue} baseline={bm} bytes/frame");
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
