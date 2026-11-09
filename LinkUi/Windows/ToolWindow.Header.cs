using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace LinkUi;

/// <summary>The header: painting, the fold and clip layout, the buttons and the click-through pill.</summary>
public abstract partial class ToolWindow
{
    private const string PillText = "Click-through on, click to turn off";

    /// <summary>Unfocused accents keep this much of their alpha.</summary>
    private const float DimAlpha = 0.55f;

    /// <summary>Built once, so measuring a width makes no closure.</summary>
    private static readonly Func<string, float> MeasureText = static text => ImGui.CalcTextSize(text).X;

    // Mutable structs: never readonly, or every Get measures again.
    private WidthSlot titleWidth;
    private WidthSlot subtitleWidth;
    private WidthSlot pillTextWidth;

    /// <summary>
    /// Paints the header across the top of the window and draws its buttons. Returns the plugin action
    /// clicked this frame (run by the caller once the header is done); <paramref name="optionsAnchor"/> is
    /// the top-right corner the options menu opens at.
    /// </summary>
    private HeaderAction? DrawHeader(Vector2 windowPos, float headerHeight, bool focused, int actionCount,
        out HeaderResult layout, out Vector2 optionsAnchor)
    {
        var p = Theme.Current.Palette;
        var m = Theme.Current.Metrics;
        var width = ImGui.GetWindowWidth();
        var headerMax = windowPos + new Vector2(width, headerHeight);
        var drawList = ImGui.GetWindowDrawList();
        PaintHeader(drawList, windowPos, headerMax, focused);

        var buttonSize = ImGui.GetFrameHeight();
        var fontSize = ImGui.GetFontSize();
        var iconGlyph = host.Options.HeaderIcon is { } icon ? Glyphs.Of(icon) : null;
        var iconSize = iconGlyph != null ? GlyphSize(iconGlyph) : Vector2.Zero;
        var subtitle = Subtitle;
        var titleFull = titleWidth.Get(title, fontSize, MeasureText);
        var subtitleFull = string.IsNullOrEmpty(subtitle) ? 0 : subtitleWidth.Get(subtitle, fontSize, MeasureText);
        var clickThrough = IsClickthrough;
        var pointerGlyph = Glyphs.Of(FontAwesomeIcon.MousePointer);
        var pointerSize = Vector2.Zero;
        var pillWidth = 0f;
        if (clickThrough)
        {
            pointerSize = GlyphSize(pointerGlyph);
            pillWidth = m.PillPadX * 2 + pointerSize.X + m.HeaderGap + pillTextWidth.Get(PillText, fontSize, MeasureText);
        }
        layout = HeaderLayout.Compute(new HeaderInput(width, buttonSize, iconSize.X, iconGlyph != null, titleFull, subtitleFull,
            actionCount, AllowPinning, ShowCloseButton, clickThrough, pillWidth, m));

        // Left to right: icon, title, subtitle. Text only, so the empty header still moves the window.
        var centreY = windowPos.Y + headerHeight / 2;
        var textY = centreY - fontSize / 2;
        var x = windowPos.X + m.HeaderPadLeft;
        if (iconGlyph != null)
        {
            DrawGlyph(drawList, iconGlyph, new Vector2(x, centreY - iconSize.Y / 2), focused ? p.Accent : Dim(p.Accent));
            x += iconSize.X + m.HeaderGap;
        }
        ClippedText(drawList, title, new Vector2(x, textY), layout.TitleShown, titleFull, focused ? p.Title : p.Muted);
        if (layout.SubtitleShown > 0)
            ClippedText(drawList, subtitle!, new Vector2(x + layout.TitleShown + m.HeaderGap, textY), layout.SubtitleShown, subtitleFull, p.Muted);

        // Right to left: close, options, pin, then the plugin actions behind a separator.
        var top = windowPos.Y + (headerHeight - buttonSize) / 2;
        var right = headerMax.X - m.HeaderPadRight;
        optionsAnchor = new Vector2(right, headerMax.Y);
        if (layout.ShowPill)
        {
            DrawPill(drawList, new Vector2(right - layout.PillShown, top), new Vector2(right, top + buttonSize),
                pointerGlyph, pointerSize, centreY, textY);
            return null;
        }
        if (!layout.ShowButtons)
            return null;

        if (ShowCloseButton)
        {
            right -= buttonSize;
            if (HeaderButton("##lu-close", Glyphs.Of(FontAwesomeIcon.Times), "Close", new Vector2(right, top), buttonSize, false, focused))
                IsOpen = false;
            right -= m.HeaderButtonGap;
        }
        optionsAnchor = new Vector2(right, headerMax.Y);
        right -= buttonSize;
        if (HeaderButton("##lu-opt", Glyphs.Of(FontAwesomeIcon.Bars), "Window options", new Vector2(right, top), buttonSize, false, focused))
            ImGui.OpenPopup(OptionsPopup);
        if (AllowPinning)
        {
            right -= m.HeaderButtonGap + buttonSize;
            var pinned = IsPinned;
            if (HeaderButton("##lu-pin", Glyphs.Of(FontAwesomeIcon.Thumbtack), pinned ? "Unpin window" : "Pin window",
                    new Vector2(right, top), buttonSize, pinned, focused))
                IsPinned = !pinned;
        }
        if (!layout.ActionsInHeader)
            return null;

        right -= m.HeaderButtonGap + m.SeparatorMargin + 1;
        var lineHalf = ImGui.GetTextLineHeight() / 2;
        drawList.AddRectFilled(new Vector2(right, centreY - lineHalf), new Vector2(right + 1, centreY + lineHalf), ImGui.GetColorU32(p.Hairline));
        right -= m.SeparatorMargin;
        var actionX = right - actionCount * buttonSize - (actionCount - 1) * m.HeaderButtonGap;
        HeaderAction? clicked = null;
        for (var i = 0; i < actionCount; i++)
        {
            var action = HeaderActions[i];
            var lit = action.Lit?.Invoke() ?? false;
            ImGui.PushID(i);
            try
            {
                if (HeaderButton("##lu-a", Glyphs.Of(action.Icon), action.Tooltip, new Vector2(actionX, top), buttonSize, lit, focused))
                    clicked = action;
            }
            finally
            {
                ImGui.PopID();
            }
            actionX += buttonSize + m.HeaderButtonGap;
        }
        return clicked;
    }

