using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace LinkUi;

/// <summary>
/// LinkUi's own widgets. Each is an invisible button or a frame-height row with draw-list shapes in the
/// <see cref="Theme"/> colours. Labels show the text before <c>##</c>; ids are the caller's strings.
/// </summary>
public static partial class Ui
{
    /// <summary>A filled accent button for the main action of a window or row.</summary>
    public static bool PrimaryButton(string label, FontAwesomeIcon? icon = null, bool enabled = true, float width = 0)
    {
        var p = Theme.Current.Palette;
        var visible = Labels.Visible(label);
        var glyph = icon is { } i ? Glyphs.Of(i) : null;
        var size = ButtonSize(visible, glyph, width);
        var shift = (size.X - ButtonSize(visible, glyph).X) / 2;
        var clicked = Hit(label, size, enabled, out var hovered, out var held);
        if (!enabled)
        {
            Frame(p.AccentMuted, p.AccentMutedBorder);
            Content(visible, glyph, p.Accent with { W = p.Accent.W * 0.45f }, shift);
            return false;
        }
        Frame(held && hovered ? p.AccentPressed : hovered ? p.AccentHovered : p.Accent, p.AccentBorder);
        Content(visible, glyph, p.OnAccent, shift);
        return clicked;
    }

    /// <summary>A red-tinted button for actions that throw work away.</summary>
    public static bool DangerButton(string label, bool enabled = true)
    {
        var p = Theme.Current.Palette;
        var visible = Labels.Visible(label);
        var clicked = Hit(label, ButtonSize(visible, null), enabled, out var hovered, out var held);
        if (!enabled)
        {
            Frame(DisabledFill(p), p.Border);
            Content(visible, null, p.Disabled);
            return false;
        }
        Frame(held && hovered ? p.DangerActive : hovered ? p.DangerHovered : p.Danger, p.DangerBorder);
        Content(visible, null, p.DangerText);
        return clicked;
    }

    /// <summary>A square button, one frame height on each side, with a glyph on a control fill.</summary>
    public static bool IconButton(string id, FontAwesomeIcon icon, string? tooltip = null, bool enabled = true)
    {
        var p = Theme.Current.Palette;
        var clicked = Hit(id, new Vector2(ImGui.GetFrameHeight()), enabled, out var hovered, out var held);
        if (enabled)
            Frame(held && hovered ? p.ControlActive : hovered ? p.ControlHovered : p.Control, null);
        CentredGlyph(Glyphs.Of(icon), enabled ? p.Text : p.Disabled);
        if (tooltip != null)
            Tooltip(tooltip);
        return clicked;
    }

    /// <summary>The <see cref="IconButton"/> look with the glyph, then the label. A width of 0 sizes it to the content.</summary>
    public static bool IconTextButton(FontAwesomeIcon icon, string label, string? tooltip = null, float width = 0, bool enabled = true)
    {
        var p = Theme.Current.Palette;
        var visible = Labels.Visible(label);
        var glyph = Glyphs.Of(icon);
        var size = ButtonSize(visible, glyph, width);
        var shift = (size.X - ButtonSize(visible, glyph).X) / 2;
        var clicked = Hit(label, size, enabled, out var hovered, out var held);
        if (enabled)
            Frame(held && hovered ? p.ControlActive : hovered ? p.ControlHovered : p.Control, null);
        Content(visible, glyph, enabled ? p.Text : p.Disabled, shift);
        if (tooltip != null)
            Tooltip(tooltip);
        return clicked;
    }

    /// <summary>A glyph button with no fill until hovered, for toolbars and headers. A default size is one frame height square.</summary>
    public static bool GhostIconButton(string id, FontAwesomeIcon icon, string? tooltip = null, bool enabled = true, Vector2 size = default)
    {
        var p = Theme.Current.Palette;
        var clicked = Hit(id, size == default ? new Vector2(ImGui.GetFrameHeight()) : size, enabled, out var hovered, out var held);
        if (hovered)
            Frame(held ? p.ControlActive : p.Control, null);
        CentredGlyph(Glyphs.Of(icon), !enabled ? p.Disabled : hovered ? Vector4.One : p.Glyph);
        if (tooltip != null)
            Tooltip(tooltip);
        return clicked;
    }

