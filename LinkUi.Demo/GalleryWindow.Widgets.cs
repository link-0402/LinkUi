using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace LinkUi.Demo;

/// <summary>LinkUi's own widgets, part 1: at rest, in each forced state, and once each in the allocation batch.</summary>
internal sealed partial class GalleryWindow
{
    private const string EmptyHint = "Mods you open show their items here. Drop a modpack on the window or pick one from the list.";
    private const string WrappedHint = "A longer hint wraps to the width of the window, so it reads as a short paragraph under the control it explains.";

    private static readonly string[] CollectionItems = ["Default", "Screenshots", "Testing"];

    private bool toggleLive;
    private int collectionIndex;
    private float tileSize = 1f;

    private static bool batchToggle;

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
}
