using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace LinkUi;

public static partial class Ui
{
    /// <summary>
    /// A row of flat segments, one per item; the active one is lighter with an accent underline. Each segment's
    /// id is its position, so the ids stay put while the text changes. True on the frame the index changed.
    /// </summary>
    public static bool TabStrip(string id, ReadOnlySpan<string> items, ref int index)
    {
        var p = Theme.Current.Palette;
        var m = Theme.Current.Metrics;
        var height = ImGui.GetFrameHeight();
        var padX = ImGui.GetStyle().FramePadding.X;
        // Every segment compares against the index from before the loop and a click applies after it,
        // so two segments never show as active in the same frame.
        var current = index;
        var picked = -1;
        ImGui.PushID(id);
        try
        {
            for (var i = 0; i < items.Length; i++)
            {
                if (i > 0)
                    ImGui.SameLine(0, 0);
                var text = Labels.Visible(items[i]);
                var textSize = ImGui.CalcTextSize(text);
                bool clicked;
                ImGui.PushID((nint)i);
                try
                {
                    clicked = ImGui.InvisibleButton("##t", new Vector2(textSize.X + padX * 2, height));
                }
                finally
                {
                    ImGui.PopID();
                }
                if (clicked)
                    picked = i;
                var active = i == current;
                var hovered = ImGui.IsItemHovered();
                var min = ImGui.GetItemRectMin();
                var max = ImGui.GetItemRectMax();
                var drawList = ImGui.GetWindowDrawList();
                drawList.AddRectFilled(min, max, ImGui.GetColorU32(active ? p.Control : hovered ? p.FrameActive : p.Panel));
                drawList.AddRect(min, max, ImGui.GetColorU32(p.Border), 0, ImDrawFlags.None, 1f);
                if (active)
                    drawList.AddRectFilled(new Vector2(min.X, max.Y - m.FocusLine), max, ImGui.GetColorU32(p.Accent));
                drawList.AddText(new Vector2(min.X + padX, (min.Y + max.Y - textSize.Y) / 2),
                    ImGui.GetColorU32(active ? p.Title : p.TabText), text);
            }
        }
        finally
        {
            ImGui.PopID();
        }
        if (picked < 0 || picked == current)
            return false;
        index = picked;
        return true;
    }
}
