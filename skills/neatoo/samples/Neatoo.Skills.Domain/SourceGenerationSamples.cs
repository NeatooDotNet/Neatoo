using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Rules;

namespace Neatoo.Skills.Domain;

// =============================================================================
// SOURCE GENERATION SAMPLES - Demonstrates what the Roslyn generator creates
// =============================================================================

// -----------------------------------------------------------------------------
// Partial Property Generation
// -----------------------------------------------------------------------------

#region api-generator-partial-property
/// <summary>
/// Entity demonstrating partial property generation.
/// The source generator completes these partial property declarations.
/// </summary>
[Factory]
public partial class SkillGenCustomer : ValidateBase<SkillGenCustomer>
{
    public SkillGenCustomer(IValidateBaseServices<SkillGenCustomer> services) : base(services) { }

    public partial string Name { get; set; }

    public partial string Email { get; set; }

    public partial DateTime BirthDate { get; set; }

    [Create]
    public void Create() { }
}
#endregion

// -----------------------------------------------------------------------------
// Factory Method Generation
// -----------------------------------------------------------------------------

#region api-generator-factory-methods
/// <summary>
/// Entity demonstrating factory method generation.
/// Source generator creates factory interface and implementation.
/// </summary>
[Factory]
public partial class SkillGenEntity : EntityBase<SkillGenEntity>
{
    public SkillGenEntity(IEntityBaseServices<SkillGenEntity> services) : base(services) { }

    public partial int Id { get; set; }

    public partial string Name { get; set; }

    [Create]
    public void Create()
    {
        Id = 0;
        Name = "";
    }

    [Fetch]
    public async Task FetchAsync(int id, [Service] ISkillGenRepository repository)
    {
        var data = await repository.FetchAsync(id);
        Id = data.Id;
        Name = data.Name;
    }

    [Insert]
    public Task InsertAsync([Service] ISkillGenRepository repository) =>
        repository.InsertAsync(Id, Name);

    [Update]
    public Task UpdateAsync([Service] ISkillGenRepository repository) =>
        repository.UpdateAsync(Id, Name);

    [Delete]
    public Task DeleteAsync([Service] ISkillGenRepository repository) =>
        repository.DeleteAsync(Id);
}
#endregion

// -----------------------------------------------------------------------------
// Save Factory Generation
// -----------------------------------------------------------------------------

#region api-generator-save-factory
/// <summary>
/// Entity demonstrating save factory generation.
/// When Insert/Update/Delete have only [Service] parameters,
/// the generator creates a unified SaveAsync method.
/// </summary>
[Factory]
public partial class SkillGenSaveEntity : EntityBase<SkillGenSaveEntity>
{
    public SkillGenSaveEntity(IEntityBaseServices<SkillGenSaveEntity> services) : base(services) { }

    public partial int Id { get; set; }

    public partial string Data { get; set; }

    public void DoMarkNew() => MarkNew();
    public void DoMarkOld() => MarkOld();

    [Create]
    public void Create()
    {
        Id = 0;
        Data = "";
    }

    [Insert]
    public Task InsertAsync([Service] ISkillGenRepository repository) =>
        repository.InsertAsync(Id, Data);

    [Update]
    public Task UpdateAsync([Service] ISkillGenRepository repository) =>
        repository.UpdateAsync(Id, Data);

    [Delete]
    public Task DeleteAsync([Service] ISkillGenRepository repository) =>
        repository.DeleteAsync(Id);
}
#endregion

// -----------------------------------------------------------------------------
// Rule ID Generation
// -----------------------------------------------------------------------------

#region api-generator-ruleid
/// <summary>
/// Entity demonstrating RuleId generation.
/// Lambda expressions in AddRule generate stable RuleId entries.
/// </summary>
[Factory]
public partial class SkillGenRuleEntity : ValidateBase<SkillGenRuleEntity>
{
    public SkillGenRuleEntity(IValidateBaseServices<SkillGenRuleEntity> services) : base(services)
    {
        RuleManager.AddValidation(
            entity => entity.Value > 0 ? "" : "Value must be positive",
            e => e.Value);

        RuleManager.AddValidation(
            entity => entity.Value <= 100 ? "" : "Value cannot exceed 100",
            e => e.Value);
    }

    public partial int Value { get; set; }

    [Create]
    public void Create() { }
}
#endregion

// -----------------------------------------------------------------------------
// SuppressFactory Attribute
// -----------------------------------------------------------------------------

#region api-attributes-suppressfactory
/// <summary>
/// [SuppressFactory] prevents factory generation.
/// Used for test classes, abstract bases, or manual factory implementations.
/// </summary>
[SuppressFactory]
public class SkillGenTestObject : ValidateBase<SkillGenTestObject>
{
    public SkillGenTestObject(IValidateBaseServices<SkillGenTestObject> services) : base(services) { }

    // Using traditional Getter/Setter instead of partial properties
    // (partial properties also work, but this shows the alternative)
    public string Name { get => Getter<string>(); set => Setter(value); }

    public int Amount { get => Getter<int>(); set => Setter(value); }
}
#endregion

// -----------------------------------------------------------------------------
// Repository Interface (for samples above)
// -----------------------------------------------------------------------------

public interface ISkillGenRepository
{
    Task<EntityData> FetchAsync(int id);
    Task InsertAsync(int id, string data);
    Task UpdateAsync(int id, string data);
    Task DeleteAsync(int id);
}

public record EntityData(int Id, string Name);
