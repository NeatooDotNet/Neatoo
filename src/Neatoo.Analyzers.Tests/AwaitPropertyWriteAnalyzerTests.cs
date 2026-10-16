using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.CodeFixes;

namespace Neatoo.Analyzers.Tests;

/// <summary>
/// NEATOO011: an assignment to a Neatoo property inside a rule or a verb is reported; the fix awaits Set{Name}.
/// </summary>
[TestClass]
public class AwaitPropertyWriteAnalyzerTests
{
    private static string Source(string orderMembers, string outside = "") => $$"""
        using System.Threading;
        using System.Threading.Tasks;
        using Neatoo.Rules;
        {{AnalyzerTestHelper.NeatooStubs}}

        namespace TestNamespace
        {
            public partial interface IOrder : Neatoo.IEntityBase
            {
                string? Status { get; set; }
                Task SetStatus(string? value);
            }

            public interface IPlainOrder : Neatoo.IEntityBase
            {
                string? Status { get; set; }
            }

            public class Holder { public int Value { get; set; } }

            [Neatoo.RemoteFactory.Factory]
            public partial class Order : Neatoo.EntityBase<Order>, IOrder, IPlainOrder
            {
                private string? _status;
                private int _count;
                private int _code;

                public partial string? Status { get; set; }
                public partial string? Status { get => _status; set => _status = value; }
                public Task SetStatus(string? value) { _status = value; return Task.CompletedTask; }

                public partial int Count { get; set; }
                public partial int Count { get => _count; set => _count = value; }
                public Task SetCount(int value) { _count = value; return Task.CompletedTask; }

                // A hand-written member named SetCode means the generator made no Set{Name} for Code.
                public partial int Code { get; set; }
                public partial int Code { get => _code; set => _code = value; }
                public void SetCode(string code) { }

                public partial Neatoo.EntityLazyLoad<string>? Lazy { get; set; }
                public partial Neatoo.EntityLazyLoad<string>? Lazy { get => null; set { } }

                public string? Plain { get; set; }

                {{orderMembers}}
            }

            {{outside}}
        }
        """;

