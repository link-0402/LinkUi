using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace LinkUi;

/// <summary>
/// The plugin-wide LinkUi host: one per plugin. Hooks <c>UiBuilder.Draw</c> once and runs every
/// <see cref="Draw"/> subscriber under the full LinkUi style, popped in a <c>finally</c>.
/// </summary>
public sealed class LinkUiHost : IDisposable
{
    private readonly IUiBuilder ui;
    private readonly Palette palette;
    private readonly Vector4[] colours;
    private readonly HandlerList handlers = new();
    private readonly Action drawHook;

    /// <summary>Built once so the frame allocates no delegate.</summary>
    private readonly Action<Action, Exception> onError;

    private float scale;
    private bool disposed;

    /// <exception cref="InvalidOperationException">A host already exists, or (Debug builds) the colour table misses an <c>ImGuiCol</c>.</exception>
    public LinkUiHost(IDalamudPluginInterface pluginInterface, IPluginLog log, LinkUiOptions options)
    {
        if (Current != null)
            throw new InvalidOperationException("LinkUi: a host already exists; dispose it before creating another.");
        Options = options;
        Log = log;
        ui = pluginInterface.UiBuilder;
        palette = Palette.From(options.Accent);
        colours = Style.BuildColours(palette);
#if DEBUG
        if (Style.CheckCoverage(palette) is { } problem)
        {
            log.Error("LinkUi: the colour table is incomplete. {Problem}", problem);
            throw new InvalidOperationException("LinkUi: the colour table is incomplete. " + problem);
        }
#endif
        Windows = WindowStore.Load(Path.Combine(pluginInterface.GetPluginConfigDirectory(), "linkui-windows.json"),
            message => log.Warning("{Message}", message));
        onError = (_, e) => log.Error(e, "LinkUi: a draw handler failed");
        drawHook = OnDraw;
        Fonts.Initialize(ui);
        scale = ImGuiHelpers.GlobalScale;
        Theme.Current = new ThemeState(palette, Metrics.At(scale));
        ui.Draw += drawHook;
        Current = this;
    }

    /// <summary>Drawn every frame under the LinkUi style. A throwing subscriber is logged once and never stops the others.</summary>
    public event Action? Draw
    {
        add { if (value != null) handlers.Add(value); }
        remove { if (value != null) handlers.Remove(value); }
    }

    /// <summary>The live host, or null before one is created and after it is disposed.</summary>
    public static LinkUiHost? Current { get; private set; }

    public LinkUiOptions Options { get; }

    internal WindowStore Windows { get; }

    internal IPluginLog Log { get; }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        ui.Draw -= drawHook;
        Fonts.Dispose();
        if (Windows.IsDirty)
            Windows.Save();
        Theme.Current = default;
        if (Current == this)
            Current = null;
    }

    private void OnDraw()
    {
        var now = ImGuiHelpers.GlobalScale;
        if (now != scale)
        {
            scale = now;
            Theme.Current = new ThemeState(palette, Metrics.At(now));
        }
        Fonts.BeginFrame();  // one lock per font a frame; widget reads then hit the cache
        var metrics = Theme.Current.Metrics;
        Style.PushAll(colours, in metrics);
        try
        {
            handlers.InvokeAll(onError);
        }
        finally
        {
            Style.PopAll();
        }
    }
}
