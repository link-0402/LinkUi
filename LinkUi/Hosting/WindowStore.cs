using System.Text.Json;

namespace LinkUi;

/// <summary>One window's header settings.</summary>
internal readonly record struct WindowPrefs(float Opacity, bool Pinned, bool ClickThrough, bool TopMost)
{
    public static readonly WindowPrefs Default = new(1f, false, false, false);
}

/// <summary>
/// Per-window settings kept in <c>linkui-windows.json</c>, keyed by window id. A bad file never throws:
/// wrong-typed fields fall back to their defaults, opacity is clamped, and a file that doesn't parse is
/// moved to <c>.bad</c>. Saves go through a temp file and report failure instead of throwing.
/// </summary>
internal sealed class WindowStore
{
    private const float MinOpacity = 0.2f;

    private readonly string path;
    private readonly Action<string> log;
    private readonly Dictionary<string, WindowPrefs> windows = new(StringComparer.Ordinal);
    private bool saveFailureLogged;

    /// <summary>The file exists but couldn't be read: saving would overwrite it with only this session's windows.</summary>
    private bool readFailed;

    private WindowStore(string path, Action<string> log)
    {
        this.path = path;
        this.log = log;
    }

    public bool IsDirty { get; private set; }

    /// <summary>Reads the file at <paramref name="path"/>; a missing file gives an empty store and writes nothing.</summary>
    public static WindowStore Load(string path, Action<string> log)
    {
        var store = new WindowStore(path, log);
        if (!File.Exists(path))
            return store;
        try
        {
            var bytes = File.ReadAllBytes(path);
            var start = bytes.AsSpan().StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]) ? 3 : 0;  // a BOM from a hand edit
            using var doc = JsonDocument.Parse(bytes.AsMemory(start));
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                store.SetAside("its root is not an object");
                return store;
            }
            foreach (var window in doc.RootElement.EnumerateObject())
            {
                if (window.Value.ValueKind == JsonValueKind.Object)
                    store.windows[window.Name] = Read(window.Value);
            }
        }
        catch (JsonException)
        {
            store.SetAside("it is not valid JSON");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            store.readFailed = true;
            log($"LinkUi: could not read {path}, using defaults: {e.Message}");
        }
        return store;
    }

    /// <summary>The window's settings, or <see cref="WindowPrefs.Default"/> when it has none.</summary>
    public WindowPrefs Get(string windowId) => windows.TryGetValue(windowId, out var prefs) ? prefs : WindowPrefs.Default;

    /// <summary>Stores the settings with opacity clamped to [0.2, 1] (NaN becomes 1); marks dirty only on a change.</summary>
    public void Set(string windowId, WindowPrefs prefs)
    {
        prefs = prefs with { Opacity = ClampOpacity(prefs.Opacity) };
        if (Get(windowId) == prefs)
            return;
        windows[windowId] = prefs;
        IsDirty = true;
    }

    /// <summary>
    /// Writes the file through <c>.tmp</c>; false on failure, logged once until a save succeeds.
    /// After a failed read it never writes and returns false (the read failure was already logged).
    /// </summary>
    public bool Save()
    {
        if (readFailed)
            return false;
        var tmp = path + ".tmp";
        try
        {
            var folder = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);
            using (var stream = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                foreach (var (id, prefs) in windows)
                {
                    writer.WriteStartObject(id);
                    writer.WriteNumber("opacity", prefs.Opacity);
                    writer.WriteBoolean("pinned", prefs.Pinned);
                    writer.WriteBoolean("clickthrough", prefs.ClickThrough);
                    writer.WriteBoolean("topmost", prefs.TopMost);
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }
            File.Move(tmp, path, overwrite: true);
            IsDirty = false;
            saveFailureLogged = false;
            return true;
        }
        catch (Exception e)
        {
            if (!saveFailureLogged)
            {
                saveFailureLogged = true;
                log($"LinkUi: could not save {path}: {e.Message}");
            }
            return false;
        }
    }

    private static WindowPrefs Read(JsonElement window)
    {
        var prefs = WindowPrefs.Default;
        if (window.TryGetProperty("opacity", out var opacity) && opacity.ValueKind == JsonValueKind.Number)
            prefs = prefs with { Opacity = ClampOpacity((float)opacity.GetDouble()) };
        if (Flag(window, "pinned") is { } pinned)
            prefs = prefs with { Pinned = pinned };
        if (Flag(window, "clickthrough") is { } clickThrough)
            prefs = prefs with { ClickThrough = clickThrough };
        if (Flag(window, "topmost") is { } topMost)
            prefs = prefs with { TopMost = topMost };
        return prefs;
    }

    /// <summary>The field's value when it is a JSON boolean, else null.</summary>
    private static bool? Flag(JsonElement window, string name) =>
        window.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;

    private static float ClampOpacity(float opacity) => float.IsNaN(opacity) ? 1f : Math.Clamp(opacity, MinOpacity, 1f);

    /// <summary>Moves a corrupt file to <c>.bad</c> (replacing an older one) and logs one line.</summary>
    private void SetAside(string reason)
    {
        try
        {
            File.Move(path, path + ".bad", overwrite: true);
            log($"LinkUi: {path} was set aside as .bad because {reason}; using defaults.");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            log($"LinkUi: {path} is unreadable because {reason} and could not be set aside: {e.Message}");
        }
    }
}