    private static List<string> Messages(string source)
        => AnalyzerTestHelper.Analyze(new AwaitPropertyWriteAnalyzer(), source).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)).ToList();

    [TestMethod]
    public void AddActionLambda_Assignment_IsReported()
    {
        var messages = Messages(Source("""
            public Order() { RuleManager.AddAction(t => t.Status = "x", t => t.Count); }
            """));

        Assert.AreEqual(1, messages.Count);
        StringAssert.Contains(messages[0], "'Status' is written inside a rule");
        StringAssert.Contains(messages[0], "await SetStatus(...) instead");
    }

    [TestMethod]
    public void AsyncLambdaAndValidationLambda_Assignments_AreReported()
    {
        var messages = Messages(Source("""
            public Order()
            {
                RuleManager.AddActionAsync(async t => { await Task.Yield(); t.Count = 2; }, t => t.Status);
                RuleManager.AddValidation(t => { t.Status = "y"; return ""; }, t => t.Count);
            }
            """));

        Assert.AreEqual(2, messages.Count);
        Assert.IsTrue(messages.All(m => m.Contains("inside a rule")), string.Join(" | ", messages));
    }

    [TestMethod]
    public void CompoundAssignmentsAndIncrements_InVerb_AreReported()
    {
        var messages = Messages(Source("""
            public void Bump() { Count += 2; Count++; --Count; Status ??= "a"; }
            """));

        Assert.AreEqual(4, messages.Count, string.Join(" | ", messages));
        Assert.IsTrue(messages.All(m => m.Contains("inside a verb")));
    }

    [TestMethod]
    public void HelperCalledFromRuleOrVerb_IsReported_HelperCalledOnlyFromOperation_IsNot()
    {
        var messages = Messages(Source("""
            public Order()
            {
                RuleManager.AddAction(t => t.Recalculate(), t => t.Count);
                RuleManager.AddAction(Restamp, t => t.Code);
            }

            private void Recalculate() { Status = "recalculated"; }
            private void Restamp(Order t) { t.Count = 0; }

            public void Approve() { Mark(); }
            private void Mark() { Inner(); }
            private void Inner() { Status = "marked"; }

            [Neatoo.RemoteFactory.Fetch]
            public void Fetch(int id) { LoadFrom(id); }
            private void LoadFrom(int id) { Count = id; }
            """));

        Assert.AreEqual(3, messages.Count, string.Join(" | ", messages));
        Assert.IsTrue(messages.All(m => m.Contains("inside a method a rule or verb calls")), string.Join(" | ", messages));
    }

    [TestMethod]
    public void OverrideOfNeatooLifecycleHook_IsNotReported()
    {
        var messages = Messages(Source("""
            public override void FactoryComplete(int operation) { Status = "loaded"; }
            """));

        Assert.AreEqual(0, messages.Count, string.Join(" | ", messages));
    }

    [TestMethod]
    public void Fix_CompoundAssignmentAndIncrement_KeepTheValue()
    {
        var compound = Source("""
            public Order() { RuleManager.AddActionAsync(async t => { await Task.Yield(); t.Count += 2 * 3; }, t => t.Status); }
            """);
        var increment = Source("""
            public Order() { RuleManager.AddActionAsync(async t => { await Task.Yield(); t.Count++; }, t => t.Status); }
            """);

        var fixedCompound = AnalyzerTestHelper.ApplyFirstFix(new AwaitPropertyWriteAnalyzer(), new AwaitPropertyWriteCodeFixProvider(), compound);
        var fixedIncrement = AnalyzerTestHelper.ApplyFirstFix(new AwaitPropertyWriteAnalyzer(), new AwaitPropertyWriteCodeFixProvider(), increment);

        Assert.IsNotNull(fixedCompound);
        StringAssert.Contains(fixedCompound, "await t.SetCount(t.Count + (2 * 3));");
        Assert.AreEqual(0, AnalyzerTestHelper.CompileErrors(fixedCompound).Length);
        Assert.IsNotNull(fixedIncrement);
        StringAssert.Contains(fixedIncrement, "await t.SetCount(t.Count + 1);");
        Assert.AreEqual(0, AnalyzerTestHelper.CompileErrors(fixedIncrement).Length);
    }

    [TestMethod]
    public void ObjectInitializer_InsideVerb_IsNotReported()
    {
        // A verb is a reporting context; the initializer's member assignment is construction, not a write.
        var messages = Messages(Source("""
            public Order Copy() { var copy = new Order { Count = 1 }; return copy; }
            """));

        Assert.AreEqual(0, messages.Count, string.Join(" | ", messages));
    }

    [TestMethod]
    public void ExecuteOverride_Assignment_IsReported()
    {
        var messages = Messages(Source("", """
            public class SyncRule : RuleBase<Order>
            {
                protected override IRuleMessages Execute(Order target) { target.Count = 1; return null!; }
            }

            public class AsyncRule : AsyncRuleBase<IOrder>
            {
                protected override async Task<IRuleMessages> Execute(IOrder target, CancellationToken? token = null)
                {
                    await Task.Yield();
                    target.Status = "z";
                    return null!;
                }
            }
            """));

        Assert.AreEqual(2, messages.Count);
        Assert.IsTrue(messages.All(m => m.Contains("inside a rule")));
        Assert.IsFalse(messages[1].Contains("declare"), "IOrder is partial and has SetStatus: no reason is appended");
    }

    [TestMethod]
    public void Verb_Assignments_AreReported()
    {
        var messages = Messages(Source("""
            public void Approve() { Status = "Approved"; this.Count = 1; }
            """));

        Assert.AreEqual(2, messages.Count);
        Assert.IsTrue(messages.All(m => m.Contains("inside a verb")));
    }

    [TestMethod]
    public void OperationsConstructorsInitializersAndNonNeatooTargets_AreNotReported()
    {
        var messages = Messages(Source("""
            public Order() { Count = 1; var holder = new Holder { Value = 2 }; }

            [Neatoo.RemoteFactory.Create]
            public void Create() { Status = "New"; Count = 0; }

            [Neatoo.RemoteFactory.Fetch]
            public void Fetch(int id) { Count = id; }

            private void Load(int id) { Count = id; }

            public static void Make(Order o) { o.Count = 3; }

            public void Touch(Holder holder) { Plain = "p"; holder.Value = 4; Lazy = null; }
            """));

        Assert.AreEqual(0, messages.Count, string.Join(" | ", messages));
    }

    [TestMethod]
    public void MissingSetter_MessageSaysWhy()
    {
        var messages = Messages(Source("""
            public void Recode() { Code = 5; }
            """, """
            public class PlainRule : AsyncRuleBase<IPlainOrder>
            {
                protected override async Task<IRuleMessages> Execute(IPlainOrder target, CancellationToken? token = null)
                {
                    await Task.Yield();
                    target.Status = "q";
                    return null!;
                }
            }
            """));

        Assert.AreEqual(2, messages.Count);
        StringAssert.Contains(messages[0], "no SetCode was generated because a member with that name already exists");
        StringAssert.Contains(messages[1], "declare IPlainOrder partial so the generator adds SetStatus");
    }

    [TestMethod]
    public void Fix_AddActionLambda_BecomesAddActionAsyncWithAwaitedWrite()
    {
        var source = Source("""
            public Order() { RuleManager.AddAction(t => t.Status = "x", t => t.Count); }
            """);

        var fixedSource = AnalyzerTestHelper.ApplyFirstFix(new AwaitPropertyWriteAnalyzer(), new AwaitPropertyWriteCodeFixProvider(), source);

        Assert.IsNotNull(fixedSource, "a fix is offered");
        StringAssert.Contains(fixedSource, "RuleManager.AddActionAsync(async t => await t.SetStatus(\"x\"), t => t.Count)");
        Assert.AreEqual(0, AnalyzerTestHelper.CompileErrors(fixedSource).Length, "the fixed source compiles");
        Assert.AreEqual(0, Messages(fixedSource).Count, "nothing left to report");
    }

    [TestMethod]
    public void Fix_BlockBodiedAddActionLambda_BecomesAsync()
    {
        var source = Source("""
            public Order() { RuleManager.AddAction(t => { t.Status = "x"; }, t => t.Count); }
            """);

        var fixedSource = AnalyzerTestHelper.ApplyFirstFix(new AwaitPropertyWriteAnalyzer(), new AwaitPropertyWriteCodeFixProvider(), source);

        Assert.IsNotNull(fixedSource);
        StringAssert.Contains(fixedSource, "RuleManager.AddActionAsync(async t => { await t.SetStatus(\"x\"); }, t => t.Count)");
        Assert.AreEqual(0, AnalyzerTestHelper.CompileErrors(fixedSource).Length, "the fixed source compiles");
    }

    [TestMethod]
    public void FixAll_TwoWritesInOneAddActionLambda_BothAwaited()
    {
        var source = Source("""
            public Order() { RuleManager.AddAction(t => { t.Status = "x"; t.Count = 2; }, t => t.Code); }
            """);

        var fixedSource = AnalyzerTestHelper.ApplyFixAll(new AwaitPropertyWriteAnalyzer(), new AwaitPropertyWriteCodeFixProvider(), source);

        Assert.IsNotNull(fixedSource, "a Fix All is offered");
        StringAssert.Contains(fixedSource, "await t.SetStatus(\"x\");");
        StringAssert.Contains(fixedSource, "await t.SetCount(2);");
        StringAssert.Contains(fixedSource, "RuleManager.AddActionAsync(async t =>");
        Assert.AreEqual(0, AnalyzerTestHelper.CompileErrors(fixedSource).Length,
            "the merged fix compiles: " + string.Join(" | ", AnalyzerTestHelper.CompileErrors(fixedSource)));
        Assert.AreEqual(0, Messages(fixedSource).Count, "nothing left to report");
    }

    [TestMethod]
    public void Fix_StatementInAsyncLambda_IsAwaited()
    {
        var source = Source("""
            public Order() { RuleManager.AddActionAsync(async t => { await Task.Yield(); t.Count = 2; }, t => t.Status); }
            """);

        var fixedSource = AnalyzerTestHelper.ApplyFirstFix(new AwaitPropertyWriteAnalyzer(), new AwaitPropertyWriteCodeFixProvider(), source);

        Assert.IsNotNull(fixedSource);
        StringAssert.Contains(fixedSource, "await t.SetCount(2);");
        Assert.AreEqual(0, AnalyzerTestHelper.CompileErrors(fixedSource).Length);
    }

    [TestMethod]
    public void Fix_AsyncVerb_IsAwaited()
    {
        var source = Source("""
            public async Task Approve() { await Task.Yield(); Status = "Approved"; }
            """);

        var fixedSource = AnalyzerTestHelper.ApplyFirstFix(new AwaitPropertyWriteAnalyzer(), new AwaitPropertyWriteCodeFixProvider(), source);

        Assert.IsNotNull(fixedSource);
        StringAssert.Contains(fixedSource, "await SetStatus(\"Approved\");");
        Assert.AreEqual(0, AnalyzerTestHelper.CompileErrors(fixedSource).Length);
    }

    [TestMethod]
    public void Fix_NotOffered_ForSyncVerbOrMissingSetter()
    {
        var syncVerb = Source("""
            public void Approve() { Status = "Approved"; }
            """);
        var missingSetter = Source("""
            public async Task Recode() { await Task.Yield(); Code = 5; }
            """);

        Assert.AreEqual(1, Messages(syncVerb).Count, "the sync verb's write is reported");
        Assert.AreEqual(1, Messages(missingSetter).Count, "the write with no SetCode is reported");
        Assert.IsNull(AnalyzerTestHelper.ApplyFirstFix(new AwaitPropertyWriteAnalyzer(), new AwaitPropertyWriteCodeFixProvider(), syncVerb),
            "making a sync method async changes its signature; that is the author's call");
        Assert.IsNull(AnalyzerTestHelper.ApplyFirstFix(new AwaitPropertyWriteAnalyzer(), new AwaitPropertyWriteCodeFixProvider(), missingSetter),
            "no matching SetCode exists to call");
    }

    [TestMethod]
    public void Source_CompilesBeforeAnyFix()
    {
        var errors = AnalyzerTestHelper.CompileErrors(Source("""
            public Order() { RuleManager.AddAction(t => t.Status = "x", t => t.Count); }
            """));

        Assert.AreEqual(0, errors.Length, string.Join(" | ", errors.Select(e => e.ToString())));
    }
}
