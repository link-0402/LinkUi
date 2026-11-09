using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace LinkUi;

/// <summary>The options menu behind the header's three-line button.</summary>
public abstract partial class ToolWindow
{
    private const string OptionsPopup = "##lu-options";

    /// <summary>The opacity slider's width at 100% scale.</summary>
    private const float OpacitySliderWidth = 220;

    /// <summary>Each menu row's label, composed when its action or the glyph room changes.</summary>
    private readonly List<MenuLabel> menuLabels = new();

    /// <summary>The opacity slider changed during its current drag, so the drag's end saves.</summary>
    private bool opacityEdited;

    /// <summary>Draws the menu when it's open, opening under <paramref name="anchor"/>. Returns the plugin action picked from it.</summary>
    private HeaderAction? DrawOptions(bool actionsInMenu, int actionCount, Vector2 anchor)
    {
        var m = Theme.Current.Metrics;
        // Read here: inside the menu, the viewport is the menu's own.
        var inMainViewport = ImGui.GetWindowViewport().ID == ImGui.GetMainViewport().ID;
        ImGui.SetNextWindowPos(anchor, ImGuiCond.Appearing, new Vector2(1, 0));
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 1f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, m.WindowPadding);
        try
        {
            if (!ImGui.BeginPopup(OptionsPopup, ImGuiWindowFlags.NoMove))
            {
                // Closed mid-drag (Escape): the slider never saw its drag end, so save here.
                if (opacityEdited)
                {
                    opacityEdited = false;
                    host.Windows.Save();
                }
                return null;
            }
            try
            {
                DrawWindowOptions(inMainViewport, m.Scale);
                if (!actionsInMenu)
                    return null;
                Ui.SectionHeader("Plugin actions");
                return DrawMenuActions(actionCount);
            }
            finally
            {
                ImGui.EndPopup();
            }
        }
        finally
        {
            ImGui.PopStyleVar(2);
        }
    }

    private void DrawWindowOptions(bool inMainViewport, float scale)
    {
        if (AllowPinning)
        {
            var pinned = IsPinned;
            if (ImGui.Checkbox("Pin window", ref pinned))
                IsPinned = pinned;
        }
        if (AllowClickthrough)
        {
            var clickThrough = IsClickthrough;
            if (ImGui.Checkbox("Make click-through", ref clickThrough))
                IsClickthrough = clickThrough;
            // Only a window outside the game window has a viewport of its own to keep on top.
            ImGui.BeginDisabled(inMainViewport);
            try
            {
                var topMost = IsTopMost;
                if (ImGui.Checkbox("Stay on top", ref topMost))
                    IsTopMost = topMost;
            }
            finally
            {
                ImGui.EndDisabled();
            }
        }

        Ui.Muted("Opacity");
        var store = host.Windows;
        var stored = store.Get(storeId);
        var percent = stored.Opacity * 100f;
        ImGui.SetNextItemWidth(OpacitySliderWidth * scale);
        if (Ui.Slider("##lu-opacity", ref percent, 20, 100, "0'%'"))
        {
            // Whole percents, as the slider shows them.
            store.Set(storeId, stored with { Opacity = MathF.Round(percent) / 100f });
            opacityEdited = true;
        }
        if (opacityEdited && ImGui.IsItemDeactivated())
        {
            opacityEdited = false;
            store.Save();
        }
    }

    /// <summary>One row per plugin action: its glyph, then its tooltip as the label.</summary>
    private HeaderAction? DrawMenuActions(int actionCount)
    {
        var p = Theme.Current.Palette;
        var fontSize = ImGui.GetFontSize();
        var spaceWidth = MathF.Max(1, ImGui.CalcTextSize(" ").X);
        var gap = ImGui.GetStyle().ItemInnerSpacing.X;
        // The label starts with spaces that leave room for the glyph, so the row's highlight covers both.
        var spaces = (int)MathF.Ceiling((fontSize + gap) / spaceWidth);
        var glyphRoom = spaces * spaceWidth - gap;
        var drawList = ImGui.GetWindowDrawList();
        HeaderAction? clicked = null;
        for (var i = 0; i < actionCount; i++)
        {
            var action = HeaderActions[i];
            var rowStart = ImGui.GetCursorScreenPos();
            ImGui.PushID(i);
            try
            {
                if (ImGui.Selectable(MenuLabelFor(i, action, spaces)))
                    clicked = action;
            }
            finally
            {
                ImGui.PopID();
            }
            var lit = action.Lit?.Invoke() ?? false;
            CentredGlyph(drawList, Glyphs.Of(action.Icon), rowStart, rowStart + new Vector2(glyphRoom, fontSize), lit ? p.Accent : p.Text);
        }
        return clicked;
    }

    private string MenuLabelFor(int index, HeaderAction action, int spaces)
    {
        while (menuLabels.Count <= index)
            menuLabels.Add(new MenuLabel());
        var entry = menuLabels[index];
        if (!ReferenceEquals(entry.Action, action) || entry.Spaces != spaces)
        {
            entry.Action = action;
            entry.Spaces = spaces;
            entry.Text = new string(' ', spaces) + action.Tooltip + "##lu-m";
        }
        return entry.Text;
    }

    private sealed class MenuLabel
    {
        public HeaderAction? Action;
        public int Spaces;
        public string Text = string.Empty;
    }
}
