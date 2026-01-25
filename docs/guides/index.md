[↑ Up](../index.md)

# Guides

The Neatoo guides cover core framework concepts and integration patterns. Each guide progresses from basic usage to advanced scenarios, focusing on Neatoo-specific patterns like source-generated properties, validation rules, and parent-child aggregate graphs.

## Core Concepts

### [Async](async.md)
Async validation rules, WaitForAllTasksAsync, CancellationToken support, and task coordination.

### [Business Rules](business-rules.md)
Business rule attributes, cross-property validation, aggregate-level rules, rule execution order, and async business rules.

### [Change Tracking](change-tracking.md)
IsDirty tracking, MarkClean/MarkDirty, cascade to parent, change tracking in collections, and dirty state relationship with validation.

### [Collections](collections.md)
EntityListBase for entity collections, ValidateListBase for value object collections, Add/Remove operations, parent property cascade, and collection validation.

### [Entities](entities.md)
EntityBase vs ValidateBase, aggregate root pattern, identity and IsNew, entity lifecycle (New, Fetch, Save, Delete), and entity state management.

### [Parent-Child](parent-child.md)
Parent property behavior, child entity lifecycle, cascade validation, cascade dirty state, aggregate boundaries, and ContainingList property.

### [Properties](properties.md)
Getter/Setter pattern, source-generated backing fields, PropertyChanged events, NeatooPropertyChanged vs INotifyPropertyChanged, LoadValue vs direct assignment, and meta-properties.

### [Validation](validation.md)
ValidateBase inheritance, property declarations with Getter/Setter, built-in validation attributes, custom validation rules, RunRulesAsync, error messages, and PauseAllActions for batching.

## Integration

### [Blazor](blazor.md)
MudNeatoo Blazor integration, component integration, property binding, validation display, form integration, and change tracking UI.

### [Remote Factory](remote-factory.md)
RemoteFactory overview, factory method generation, client-server serialization, Fetch/Save patterns, DTOs vs domain models, and dependency injection integration.

---

**UPDATED:** 2026-01-24
