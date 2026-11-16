using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace LinkUi;

public static partial class Ui
{
    private const string NameSeparator = ": ";

    /// <summary>A small tinted tag, one frame height tall so it lines up with buttons in a row. True on the frame it is clicked.</summary>
    public static bool Badge(string text, Vector4 colour)
    {
        var scale = Theme.Current.Metrics.Scale;
        var padX = 6 * scale;
        var textSize = ImGui.CalcTextSize(text);
        var frame = ImGui.GetFrameHeight();
        var position = ImGui.GetCursorScreenPos();
        ImGui.Dummy(new Vector2(textSize.X + padX * 2, Math.Max(frame, textSize.Y)));
        var clicked = ImGui.IsItemClicked();
        var top = position.Y + Math.Max(0, (frame - textSize.Y) / 2);
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(new Vector2(position.X, top), new Vector2(position.X + textSize.X + padX * 2, top + textSize.Y),
            ImGui.GetColorU32(colour with { W = colour.W * 0.16f }), 2 * scale);
        drawList.AddText(new Vector2(position.X + padX, top), ImGui.GetColorU32(colour), text);
        return clicked;
    }

    /// <summary>A dot in the state colour, then "name: value" in the muted colour.</summary>
    public static void StatusDot(string name, Vector4 colour, string value, string? tooltip = null)
    {
        var p = Theme.Current.Palette;
        var scale = Theme.Current.Metrics.Scale;
        var radius = 3.5f * scale;
        var gap = 5 * scale;
        var nameWidth = ImGui.CalcTextSize(name).X;
        var separatorWidth = ImGui.CalcTextSize(NameSeparator).X;
        var valueWidth = ImGui.CalcTextSize(value).X;
        var frame = ImGui.GetFrameHeight();
        var position = ImGui.GetCursorScreenPos();
        ImGui.Dummy(new Vector2(radius * 2 + gap + nameWidth + separatorWidth + valueWidth, frame));

        var centreY = position.Y + frame / 2;
        var textY = centreY - ImGui.GetTextLineHeight() / 2;
        var muted = ImGui.GetColorU32(p.Muted);
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddCircleFilled(new Vector2(position.X + radius, centreY), radius, ImGui.GetColorU32(colour));
        var x = position.X + radius * 2 + gap;
        drawList.AddText(new Vector2(x, textY), muted, name);
        x += nameWidth;
        drawList.AddText(new Vector2(x, textY), muted, NameSeparator);
        drawList.AddText(new Vector2(x + separatorWidth, textY), muted, value);
        if (tooltip != null)
            Tooltip(tooltip);
    }

