using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;

namespace LinkUi;

/// <summary>
/// The ImGui style LinkUi draws under: one colour for every <see cref="ImGuiCol"/> and the sizes from
/// <see cref="Metrics"/>. The host pushes it all once a frame around the draw handlers.
/// </summary>
public static class Style
{
    /// <summary>Every <see cref="ImGuiCol"/> but <c>Count</c>.</summary>
    internal const int ColourCount = (int)ImGuiCol.Count;

    /// <summary>The style vars <see cref="PushAll"/> pushes.</summary>
    internal const int VarCount = 19;

    /// <summary>The colour for each <see cref="ImGuiCol"/>, indexed by <c>(int)ImGuiCol</c>.</summary>
    internal static Vector4[] BuildColours(Palette p) => Build(p, null);

    /// <summary>Null when the table assigns every <see cref="ImGuiCol"/> but <c>Count</c> exactly once; otherwise what is missing or doubled.</summary>
    internal static string? CheckCoverage(Palette p)
    {
        var counts = new int[ColourCount];
        Build(p, counts);
        var problems = new StringBuilder();
        var missing = new List<string>();
        var doubled = new List<string>();
        foreach (var col in Enum.GetValues<ImGuiCol>())
        {
            var i = (int)col;
            if (col == ImGuiCol.Count)
                continue;
            if (i < 0 || i >= ColourCount || counts[i] == 0)  // a colour past ColourCount came with a newer ImGui
                missing.Add(col.ToString());
            else if (counts[i] > 1)
                doubled.Add(col.ToString());
        }
        if (missing.Count > 0)
            problems.Append("Missing: ").Append(string.Join(", ", missing)).Append(". ");
        if (doubled.Count > 0)
            problems.Append("Assigned more than once: ").Append(string.Join(", ", doubled)).Append(". ");
        return problems.Length == 0 ? null : problems.ToString().TrimEnd();
    }

