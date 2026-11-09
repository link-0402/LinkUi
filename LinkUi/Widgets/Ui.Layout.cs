using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace LinkUi;

public static partial class Ui
{
    /// <summary>A section start: muted text, an optional detail in the hint colour, then a hairline to the content edge.</summary>
    public static void SectionHeader(string text, string? detail = null)
    {
        var p = Theme.Current.Palette;
        var gap = 8 * Theme.Current.Metrics.Scale;
        using (var _ = Style.Override().Colour(ImGuiCol.Text, p.Muted))
            ImGui.TextUnformatted(text);
        if (detail != null)
        {
            ImGui.SameLine(0, gap);
            using var _ = Style.Override().Colour(ImGuiCol.Text, p.Hint);
            ImGui.TextUnformatted(detail);
        }
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var start = max.X + gap;
        var end = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
        if (end <= start)
            return;
        var y = MathF.Floor((min.Y + max.Y) / 2) + 0.5f;
        ImGui.GetWindowDrawList().AddLine(new Vector2(start, y), new Vector2(end, y), ImGui.GetColorU32(p.Hairline), 1f);
    }

    /// <summary>A muted label in the property gutter; the next item fills the rest of the row.</summary>
    public static void PropertyRow(string label)
    {
        ImGui.AlignTextToFramePadding();
        using (var _ = Style.Override().Colour(ImGuiCol.Text, Theme.Current.Palette.Muted))
            ImGui.TextUnformatted(Labels.Visible(label));
        ImGui.SameLine(Theme.Current.Metrics.PropertyGutter);
        ImGui.SetNextItemWidth(-1);
    }
}
