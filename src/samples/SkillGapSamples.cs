using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Samples;

// =============================================================================
// GAP ANALYSIS SAMPLES - Class-based rules with DI, FactoryComplete,
// object-per-property architecture
// =============================================================================

// -----------------------------------------------------------------------------
// Async class-based rule with dependency injection
// -----------------------------------------------------------------------------

#region skill-async-rule-class
public class SkillGapUniqueEmailRule : AsyncRuleBase<SkillGapEmployee>
{
    private readonly ISkillUserValidationService _validationService;

    public SkillGapUniqueEmailRule(ISkillUserValidationService validationService)
        : base(e => e.Email)  // trigger property
    {
        _validationService = validationService;
    }

    protected override async Task<IRuleMessages> Execute(
        SkillGapEmployee target, CancellationToken? token = null)
    {
        if (string.IsNullOrEmpty(target.Email))
            return None;

        var isUnique = await _validationService.IsEmailUniqueAsync(target.Email);
        return isUnique
            ? None
            : (nameof(target.Email), "Email is already in use").AsRuleMessages();
    }
}
#endregion

// -----------------------------------------------------------------------------
// Synchronous class-based rule
// -----------------------------------------------------------------------------

#region skill-sync-rule-class
public class SkillGapHireDateRule : RuleBase<SkillGapEmployee>
{
    public SkillGapHireDateRule()
        : base(e => e.HireDate, e => e.TermDate)  // trigger properties
    { }

    protected override IRuleMessages Execute(SkillGapEmployee target)
    {
        if (target.TermDate != default && target.TermDate <= target.HireDate)
        {
            return (nameof(target.TermDate), "Termination date must be after hire date")
                .AsRuleMessages();
        }
        return None;
    }
}
#endregion

// -----------------------------------------------------------------------------
// Entity registering class-based rules with DI
// -----------------------------------------------------------------------------

[Factory]
public partial class SkillGapEmployee : EntityBase<SkillGapEmployee>
{
    #region skill-rule-registration
    public SkillGapEmployee(
        IEntityBaseServices<SkillGapEmployee> services,
        ISkillUserValidationService validationService) : base(services)
    {
        // Class-based rules — inject deps into entity, pass to rule constructor
        RuleManager.AddRule(new SkillGapUniqueEmailRule(validationService));
        RuleManager.AddRule(new SkillGapHireDateRule());
    }
    #endregion

    [Required]
    public partial string Name { get; set; }

    [EmailAddress]
    public partial string Email { get; set; }

    public partial DateTime HireDate { get; set; }

    public partial DateTime TermDate { get; set; }

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(int id, string name, string email)
    {
        Name = name;
        Email = email;
        HireDate = DateTime.Today.AddYears(-1);
    }
}

// -----------------------------------------------------------------------------
// FactoryComplete lifecycle hook
// -----------------------------------------------------------------------------

[Factory]
public partial class SkillGapHookEntity : EntityBase<SkillGapHookEntity>
{
    public SkillGapHookEntity(IEntityBaseServices<SkillGapHookEntity> services)
        : base(services) { }

    public partial string Name { get; set; }

    public partial string Status { get; set; }

    #region skill-factory-complete
    public override void FactoryComplete(FactoryOperation operation)
    {
        base.FactoryComplete(operation); // ALWAYS call base first

        if (operation == FactoryOperation.Create)
        {
            // Logic after Create — e.g., set defaults
            Status = "Draft";
        }
        else if (operation == FactoryOperation.Fetch)
        {
            // Logic after Fetch — e.g., compute derived state from loaded data
        }
    }
    #endregion

    [Create]
    public void Create() { Name = ""; }

    [Fetch]
    public void Fetch(int id, string name, string status)
    {
        Name = name;
        Status = status;
    }
}

// =============================================================================
// TESTS
// =============================================================================

public class SkillGapSampleTests : SamplesTestBase
{
    #region skill-property-object-access
    [Fact]
    public void PropertyObjectAccess_IndexerReturnsMetadata()
    {
        var factory = GetRequiredService<ISkillGapEmployeeFactory>();
        var employee = factory.Create();
        employee.Email = "test@example.com";

        // Each property is backed by its own IValidateProperty object
        IValidateProperty emailProp = employee["Email"];
        bool valid = emailProp.IsValid;
        var errors = emailProp.PropertyMessages;

        Assert.NotNull(emailProp);
        Assert.True(valid);
        Assert.Empty(errors);
    }
    #endregion

    [Fact]
    public async Task AsyncRuleClass_ValidatesEmail()
    {
        var factory = GetRequiredService<ISkillGapEmployeeFactory>();
        var employee = factory.Create();

        // "taken" email triggers mock uniqueness failure
        employee.Email = "taken@example.com";
        await employee.WaitForTasks();

        Assert.False(employee["Email"].IsValid);
    }

    [Fact]
    public void SyncRuleClass_ValidatesDateRange()
    {
        var factory = GetRequiredService<ISkillGapEmployeeFactory>();
        var employee = factory.Fetch(1, "Test", "test@example.com");

        // TermDate before HireDate should fail
        employee.TermDate = employee.HireDate.AddDays(-1);

        Assert.False(employee["TermDate"].IsValid);
    }

    [Fact]
    public void FactoryComplete_SetsDefaults()
    {
        var factory = GetRequiredService<ISkillGapHookEntityFactory>();
        var entity = factory.Create();

        Assert.Equal("Draft", entity.Status);
    }
}