    /// <summary>A button that stays pressed while on. True on the frame it flipped.</summary>
    public static bool ToggleButton(string label, ref bool on, bool enabled = true)
    {
        var p = Theme.Current.Palette;
        var visible = Labels.Visible(label);
        var clicked = Hit(label, ButtonSize(visible, null), enabled, out var hovered, out var held);
        if (!enabled)
        {
            if (on)
            {
                Frame(p.Selection with { W = p.Selection.W * 0.55f }, p.AccentMutedBorder);
                Content(visible, null, p.SelectionText with { W = p.SelectionText.W * 0.45f });
            }
            else
            {
                Frame(DisabledFill(p), p.Border);
                Content(visible, null, p.Disabled);
            }
            return false;
        }
        if (clicked)
            on = !on;
        if (on)
        {
            Frame(p.Selection, p.AccentBorder);
            Content(visible, null, p.SelectionText);
        }
        else
        {
            Frame(held && hovered ? p.ControlActive : hovered ? p.ControlHovered : p.Control, p.Border);
            Content(visible, null, p.Text);
        }
        return clicked;
    }

    /// <summary>
    /// The invisible button under a widget. Only the button sits inside <c>BeginDisabled</c>, so the shapes
    /// drawn after it keep their own disabled colours instead of being dimmed again.
    /// </summary>
    private static bool Hit(string id, Vector2 size, bool enabled, out bool hovered, out bool held)
    {
        bool clicked;
        ImGui.BeginDisabled(!enabled);
        try
        {
            clicked = ImGui.InvisibleButton(id, size);
        }
        finally
        {
            ImGui.EndDisabled();
        }
        hovered = enabled && ImGui.IsItemHovered();
        held = enabled && ImGui.IsItemActive();
        return clicked && enabled;
    }

    /// <summary>A disabled control button: the control grey at 55%, about the sheet's #303030 on the window.</summary>
    private static Vector4 DisabledFill(Palette p) => p.Control with { W = p.Control.W * 0.55f };

    /// <summary>Frame height tall; the label (and glyph) plus the frame padding wide.</summary>
    private static Vector2 ButtonSize(string visible, string? glyph, float minWidth = 0)
    {
        var style = ImGui.GetStyle();
        var width = ImGui.CalcTextSize(visible).X + style.FramePadding.X * 2;
        if (glyph != null)
        {
            width += GlyphWidth(glyph);
            if (visible.Length > 0)
                width += style.ItemInnerSpacing.X;
        }
        return new Vector2(Math.Max(width, minWidth), ImGui.GetFrameHeight());
    }

    /// <summary>The last item's rectangle filled and optionally outlined at 1 px, with the frame rounding.</summary>
    private static void Frame(Vector4 fill, Vector4? outline)
    {
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var rounding = Theme.Current.Metrics.FrameRounding;
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(min, max, ImGui.GetColorU32(fill), rounding);
        if (outline is { } line)
            drawList.AddRect(min, max, ImGui.GetColorU32(line), rounding, ImDrawFlags.None, 1f);
    }

    /// <summary>The glyph and label inside the last item, after the frame padding, centred vertically.</summary>
    private static void Content(string visible, string? glyph, Vector4 colour, float shift = 0)
    {
        var min = ImGui.GetItemRectMin();
        var centreY = (min.Y + ImGui.GetItemRectMax().Y) / 2;
        var style = ImGui.GetStyle();
        var x = min.X + style.FramePadding.X + shift;
        var col = ImGui.GetColorU32(colour);
        var drawList = ImGui.GetWindowDrawList();
        if (glyph != null)
        {
            ImGui.PushFont(Fonts.Icon);
            try
            {
                var size = ImGui.CalcTextSize(glyph);
                drawList.AddText(new Vector2(x, centreY - size.Y / 2), col, glyph);
                x += size.X;
            }
            finally
            {
                ImGui.PopFont();
            }
            if (visible.Length > 0)
                x += style.ItemInnerSpacing.X;
        }
        if (visible.Length > 0)
            drawList.AddText(new Vector2(x, centreY - ImGui.GetTextLineHeight() / 2), col, visible);
    }

    /// <summary>A glyph centred in the last item.</summary>
    private static void CentredGlyph(string glyph, Vector4 colour)
    {
        var centre = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) / 2;
        ImGui.PushFont(Fonts.Icon);
        try
        {
            var size = ImGui.CalcTextSize(glyph);
            ImGui.GetWindowDrawList().AddText(centre - size / 2, ImGui.GetColorU32(colour), glyph);
        }
        finally
        {
            ImGui.PopFont();
        }
    }

    /// <summary>A glyph's width in the icon font.</summary>
    private static float GlyphWidth(string glyph)
    {
        ImGui.PushFont(Fonts.Icon);
        try
        {
            return ImGui.CalcTextSize(glyph).X;
        }
        finally
        {
            ImGui.PopFont();
        }
    }
}
