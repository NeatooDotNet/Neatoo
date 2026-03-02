// -----------------------------------------------------------------------------
// Design.Domain - IEntityRoot vs IEntityBase Interface Pattern
// -----------------------------------------------------------------------------
// This file demonstrates the root vs child interface distinction.
// Aggregate roots extend IEntityRoot to expose IsSavable and Save().
// Child entities extend IEntityBase — no IsSavable, no Save().
// -----------------------------------------------------------------------------
//
// DESIGN DECISION: The user signals root vs child by choosing which interface
// their entity interface extends. No attributes, no inference, no RemoteFactory
// involvement.
//
// This eliminates the IsSavable trap where developers check IsSavable on child
// entities (always false due to !IsChild) and silently skip saves.
//
// COMMON MISTAKE: Extending IEntityRoot for child entities.
//
// WRONG:
//   public interface IOrderItem : IEntityRoot { ... }
//   // IOrderItem.IsSavable is always false (IsChild=true), confusing
//
// RIGHT:
//   public interface IOrderItem : IEntityBase { ... }
//   // No IsSavable on the interface — can't accidentally check it
// -----------------------------------------------------------------------------

using Neatoo;

namespace Design.Domain.Aggregates.OrderAggregate;

/// <summary>
/// Aggregate root interface — extends IEntityRoot.
/// Exposes IsSavable and Save() for the root entity.
/// </summary>
public interface IOrder : IEntityRoot
{
    int Id { get; }
    string? OrderNumber { get; set; }
    string? CustomerName { get; set; }
    DateTime OrderDate { get; set; }
    string? Status { get; set; }
    decimal TotalAmount { get; }
    OrderItemList? Items { get; }
}

/// <summary>
/// Child entity interface — extends IEntityBase only.
/// No IsSavable, no Save(). Child entities are saved through the aggregate root.
/// </summary>
public interface IOrderItem : IEntityBase
{
    int Id { get; }
    string? ProductName { get; set; }
    int Quantity { get; set; }
    decimal UnitPrice { get; set; }
    decimal LineTotal { get; }
}
