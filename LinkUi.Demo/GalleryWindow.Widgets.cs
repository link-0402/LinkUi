using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace LinkUi.Demo;

/// <summary>LinkUi's own widgets, parts 1 and 2: at rest, in each forced state, and once each in the allocation batch.</summary>
internal sealed partial class GalleryWindow
{
    private const string EmptyHint = "Mods you open show their items here. Drop a modpack on the window or pick one from the list.";
    private const string WrappedHint = "A longer hint wraps to the width of the window, so it reads as a short paragraph under the control it explains.";

    private static readonly string[] CollectionItems = ["Default", "Screenshots", "Testing"];

    private bool toggleLive;
    private int collectionIndex;
    private float tileSize = 1f;

    private static readonly string[] TabItems = ["Model", "Textures", "Physics"];

    /// <summary>Tab labels whose counts change each second, made once so the strip's text changes without allocating.</summary>
    private static readonly string[][] CountingTabs = MakeCountingTabs(10);

    private int tabLive;
    private int tabCounting;
    private float sliderLive = 0.35f;
    private float sliderAngle = 45f;
    private bool chipLive = true;
    private string searchLive = string.Empty;
    private int comboLive;
    private float splitLeft = 200f;

    private static bool batchToggle;
    private static int batchTab;
    private static float batchSlider = 0.5f;
    private static string batchSearch = string.Empty;
    private static int batchCombo;
    private static float batchSplit = 50f;

    private static string[][] MakeCountingTabs(int count)
    {
        var sets = new string[count][];
        for (var i = 0; i < count; i++)
            sets[i] = ["Mods " + i, "Items " + (i * 3), "Errors " + (count - i)];
        return sets;
    }

    private void DrawWidgetsSection()
    {
        SectionTitle("LinkUi widgets (part 1)");
        var p = Theme.Current.Palette;
        if (p == null)
        {
            ImGui.TextUnformatted("No palette: LinkUi has no host.");
            return;
        }

        Ui.SectionHeader("Buttons", "rest, then disabled");
        Ui.PrimaryButton("Edit in Blender##w-primary");
        ImGui.SameLine();
        Ui.PrimaryButton("Edit in Blender##w-primary-icon", FontAwesomeIcon.Cube);
        ImGui.SameLine();
        Ui.PrimaryButton("Edit in Blender##w-primary-off", null, false);

        Ui.DangerButton("Discard##w-danger");
        ImGui.SameLine();
        Ui.DangerButton("Discard##w-danger-off", false);

        Ui.IconButton("w-icon", FontAwesomeIcon.Redo, "Refresh");
        ImGui.SameLine();
        Ui.IconButton("w-icon-off", FontAwesomeIcon.Redo, "Refresh (disabled)", false);
        ImGui.SameLine();
        Ui.GhostIconButton("w-ghost", FontAwesomeIcon.EllipsisH, "More options");
        ImGui.SameLine();
        Ui.GhostIconButton("w-ghost-off", FontAwesomeIcon.EllipsisH, "More options (disabled)", false);

        Ui.ToggleButton("Loop##w-toggle", ref toggleLive);
        ImGui.SameLine();
        var forcedOn = true;
        Ui.ToggleButton("Loop##w-toggle-on", ref forcedOn);
        ImGui.SameLine();
        var forcedOff = false;
        Ui.ToggleButton("Loop##w-toggle-off", ref forcedOff, false);
        ImGui.SameLine();
        var disabledOn = true;
        Ui.ToggleButton("Loop##w-toggle-on-off", ref disabledOn, false);

        Ui.SectionHeader("Badges and status");
        Ui.Badge("Mod", p.Success);
        ImGui.SameLine();
        Ui.Badge("Game", p.Info);
        ImGui.SameLine();
        Ui.StatusDot("Blender", p.Success, "connected", "Blender answers on its port.");
        ImGui.SameLine();
        Ui.Spinner();
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        Ui.Muted("Refreshing resources");

        Ui.SectionHeader("Banners");
        Ui.Banner("w-banner-success", Severity.Success, "Saved into the mod");
        Ui.Banner("w-banner-warning", Severity.Warning, "Blender is not running");
        Ui.Banner("w-banner-error", Severity.Error, "The texture could not be read");
        Ui.Banner("w-banner-info", Severity.Info, "Redraw to see the change");
        Ui.Banner("w-banner-long", Severity.Info, WrappedHint);

        Ui.SectionHeader("Text", "hint, wrapped hint, muted");
        Ui.Hint("One hint line under a control.");
        Ui.HintWrapped(WrappedHint);
        Ui.Muted("Muted secondary text.");
        ImGui.Button("Hover for a LinkUi tooltip");
        Ui.Tooltip("Edit this model in Blender");

        Ui.SectionHeader("Property rows", "3 rows");
        Ui.PropertyRow("Collection");
        if (ImGui.BeginCombo("##w-collection", CollectionItems[collectionIndex]))
        {
            try
            {
                for (var i = 0; i < CollectionItems.Length; i++)
                {
                    if (ImGui.Selectable(CollectionItems[i], i == collectionIndex))
                        collectionIndex = i;
                }
            }
            finally
            {
                ImGui.EndCombo();
            }
        }
        Ui.PropertyRow("Tile size");
        ImGui.SliderFloat("##w-tile", ref tileSize, 0.5f, 2f);
        Ui.PropertyRow("Status##w-status-row");
        Ui.Muted("Ready");

        Ui.SectionHeader("Empty state");
        Ui.EmptyState(FontAwesomeIcon.FolderOpen, "No mods open", EmptyHint);
    }

