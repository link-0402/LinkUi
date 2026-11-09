using LinkUi.Tests.Support;

namespace LinkUi.Tests;

/// <summary>The host's draw subscribers: a throwing or self-removing handler, error reports, no allocation.</summary>
internal static class HostScenarios
{
    public static void Run()
    {
        Scenarios.Run(OthersStillRun);
        Scenarios.Run(NewMessageReportedAgain);
        Scenarios.Run(UnsubscribeDuringCall);
        Scenarios.Run(NoAllocation);
    }

    static void OthersStillRun()  // Review Focus 3
    {
        var list = new HandlerList(); var ran = 0; var errors = 0;
        list.Add(() => throw new InvalidOperationException("boom")); list.Add(() => ran++);
        for (var i = 0; i < 3; i++) list.InvokeAll((_, _) => errors++);
        Assertions.Check(ran == 3 && errors == 1, "a throwing handler doesn't stop the next; same error reported once");
    }
    static void NewMessageReportedAgain()
    {
        var list = new HandlerList(); var n = 0; var errors = 0;
        list.Add(() => throw new InvalidOperationException("call " + (n++ % 2)));
        for (var i = 0; i < 4; i++) list.InvokeAll((_, _) => errors++);
        Assertions.Check(errors == 2, "two distinct messages, two reports");
    }
    static void UnsubscribeDuringCall()  // Review Focus 3
    {
        var list = new HandlerList(); var a = 0; var b = 0; Action? self = null;
        self = () => { a++; list.Remove(self!); }; list.Add(self); list.Add(() => b++);
        list.InvokeAll((_, _) => { }); list.InvokeAll((_, _) => { });
        Assertions.Check(a == 1 && b == 2 && list.Count == 1, "self-removal takes effect next pass, the other handler keeps running");
    }
    static void NoAllocation()
    {
        var list = new HandlerList(); var n = 0; list.Add(() => n++); list.Add(() => n++);
        Action<Action, Exception> ignore = static (_, _) => { };  // created before measuring: the lambda's cached delegate is built on first use
        list.InvokeAll(ignore);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++) list.InvokeAll(ignore);
        Assertions.Check(GC.GetAllocatedBytesForCurrentThread() - before == 0, "InvokeAll allocates nothing");
    }
}
