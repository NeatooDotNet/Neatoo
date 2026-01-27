using Neatoo;
using Neatoo.RemoteFactory;

namespace Neatoo.Skills.Domain;

// =============================================================================
// COLLECTION SAMPLES - Demonstrates EntityListBase and ValidateListBase
// =============================================================================

// -----------------------------------------------------------------------------
// Entity List Definition
// -----------------------------------------------------------------------------

/// <summary>
/// Order line item entity for collection samples.
/// </summary>
public interface ISkillCollOrderItem : IEntityBase
{
    int Id { get; set; }
    string ProductCode { get; set; }
    decimal Price { get; set; }
    int Quantity { get; set; }
}

[Factory]
public partial class SkillCollOrderItem : EntityBase<SkillCollOrderItem>, ISkillCollOrderItem
{
    public SkillCollOrderItem(IEntityBaseServices<SkillCollOrderItem> services) : base(services)
    {
        RuleManager.AddValidation(
            item => !string.IsNullOrEmpty(item.ProductCode) ? "" : "Product code is required",
            i => i.ProductCode);

        RuleManager.AddValidation(
            item => item.Quantity > 0 ? "" : "Quantity must be positive",
            i => i.Quantity);
    }

    public partial int Id { get; set; }
    public partial string ProductCode { get; set; }
    public partial decimal Price { get; set; }
    public partial int Quantity { get; set; }

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(int id, string productCode, decimal price, int quantity)
    {
        Id = id;
        ProductCode = productCode;
        Price = price;
        Quantity = quantity;
    }
}

/// <summary>
/// Collection interface for order items.
/// </summary>
public interface ISkillCollOrderItemList : IEntityListBase<ISkillCollOrderItem>
{
    int DeletedCount { get; }
}

#region collections-entity-list-definition
/// <summary>
/// EntityListBase for order line items.
/// Tracks deletions for persistence and cascades parent relationship.
/// </summary>
public class SkillCollOrderItemList : EntityListBase<ISkillCollOrderItem>, ISkillCollOrderItemList
{
    // DeletedList tracks removed existing items for DELETE persistence
    public int DeletedCount => DeletedList.Count;
}
#endregion

/// <summary>
/// Order aggregate root containing line items.
/// </summary>
[Factory]
public partial class SkillCollOrder : EntityBase<SkillCollOrder>
{
    public SkillCollOrder(IEntityBaseServices<SkillCollOrder> services) : base(services)
    {
        ItemsProperty.LoadValue(new SkillCollOrderItemList());
    }

    public partial int Id { get; set; }
    public partial string OrderNumber { get; set; }
    public partial DateTime OrderDate { get; set; }
    public partial ISkillCollOrderItemList Items { get; set; }

    // Expose protected method for samples
    public void DoMarkUnmodified() => MarkUnmodified();

    [Create]
    public void Create()
    {
        OrderDate = DateTime.Today;
    }

    [Fetch]
    public void Fetch(int id, string orderNumber)
    {
        Id = id;
        OrderNumber = orderNumber;
        OrderDate = DateTime.Today;
    }
}

// -----------------------------------------------------------------------------
// Validate List Definition
// -----------------------------------------------------------------------------

/// <summary>
/// Phone number value object for validate list samples.
/// </summary>
public interface ISkillCollPhoneNumber : IValidateBase
{
    string Number { get; set; }
    string PhoneType { get; set; }
}

[Factory]
public partial class SkillCollPhoneNumber : ValidateBase<SkillCollPhoneNumber>, ISkillCollPhoneNumber
{
    public SkillCollPhoneNumber(IValidateBaseServices<SkillCollPhoneNumber> services) : base(services)
    {
        RuleManager.AddValidation(
            phone => !string.IsNullOrEmpty(phone.Number) ? "" : "Phone number is required",
            p => p.Number);
    }

    public partial string Number { get; set; }
    public partial string PhoneType { get; set; }

    [Create]
    public void Create() { }
}

#region collections-validate-list-definition
/// <summary>
/// ValidateListBase for phone numbers (value object collection).
/// No deletion tracking - items are simply removed.
/// </summary>
public class SkillCollPhoneNumberList : ValidateListBase<ISkillCollPhoneNumber>
{
}
#endregion