    private void DrawWidgetsPart2Section()
    {
        SectionTitle("LinkUi widgets (part 2)");
        var p = Theme.Current.Palette;
        if (p == null)
        {
            ImGui.TextUnformatted("No palette: LinkUi has no host.");
            return;
        }
        var scale = Theme.Current.Metrics.Scale;

        Ui.SectionHeader("Tab strip", "live, then labels that change each second");
        Ui.TabStrip("w-tabs", TabItems, ref tabLive);
        var second = (int)ImGui.GetTime() % CountingTabs.Length;
        Ui.TabStrip("w-tabs-counting", CountingTabs[second], ref tabCounting);

        Ui.SectionHeader("Fill slider", "live, format, disabled");
        Ui.PropertyRow("Strength");
        Ui.Slider("##w-slider", ref sliderLive, 0f, 1f);
        Ui.PropertyRow("Angle");
        Ui.Slider("##w-slider-angle", ref sliderAngle, -180f, 180f, "0");
        Ui.PropertyRow("Locked");
        var locked = 0.4f;
        Ui.Slider("##w-slider-off", ref locked, 0f, 1f, "0.00", false);

        Ui.SectionHeader("Chips", "live, selected, swatches");
        if (Ui.Chip("All##w-chip", 12, chipLive))
            chipLive = !chipLive;
        ImGui.SameLine();
        Ui.Chip("Selected##w-chip-on", 4, true);
        ImGui.SameLine();
        Ui.Chip("Errors##w-chip-dot", 2, false, p.Error);
        ImGui.SameLine();
        Ui.Chip("Mixed##w-chip-pie", 7, false, p.Error, p.Success, p.Info);

        Ui.SectionHeader("Search box", "Ctrl+F focuses it");
        Ui.SearchBox("##w-search", ref searchLive, "Search mods");

        Ui.SectionHeader("Wheel combo", "Ctrl + wheel steps through it");
        Ui.PropertyRow("Collection##w-combo-row");
        Ui.Combo("##w-combo", ref comboLive, CollectionItems);

        Ui.SectionHeader("Splitter", "between two panes");
        var height = 80 * scale;
        var total = ImGui.GetContentRegionAvail().X;
        var maxLeft = Math.Max(60 * scale, total - 60 * scale - Theme.Current.Metrics.SplitterHit);
        splitLeft = Math.Clamp(splitLeft, 60 * scale, maxLeft);
        DrawPane("w-pane-left", new Vector2(splitLeft, height), "Left pane");
        ImGui.SameLine(0, 0);
        Ui.Splitter("w-splitter", ref splitLeft, 60 * scale, maxLeft, height);
        ImGui.SameLine(0, 0);
        DrawPane("w-pane-right", new Vector2(0, height), "Right pane");
    }

    private static void DrawPane(string id, Vector2 size, string text)
    {
        var open = ImGui.BeginChild(id, size, true);
        try
        {
            if (open)
                Ui.Muted(text);
        }
        finally
        {
            ImGui.EndChild();
        }
    }

    // The allocation batch: one call per widget, constant arguments, state in static fields.
    private static void BatchPrimary() => Ui.PrimaryButton("Primary##batch-primary", FontAwesomeIcon.Cube);
    private static void BatchDanger() => Ui.DangerButton("Danger##batch-danger");
    private static void BatchIcon() => Ui.IconButton("batch-icon", FontAwesomeIcon.Redo, "Icon button");
    private static void BatchGhost() => Ui.GhostIconButton("batch-ghost", FontAwesomeIcon.EllipsisH, "Ghost icon button");
    private static void BatchToggle() => Ui.ToggleButton("Toggle##batch-toggle", ref batchToggle);
    private static void BatchTooltip() => Ui.Tooltip("Tooltip for the toggle");
    private static void BatchBadge() => Ui.Badge("Badge", Theme.Current.Palette.Success);
    private static void BatchStatusDot() => Ui.StatusDot("Status", Theme.Current.Palette.Info, "dot", "Status dot");
    private static void BatchBanner() => Ui.Banner("batch-banner", Severity.Warning, "Banner");
    private static void BatchEmptyState() => Ui.EmptyState(FontAwesomeIcon.Inbox, "Empty state", "Empty state hint");
    private static void BatchSpinner() => Ui.Spinner();
    private static void BatchHint() => Ui.Hint("Hint");
    private static void BatchHintWrapped() => Ui.HintWrapped("Wrapped hint");
    private static void BatchMuted() => Ui.Muted("Muted");
    private static void BatchSectionHeader() => Ui.SectionHeader("Section header", "detail");

    /// <summary>The row's label, then the item it labels.</summary>
    private static void BatchPropertyRow()
    {
        Ui.PropertyRow("Property##batch-property");
        Ui.Muted("value");
    }

    private static void BatchTabStrip() => Ui.TabStrip("batch-tabs", TabItems, ref batchTab);
    private static void BatchSlider() => Ui.Slider("##batch-slider", ref batchSlider, 0f, 1f);
    private static void BatchChip() => Ui.Chip("Chip##batch-chip", 3, false, Theme.Current.Palette.Info);
    private static void BatchSearchBox() => Ui.SearchBox("##batch-search", ref batchSearch, "Search");
    private static void BatchCombo() => Ui.Combo("##batch-combo", ref batchCombo, CollectionItems);
    private static void BatchSplitter() => Ui.Splitter("batch-splitter", ref batchSplit, 0f, 100f, 20f, false);
}
