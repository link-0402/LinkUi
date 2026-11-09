using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace LinkUi;

public static partial class Ui
{
    /// <summary>
    /// A search field that fills the row, with a clear button while it has text. Ctrl+F in the window focuses
    /// it. True when the text changed.
    /// </summary>
    public static bool SearchBox(string id, ref string text, string hint)
    {
        var io = ImGui.GetIO();
        if (io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.F) && ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
            ImGui.SetKeyboardFocusHere();
        // The clear button is drawn at exactly the size kept for it: one wider than that overflows the row,
        // and a window that sizes itself to its content (a popup) then grows by the overflow every frame.
        var clear = ImGui.GetFrameHeight();
        var clearWidth = text.Length > 0 ? clear + ImGui.GetStyle().ItemSpacing.X : 0;
        ImGui.SetNextItemWidth(Math.Max(80 * Theme.Current.Metrics.Scale, ImGui.GetContentRegionAvail().X - clearWidth));
        var changed = ImGui.InputTextWithHint(id, hint, ref text, 256);
        if (text.Length == 0)
            return changed;
        ImGui.SameLine();
        ImGui.PushID(id);
        try
        {
            if (GhostIconButton("clear", FontAwesomeIcon.Times, "Clear the search"))
            {
                text = string.Empty;
                changed = true;
            }
        }
        finally
        {
            ImGui.PopID();
        }
        return changed;
    }
}
