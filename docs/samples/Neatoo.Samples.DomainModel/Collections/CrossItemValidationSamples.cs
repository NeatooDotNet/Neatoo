/// <summary>
/// Code samples for docs/collections.md - Cross-item validation section
///
/// Snippets in this file:
/// - docs:collections:cross-item-validation
///
/// Corresponding tests: CrossItemValidationSamplesTests.cs
/// </summary>

using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Samples.DomainModel.Collections;

/// <summary>
/// Child entity for contact phones.
/// </summary>
public partial interface IContactPhone : IEntityBase
{
    Guid Id { get; }

    [Required(ErrorMessage = "Phone number required")]
    string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Phone type required")]
    string? PhoneType { get; set; }
}

[Factory]
internal partial class ContactPhone : EntityBase<ContactPhone>, IContactPhone
{
    public ContactPhone(IEntityBaseServices<ContactPhone> services) : base(services) { }

    public partial Guid Id { get; set; }
    public partial string? PhoneNumber { get; set; }
    public partial string? PhoneType { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }
}

#region docs:collections:cross-item-validation
/// <summary>
/// List that re-validates siblings when properties change.
/// </summary>
public interface IContactPhoneList : IEntityListBase<IContactPhone>
{
    IContactPhone AddPhone();
}

[Factory]
internal class ContactPhoneList : EntityListBase<IContactPhone>, IContactPhoneList
{
    private readonly IContactPhoneFactory _phoneFactory;

    public ContactPhoneList([Service] IContactPhoneFactory phoneFactory)
    {
        _phoneFactory = phoneFactory;
    }

    public IContactPhone AddPhone()
    {
        var phone = _phoneFactory.Create();
        Add(phone);
        return phone;
    }

    /// <summary>
    /// Re-validate siblings when PhoneType changes to enforce uniqueness.
    /// </summary>
    protected override async Task HandleNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        await base.HandleNeatooPropertyChanged(eventArgs);

        // When PhoneType changes, re-validate all other items for uniqueness
        if (eventArgs.PropertyName == nameof(IContactPhone.PhoneType))
        {
            if (eventArgs.Source is IContactPhone changedPhone)
            {
                // Re-run rules on all OTHER items
                await Task.WhenAll(
                    this.Except([changedPhone])
                        .Select(phone => phone.RunRules()));
            }
        }
    }

    [Create]
    public void Create() { }
}
#endregion
