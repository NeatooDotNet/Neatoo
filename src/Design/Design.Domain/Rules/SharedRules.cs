// -----------------------------------------------------------------------------
// Design.Domain - Shared Rule Through an Interface
// -----------------------------------------------------------------------------
// One rule, two entity types. The rule is typed on a small interface the
// entities implement, so it is written once, registered in DI once, and
// injected into each entity's constructor. The rule calls the server through
// a [Remote, Execute] command delegate, never through a repository.
//
// DESIGN DECISION: The rule's type parameter is the shared interface, not an
// entity. RuleManager.AddRule<T> infers T from the rule argument, so an
// IRule<IHasUniqueCode> runs against any entity that implements IHasUniqueCode.
//
// DESIGN DECISION: The shared interface extends IValidateBase. That satisfies
// AsyncRuleBase<T>'s constraint, and every Neatoo base class already
// implements it, so the entities take on no new obligation.
//
// DID NOT DO THIS: A generic rule, UniqueCodeRule<TEntity>, closed per entity.
//
// WHY NOT: One DI registration per entity type, and nothing shared but the
// source text. The interface form has one registration and one type.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Rules;

namespace Design.Domain.Rules;

#region skill-shared-rule-interface
/// <summary>
/// What the rule needs from any entity it validates. Extends IValidateBase so
/// it satisfies AsyncRuleBase&lt;T&gt;'s constraint.
/// </summary>
public interface IHasUniqueCode : IValidateBase
{
    Guid Id { get; }
    string? Code { get; }
}
#endregion

#region skill-shared-rule-command
/// <summary>
/// The server call the rule makes. [Remote]: a client call crosses to the server.
/// </summary>
[Factory]
public static partial class CodeUniqueness
{
    [Remote]
    [Execute]
    private static Task<bool> _IsUnique(Guid id, string code, [Service] ICodeRepository repository)
    {
        return Task.FromResult(!repository.CodeExists(id, code));
    }
}
#endregion

#region skill-shared-rule-di-interface
/// <summary>
/// DI interface for the rule. Extends IRule&lt;IHasUniqueCode&gt; so an entity
/// can hand it straight to RuleManager.AddRule.
/// </summary>
public interface IUniqueCodeRule : IRule<IHasUniqueCode> { }
#endregion

#region skill-shared-rule-class
/// <summary>
/// Demonstrates: a rule typed on a shared interface, calling the server
/// through a command delegate.
/// </summary>
internal class UniqueCodeRule : AsyncRuleBase<IHasUniqueCode>, IUniqueCodeRule
{
    private readonly CodeUniqueness.IsUnique _isUnique;

    public UniqueCodeRule(CodeUniqueness.IsUnique isUnique) : base(t => t.Code)
    {
        _isUnique = isUnique;
    }

    protected override async Task<IRuleMessages> Execute(IHasUniqueCode target, CancellationToken? token = null)
    {
        if (string.IsNullOrWhiteSpace(target.Code))
        {
            return None;
        }

        return await _isUnique(target.Id, target.Code)
            ? None
            : (nameof(IHasUniqueCode.Code), $"Code '{target.Code}' is already in use").AsRuleMessages();
    }
}
#endregion

// =============================================================================
// Two roots that share the rule
// =============================================================================

public interface IWarehouse : IEntityRoot, IHasUniqueCode
{
    string? Name { get; set; }
    new string? Code { get; set; }
}

public interface ISupplier : IEntityRoot, IHasUniqueCode
{
    string? ContactEmail { get; set; }
    new string? Code { get; set; }
}

#region skill-shared-rule-entity
/// <summary>
/// Demonstrates: an entity implementing the shared interface and taking the
/// rule from DI.
/// </summary>
[Factory]
internal partial class Warehouse : EntityBase<Warehouse>, IWarehouse
{
    public partial Guid Id { get; set; }
    public partial string? Name { get; set; }
    public partial string? Code { get; set; }

    public Warehouse(IEntityBaseServices<Warehouse> services, IUniqueCodeRule uniqueCodeRule) : base(services)
    {
        RuleManager.AddRule(uniqueCodeRule);
    }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }
}
#endregion

[Factory]
internal partial class Supplier : EntityBase<Supplier>, ISupplier
{
    public partial Guid Id { get; set; }
    public partial string? ContactEmail { get; set; }
    public partial string? Code { get; set; }

    public Supplier(IEntityBaseServices<Supplier> services, IUniqueCodeRule uniqueCodeRule) : base(services)
    {
        RuleManager.AddRule(uniqueCodeRule);
    }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }
}

// =============================================================================
// Support Interfaces
// =============================================================================

/// <summary>
/// Server-side: is this code already used by another record?
/// </summary>
public interface ICodeRepository
{
    bool CodeExists(Guid excludeId, string code);
}
