using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neatoo.Analyzers.Tests;

/// <summary>
/// NEATOO012: an async rule triggered on IsBusy re-triggers itself; a sync one settles.
/// </summary>
[TestClass]
public class AsyncRuleOnIsBusyAnalyzerTests
{
    private static string Source(string gateMembers, string outside = "") => $$"""
        using System.Threading;
        using System.Threading.Tasks;
        using Neatoo.Rules;
        {{AnalyzerTestHelper.NeatooStubs}}

        namespace TestNamespace
        {
            [Neatoo.RemoteFactory.Factory]
            public partial class Gate : Neatoo.EntityBase<Gate>
            {
                private bool _wasBusy;
                private string? _status;

                public partial bool WasBusy { get; set; }
                public partial bool WasBusy { get => _wasBusy; set => _wasBusy = value; }
                public Task SetWasBusy(bool value) { _wasBusy = value; return Task.CompletedTask; }

                public partial string? Status { get; set; }
                public partial string? Status { get => _status; set => _status = value; }
                public Task SetStatus(string? value) { _status = value; return Task.CompletedTask; }

                {{gateMembers}}
            }

            {{outside}}
        }
        """;

    private static int Count(string source) => AnalyzerTestHelper.Analyze(new AsyncRuleOnIsBusyAnalyzer(), source).Count(d => d.Id == "NEATOO012");

    [TestMethod]
    public void AddActionAsync_TriggeredOnIsBusy_IsAnError()
    {
        var source = Source("""
            public Gate()
            {
                RuleManager.AddActionAsync(async t => await t.SetWasBusy(t.IsBusy), t => t.IsBusy);
                RuleManager.AddActionAsync(async t => await t.SetStatus("x"), t => t.Status, t => t.IsBusy);
                RuleManager.AddActionAsync(async t => await t.SetStatus("y"), t => t.IsSavable);
            }
            """);

        var diagnostics = AnalyzerTestHelper.Analyze(new AsyncRuleOnIsBusyAnalyzer(), source);

        Assert.AreEqual(3, diagnostics.Length);
        Assert.AreEqual(1, diagnostics.Count(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture).Contains("triggered on IsSavable")));
        Assert.IsTrue(diagnostics.All(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.IsTrue(diagnostics.All(d => d.Id == "NEATOO012"));
    }

    [TestMethod]
    public void AsyncRuleBase_TriggeredOnIsBusy_IsAnError()
    {
        var count = Count(Source("", """
            public class BusyRule : AsyncRuleBase<Gate>
            {
                public BusyRule() : base(t => t.IsBusy) { }
                protected override Task<IRuleMessages> Execute(Gate target, CancellationToken? token = null) => Task.FromResult<IRuleMessages>(null!);
            }

            public class LaterBusyRule : AsyncRuleBase<Gate>
            {
                public LaterBusyRule() { AddTriggerProperties(t => t.Status, t => t.IsBusy); }
                protected override Task<IRuleMessages> Execute(Gate target, CancellationToken? token = null) => Task.FromResult<IRuleMessages>(null!);
            }
            """));

        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public void SyncRulesOnIsBusy_AndAsyncRulesOnOtherMetaState_AreAllowed()
    {
        var source = Source("""
            public Gate()
            {
                RuleManager.AddAction(t => t.WasBusy = t.IsBusy, t => t.IsBusy);
                RuleManager.AddActionAsync(async t => await t.SetStatus("valid"), t => t.IsValid);
                RuleManager.AddActionAsync(async t => await t.SetStatus(t.IsBusy ? "busy" : "idle"), t => t.Status);
            }
            """, """
            public class SyncBusyRule : RuleBase<Gate>
            {
                public SyncBusyRule() : base(t => t.IsBusy) { }
                protected override IRuleMessages Execute(Gate target) => null!;
            }

            public class LaterSyncBusyRule : RuleBase<Gate>
            {
                public LaterSyncBusyRule() { AddTriggerProperties(t => t.IsBusy); }
                protected override IRuleMessages Execute(Gate target) => null!;
            }
            """);

        Assert.AreEqual(0, AnalyzerTestHelper.CompileErrors(source).Length, string.Join(" | ", AnalyzerTestHelper.CompileErrors(source)));
        Assert.AreEqual(0, Count(source), "a sync IsBusy rule settles; reading IsBusy in a body is not a trigger");
    }

    [TestMethod]
    public void Neatoo011_IsSilent_OnARuleClassOrMethodGroupTriggeredOnIsBusy()
    {
        var source = Source("""
            public Gate() { RuleManager.AddAction(Track, t => t.IsBusy); }
            private void Track(Gate t) { t.WasBusy = t.IsBusy; }
            """, """
            public class BusyTracker : RuleBase<Gate>
            {
                public BusyTracker() : base(t => t.IsBusy) { }
                protected override IRuleMessages Execute(Gate target) { target.WasBusy = target.IsBusy; return null!; }
            }

            public class StatusRule : RuleBase<Gate>
            {
                public StatusRule() : base(t => t.Status) { }
                protected override IRuleMessages Execute(Gate target) { target.WasBusy = false; return null!; }
            }
            """);

        var messages = AnalyzerTestHelper.Analyze(new AwaitPropertyWriteAnalyzer(), source)
            .Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)).ToList();

        Assert.AreEqual(1, messages.Count, string.Join(" | ", messages));
        StringAssert.Contains(messages[0], "inside a rule", "only the rule class not triggered on IsBusy is asked to await");
    }

    [TestMethod]
    public void Neatoo011_IsSilent_OnASyncIsBusyRulesWrite()
    {
        var source = Source("""
            public Gate()
            {
                RuleManager.AddAction(t => t.WasBusy = t.IsBusy, t => t.IsBusy);
                RuleManager.AddAction(t => t.Status = "x", t => t.Status);
            }
            """);

        var messages = AnalyzerTestHelper.Analyze(new AwaitPropertyWriteAnalyzer(), source)
            .Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)).ToList();

        Assert.AreEqual(1, messages.Count, string.Join(" | ", messages));
        StringAssert.Contains(messages[0], "'Status'", "the IsBusy rule's write is exempt; the other rule's write is not");
    }
}