    /// <summary>The header fill with the window's top rounding, the line under it and, when focused, the accent strip.</summary>
    private static void PaintHeader(ImDrawListPtr drawList, Vector2 min, Vector2 max, bool focused)
    {
        var p = Theme.Current.Palette;
        var m = Theme.Current.Metrics;
        drawList.AddRectFilled(min, max, ImGui.GetColorU32(p.Header), m.WindowRounding, ImDrawFlags.RoundCornersTop);
        drawList.AddRectFilled(new Vector2(min.X, max.Y - 1), max, ImGui.GetColorU32(p.WindowBorder));
        if (!focused)
            return;
        // The window's clip rect leaves out the border row; the strip shows FocusLine high under it, and
        // drawing the whole rounded header through a clip keeps the strip inside the rounded corners.
        drawList.PushClipRect(min, new Vector2(max.X, min.Y + m.BorderSize + m.FocusLine), true);
        try
        {
            drawList.AddRectFilled(min, max, ImGui.GetColorU32(p.Accent), m.WindowRounding, ImDrawFlags.RoundCornersTop);
        }
        finally
        {
            drawList.PopClipRect();
        }
    }

    /// <summary>
    /// Text clipped to <paramref name="shown"/> pixels. When that cuts it short, the end fades into the
    /// header colour instead of an ellipsis.
    /// </summary>
    private static void ClippedText(ImDrawListPtr drawList, string text, Vector2 pos, float shown, float full, Vector4 colour)
    {
        if (shown <= 0)
            return;
        var p = Theme.Current.Palette;
        var max = new Vector2(pos.X + shown, pos.Y + ImGui.GetFontSize());
        drawList.PushClipRect(pos, max, true);
        try
        {
            drawList.AddText(pos, ImGui.GetColorU32(colour), text);
            if (shown >= full)
                return;
            var clear = ImGui.GetColorU32(p.Header with { W = 0 });
            var solid = ImGui.GetColorU32(p.Header);
            var start = max.X - MathF.Min(Theme.Current.Metrics.TitleFade, shown);
            drawList.AddRectFilledMultiColor(new Vector2(start, pos.Y), max, clear, solid, solid, clear);
        }
        finally
        {
            drawList.PopClipRect();
        }
    }

