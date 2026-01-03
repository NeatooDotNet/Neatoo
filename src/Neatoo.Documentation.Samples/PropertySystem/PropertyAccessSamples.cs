/// <summary>
/// Code samples for docs/property-system.md - Property access section
///
/// Snippets in this file:
/// - docs:property-system:property-access
/// - docs:property-system:display-name
/// - docs:property-system:setvalue-loadvalue
///
/// Corresponding tests: PropertyAccessSamplesTests.cs
/// </summary>

using Neatoo.RemoteFactory;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Documentation.Samples.PropertySystem;

#region docs:property-system:property-access
/// <summary>
/// Entity demonstrating property access patterns.
/// </summary>
public partial interface IPropertyAccessDemo : IEntityBase
{
    string? Name { get; set; }
    string? Email { get; set; }
    int Age { get; set; }
}

[Factory]
internal partial class PropertyAccessDemo : EntityBase<PropertyAccessDemo>, IPropertyAccessDemo
{
    public PropertyAccessDemo(IEntityBaseServices<PropertyAccessDemo> services) : base(services) { }

    public partial string? Name { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format")]
    public partial string? Email { get; set; }

    public partial int Age { get; set; }

    [Create]
    public void Create() { }
}
#endregion

#region docs:property-system:display-name
/// <summary>
/// Entity demonstrating DisplayName attribute.
/// </summary>
public partial interface IDisplayNameDemo : IEntityBase
{
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? EmailAddress { get; set; }
}

[Factory]
internal partial class DisplayNameDemo : EntityBase<DisplayNameDemo>, IDisplayNameDemo
{
    public DisplayNameDemo(IEntityBaseServices<DisplayNameDemo> services) : base(services) { }

    [DisplayName("First Name*")]
    [Required]
    public partial string? FirstName { get; set; }

    [DisplayName("Last Name*")]
    [Required]
    public partial string? LastName { get; set; }

    [DisplayName("Email Address")]
    public partial string? EmailAddress { get; set; }

    [Create]
    public void Create() { }
}
#endregion

#region docs:property-system:setvalue-loadvalue
/// <summary>
/// Entity demonstrating SetValue vs LoadValue.
/// </summary>
public partial interface ILoadValueDemo : IEntityBase
{
    Guid Id { get; }
    string? Name { get; set; }
    DateTime? LastModified { get; set; }

    /// <summary>
    /// Load data from database using LoadValue (no modification tracking).
    /// </summary>
    void LoadFromDatabase(Guid id, string name, DateTime lastModified);
}

[Factory]
internal partial class LoadValueDemo : EntityBase<LoadValueDemo>, ILoadValueDemo
{
    public LoadValueDemo(IEntityBaseServices<LoadValueDemo> services) : base(services) { }

    public partial Guid Id { get; set; }
    public partial string? Name { get; set; }
    public partial DateTime? LastModified { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Demonstrates using LoadValue for identity fields.
    /// </summary>
    public void LoadFromDatabase(Guid id, string name, DateTime lastModified)
    {
        // LoadValue - silent set, no rules or modification tracking
        this[nameof(Id)].LoadValue(id);
        this[nameof(Name)].LoadValue(name);
        this[nameof(LastModified)].LoadValue(lastModified);
    }
}
#endregion
