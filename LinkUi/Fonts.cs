using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;

namespace LinkUi;

/// <summary>
/// The larger fonts titles and headings use: the game's own Axis face at two sizes. Built by
/// Dalamud's font atlas in the background; until a font is ready, text simply draws in the default
/// font.
/// </summary>
/// <remarks>
/// Each font is locked once a frame and pushed straight into ImGui: a game font handle's own Push
/// (and even its Available) allocates several hundred bytes, and lists push a font for every row.
/// The icon font goes through a slot too: <c>UiBuilder.IconFont</c> takes a new lock on every read
/// (32 bytes each), and widgets read it for every glyph they measure or draw.
/// </remarks>
public static class Fonts
{
    private static Slot? _title;
    private static Slot? _heading;
    private static Slot? _icon;

    internal static void Initialize(IUiBuilder ui)
    {
        Dispose();
        _title   = new Slot(ui.FontAtlas.NewGameFontHandle(new GameFontStyle(GameFontFamilyAndSize.Axis14)), ownsHandle: true);
        _heading = new Slot(ui.FontAtlas.NewGameFontHandle(new GameFontStyle(GameFontFamilyAndSize.Axis18)), ownsHandle: true);
        _icon    = new Slot(ui.IconFontHandle, ownsHandle: false);  // Dalamud's handle: never disposed here
    }

    /// <summary>Locks every font for this frame; the host calls it once at the top of its draw.</summary>
    internal static void BeginFrame()
    {
        _title?.Current();
        _heading?.Current();
        _icon?.Current();
    }

    /// <summary>
    /// Dalamud's icon font, locked for this frame; the current font while it isn't built, so a caller's
    /// <c>PushFont</c>/<c>PopFont</c> pair always balances.
    /// </summary>
    internal static ImFontPtr Icon => _icon?.Current() is { } font ? font.ImFont : ImGui.GetFont();

    /// <summary>Titles: a little larger than body text. Dispose to pop.</summary>
    public static IDisposable Title() => Push(_title);

    /// <summary>Headings. Dispose to pop.</summary>
    public static IDisposable Heading() => Push(_heading);

    /// <summary>The icon font, from the cached slot. Dispose to pop.</summary>
    public static IDisposable Icons() => Push(_icon);

    private static IDisposable Push(Slot? slot)
    {
        if (slot?.Current() is not { } font)
            return Nothing.Instance;
        ImGui.PushFont(font.ImFont);
        return Pop.Instance;
    }

    internal static void Dispose()
    {
        _title?.Dispose();
        _heading?.Dispose();
        _icon?.Dispose();
        _title = _heading = _icon = null;
    }

    /// <summary> A font handle and its lock for the current frame; disposes the handle only when it owns it. </summary>
    private sealed class Slot(IFontHandle handle, bool ownsHandle) : IDisposable
    {
        private ILockedImFont? _locked;
        private int _frame = -1;

        /// <summary> The font locked for this frame; null while it isn't built. Locked again each frame, so a rebuilt atlas is picked up. </summary>
        public ILockedImFont? Current()
        {
            var frame = ImGui.GetFrameCount();
            if (frame == _frame)
                return _locked;
            _frame = frame;
            _locked?.Dispose();
            _locked = null;
            try
            {
                if (handle.Available)
                    _locked = handle.Lock();
            }
            catch (Exception)
            {
                // Not built yet, or failed to build: text draws in the default font.
            }
            return _locked;
        }

        public void Dispose()
        {
            _locked?.Dispose();
            _locked = null;
            if (ownsHandle)
                handle.Dispose();
        }
    }

    private sealed class Pop : IDisposable
    {
        public static readonly Pop Instance = new();
        public void Dispose() => ImGui.PopFont();
    }

    private sealed class Nothing : IDisposable
    {
        public static readonly Nothing Instance = new();
        public void Dispose() { }
    }
}
