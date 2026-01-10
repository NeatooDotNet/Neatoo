using DomainModel;
using Neatoo;

namespace DomainModel.Tests.TestDoubles;

/// <summary>
/// Test stub for Person that allows controlling IsSavable and RunRules behavior.
/// Inherits from Person so all other methods call real implementations.
/// </summary>
internal class TestPerson : Person
{
    public bool IsSavableOverride { get; set; } = true;
    public int RunRulesCallCount { get; private set; }

    public TestPerson(IEntityBaseServices<Person> services, IUniqueNameRule rule)
        : base(services, rule)
    {
    }

    public override bool IsSavable => IsSavableOverride;

    public override Task RunRules(RunRulesFlag runRules = RunRulesFlag.All, CancellationToken? token = null)
    {
        RunRulesCallCount++;
        return Task.CompletedTask;
    }
}
