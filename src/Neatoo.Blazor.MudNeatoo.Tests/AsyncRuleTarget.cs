using Neatoo.Internal;
using Neatoo.RemoteFactory;

namespace Neatoo.Blazor.MudNeatoo.Tests;

/// <summary>
/// An entity whose Name has an async rule that awaits a gate the test holds, standing in for
/// a slow server reply. The rule throws when the name is "throw". Built directly (no factory).
/// </summary>
[Factory]
public partial class AsyncRuleTarget : EntityBase<AsyncRuleTarget>
{
    public AsyncRuleTarget() : base(new EntityBaseServices<AsyncRuleTarget>())
    {
        RuleManager.AddValidationAsync(async t =>
        {
            await (t.Gate?.Invoke() ?? Task.CompletedTask);

            if (t.Name == "throw")
            {
                throw new InvalidOperationException("Rule failed");
            }

            return string.Empty;
        }, t => t.Name);
    }

    public partial string? Name { get; set; }

    public partial AsyncRuleTarget? Child { get; set; }

    public Func<Task>? Gate { get; set; }
}
