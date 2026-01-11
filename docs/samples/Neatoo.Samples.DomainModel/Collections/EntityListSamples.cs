/// <summary>
/// Code samples for docs/collections.md - EntityListBase section
///
/// Snippets injected into docs:
/// - docs:collections:interface-definition
/// - docs:collections:list-implementation
/// - docs:collections:fetch-operation
/// - docs:collections:update-operation
///
/// Compile-time validation only (child entity for list samples):
/// - docs:collections:child-item
///
/// Corresponding tests: EntityListSamplesTests.cs
/// </summary>

using Neatoo.RemoteFactory;

namespace Neatoo.Samples.DomainModel.Collections;

/// <summary>
/// Mock EF entity for phone numbers.
/// </summary>
public class PhoneEntity
{
    public Guid Id { get; set; }
    public string PhoneNumber { get; set; } = "";
    public string PhoneType { get; set; } = "";
}

#region child-item
/// <summary>
/// Child entity for phone numbers.
/// </summary>
public partial interface IPhone : IEntityBase
{
    Guid Id { get; }
    string? PhoneNumber { get; set; }
    string? PhoneType { get; set; }
}

[Factory]
internal partial class Phone : EntityBase<Phone>, IPhone
{
    public Phone(IEntityBaseServices<Phone> services) : base(services) { }

    public partial Guid Id { get; set; }
    public partial string? PhoneNumber { get; set; }
    public partial string? PhoneType { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }

    [Fetch]
    public void Fetch(PhoneEntity entity)
    {
        Id = entity.Id;
        PhoneNumber = entity.PhoneNumber;
        PhoneType = entity.PhoneType;
    }

    [Insert]
    public void Insert(PhoneEntity entity)
    {
        entity.Id = Id;
        entity.PhoneNumber = PhoneNumber ?? "";
        entity.PhoneType = PhoneType ?? "";
    }

    [Update]
    public void Update(PhoneEntity entity)
    {
        if (this[nameof(PhoneNumber)].IsModified)
            entity.PhoneNumber = PhoneNumber ?? "";
        if (this[nameof(PhoneType)].IsModified)
            entity.PhoneType = PhoneType ?? "";
    }
}
#endregion

#region interface-definition
/// <summary>
/// Collection interface with domain-specific methods.
/// </summary>
public interface IPhoneList : IEntityListBase<IPhone>
{
    IPhone AddPhoneNumber();
    void RemovePhoneNumber(IPhone phone);
}
#endregion

#region list-implementation
/// <summary>
/// EntityListBase implementation with factory injection.
/// </summary>
[Factory]
internal class PhoneList : EntityListBase<IPhone>, IPhoneList
{
    private readonly IPhoneFactory _phoneFactory;

    public PhoneList([Service] IPhoneFactory phoneFactory)
    {
        _phoneFactory = phoneFactory;
    }

    public IPhone AddPhoneNumber()
    {
        var phone = _phoneFactory.Create();
        Add(phone);  // Marks as child, sets parent
        return phone;
    }

    public void RemovePhoneNumber(IPhone phone)
    {
        Remove(phone);  // Marks for deletion if not new
    }

    #region fetch-operation
    [Fetch]
    public void Fetch(IEnumerable<PhoneEntity> entities,
                      [Service] IPhoneFactory phoneFactory)
    {
        foreach (var entity in entities)
        {
            var phone = phoneFactory.Fetch(entity);
            Add(phone);
        }
    }
    #endregion

    #region collections-update-operation
    [Update]
    public void Update(ICollection<PhoneEntity> entities,
                       [Service] IPhoneFactory phoneFactory)
    {
        // Process all items including deleted ones
        foreach (var phone in this.Union(DeletedList))
        {
            PhoneEntity entity;

            if (phone.IsNew)
            {
                // Create new EF entity
                entity = new PhoneEntity();
                entities.Add(entity);
            }
            else
            {
                // Find existing EF entity
                entity = entities.Single(e => e.Id == phone.Id);
            }

            if (phone.IsDeleted)
            {
                // Remove from EF collection
                entities.Remove(entity);
            }
            else
            {
                // Save the item (insert or update)
                phoneFactory.Save(phone, entity);
            }
        }
    }
    #endregion

    [Create]
    public void Create() { }
}
#endregion
