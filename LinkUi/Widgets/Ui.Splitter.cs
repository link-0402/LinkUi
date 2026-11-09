using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace LinkUi;

public static partial class Ui
{
    /// <summary>
    /// A drag handle between two panes that resizes <paramref name="size"/> within [min, max] while dragged.
    /// Vertical is a line between a left and a right pane, dragged sideways; otherwise a line between a top and
    /// a bottom pane. <paramref name="length"/> is the line's length. With <paramref name="invert"/>, dragging
    /// right (or down) shrinks <paramref name="size"/>, for a pane on the far side. True on the frame the drag ends.
    /// </summary>
    public static bool Splitter(string id, ref float size, float min, float max, float length, bool vertical = true, bool invert = false)
    {
        var p = Theme.Current.Palette;
        var m = Theme.Current.Metrics;
        length = Math.Max(1, length);
        ImGui.InvisibleButton(id, vertical ? new Vector2(m.SplitterHit, length) : new Vector2(length, m.SplitterHit));
        var hovered = ImGui.IsItemHovered();
        var active = ImGui.IsItemActive();
        if (hovered || active)
            ImGui.SetMouseCursor(vertical ? ImGuiMouseCursor.ResizeEw : ImGuiMouseCursor.ResizeNs);
        if (active)
        {
            var delta = vertical ? ImGui.GetIO().MouseDelta.X : ImGui.GetIO().MouseDelta.Y;
            size = Math.Clamp(size + (invert ? -delta : delta), min, Math.Max(min, max));
        }

        var rectMin = ImGui.GetItemRectMin();
        var rectMax = ImGui.GetItemRectMax();
        var half = m.SplitterLine / 2;
        var centre = (rectMin + rectMax) / 2;
        var colour = ImGui.GetColorU32(active ? p.Accent : hovered ? p.Muted : p.Hairline);
        if (vertical)
            ImGui.GetWindowDrawList().AddRectFilled(new Vector2(centre.X - half, rectMin.Y), new Vector2(centre.X + half, rectMax.Y), colour);
        else
            ImGui.GetWindowDrawList().AddRectFilled(new Vector2(rectMin.X, centre.Y - half), new Vector2(rectMax.X, centre.Y + half), colour);
        return ImGui.IsItemDeactivated();
    }
}
