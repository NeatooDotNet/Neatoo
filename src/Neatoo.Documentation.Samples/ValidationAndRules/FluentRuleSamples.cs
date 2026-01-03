/// <summary>
/// Code samples for docs/validation-and-rules.md - Fluent Rules section
///
/// Snippets injected into docs:
/// - docs:validation-and-rules:fluent-validation
/// - docs:validation-and-rules:fluent-validation-async
/// - docs:validation-and-rules:fluent-action
///
/// Compile-time validation only (supporting entity for above snippets):
/// - docs:validation-and-rules:fluent-rules-person
///
/// Corresponding tests: FluentRuleSamplesTests.cs
/// </summary>

using Neatoo.Documentation.Samples.SampleDomain;
using Neatoo.RemoteFactory;

namespace Neatoo.Documentation.Samples.ValidationAndRules;

#region docs:validation-and-rules:fluent-rules-person
/// <summary>
/// Sample person that demonstrates fluent rule registration.
/// </summary>
[Factory]
internal partial class PersonWithFluentRules : EntityBase<PersonWithFluentRules>, IPersonWithFluentRules
{
    public PersonWithFluentRules(IEntityBaseServices<PersonWithFluentRules> services,
                                  IEmailService emailService) : base(services)
    {
        #region docs:validation-and-rules:fluent-validation
        // Inline validation rule
        RuleManager.AddValidation(
            target => string.IsNullOrEmpty(target.Name) ? "Name is required" : "",
            t => t.Name);
        #endregion

        #region docs:validation-and-rules:fluent-validation-async
        // Async validation rule
        RuleManager.AddValidationAsync(
            async target => await emailService.EmailExistsAsync(target.Email!) ? "Email in use" : "",
            t => t.Email);
        #endregion

        #region docs:validation-and-rules:fluent-action
        // Action rule for calculated values
        RuleManager.AddAction(
            target => target.FullName = $"{target.FirstName} {target.LastName}",
            t => t.FirstName,
            t => t.LastName);
        #endregion
    }

    public partial Guid? Id { get; set; }
    public partial string? Name { get; set; }
    public partial string? FirstName { get; set; }
    public partial string? LastName { get; set; }
    public partial string? Email { get; set; }
    public partial string? FullName { get; set; }
    public partial string? ZipCode { get; set; }
    public partial decimal TaxRate { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }
}
#endregion

public partial interface IPersonWithFluentRules : IEntityBase
{
    Guid? Id { get; set; }
    string? Name { get; set; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
    string? FullName { get; set; }
    string? ZipCode { get; set; }
    decimal TaxRate { get; set; }
}

/// <summary>
/// Mock tax service for async action example.
/// </summary>
public interface ITaxService
{
    Task<decimal> GetRateAsync(string zipCode);
}

public class MockTaxService : ITaxService
{
    public Task<decimal> GetRateAsync(string zipCode)
    {
        // Simple mock implementation
        return Task.FromResult(zipCode?.StartsWith('9') == true ? 0.0875m : 0.06m);
    }
}
