using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.Integration.Concepts.MetaStateTriggers;

/// <summary>
/// An entity whose rules trigger on its own meta state.
/// </summary>
public partial interface IMetaGate : IEntityRoot
{
    string? Name { get; set; }
    string? Status { get; set; }
    string? Lookup { get; set; }
    bool CanApprove { get; }
    bool HasUnsavedWork { get; }
    bool WasEverBusy { get; }
    string? LookupResult { get; }
    int CanApproveRuns { get; }
    int ModifiedRuns { get; }
    int BusyRuns { get; }
}

[Factory]
internal partial class MetaGate : EntityBase<MetaGate>, IMetaGate
{
    public MetaGate(IEntityBaseServices<MetaGate> services) : base(services)
    {
        RuleManager.AddValidation(t => string.IsNullOrEmpty(t.Name) ? "Name is required" : string.Empty, t => t.Name);

        // Admission: a fact a rule maintains, reading validity. Triggered by Status and by IsValid.
        RuleManager.AddActionAsync(async t =>
        {
            t.CanApproveRuns++;
            await t.SetCanApprove(t.Status == "Pending" && t.IsValid);
        }, t => t.Status, t => t.IsValid);

        RuleManager.AddActionAsync(async t =>
        {
            t.ModifiedRuns++;
            await t.SetHasUnsavedWork(t.IsModified);
        }, t => t.IsModified);

        // A sync rule on IsBusy: it cannot be async (NEATOO012), and its write does not make the object busy.
        RuleManager.AddAction(t =>
        {
            t.BusyRuns++;
            if (t.IsBusy)
            {
                t.WasEverBusy = true;
            }
        }, t => t.IsBusy);

        // Makes the object busy for a moment.
        RuleManager.AddActionAsync(async t =>
        {
            await Task.Delay(20);
            await t.SetLookupResult($"found {t.Lookup}");
        }, t => t.Lookup);
    }

    public partial string? Name { get; set; }
    public partial string? Status { get; set; }
    public partial string? Lookup { get; set; }
    public partial bool CanApprove { get; private set; }
    public partial bool HasUnsavedWork { get; private set; }
    public partial bool WasEverBusy { get; private set; }
    public partial string? LookupResult { get; private set; }

    public int CanApproveRuns { get; private set; }
    public int ModifiedRuns { get; private set; }
    public int BusyRuns { get; private set; }

    /// <summary>A valid, pending gate: the operation's own rules compute the baseline.</summary>
    [Create]
    public async Task Create()
    {
        Name = "Gate";
        Status = "Pending";
        await RunRules();
    }

    /// <summary>
    /// An operation that makes the gate invalid while paused: validity flips inside the pause,
    /// and no meta rule may run for it.
    /// </summary>
    [Create]
    public async Task CreateInvalid(bool invalid)
    {
        Status = "Pending";
        if (invalid)
        {
            await RunRules(nameof(Name));
        }
    }
}

/// <summary>
/// A child whose IsValid-triggered rule is async and really yields, so the meta rule's task is tracked.
/// </summary>
public partial interface IMetaChild : IEntityBase
{
    string? Code { get; set; }
    bool? CheckedValid { get; }
}

[Factory]
internal partial class MetaChild : EntityBase<MetaChild>, IMetaChild
{
    public MetaChild(IEntityBaseServices<MetaChild> services) : base(services)
    {
        RuleManager.AddValidation(t => string.IsNullOrEmpty(t.Code) ? "Code is required" : string.Empty, t => t.Code);

        RuleManager.AddActionAsync(async t =>
        {
            await Task.Delay(30);
            await t.SetCheckedValid(t.IsValid);
        }, t => t.IsValid);
    }

    public partial string? Code { get; set; }
    public partial bool? CheckedValid { get; private set; }

    [Create]
    public void Create() { Code = "C1"; }
}

public partial interface IMetaParent : IEntityRoot
{
    IMetaChild? Child { get; set; }
    int SavableRuns { get; }
}

[Factory]
internal partial class MetaParent : EntityBase<MetaParent>, IMetaParent
{
    public MetaParent(IEntityBaseServices<MetaParent> services) : base(services)
    {
        // Counts IsSavable flips: the other entity meta properties trigger rules the same way.
        RuleManager.AddAction(t => t.SavableRuns++, t => t.IsSavable);
    }

    public partial IMetaChild? Child { get; set; }
    public int SavableRuns { get; private set; }

    [Create]
    public void Create([Service] IMetaChildFactory childFactory) { Child = childFactory.Create(); }
}

/// <summary>
/// A meta rule that throws: the fault surfaces where the write that flipped validity was made.
/// </summary>
public partial interface IMetaFault : IEntityRoot
{
    string? Name { get; set; }
}

[Factory]
internal partial class MetaFault : EntityBase<MetaFault>, IMetaFault
{
    public MetaFault(IEntityBaseServices<MetaFault> services) : base(services)
    {
        RuleManager.AddValidation(t => string.IsNullOrEmpty(t.Name) ? "Name is required" : string.Empty, t => t.Name);
        RuleManager.AddAction(t =>
        {
            if (!t.IsValid)
            {
                throw new InvalidOperationException("the meta rule failed");
            }
        }, t => t.IsValid);
    }

    public partial string? Name { get; set; }

    [Create]
    public void Create() { Name = "ok"; }
}

/// <summary>
/// A sync rule on IsBusy whose write starts an async rule: WaitForTasks covers the async rule, and the
/// pair settles, because the next idle change finds Settled already true. (Whether the idle change is
/// seen inside the task sequence's completion callback depends on timing; the AsyncTasks unit test
/// AsyncTasksCompletionCallbackTests pins that window directly.)
/// </summary>
public partial interface IMetaSavable : IEntityRoot
{
    string? Lookup { get; set; }
    bool Settled { get; }
    string? SettledNote { get; }
}

[Factory]
internal partial class MetaSavable : EntityBase<MetaSavable>, IMetaSavable
{
    public MetaSavable(IEntityBaseServices<MetaSavable> services) : base(services)
    {
        RuleManager.AddAction(t =>
        {
            if (!t.IsBusy && t.Lookup != null && !t.Settled)
            {
                t.Settled = true;
            }
        }, t => t.IsBusy);

        RuleManager.AddActionAsync(async t =>
        {
            await Task.Delay(30);
            await t.SetSettledNote($"settled after {t.Lookup}");
        }, t => t.Settled);

        RuleManager.AddActionAsync(async t => await Task.Delay(20), t => t.Lookup);
    }

    public partial string? Lookup { get; set; }
    public partial bool Settled { get; private set; }
    public partial string? SettledNote { get; private set; }

    [Create]
    public void Create() { }
}