    /// <summary>
    /// A square glyph button: no fill until hovered, the glyph white on hover and in the accent when lit,
    /// dimmed when the window isn't focused.
    /// </summary>
    private static bool HeaderButton(string id, string glyph, string tooltip, Vector2 pos, float size, bool lit, bool focused)
    {
        var p = Theme.Current.Palette;
        ImGui.SetCursorScreenPos(pos);
        var clicked = ImGui.InvisibleButton(id, new Vector2(size));
        var hovered = ImGui.IsItemHovered();
        var held = ImGui.IsItemActive();
        var max = pos + new Vector2(size);
        var drawList = ImGui.GetWindowDrawList();
        if (hovered)
            drawList.AddRectFilled(pos, max, ImGui.GetColorU32(held ? p.ControlActive : p.Control), Theme.Current.Metrics.FrameRounding);
        var colour = lit ? (focused ? p.Accent : Dim(p.Accent)) : hovered ? Vector4.One : focused ? p.Glyph : p.Hint;
        CentredGlyph(drawList, glyph, pos, max, colour);
        Ui.Tooltip(tooltip);
        return clicked;
    }

    /// <summary>
    /// The click-through pill. The window takes no input, so the pill is hit-tested by hand and the button
    /// read from Win32; a press turns click-through off.
    /// </summary>
    private void DrawPill(ImDrawListPtr drawList, Vector2 min, Vector2 max, string pointerGlyph, Vector2 pointerSize, float centreY, float textY)
    {
        var p = Theme.Current.Palette;
        var m = Theme.Current.Metrics;
        var pad = ImGui.GetStyle().TouchExtraPadding;
        var mouse = ImGui.GetMousePos();
        var hovered = mouse.X >= min.X - pad.X && mouse.X < max.X + pad.X && mouse.Y >= min.Y - pad.Y && mouse.Y < max.Y + pad.Y;
        if (hovered)
        {
            // Outside the game window the viewport ignores the mouse too: let it through while over the pill.
            var viewport = ImGui.GetWindowViewport();
            if (viewport.ID != ImGui.GetMainViewport().ID)
                viewport.Flags &= ~ImGuiViewportFlags.NoInputs;
            if (Native.LeftButtonDown())
                IsClickthrough = false;
        }
        if (max.X <= min.X)
            return;
        var colour = p.SelectionText;
        drawList.AddRectFilled(min, max, ImGui.GetColorU32(p.Selection), m.FrameRounding);
        drawList.PushClipRect(min, max, true);
        try
        {
            var x = min.X + m.PillPadX;
            DrawGlyph(drawList, pointerGlyph, new Vector2(x, centreY - pointerSize.Y / 2), colour);
            drawList.AddText(new Vector2(x + pointerSize.X + m.HeaderGap, textY), ImGui.GetColorU32(colour), PillText);
        }
        finally
        {
            drawList.PopClipRect();
        }
    }

    private static Vector4 Dim(Vector4 colour) => colour with { W = colour.W * DimAlpha };

    /// <summary>A glyph's size in the icon font.</summary>
    private static Vector2 GlyphSize(string glyph)
    {
        ImGui.PushFont(Fonts.Icon);
        try
        {
            return ImGui.CalcTextSize(glyph);
        }
        finally
        {
            ImGui.PopFont();
        }
    }

    private static void DrawGlyph(ImDrawListPtr drawList, string glyph, Vector2 pos, Vector4 colour)
    {
        ImGui.PushFont(Fonts.Icon);
        try
        {
            drawList.AddText(pos, ImGui.GetColorU32(colour), glyph);
        }
        finally
        {
            ImGui.PopFont();
        }
    }

    private static void CentredGlyph(ImDrawListPtr drawList, string glyph, Vector2 min, Vector2 max, Vector4 colour)
    {
        ImGui.PushFont(Fonts.Icon);
        try
        {
            var size = ImGui.CalcTextSize(glyph);
            drawList.AddText((min + max - size) / 2, ImGui.GetColorU32(colour), glyph);
        }
        finally
        {
            ImGui.PopFont();
        }
    }
}
