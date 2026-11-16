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
        // The rule ends where this line's content region ends, so it stays inside a table cell, child or pane.
        var end = ImGui.GetCursorScreenPos().X + ImGui.GetContentRegionAvail().X;
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
        if (end <= start)
            return;
        var y = MathF.Floor((min.Y + max.Y) / 2) + 0.5f;
        ImGui.GetWindowDrawList().AddLine(new Vector2(start, y), new Vector2(end, y), ImGui.GetColorU32(p.Hairline), 1f);
    }

    /// <summary>A muted label in the property gutter; the next item fills the rest of the row.</summary>
    public static void PropertyRow(string label) => RowLabel(label);

    /// <summary>A <see cref="PropertyRow"/> label with an optional tooltip on it; the next item fills the rest of the row.</summary>
    public static void Label(string label, string? help = null)
    {
        RowLabel(label);
        if (help != null)
            Tooltip(help);
    }

    /// <summary>A label and a read-only value beside it. Hovering shows the help and "Click to copy."; a click copies the value.</summary>
    public static void LabeledValue(string label, string value, string? help = null)
    {
        RowLabel(label);
        ImGui.TextUnformatted(value);
        Tooltip(Labels.CopyTip(help));
        if (ImGui.IsItemClicked())
            ImGui.SetClipboardText(value);
    }

    /// <summary>A label and a text field beside it. True when the text changed.</summary>
    public static bool LabeledInput(string label, string id, ref string value, int maxLength, string hint = "", string? help = null, float width = -1)
    {
        Label(label, help);
        ImGui.SetNextItemWidth(width);
        return ImGui.InputTextWithHint(id, hint, ref value, maxLength);
    }

    /// <summary>A label and a <see cref="Combo"/> beside it. True when the index changed.</summary>
    public static bool LabeledCombo(string label, string id, ref int index, string[] items, string? help = null, float width = -1)
    {
        Label(label, help);
        ImGui.SetNextItemWidth(width);
        return Combo(id, ref index, items);
    }

    private static void RowLabel(string label)
    {
        // The gutter counts from the row's own start, so an indented row keeps its value column with its label.
        var start = ImGui.GetCursorPosX();
        ImGui.AlignTextToFramePadding();
        using (var _ = Style.Override().Colour(ImGuiCol.Text, Theme.Current.Palette.Muted))
            ImGui.TextUnformatted(Labels.Visible(label));
        // SetCursorPosX, not SameLine(offset): SameLine adds the group and column offsets on top, which
        // start already holds, so a row in a group or table cell would land twice as far in.
        ImGui.SameLine();
        ImGui.SetCursorPosX(start + Theme.Current.Metrics.PropertyGutter);
        ImGui.SetNextItemWidth(-1);
    }
}