    /// <summary>A bordered, tinted box with the severity's glyph and the text wrapped to the available width.</summary>
    public static void Banner(string id, Severity severity, string text)
    {
        var p = Theme.Current.Palette;
        var m = Theme.Current.Metrics;
        var (colour, tint, icon) = severity switch
        {
            Severity.Success => (p.Success, p.SuccessTint, FontAwesomeIcon.CheckCircle),
            Severity.Warning => (p.Warning, p.WarningTint, FontAwesomeIcon.ExclamationTriangle),
            Severity.Error => (p.Error, p.ErrorTint, FontAwesomeIcon.TimesCircle),
            _ => (p.Info, p.InfoTint, FontAwesomeIcon.InfoCircle),
        };
        var glyph = Glyphs.Of(icon);
        var padding = new Vector2(8, 5) * m.Scale;
        var gap = 8 * m.Scale;

        // Measured with the wrap width the child gives the text, so tall banners don't clip.
        Vector2 glyphSize;
        ImGui.PushFont(Fonts.Icon);
        try
        {
            glyphSize = ImGui.CalcTextSize(glyph);
        }
        finally
        {
            ImGui.PopFont();
        }
        var width = ImGui.GetContentRegionAvail().X;
        var wrapWidth = Math.Max(1, width - padding.X * 2 - glyphSize.X - gap);
        var textHeight = ImGui.CalcTextSize(text, false, wrapWidth).Y;
        // The glyph lines up with the first line of text: whichever of the two is shorter moves down.
        var lineHeight = ImGui.GetTextLineHeight();
        var glyphOffset = Math.Max(0, (lineHeight - glyphSize.Y) / 2);
        var textOffset = Math.Max(0, (glyphSize.Y - lineHeight) / 2);
        var height = Math.Max(textHeight + textOffset, glyphSize.Y + glyphOffset) + padding.Y * 2;

        using var _ = Style.Override()
            .Colour(ImGuiCol.ChildBg, tint)
            .Colour(ImGuiCol.Border, colour)
            .Var(ImGuiStyleVar.ChildRounding, m.FrameRounding)
            .Var(ImGuiStyleVar.ChildBorderSize, 1f)
            .Var(ImGuiStyleVar.WindowPadding, padding);
        var open = ImGui.BeginChild(id, new Vector2(0, height), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        try
        {
            if (!open)
                return;
            var top = ImGui.GetCursorPosY();
            ImGui.PushFont(Fonts.Icon);
            try
            {
                using var glyphColour = Style.Override().Colour(ImGuiCol.Text, colour);
                ImGui.SetCursorPosY(top + glyphOffset);
                ImGui.TextUnformatted(glyph);
            }
            finally
            {
                ImGui.PopFont();
            }
            ImGui.SameLine(0, gap);
            ImGui.SetCursorPosY(top + textOffset);
            using var textColour = Style.Override().Colour(ImGuiCol.Text, p.Text);
            ImGui.PushTextWrapPos(0f);
            try
            {
                ImGui.TextUnformatted(text);
            }
            finally
            {
                ImGui.PopTextWrapPos();
            }
        }
        finally
        {
            ImGui.EndChild();
        }
    }

    /// <summary>A centred placeholder for an empty list: a large glyph, a title and an optional hint.</summary>
    public static void EmptyState(FontAwesomeIcon icon, string title, string? hint = null)
    {
        var p = Theme.Current.Palette;
        var scale = Theme.Current.Metrics.Scale;
        ImGui.Dummy(new Vector2(0, 28 * scale));
        var glyph = Glyphs.Of(icon);
        ImGui.PushFont(Fonts.Icon);
        try
        {
            CentreCursor(ImGui.CalcTextSize(glyph).X);
            using var _ = Style.Override().Colour(ImGuiCol.Text, p.Hairline);
            ImGui.TextUnformatted(glyph);
        }
        finally
        {
            ImGui.PopFont();
        }
        ImGui.Spacing();
        CentreCursor(ImGui.CalcTextSize(title).X);
        using (var _ = Style.Override().Colour(ImGuiCol.Text, p.Text))
            ImGui.TextUnformatted(title);
        if (hint == null)
            return;
        var width = Math.Min(ImGui.GetContentRegionAvail().X, 440 * scale);
        CentreCursor(Math.Min(width, ImGui.CalcTextSize(hint).X));
        using var muted = Style.Override().Colour(ImGuiCol.Text, p.Muted);
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + width);
        try
        {
            ImGui.TextUnformatted(hint);
        }
        finally
        {
            ImGui.PopTextWrapPos();
        }
    }

    /// <summary>A turning 270° arc over a faint ring, one frame height square, for background work.</summary>
    public static void Spinner()
    {
        var p = Theme.Current.Palette;
        var scale = Theme.Current.Metrics.Scale;
        var size = ImGui.GetFrameHeight();
        var position = ImGui.GetCursorScreenPos();
        ImGui.Dummy(new Vector2(size));
        var centre = position + new Vector2(size / 2);
        var radius = size * 0.32f;
        var thickness = 2 * scale;
        var start = (float)(ImGui.GetTime() * 5.0 % (Math.PI * 2));
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddCircle(centre, radius, ImGui.GetColorU32(p.Hairline), 24, thickness);
        drawList.PathArcTo(centre, radius, start, start + MathF.PI * 1.5f, 18);
        drawList.PathStroke(ImGui.GetColorU32(p.Accent), ImDrawFlags.None, thickness);
    }

    private static void CentreCursor(float width)
    {
        var available = ImGui.GetContentRegionAvail().X;
        if (available > width)
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (available - width) / 2);
    }
}
