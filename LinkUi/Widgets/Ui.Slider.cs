using System.Globalization;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace LinkUi;

public static partial class Ui
{
    private const int MaxSliderTexts = 1024;

    /// <summary>Each slider's value text by ImGui id, re-made only when its value or format changes.</summary>
    private static readonly Dictionary<uint, SliderText> SliderTexts = new();

    /// <summary>
    /// A track filled from the left up to the value, with the value centred on it. Dragging anywhere on the
    /// track sets the value; there's no typed input. The item width (<c>SetNextItemWidth</c>) sets its length.
    /// <paramref name="format"/> is a C# numeric format, applied with the invariant culture.
    /// True on each frame the value changed.
    /// </summary>
    public static bool Slider(string id, ref float value, float min, float max, string format = "0.00", bool enabled = true)
    {
        var p = Theme.Current.Palette;
        var width = Math.Max(1, ImGui.CalcItemWidth());
        Hit(id, new Vector2(width, ImGui.GetFrameHeight()), enabled, out var hovered, out var held);
        var rectMin = ImGui.GetItemRectMin();
        var rectMax = ImGui.GetItemRectMax();
        var changed = false;
        if (held && max > min)
        {
            var t = Math.Clamp((ImGui.GetIO().MousePos.X - rectMin.X) / width, 0, 1);
            var next = min + t * (max - min);
            if (next != value)
            {
                value = next;
                changed = true;
            }
        }

        // Track, then the fill, then the outline on top, so the fill doesn't hide the outline's left edge.
        var drawList = ImGui.GetWindowDrawList();
        var rounding = Theme.Current.Metrics.FrameRounding;
        Frame(p.Frame, null);
        var fraction = max > min ? Math.Clamp((value - min) / (max - min), 0, 1) : 0;
        var fillEnd = rectMin.X + width * fraction;
        if (fraction > 0)
        {
            var fill = !enabled ? p.FrameActive : held ? p.Accent : hovered ? p.Selection : p.AccentMuted;
            drawList.AddRectFilled(rectMin, new Vector2(fillEnd, rectMax.Y), ImGui.GetColorU32(fill),
                rounding, fraction >= 1 ? ImDrawFlags.RoundCornersAll : ImDrawFlags.RoundCornersLeft);
        }
        drawList.AddRect(rectMin, rectMax, ImGui.GetColorU32(p.Border), rounding, ImDrawFlags.None, 1f);

        var text = SliderValueText(ImGuiP.GetItemID(), value, format);
        var textPos = (rectMin + rectMax - ImGui.CalcTextSize(text)) / 2;
        if (!held)
        {
            drawList.AddText(textPos, ImGui.GetColorU32(enabled ? p.Text : p.Disabled), text);
            return changed;
        }
        // While dragging the fill is the accent: on-accent text over the fill, normal text over the rest.
        ClippedText(rectMin, new Vector2(fillEnd, rectMax.Y), textPos, p.OnAccent, text);
        ClippedText(new Vector2(fillEnd, rectMin.Y), rectMax, textPos, p.Text, text);
        return changed;
    }

    private static void ClippedText(Vector2 clipMin, Vector2 clipMax, Vector2 position, Vector4 colour, string text)
    {
        if (clipMax.X <= clipMin.X)
            return;
        var drawList = ImGui.GetWindowDrawList();
        drawList.PushClipRect(clipMin, clipMax, true);
        try
        {
            drawList.AddText(position, ImGui.GetColorU32(colour), text);
        }
        finally
        {
            drawList.PopClipRect();
        }
    }

    private static string SliderValueText(uint id, float value, string format)
    {
        if (SliderTexts.TryGetValue(id, out var entry))
        {
            // Equals, not ==, so a NaN value matches itself and isn't re-formatted every frame.
            if (entry.Value.Equals(value) && string.Equals(entry.Format, format, StringComparison.Ordinal))
                return entry.Text;
        }
        else
        {
            if (SliderTexts.Count >= MaxSliderTexts)
                SliderTexts.Clear();
            SliderTexts[id] = entry = new SliderText();
        }
        entry.Value = value;
        entry.Format = format;
        entry.Text = value.ToString(format, CultureInfo.InvariantCulture);
        return entry.Text;
    }

    private sealed class SliderText
    {
        public float Value;
        public string Format = string.Empty;
        public string Text = string.Empty;
    }
}
