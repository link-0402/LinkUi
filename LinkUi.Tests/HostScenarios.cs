using LinkUi.Tests.Support;

namespace LinkUi.Tests;

/// <summary>The host's draw subscribers: a throwing or self-removing handler, error reports, no allocation.</summary>
internal static class HostScenarios
{
    public static void Run()
    {
        Scenarios.Run(OthersStillRun);
        Scenarios.Run(NewMessageReportedAgain);
        Scenarios.Run(ReportsCapped);
        Scenarios.Run(RemoveForgetsReports);
        Scenarios.Run(UnsubscribeDuringCall);
        Scenarios.Run(NoAllocation);
    }

    static readonly Action<Action> NoSuppressed = static _ => { };

    static void OthersStillRun()  // Review Focus 3
    {
        var list = new HandlerList(); var ran = 0; var errors = 0;
        list.Add(() => throw new InvalidOperationException("boom")); list.Add(() => ran++);
        for (var i = 0; i < 3; i++) list.InvokeAll((_, _) => errors++, NoSuppressed);
        Assertions.Check(ran == 3 && errors == 1, "a throwing handler doesn't stop the next; same error reported once");
    }
    static void NewMessageReportedAgain()
    {
        var list = new HandlerList(); var n = 0; var errors = 0;
        list.Add(() => throw new InvalidOperationException("call " + (n++ % 2)));
        for (var i = 0; i < 4; i++) list.InvokeAll((_, _) => errors++, NoSuppressed);
        Assertions.Check(errors == 2, "two distinct messages, two reports");
    }
    static void ReportsCapped()  // final review A-M3
    {
        var list = new HandlerList(); var n = 0; var other = 0; var errors = 0; var suppressed = 0; Action? suppressedFor = null;
        Action changing = () => throw new InvalidOperationException("index " + n++);
        list.Add(changing); list.Add(() => throw new InvalidOperationException("other " + other++));
        for (var i = 0; i < 20; i++) list.InvokeAll((_, _) => errors++, h => { suppressed++; suppressedFor ??= h; });
        Assertions.Check(errors == 10 && suppressed == 2, $"five reports per handler, then one suppressed line each ({errors} reports, {suppressed} suppressed)");
        Assertions.Check(ReferenceEquals(suppressedFor, changing), "the suppressed line names the handler");
        Assertions.Check(list.TrackedErrors == 10, $"no more entries kept after the cap ({list.TrackedErrors})");
    }
    static void RemoveForgetsReports()  // final review A-M3
    {
        var list = new HandlerList(); var n = 0; var errors = 0; var suppressed = 0;
        Action same = () => throw new InvalidOperationException("same");
        Action changing = () => throw new InvalidOperationException("index " + n++);
        list.Add(same); list.Add(changing);
        for (var i = 0; i < 8; i++) list.InvokeAll((_, _) => errors++, _ => suppressed++);
        list.Remove(same); list.Remove(changing);
        Assertions.Check(list.TrackedErrors == 0, $"Remove drops the handler's entries ({list.TrackedErrors} left)");
        errors = suppressed = 0;
        list.Add(same); list.Add(changing);
        list.InvokeAll((_, _) => errors++, _ => suppressed++);
        Assertions.Check(errors == 2 && suppressed == 0, "a handler added again reports again, its cap reset");
        list.Add(same); list.Remove(same);
        list.InvokeAll((_, _) => errors++, _ => suppressed++);
        Assertions.Check(errors == 3 && list.TrackedErrors == 3, "removing one of two equal subscriptions keeps the other's entries");
    }
    static void UnsubscribeDuringCall()  // Review Focus 3
    {
        var list = new HandlerList(); var a = 0; var b = 0; Action? self = null;
        self = () => { a++; list.Remove(self!); }; list.Add(self); list.Add(() => b++);
        list.InvokeAll((_, _) => { }, NoSuppressed); list.InvokeAll((_, _) => { }, NoSuppressed);
        Assertions.Check(a == 1 && b == 2 && list.Count == 1, "self-removal takes effect next pass, the other handler keeps running");
    }
    static void NoAllocation()
    {
        var list = new HandlerList(); var n = 0; list.Add(() => n++); list.Add(() => n++);
        Action<Action, Exception> ignore = static (_, _) => { };  // created before measuring: the lambda's cached delegate is built on first use
        list.InvokeAll(ignore, NoSuppressed);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++) list.InvokeAll(ignore, NoSuppressed);
        Assertions.Check(GC.GetAllocatedBytesForCurrentThread() - before == 0, "InvokeAll allocates nothing");
    }
}
