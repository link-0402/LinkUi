using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace LinkUi;

public static partial class Ui
{
    /// <summary>One explanatory line in the hint colour.</summary>
    public static void Hint(string text) => Coloured(text, Theme.Current.Palette.Hint, false);

    /// <summary>An explanatory paragraph in the hint colour, wrapped to the available width.</summary>
    public static void HintWrapped(string text) => Coloured(text, Theme.Current.Palette.Hint, true);

    /// <summary>Secondary text in the muted colour.</summary>
    public static void Muted(string text) => Coloured(text, Theme.Current.Palette.Muted, false);

    /// <summary>A glyph from the icon font, as text in <paramref name="colour"/>.</summary>
    public static void Icon(FontAwesomeIcon icon, Vector4 colour)
    {
        using var _ = Style.Override().Colour(ImGuiCol.Text, colour);
        using var font = Fonts.Icons();
        ImGui.TextUnformatted(Glyphs.Of(icon));
    }

    /// <summary>A question-mark glyph on the same line, with <paramref name="text"/> as its tooltip.</summary>
    public static void HelpTip(string text)
    {
        ImGui.SameLine();
        Icon(FontAwesomeIcon.QuestionCircle, Theme.Current.Palette.Hint);
        Tooltip(text);
    }

    /// <summary>
    /// A tooltip for the last item, shown only while it's hovered (disabled items too). Pushes the tooltip
    /// colours and the theme's window padding, so it looks the same whatever the caller has pushed.
    /// </summary>
    public static void Tooltip(string text)
    {
        if (!ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            return;
        var p = Theme.Current.Palette;
        using var _ = Style.Override()
            .Colour(ImGuiCol.PopupBg, p.Tooltip)
            .Colour(ImGuiCol.Border, p.TooltipBorder)
            .Colour(ImGuiCol.Text, p.Text)
            .Var(ImGuiStyleVar.WindowPadding, Theme.Current.Metrics.WindowPadding);
        ImGui.BeginTooltip();
        try
        {
            ImGui.TextUnformatted(text);
        }
        finally
        {
            ImGui.EndTooltip();
        }
    }

    private static void Coloured(string text, Vector4 colour, bool wrapped)
    {
        using var _ = Style.Override().Colour(ImGuiCol.Text, colour);
        if (!wrapped)
        {
            ImGui.TextUnformatted(text);
            return;
        }
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
}