    /// <summary>Pushes every colour and the <see cref="VarCount"/> style vars; pair with <see cref="PopAll"/> in a <c>finally</c>.</summary>
    internal static void PushAll(Vector4[] colours, in Metrics m)
    {
        for (var i = 0; i < colours.Length; i++)
            ImGui.PushStyleColor((ImGuiCol)i, colours[i]);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, m.WindowPadding);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, m.WindowRounding);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, m.BorderSize);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, m.ChildRounding);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, m.BorderSize);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, m.PopupRounding);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, m.BorderSize);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, m.FramePadding);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, m.FrameRounding);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, m.BorderSize);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, m.ItemSpacing);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, m.ItemInnerSpacing);
        ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, m.IndentSpacing);
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, m.CellPadding);
        ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, m.ScrollbarSize);
        ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarRounding, m.ScrollbarRounding);
        ImGui.PushStyleVar(ImGuiStyleVar.GrabMinSize, m.GrabMinSize);
        ImGui.PushStyleVar(ImGuiStyleVar.GrabRounding, m.GrabRounding);
        ImGui.PushStyleVar(ImGuiStyleVar.TabRounding, m.TabRounding);
    }

    /// <summary>Pops what <see cref="PushAll"/> pushed.</summary>
    internal static void PopAll()
    {
        ImGui.PopStyleVar(VarCount);
        ImGui.PopStyleColor(ColourCount);
    }

    /// <summary>The coverage problem with the live palette, or null when the table is complete (for the demo's Gallery).</summary>
    public static string? SelfTest() => CheckCoverage(Theme.Current.Palette);

    /// <summary>
    /// Starts a set of pushes popped together on dispose: <c>using var _ = Style.Override().Colour(ImGuiCol.Text, p.Muted);</c>.
    /// Calls may be chained or made on the bound value, also conditionally:
    /// <c>using var o = Style.Override(); if (marked) o.Colour(ImGuiCol.Text, p.Accent);</c>.
    /// </summary>
    public static StyleOverride Override() => new(StyleOverride.Stack.Mark());

    private static Vector4[] Build(Palette p, int[]? counts)
    {
        var c = new Vector4[ColourCount];
        var white = new Vector4(1, 1, 1, 1);
        var transparent = Vector4.Zero;

        Set(ImGuiCol.Text, p.Text);
        Set(ImGuiCol.TextDisabled, p.Disabled);
        Set(ImGuiCol.WindowBg, p.Window);
        Set(ImGuiCol.ChildBg, p.Panel);
        Set(ImGuiCol.PopupBg, p.Popup);
        Set(ImGuiCol.Border, p.Border);
        Set(ImGuiCol.BorderShadow, transparent);
        Set(ImGuiCol.FrameBg, p.Frame);
        Set(ImGuiCol.FrameBgHovered, p.FrameHovered);
        Set(ImGuiCol.FrameBgActive, p.FrameActive);
        Set(ImGuiCol.TitleBg, p.Header);
        Set(ImGuiCol.TitleBgActive, p.Header);
        Set(ImGuiCol.TitleBgCollapsed, p.Header);
        Set(ImGuiCol.MenuBarBg, p.Header);
        Set(ImGuiCol.ScrollbarBg, transparent);
        Set(ImGuiCol.ScrollbarGrab, Oklch.Hex("#3a3a3a"));
        Set(ImGuiCol.ScrollbarGrabHovered, Oklch.Hex("#4a4a4a"));
        Set(ImGuiCol.ScrollbarGrabActive, Oklch.Hex("#5a5a5a"));
        Set(ImGuiCol.CheckMark, p.Accent);
        Set(ImGuiCol.SliderGrab, Oklch.Hex("#6a6a6a"));
        Set(ImGuiCol.SliderGrabActive, p.Accent);
        Set(ImGuiCol.Button, p.Control);
        Set(ImGuiCol.ButtonHovered, p.ControlHovered);
        Set(ImGuiCol.ButtonActive, p.ControlActive);
        // Grey, not accent: ImGui shares these with selected selectables and table rows.
        Set(ImGuiCol.Header, Oklch.Hex("#2e2e2e"));
        Set(ImGuiCol.HeaderHovered, Oklch.Hex("#363636"));
        Set(ImGuiCol.HeaderActive, Oklch.Hex("#3d3d3d"));
        Set(ImGuiCol.TextSelectedBg, WithAlpha(p.Accent, 0.35f));
        Set(ImGuiCol.DragDropTarget, p.Accent);
        Set(ImGuiCol.NavHighlight, p.Accent);
        Set(ImGuiCol.NavWindowingHighlight, WithAlpha(white, 0.70f));
        Set(ImGuiCol.NavWindowingDimBg, WithAlpha(Oklch.Hex("#cccccc"), 0.20f));

        Set(ImGuiCol.Separator, p.Hairline);
        Set(ImGuiCol.SeparatorHovered, p.Muted);
        Set(ImGuiCol.SeparatorActive, p.Accent);
        Set(ImGuiCol.ResizeGrip, WithAlpha(white, 0.06f));
        Set(ImGuiCol.ResizeGripHovered, WithAlpha(p.Accent, 0.60f));
        Set(ImGuiCol.ResizeGripActive, p.Accent);
        Set(ImGuiCol.Tab, Oklch.Hex("#2e2e2e"));
        Set(ImGuiCol.TabHovered, Oklch.Hex("#363636"));
        Set(ImGuiCol.TabActive, Oklch.Hex("#3d3d3d"));
        Set(ImGuiCol.TabUnfocused, Oklch.Hex("#2a2a2a"));
        Set(ImGuiCol.TabUnfocusedActive, Oklch.Hex("#363636"));
        Set(ImGuiCol.DockingPreview, WithAlpha(p.Accent, 0.40f));
        Set(ImGuiCol.DockingEmptyBg, p.Frame);
        Set(ImGuiCol.PlotLines, p.Muted);
        Set(ImGuiCol.PlotLinesHovered, p.Accent);
        Set(ImGuiCol.PlotHistogram, p.Accent);
        Set(ImGuiCol.PlotHistogramHovered, p.AccentHovered);
        Set(ImGuiCol.TableHeaderBg, p.Header);
        Set(ImGuiCol.TableBorderStrong, p.Border);
        Set(ImGuiCol.TableBorderLight, Oklch.Hex("#2e2e2e"));
        Set(ImGuiCol.TableRowBg, transparent);
        Set(ImGuiCol.TableRowBgAlt, WithAlpha(white, 0.025f));
        Set(ImGuiCol.ModalWindowDimBg, new Vector4(0, 0, 0, 0.55f));
        return c;

        void Set(ImGuiCol col, Vector4 value)
        {
            c[(int)col] = value;
            if (counts != null)
                counts[(int)col]++;
        }
    }

    private static Vector4 WithAlpha(Vector4 colour, float alpha) => colour with { W = alpha };
}

/// <summary>
/// Style pushes popped together by <see cref="Dispose"/>. It holds only the mark <see cref="Style.Override"/>
/// took, and dispose pops back to that mark, so chained, unchained and conditional calls all balance, and
/// nested overrides unwind in order. Start one with <see cref="Style.Override"/>.
/// </summary>
public readonly ref struct StyleOverride
{
    /// <summary>What every override in this plugin has pushed; each plugin loads its own copy of LinkUi.</summary>
    internal static readonly OverrideStack Stack = new();

    private readonly (int Colours, int Vars) mark;

    internal StyleOverride((int Colours, int Vars) mark) => this.mark = mark;

    public StyleOverride Colour(ImGuiCol col, Vector4 value)
    {
        ImGui.PushStyleColor(col, value);
        Stack.PushedColour();
        return this;
    }

    public StyleOverride Var(ImGuiStyleVar v, float value)
    {
        ImGui.PushStyleVar(v, value);
        Stack.PushedVar();
        return this;
    }

    public StyleOverride Var(ImGuiStyleVar v, Vector2 value)
    {
        ImGui.PushStyleVar(v, value);
        Stack.PushedVar();
        return this;
    }

    public void Dispose()
    {
        var (colours, vars) = Stack.Unwind(mark);
        if (vars > 0)
            ImGui.PopStyleVar(vars);
        if (colours > 0)
            ImGui.PopStyleColor(colours);
    }
}
