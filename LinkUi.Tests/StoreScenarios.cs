using LinkUi.Tests.Support;

namespace LinkUi.Tests;

/// <summary>The per-window settings file: round trips, bad and corrupt files, the dirty flag and failed saves.</summary>
internal static class StoreScenarios
{
    private static readonly List<string> Folders = [];

    public static void Run()
    {
        try
        {
            Scenarios.Run(RoundTrip);
            Scenarios.Run(MissingFile);
            Scenarios.Run(CorruptFile);
            Scenarios.Run(BadFields);
            Scenarios.Run(Dirty);
            Scenarios.Run(StaleTemp);
            Scenarios.Run(SaveFailure);
            Scenarios.Run(CreatesFolder);
            Scenarios.Run(ByteOrderMark);
            Scenarios.Run(HandEdits);
            Scenarios.Run(NoSaveAfterFailedRead);
        }
        finally
        {
            foreach (var folder in Folders)
            {
                try { Directory.Delete(folder, recursive: true); }
                catch (Exception) { }
            }
            Folders.Clear();
        }
    }

    /// <summary>A fresh folder under the temp path and <c>linkui-windows.json</c> inside it (not created); deleted when the suite ends.</summary>
    static (string Dir, string Path) Temp()
    {
        var dir = Path.Combine(Path.GetTempPath(), "linkui-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        Folders.Add(dir);
        return (dir, Path.Combine(dir, "linkui-windows.json"));
    }

    static void RoundTrip()
    {
        var (dir, path) = Temp(); var store = WindowStore.Load(path, _ => { });
        store.Set("gallery", new WindowPrefs(0.85f, true, false, true));
        Assertions.Check(store.Save(), "save succeeds");
        Assertions.Check(WindowStore.Load(path, _ => { }).Get("gallery") == new WindowPrefs(0.85f, true, false, true), "values come back");
        Assertions.Check(!File.Exists(path + ".tmp"), "no temp file left");
    }
    static void MissingFile() { var (_, path) = Temp(); var s = WindowStore.Load(path, _ => { }); Assertions.Check(s.Get("x") == WindowPrefs.Default && !File.Exists(path) && !s.IsDirty, "missing file: defaults, nothing written"); }
    static void CorruptFile()
    {
        var (_, path) = Temp(); File.WriteAllText(path, "{not json"); File.WriteAllText(path + ".bad", "old");
        var logs = 0; var s = WindowStore.Load(path, _ => logs++);
        Assertions.Check(s.Get("x") == WindowPrefs.Default && File.Exists(path + ".bad") && !File.Exists(path) && logs == 1, "corrupt: defaults, renamed to .bad (replacing an old one), one log line");
        File.WriteAllText(path, "[]"); Assertions.Check(WindowStore.Load(path, _ => { }).Get("x") == WindowPrefs.Default && File.Exists(path + ".bad"), "array root counts as corrupt");
    }
    static void BadFields()  // Review Focus 5
    {
        var (_, path) = Temp();
        File.WriteAllText(path, """{ "a": { "opacity": "high", "pinned": 1 }, "b": { "opacity": 0.05 }, "c": { "opacity": 7, "topmost": true } }""");
        var s = WindowStore.Load(path, _ => { });
        Assertions.Check(s.Get("a") == WindowPrefs.Default, "wrong types fall back per field");
        Assertions.Check(s.Get("b").Opacity == 0.2f && s.Get("c").Opacity == 1f && s.Get("c").TopMost, "opacity clamped, other fields kept");
        s.Set("d", new WindowPrefs(float.NaN, false, false, false)); Assertions.Check(s.Get("d").Opacity == 1f, "NaN becomes 1");
    }
    static void Dirty()
    {
        var (_, path) = Temp(); var s = WindowStore.Load(path, _ => { });
        s.Set("w", WindowPrefs.Default); Assertions.Check(!s.IsDirty, "setting the default on an unknown id changes nothing");
        s.Set("w", WindowPrefs.Default with { Pinned = true }); Assertions.Check(s.IsDirty, "a change marks dirty");
        s.Save(); Assertions.Check(!s.IsDirty, "save clears dirty");
    }
    static void StaleTemp() { var (_, path) = Temp(); File.WriteAllText(path + ".tmp", "junk"); var s = WindowStore.Load(path, _ => { }); s.Set("w", WindowPrefs.Default with { Opacity = 0.5f }); Assertions.Check(s.Save() && !File.Exists(path + ".tmp"), "a stale temp file is replaced"); }
    static void SaveFailure()  // Review Focus 5
    {
        var (dir, _) = Temp(); var notADir = Path.Combine(dir, "file"); File.WriteAllText(notADir, "x");
        var logs = 0; var s = WindowStore.Load(Path.Combine(notADir, "linkui-windows.json"), _ => logs++);
        s.Set("w", WindowPrefs.Default with { Pinned = true });
        Assertions.Check(!s.Save() && !s.Save() && logs == 1, "unwritable path: false twice, no throw, one log line");
    }
    static void CreatesFolder() { var (dir, _) = Temp(); var path = Path.Combine(dir, "new", "linkui-windows.json"); var s = WindowStore.Load(path, _ => { }); s.Set("w", WindowPrefs.Default with { Pinned = true }); Assertions.Check(s.Save() && File.Exists(path), "save creates the folder"); }
    static void ByteOrderMark()
    {
        var (_, path) = Temp();
        File.WriteAllBytes(path, [0xEF, 0xBB, 0xBF, .. System.Text.Encoding.UTF8.GetBytes("""{ "w": { "pinned": true } }""")]);
        var s = WindowStore.Load(path, _ => { });
        Assertions.Check(s.Get("w").Pinned && !File.Exists(path + ".bad"), "a leading BOM is skipped, not corrupt");
    }
    static void HandEdits()  // final review A-M8
    {
        var (_, path) = Temp();
        File.WriteAllText(path, """{ "w": { "opacity": 0.5, "pinned": true, }, }""");
        var s = WindowStore.Load(path, _ => { });
        Assertions.Check(s.Get("w") == WindowPrefs.Default with { Opacity = 0.5f, Pinned = true } && !File.Exists(path + ".bad"), "trailing commas are accepted");
        File.WriteAllText(path, "// my windows\n{ \"w\": { /* half */ \"opacity\": 0.5 } } // end\n");
        s = WindowStore.Load(path, _ => { });
        Assertions.Check(s.Get("w").Opacity == 0.5f && !File.Exists(path + ".bad"), "comments are skipped");
    }
    static void NoSaveAfterFailedRead()
    {
        var (_, path) = Temp(); File.WriteAllText(path, """{ "a": { "pinned": true } }"""); var before = File.ReadAllBytes(path);
        var logs = 0; WindowStore s;
        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            s = WindowStore.Load(path, _ => logs++);
            Assertions.Check(s.Get("a") == WindowPrefs.Default && logs == 1, "locked file: defaults, one log line");
        }
        s.Set("w", WindowPrefs.Default with { Pinned = true });
        Assertions.Check(!s.Save() && File.ReadAllBytes(path).AsSpan().SequenceEqual(before) && logs == 1, "after a failed read, save writes nothing");
    }
}
