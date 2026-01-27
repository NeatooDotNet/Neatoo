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

// -----------------------------------------------------------------------------
// Collection Operations (documented in tests)
// -----------------------------------------------------------------------------

#region collections-add-item
// Add items to entity collections:
//
// var order = orderFactory.Create();
// var item = itemFactory.Create();
// item.ProductCode = "WIDGET-001";
// item.Price = 19.99m;
// item.Quantity = 2;
//
// order.Items.Add(item);
//
// // Item is now in the collection with parent relationship
// Assert.Single(order.Items);
// Assert.Same(order, item.Parent);
// Assert.True(item.IsChild);
#endregion

#region collections-remove-entity
// Remove from entity lists - existing items tracked for deletion:
//
// var order = orderFactory.Create();
// var item = itemFactory.Fetch(1, "WIDGET-001", 19.99m, 1);
// order.Items.Add(item);
// order.DoMarkUnmodified();  // Simulate loaded from DB
//
// order.Items.Remove(item);
//
// // Item is in DeletedList for persistence
// Assert.True(item.IsDeleted);
// Assert.Equal(1, order.Items.DeletedCount);
// Assert.Empty(order.Items);  // Removed from active list
#endregion

#region collections-remove-validate
// Remove from validate lists - items removed immediately:
//
// var list = new SkillCollPhoneNumberList();
// var phone = phoneFactory.Create();
// phone.Number = "555-1234";
//
// list.Add(phone);
// Assert.Single(list);
//
// list.Remove(phone);
// Assert.Empty(list);  // No deletion tracking
#endregion

#region collections-parent-cascade
// Parent relationship cascades to all items:
//
// var order = orderFactory.Create();
// var item1 = itemFactory.Create();
// var item2 = itemFactory.Create();
//
// order.Items.Add(item1);
// order.Items.Add(item2);
//
// // All items have parent set to aggregate root
// Assert.Same(order, item1.Parent);
// Assert.Same(order, item2.Parent);
//
// // Root walks to aggregate root
// Assert.Same(order, item1.Root);
// Assert.Same(order, item2.Root);
#endregion

#region collections-validation
// Validation state aggregates from children:
//
// var list = new SkillCollPhoneNumberList();
// var validPhone = phoneFactory.Create();
// validPhone.Number = "555-1234";
//
// var invalidPhone = phoneFactory.Create();
// // Number is empty - invalid
//
// list.Add(validPhone);
// Assert.True(list.IsValid);
//
// list.Add(invalidPhone);
// Assert.False(list.IsValid);    // Child makes list invalid
// Assert.True(list.IsSelfValid); // List itself has no validation
#endregion

#region collections-run-rules
// RunRules executes on all items:
//
// var list = new SkillCollPhoneNumberList();
// list.Add(phone1);
// list.Add(phone2);
//
// await list.RunRules(RunRulesFlag.All);
//
// // Each item's rules have been executed
// Assert.True(phone1.IsValid || !phone1.IsValid);  // State is determined
// Assert.True(phone2.IsValid || !phone2.IsValid);
#endregion

#region collections-iteration
// Standard collection operations supported:
//
// // Count
// Assert.Equal(3, order.Items.Count);
//
// // Indexer
// var first = order.Items[0];
//
// // foreach
// foreach (var item in order.Items)
// {
//     Console.WriteLine(item.ProductCode);
// }
//
// // LINQ
// var total = order.Items.Sum(i => i.Price * i.Quantity);
// var expensive = order.Items.Where(i => i.Price > 100);
#endregion

#region collections-deleted-list
// DeletedList tracks removed existing items:
//
// // Load existing items
// order.Items.Add(itemFactory.Fetch(1, "A", 10m, 1));
// order.Items.Add(itemFactory.Fetch(2, "B", 20m, 1));
// order.DoMarkUnmodified();
//
// // Remove one
// var removed = order.Items[0];
// order.Items.Remove(removed);
//
// // Active list has 1 item
// Assert.Single(order.Items);
//
// // DeletedList has the removed item
// Assert.Equal(1, order.Items.DeletedCount);
// Assert.True(removed.IsDeleted);
//
// // Removed NEW items are not tracked (never persisted)
// var newItem = itemFactory.Create();
// order.Items.Add(newItem);
// order.Items.Remove(newItem);
// Assert.Equal(1, order.Items.DeletedCount);  // Still 1
#endregion
