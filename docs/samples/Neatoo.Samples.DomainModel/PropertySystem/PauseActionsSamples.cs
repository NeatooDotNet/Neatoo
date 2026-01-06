/// <summary>
/// Code samples for docs/property-system.md - Pausing actions section
///
/// Snippets in this file:
/// - docs:property-system:pause-actions
/// - docs:property-system:bulk-updates
///
/// Corresponding tests: PauseActionsSamplesTests.cs
/// </summary>

using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Samples.DomainModel.PropertySystem;

#region docs:property-system:pause-actions
/// <summary>
/// Entity demonstrating PauseAllActions pattern.
/// </summary>
public partial interface IBulkUpdateDemo : IEntityBase
{
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
    int Age { get; set; }
}

[Factory]
internal partial class BulkUpdateDemo : EntityBase<BulkUpdateDemo>, IBulkUpdateDemo
{
    public BulkUpdateDemo(IEntityBaseServices<BulkUpdateDemo> services) : base(services) { }

    [Required]
    public partial string? FirstName { get; set; }

    [Required]
    public partial string? LastName { get; set; }

    [EmailAddress]
    public partial string? Email { get; set; }

    [Range(0, 150)]
    public partial int Age { get; set; }

    [Create]
    public void Create() { }
}
#endregion

#region docs:property-system:bulk-updates
/// <summary>
/// Examples demonstrating bulk update patterns with PauseAllActions.
/// Note: PauseAllActions is on the concrete base class, not the interface.
/// </summary>
internal static class BulkUpdateExamples
{
    /// <summary>
    /// Update multiple properties with pausing for efficiency.
    /// Without pause: 4 rule executions, 4 PropertyChanged events.
    /// With pause: 0 rule executions during block, meta-state recalculated once.
    /// </summary>
    public static async Task PerformBulkUpdate(BulkUpdateDemo person)
    {
        using (person.PauseAllActions())
        {
            person.FirstName = "John";
            person.LastName = "Doe";
            person.Email = "john@example.com";
            person.Age = 30;
        }
        // Meta-state recalculated when disposed
        // Now run rules once after all changes
        await person.RunRules();
    }

    /// <summary>
    /// Load data from external source with pause.
    /// </summary>
    public static async Task LoadExternalData(
        BulkUpdateDemo customer,
        ExternalData externalData)
    {
        using (customer.PauseAllActions())
        {
            // Load data from external source without triggering validation
            customer.FirstName = externalData.FirstName;
            customer.LastName = externalData.LastName;
            customer.Email = externalData.Email;
            customer.Age = externalData.Age;
        }
        // Validate everything once at the end
        await customer.RunRules();
    }
}

/// <summary>
/// Mock external data for demo.
/// </summary>
public class ExternalData
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public int Age { get; set; }
}
#endregion
