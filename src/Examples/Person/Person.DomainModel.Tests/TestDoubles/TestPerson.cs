using DomainModel;
using Neatoo;

namespace DomainModel.Tests.TestDoubles;

/// <summary>
/// Test stub for Person that allows controlling IsSavable and counting RunRules calls.
/// Inherits from Person so all other methods call real implementations.
/// The server-side gate in Insert/Update reads IsValid, which comes from the real rules.
/// </summary>
internal class TestPerson : Person
{
    public bool IsSavableOverride { get; set; } = true;
    public int RunRulesCallCount { get; private set; }

    public TestPerson(IEntityBaseServices<Person> services, IUniqueNameRule rule,
                      IPersonPhoneListFactory personPhoneListFactory, IEntityLazyLoadFactory lazyLoadFactory)
        : base(services, rule, personPhoneListFactory, lazyLoadFactory)
    {
    }

    public override bool IsSavable => IsSavableOverride;

    public override Task RunRules(RunRulesFlag runRules = RunRulesFlag.All, CancellationToken? token = null)
    {
        RunRulesCallCount++;
        return Task.CompletedTask;
    }
}
