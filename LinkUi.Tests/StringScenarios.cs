using LinkUi.Tests.Support;

namespace LinkUi.Tests;

/// <summary>No string literal in the library holds an ellipsis: the game font draws "…" and "..." badly.</summary>
internal static class StringScenarios
{
    public static void Run()
    {
        Scenarios.Run(FlagsLiterals);
        Scenarios.Run(LibraryHasNoEllipsis);
    }

    private static void FlagsLiterals()
    {
        Assertions.Check(EllipsisScan.Find("var a = \"Loading...\";").SequenceEqual([1]), "three dots in a literal");
        Assertions.Check(EllipsisScan.Find("x\nvar a = \"Loading…\";").SequenceEqual([2]), "ellipsis character in a literal");
        Assertions.Check(EllipsisScan.Find("var a = @\"a...b\";").SequenceEqual([1]), "verbatim literal");
        Assertions.Check(EllipsisScan.Find("// wait...\n/// <summary>more...</summary>\nvar s = \"ok\"; // trailing...").Count == 0, "comments are not literals");
        Assertions.Check(EllipsisScan.Find("/* a...\n b... */ var s = \"ok\";").Count == 0, "block comments");
        Assertions.Check(EllipsisScan.Find("var s = \"a//b...\";").SequenceEqual([1]), "// inside a literal is not a comment");
    }

    private static void LibraryHasNoEllipsis()
    {
        var root = FindRepoRoot();
        Assertions.Require(root != null, "found the LinkUi source folder");
        if (root == null)
            return;
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "LinkUi"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
            var lines = EllipsisScan.Find(File.ReadAllText(file));
            Assertions.Check(lines.Count == 0, $"no ellipsis in {Path.GetFileName(file)} (lines {string.Join(",", lines)})");
        }
    }

    /// <summary>The folder holding <c>LinkUi/LinkUi.csproj</c>, walking up from the test exe.</summary>
    private static string? FindRepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "LinkUi", "LinkUi.csproj")))
                return dir.FullName;
        }
        return null;
    }
}

/// <summary>
/// Finds C# string literals that hold "..." or "…". One pass over the text with a state for code,
/// comments, regular, verbatim and char literals. Raw (<c>"""</c>) literals aren't supported.
/// </summary>
internal static class EllipsisScan
{
    /// <summary>1-based line numbers where an offending literal starts, in order, each line once.</summary>
    public static IReadOnlyList<int> Find(string source)
    {
        var hits = new List<int>();
        var line = 1;
        var i = 0;
        while (i < source.Length)
        {
            var c = source[i];
            var next = i + 1 < source.Length ? source[i + 1] : '\0';
            if (c == '\n')
            {
                line++;
                i++;
            }
            else if (c == '/' && next == '/')
            {
                while (i < source.Length && source[i] != '\n')
                    i++;
            }
            else if (c == '/' && next == '*')
            {
                i += 2;
                while (i < source.Length && !(source[i] == '*' && i + 1 < source.Length && source[i + 1] == '/'))
                {
                    if (source[i] == '\n')
                        line++;
                    i++;
                }
                i += 2;
            }
            else if (c == '\'')
            {
                i++;
                while (i < source.Length && source[i] != '\'' && source[i] != '\n')
                    i += source[i] == '\\' ? 2 : 1;
                i++;
            }
            else if (c == '"')
            {
                var startLine = line;
                var start = i + 1;
                var verbatim = IsVerbatim(source, i);
                i++;
                while (i < source.Length)
                {
                    if (verbatim)
                    {
                        if (source[i] == '"')
                        {
                            if (i + 1 < source.Length && source[i + 1] == '"') { i += 2; continue; }
                            break;
                        }
                        if (source[i] == '\n')
                            line++;
                        i++;
                    }
                    else
                    {
                        if (source[i] == '"' || source[i] == '\n')
                            break;
                        i += source[i] == '\\' ? 2 : 1;
                    }
                }
                var end = Math.Min(i, source.Length);
                var content = source.AsSpan(start, end - start);
                if ((content.Contains("...", StringComparison.Ordinal) || content.Contains('…'))
                    && (hits.Count == 0 || hits[^1] != startLine))
                    hits.Add(startLine);
                if (i < source.Length && source[i] == '"')
                    i++;
            }
            else
            {
                i++;
            }
        }
        return hits;
    }

    /// <summary>A quote opens a verbatim literal after <c>@</c>, <c>$@</c> or <c>@$</c>.</summary>
    private static bool IsVerbatim(string source, int quote)
    {
        var p = quote - 1;
        if (p >= 0 && source[p] == '@')
            return true;
        return p >= 1 && source[p] == '$' && source[p - 1] == '@';
    }
}
