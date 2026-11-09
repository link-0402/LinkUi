using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace LinkUi;

public static partial class Ui
{
    /// <summary>
    /// A filter chip: the label and a muted count on a control pill, filled with the selection colour when
    /// selected. Swatches draw a colour dot before the label, split into equal wedges when there are several,
    /// so the chip doubles as a legend for the colours its rows use. True when clicked.
    /// </summary>
    public static bool Chip(string label, int count, bool selected, params ReadOnlySpan<Vector4> swatches)
    {
        var p = Theme.Current.Palette;
        var m = Theme.Current.Metrics;
        var visible = Labels.Visible(label);
        var number = Numbers.Of(count);
        var radius = 3.5f * m.Scale;
        var gap = 5 * m.Scale;
        var dotWidth = swatches.Length > 0 ? radius * 2 + gap : 0;
        var labelWidth = ImGui.CalcTextSize(visible).X;
        var labelGap = visible.Length > 0 ? gap : 0;
        var numberWidth = ImGui.CalcTextSize(number).X;
        var width = m.PillPadX * 2 + dotWidth + labelWidth + labelGap + numberWidth;
        var clicked = Hit(label, new Vector2(width, ImGui.GetFrameHeight()), true, out var hovered, out var held);

        // The pill is shorter than the row, centred in it, so chips sit level with buttons beside them.
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var centreY = (min.Y + max.Y) / 2;
        var lineHeight = ImGui.GetTextLineHeight();
        var half = lineHeight / 2 + 2 * m.Scale;
        var pillMin = new Vector2(min.X, centreY - half);
        var pillMax = new Vector2(max.X, centreY + half);
        var fill = selected
            ? hovered ? p.Selection with { W = p.Selection.W * 0.85f } : p.Selection
            : held && hovered ? p.ControlActive : hovered ? p.ControlHovered : p.Control;
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(pillMin, pillMax, ImGui.GetColorU32(fill), m.FrameRounding);
        drawList.AddRect(pillMin, pillMax, ImGui.GetColorU32(p.Border), m.FrameRounding, ImDrawFlags.None, 1f);

        var x = min.X + m.PillPadX;
        if (swatches.Length > 0)
        {
            var centre = new Vector2(x + radius, centreY);
            if (swatches.Length == 1)
                drawList.AddCircleFilled(centre, radius, ImGui.GetColorU32(swatches[0]));
            else
            {
                var wedge = 2 * MathF.PI / swatches.Length;
                for (var i = 0; i < swatches.Length; i++)
                {
                    var start = -MathF.PI / 2 + i * wedge;
                    drawList.PathLineTo(centre);
                    drawList.PathArcTo(centre, radius, start, start + wedge);
                    drawList.PathFillConvex(ImGui.GetColorU32(swatches[i]));
                }
            }
            x += dotWidth;
        }
        var textY = centreY - lineHeight / 2;
        if (visible.Length > 0)
        {
            drawList.AddText(new Vector2(x, textY), ImGui.GetColorU32(selected ? p.SelectionText : p.Text), visible);
            x += labelWidth + labelGap;
        }
        drawList.AddText(new Vector2(x, textY), ImGui.GetColorU32(p.Muted), number);
        return clicked;
    }
}
