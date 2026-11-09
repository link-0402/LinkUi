using System.Reflection;
using LinkUi.Tests.Support;

namespace LinkUi.Tests;

/// <summary>
/// Runs every <c>*Scenarios</c> type that has a parameterless <c>public static void Run()</c>, in name order.
/// New test files only add such a type, so tasks never edit this file.
/// </summary>
internal static class Program
{
    private static int Main()
    {
        var suites = typeof(Program).Assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Scenarios", StringComparison.Ordinal))
            .Select(t => (Type: t, Run: t.GetMethod("Run", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes)))
            .Where(s => s.Run != null && s.Run.ReturnType == typeof(void))
            .OrderBy(s => s.Type.Name, StringComparer.Ordinal);
        foreach (var (type, run) in suites)
        {
            try { run!.Invoke(null, null); }
            catch (TargetInvocationException e)
            {
                Assertions.Fail($"{type.Name}.Run stopped: {e.InnerException?.GetType().Name}: {e.InnerException?.Message}");
                Console.WriteLine(e.InnerException);
            }
        }
        return Scenarios.Finish("LinkUi.Tests");
    }
}
