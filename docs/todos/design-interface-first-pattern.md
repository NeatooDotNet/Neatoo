# Update Design Projects to Interface-First Pattern

**Status:** Complete
**Priority:** High
**Created:** 2026-03-02
**Last Updated:** 2026-03-02
**Completed:** 2026-03-02

---

## Problem

The CLAUDE.md has been updated with a "Central Pillar: Interface-First Design" section establishing that every Neatoo entity and list must have a matched public interface, with concrete classes being `internal`. The Design projects in `src/Design/` are the authoritative reference for Neatoo's API design, but they currently violate this pattern in multiple places:

- Concrete classes are `public` instead of `internal`
- Properties and list type parameters reference concretes instead of interfaces
- Several entities lack matched public interfaces entirely

Since the Design project is documentation-as-code, it must exemplify the correct pattern.

## Solution

Update all entity and list classes in `src/Design/` to follow the interface-first pattern:
1. Add missing interfaces for all entities and lists
2. Make concrete classes `internal`
3. Change all property types and list type parameters to use interfaces
4. Update tests to use interface types
5. Add DESIGN DECISION comments explaining the pattern

---

## Plans

- [Design Interface-First Pattern Plan](../plans/design-interface-first-pattern.md)

---

## Tasks

- [x] OrderAggregate: Add IOrderItemList, fix IOrder.Items type, make classes internal, implement IOrderItem
- [x] Employee/Address: Add IEmployee, IAddress, IAddressList interfaces
- [x] FactoryOperations: Add interfaces for all demo entities, make classes internal
- [x] CommonGotchas: Add interfaces for all gotcha demos, make classes internal
- [x] BaseClasses/AllBaseClasses.cs: Make all 4 demo classes internal with interfaces
- [x] PropertySystem: Fix IPropertyInterfaces.cs to match actual class properties, make all classes internal
- [x] Rules: Add IRulesInterfaces.cs, make entity and rule classes internal
- [x] ErrorHandling: Add IErrorHandlingInterfaces.cs, make classes internal
- [x] Generators: Add IGeneratorInterfaces.cs, make GeneratorDemo internal
- [x] ValueObjects: Add IValueObjectInterfaces.cs, make EmployeeList/EmployeeListItem internal
- [x] Update Design.Tests to use interface types (TestInfrastructure assembly reference)
- [x] Build and test after each change group

---

## Progress Log

- 2026-03-02: Created todo and starting implementation
- 2026-03-02: Phase 1 - OrderAggregate updated (IOrderItemList added, classes internal)
- 2026-03-02: Phase 2 - Employee/Address updated (IEmployee, IAddress, IAddressList interfaces)
- 2026-03-02: Phase 3 - FactoryOperations updated (all demo entities have interfaces)
- 2026-03-02: Phase 4 - CommonGotchas updated (all gotcha demos have interfaces)
- 2026-03-02: Phase 5 - BaseClasses, PropertySystem, Rules, ErrorHandling, Generators, ValueObjects all updated
- 2026-03-02: Phase 6 - Tests verified: 89/89 Design tests pass, 1729 Neatoo unit tests pass, 55 Person tests pass
- 2026-03-02: All work complete

---

## Results / Conclusions

All Design.Domain entity and list classes now follow the interface-first pattern:

**Interface files created:**
- `BaseClasses/IBaseClassInterfaces.cs` - IDemoValueObject, IDemoEntity, IDemoValueObjectList, IDemoEntityList
- `Aggregates/OrderAggregate/IOrderInterfaces.cs` - IOrder, IOrderItem, IOrderItemList (updated)
- `Entities/IEntityInterfaces.cs` - IEmployee, IAddress, IAddressList
- `FactoryOperations/IFactoryInterfaces.cs` - All factory demo interfaces
- `CommonGotchas/IGotchaInterfaces.cs` - All gotcha demo interfaces
- `PropertySystem/IPropertyInterfaces.cs` - All property demo interfaces
- `Rules/IRulesInterfaces.cs` - IRuleBasicsDemo, IFluentRulesDemo, ITriggerPatternsDemo, IAsyncRulesDemo
- `ErrorHandling/IErrorHandlingInterfaces.cs` - IValidationFailureDemo
- `Generators/IGeneratorInterfaces.cs` - IGeneratorDemo
- `ValueObjects/IValueObjectInterfaces.cs` - IEmployeeListItem, IEmployeeList

**Pattern applied across all entities:**
- Concrete classes made `internal`
- Interface implementations added (e.g., `internal partial class Order : EntityBase<Order>, IOrder`)
- Cross-entity property types changed to interfaces (e.g., `IOrderItemList? Items` instead of `OrderItemList? Items`)
- List type parameters use child interfaces (e.g., `EntityListBase<IOrderItem>` instead of `EntityListBase<OrderItem>`)
- Rule classes referencing internal entities also made `internal`
- Static command classes (`[Execute]` pattern) correctly left as `public static partial` -- they are not entities

**Test changes:**
- `TestInfrastructure.cs`: Changed assembly reference from concrete `DemoValueObject` to interface `IDemoValueObject`
- `OrderAggregateTests.cs`: Updated `ChildItem_CannotSaveIndependently` test to document that `IOrderItem` (extending `IEntityBase`, not `IEntityRoot`) correctly does not expose `IsSavable`

**Verification:** All 89 Design tests pass, all 1729 Neatoo unit tests pass, all 55 Person tests pass. Build succeeds with zero warnings/errors.
