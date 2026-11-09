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

        Frame(p.Frame, p.Border);
        var fraction = max > min ? Math.Clamp((value - min) / (max - min), 0, 1) : 0;
        if (fraction > 0)
        {
            var fill = !enabled ? p.FrameActive : held ? p.Accent : hovered ? p.Selection : p.AccentMuted;
            ImGui.GetWindowDrawList().AddRectFilled(rectMin, new Vector2(rectMin.X + width * fraction, rectMax.Y), ImGui.GetColorU32(fill),
                Theme.Current.Metrics.FrameRounding, fraction >= 1 ? ImDrawFlags.RoundCornersAll : ImDrawFlags.RoundCornersLeft);
        }

        var text = SliderValueText(ImGuiP.GetItemID(), value, format);
        var textSize = ImGui.CalcTextSize(text);
        var colour = !enabled ? p.Disabled : held ? p.OnAccent : p.Text;
        ImGui.GetWindowDrawList().AddText((rectMin + rectMax - textSize) / 2, ImGui.GetColorU32(colour), text);
        return changed;
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
