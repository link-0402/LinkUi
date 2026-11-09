using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace LinkUi;

/// <summary>
/// A Dalamud window with LinkUi's own header instead of the title bar: plugin icon, title, subtitle, the
/// plugin's <see cref="HeaderActions"/>, then pin, options and close. The window's pin, click-through,
/// stay-on-top and opacity are kept in the host's window store. Draw the window's body in <see cref="DrawContent"/>.
/// </summary>
public abstract partial class ToolWindow : Window
{
    /// <summary>Flags that belong to the content child rather than the outer window.</summary>
    private const ImGuiWindowFlags ContentMask = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoScrollbar
        | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.HorizontalScrollbar
        | ImGuiWindowFlags.AlwaysVerticalScrollbar | ImGuiWindowFlags.AlwaysHorizontalScrollbar;

    /// <summary>The outer window always has these: the header replaces the title bar.</summary>
    private const ImGuiWindowFlags Chrome = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse;

    private const string ContentId = "##lu-content";

    private readonly LinkUiHost host;

    /// <summary>The window name <see cref="title"/> and <see cref="storeId"/> were split from.</summary>
    private string? splitName;

    private string title = string.Empty;
    private string storeId = string.Empty;
    private bool prefsApplied;

    /// <exception cref="InvalidOperationException">No <see cref="LinkUiHost"/> exists yet.</exception>
    protected ToolWindow(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        : base(name, (flags & ~ContentMask) | Chrome)
    {
        host = LinkUiHost.Current ?? throw new InvalidOperationException("Create the LinkUiHost before any ToolWindow.");
        ContentFlags = flags & ContentMask;
    }

    /// <summary>Muted text after the title, such as the open file; null for none.</summary>
    public string? Subtitle { get; set; }

    /// <summary>The plugin's header buttons, left to right. They move into the options menu when the window is narrow.</summary>
    public List<HeaderAction> HeaderActions { get; } = new();

    /// <summary>
    /// Flags for the content child (menu bar and scrolling). Content flags added to <c>Flags</c> later move
    /// here on the next frame; remove them here.
    /// </summary>
    public ImGuiWindowFlags ContentFlags { get; set; }

    /// <summary>The window's body, drawn below the header.</summary>
    protected abstract void DrawContent();

    /// <summary>Runs at the end of <see cref="PreDraw"/>, before the window begins.</summary>
    protected virtual void OnPreDraw()
    {
    }

    /// <summary>Runs at the start of <see cref="PostDraw"/>, after the window ends.</summary>
    protected virtual void OnPostDraw()
    {
    }

    public sealed override void PreDraw()
    {
        // The scroll flags stay on the outer window (the child scrolls), except when it sizes itself.
        var forced = Chrome;
        if ((Flags & ImGuiWindowFlags.AlwaysAutoResize) == 0)
            forced |= ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        var incoming = Flags & ~forced;
        ContentFlags |= incoming & ContentMask;
        Flags = (incoming & ~ContentMask) | forced;

        SplitName();
        var prefs = host.Windows.Get(storeId);
        if (!prefsApplied)
        {
            // Dalamud restores its own preset just before the first PreDraw: ours win.
            prefsApplied = true;
            IsPinned = prefs.Pinned;
            IsClickthrough = prefs.ClickThrough;
            IsTopMost = prefs.TopMost;
        }

        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, prefs.Opacity);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleColor(ImGuiCol.Border, Theme.Current.Palette.WindowBorder);
        try
        {
            OnPreDraw();
        }
        catch
        {
            // Dalamud skips PostDraw when PreDraw throws.
            PopPreDraw();
            throw;
        }
    }

    public sealed override void Draw()
    {
        SplitName();
        var p = Theme.Current.Palette;
        var m = Theme.Current.Metrics;
        var windowPos = ImGui.GetWindowPos();
        var headerHeight = ImGui.GetFrameHeight() + m.HeaderExtra;
        // The open options menu takes focus from the window, but the window still reads as focused.
        var focused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) || ImGui.IsPopupOpen(OptionsPopup);

        // Read once: the header and the menu index the same count, and clicks run only after both,
        // so a handler that changes the list can't move it under them.
        var actionCount = HeaderActions.Count;
        var clicked = DrawHeader(windowPos, headerHeight, focused, actionCount, out var layout, out var optionsAnchor);
        clicked = DrawOptions(layout.ActionsInMenu, actionCount, optionsAnchor) ?? clicked;
        clicked?.OnClick();
        // Before the content, so a content exception can't lose a change made in the header or menu.
        SyncPrefs();

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, m.WindowPadding);
        ImGui.PushStyleColor(ImGuiCol.Border, p.Border);
        try
        {
            if ((Flags & ImGuiWindowFlags.AlwaysAutoResize) != 0)
                DrawAutoResizeContent(headerHeight, m);
            else
                DrawChildContent(windowPos, headerHeight);
        }
        finally
        {
            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
        }
    }

    public sealed override void PostDraw()
    {
        try
        {
            OnPostDraw();
        }
        finally
        {
            PopPreDraw();
        }
    }

    private static void PopPreDraw()
    {
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
    }

    /// <summary>The content in a child under the header that scrolls on its own; its background stays the window's.</summary>
    private void DrawChildContent(Vector2 windowPos, float headerHeight)
    {
        ImGui.SetCursorScreenPos(windowPos + new Vector2(0, headerHeight));
        ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
        bool visible;
        try
        {
            visible = ImGui.BeginChild(ContentId, Vector2.Zero, false, ContentFlags | ImGuiWindowFlags.AlwaysUseWindowPadding);
        }
        finally
        {
            // Only the content child is transparent: nested children keep the panel colour.
            ImGui.PopStyleColor();
        }
        try
        {
            if (visible)
                DrawContent();
        }
        finally
        {
            ImGui.EndChild();
        }
    }

    /// <summary>A self-sizing window has no child: the content sits in a group, padded like a child would be.</summary>
    private void DrawAutoResizeContent(float headerHeight, in Metrics m)
    {
        ImGui.SetCursorPos(new Vector2(m.WindowPadding.X, headerHeight + m.WindowPadding.Y));
        ImGui.BeginGroup();
        try
        {
            DrawContent();
        }
        finally
        {
            ImGui.EndGroup();
            ImGui.Dummy(m.WindowPadding);
        }
    }

    /// <summary>Splits the window name into the header title and the store id, only when the name changes.</summary>
    private void SplitName()
    {
        var name = WindowName;
        if (ReferenceEquals(name, splitName))
            return;
        splitName = name;
        title = WindowText.Title(name);
        storeId = WindowText.Id(name);
    }

    /// <summary>
    /// Saves pin, click-through and stay on top when they differ from the store, whoever changed them.
    /// Opacity is saved by the options menu when a drag ends.
    /// </summary>
    private void SyncPrefs()
    {
        var store = host.Windows;
        var stored = store.Get(storeId);
        var now = new WindowPrefs(stored.Opacity, IsPinned, IsClickthrough, IsTopMost);
        if (now == stored)
            return;
        store.Set(storeId, now);
        store.Save();
    }
}
