// -----------------------------------------------------------------------------
// Design.Domain - Interface-First Design Pattern
// -----------------------------------------------------------------------------
// This file demonstrates the interface-first pattern that is the central pillar
// of Neatoo's API design. Every entity and list gets a matched public interface.
// Concrete classes are internal. All references use interfaces.
//
// This file also demonstrates the root vs child interface distinction.
// Aggregate roots extend IEntityRoot to expose IsSavable and Save().
// Child entities extend IEntityBase — no IsSavable, no Save().
// List interfaces extend IEntityListBase<IChild> — parameterized on child interface.
// -----------------------------------------------------------------------------
//
// DESIGN DECISION: Interface-first design is the mechanism that makes the
// IEntityRoot vs IEntityBase separation work. EntityBase<T> implements BOTH
// IEntityBase and IEntityRoot, so IsSavable and Save() exist on every concrete.
// Since concretes are internal, consumers only see what the interface exposes.
// A child entity's interface extends IEntityBase (no IsSavable) — so it's
// invisible to consumers, even sibling entities within the same aggregate.
//
// DESIGN DECISION: The user signals root vs child by choosing which interface
// their entity interface extends. No attributes, no inference, no RemoteFactory
// involvement.
//
// DESIGN DECISION: List interfaces are parameterized on the CHILD INTERFACE,
// not the concrete. This ensures consumers only interact with IOrderItem, never
// the internal OrderItem class. The generator picks up the interface type.
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
//
// COMMON MISTAKE: Exposing concrete types in interface properties.
//
// WRONG:
//   public interface IOrder : IEntityRoot
//   {
//       OrderItemList? Items { get; }   // Concrete — leaks implementation
//   }
//
// RIGHT:
//   public interface IOrder : IEntityRoot
//   {
//       IOrderItemList? Items { get; }  // Interface — hides concrete
//   }
//
// COMMON MISTAKE: Parameterizing list on concrete class.
//
// WRONG:
//   public class OrderItemList : EntityListBase<OrderItem> { }
//
// RIGHT:
//   internal class OrderItemList : EntityListBase<IOrderItem>, IOrderItemList { }
// -----------------------------------------------------------------------------

using Neatoo;

namespace Design.Domain.Aggregates.OrderAggregate;

/// <summary>
/// Aggregate root interface — extends IEntityRoot.
/// Exposes IsSavable and Save() for the root entity.
/// All property types use interfaces, never concretes.
/// </summary>
public interface IOrder : IEntityRoot
{
    int Id { get; }
    string? OrderNumber { get; set; }
    string? CustomerName { get; set; }
    DateTime OrderDate { get; set; }
    string? Status { get; set; }
    decimal TotalAmount { get; }
    IOrderItemList? Items { get; }
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

/// <summary>
/// List interface — extends IEntityListBase parameterized on child INTERFACE.
/// Lists never expose IsSavable — they are always saved through the aggregate root.
/// </summary>
public interface IOrderItemList : IEntityListBase<IOrderItem>
{
    /// <summary>
    /// Test helper: Exposes the count of items in DeletedList.
    /// </summary>
    int DeletedCount { get; }
}
