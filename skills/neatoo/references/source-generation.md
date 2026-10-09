# Source Generation

Neatoo uses Roslyn source generators at compile time. **Understanding source generation is not required to use Neatoo** — declare `partial` properties and factory methods, and the generators handle the rest. This page is for curiosity and debugging.

## What Gets Generated

For each `partial` property, Neatoo.BaseGenerator creates an accessor over the property object, a getter and setter, and the registration in `InitializePropertyBackingFields`. The accessor is typed `IValidateProperty<T>` on both `ValidateBase` and `EntityBase`; on an `EntityBase` the property factory creates an entity property, which adds per-property `IsModified` and `MarkSelfUnmodified()`. `LoadValue()` is on `IValidateProperty`.

<!-- snippet: skill-generated-property-shape -->
<a id='snippet-skill-generated-property-shape'></a>
```cs
// For this declaration:
//   public partial string? Name { get; set; }
//
// GENERATOR BEHAVIOR: Neatoo.BaseGenerator produces the same shape for
// ValidateBase and EntityBase (from DemoEntity.g.cs):
//
//   protected IValidateProperty<string?> NameProperty
//       => (IValidateProperty<string?>)PropertyManager[nameof(Name)]!;
//
//   public partial string? Name
//   {
//       get => NameProperty.Value;
//       set
//       {
//           NameProperty.Value = value;
//           if (!NameProperty.Task.IsCompleted)
//           {
//               Parent?.AddChildTask(NameProperty.Task);
//               RunningTasks.AddTask(NameProperty.Task);
//           }
//       }
//   }
//
//   protected override void InitializePropertyBackingFields(IPropertyFactory<T> factory)
//   {
//       PropertyManager.Register(factory.Create<string?>(this, nameof(Name)));
//   }
//
// On an EntityBase the factory creates an entity property (modification
// tracking); the accessor is still typed IValidateProperty<T>. The real output
// is on disk under Generated/Neatoo.BaseGenerator/.
```
<sup><a href='/src/Design/Design.Domain/PropertySystem/PropertyBasics.cs#L24-L56' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-generated-property-shape' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

For each class with `[Factory]`, RemoteFactory creates a factory interface (`IMyEntityFactory`) with methods matching the `[Create]`, `[Fetch]`, etc. methods, and a `Save(target, ...)` when the class has `[Insert]`, `[Update]` or `[Delete]`.

## IFactorySave — How entity.Save() Works

When an entity's `[Insert]`, `[Update]`, and `[Delete]` methods have **no non-service parameters**, the generator creates an `IFactorySave<T>` implementation. This is injected into the entity's `Factory` property via `IEntityBaseServices<T>`, enabling `entity.Save()` to route to the correct method based on state:

<!-- snippet: skill-entity-crud -->
<a id='snippet-skill-entity-crud'></a>
```cs
/// <summary>
/// Demonstrates: EntityBase&lt;T&gt; for persistent domain entities.
///
/// Key points:
/// - Inherits all ValidateBase capabilities (validation, rules, busy tracking)
/// - Adds IsNew/IsModified/IsDeleted for persistence state
/// - IsSavable = (IsModified || IsNew) &amp;&amp; IsValid &amp;&amp; !IsBusy
/// - Save() routes to Insert/Update/Delete based on state
/// - Child entities cannot save independently: their interface has no Save()
/// </summary>
[Factory]
internal partial class DemoEntity : EntityBase<DemoEntity>, IDemoEntity
{
    public partial string? Name { get; set; }

    public partial int Value { get; set; }

    public DemoEntity(IEntityBaseServices<DemoEntity> services) : base(services)
    {
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.Name) ? "Name is required" : string.Empty,
            t => t.Name);

        RuleManager.AddValidation(
            t => t.Value < 0 ? "Value must be non-negative" : string.Empty,
            t => t.Value);
    }

    [Create]
    public void Create()
    {
        // FactoryComplete(Create) calls MarkNew(): IsNew=true, IsModified=false.
        // Nothing was set, so there is no user work - still savable, because
        // IsSavable admits IsNew.
    }

    // [Remote]: the client fetches this root, so the call crosses to the
    // server, where the repository resolves. The object is paused for the
    // body, so plain assignment is a clean baseline load.
    [Remote]
    [Fetch]
    internal void Fetch(int id, [Service] IDemoRepository repository)
    {
        var data = repository.GetById(id);
        Name = data.Name;
        Value = data.Value;
        // After Fetch: IsNew=false, IsModified=false
    }

    // Save() routes here when IsNew. FactoryComplete(Insert) then calls
    // MarkUnmodified() and MarkOld().
    [Remote]
    [Insert]
    internal void Insert([Service] IDemoRepository repository)
    {
        repository.Insert(Name!, Value);
    }

    // Save() routes here when !IsDeleted && !IsNew. Routing never consults
    // IsModified; entity.Save()'s IsSavable gate stops unmodified saves.
    [Remote]
    [Update]
    internal void Update([Service] IDemoRepository repository)
    {
        repository.Update(Name!, Value);
    }

    // Save() routes here when IsDeleted (checked first - IsDeleted wins over IsNew)
    [Remote]
    [Delete]
    internal void Delete([Service] IDemoRepository repository)
    {
        repository.Delete(Name!);
    }
}
```
<sup><a href='/src/Design/Design.Domain/BaseClasses/AllBaseClasses.cs#L224-L300' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-entity-crud' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

When the persistence methods take a non-service parameter — a child's own row — `IFactorySave<T>` is not generated and `entity.Save()` is not available. The generated factory exposes `Save(child, row)` instead, routed on the child's `IsDeleted`/`IsNew`, and the list's `[Update]` calls it. That is the child shape of the save cascade described in [entities.md](entities.md):

<!-- snippet: skill-child-insert-update -->
<a id='snippet-skill-child-insert-update'></a>
```cs
[Insert]
internal void Insert(OrderItemRow row)
{
    // The entity sets its own key. Paused - plain assignment stays clean.
    Id = Guid.NewGuid();
    MapTo(row);
}

[Update]
internal void Update(OrderItemRow row)
{
    MapTo(row);
}

private void MapTo(OrderItemRow row)
{
    row.Id = Id;
    row.ProductName = ProductName!;
    row.Quantity = Quantity;
    row.UnitPrice = UnitPrice;
    row.LineTotal = LineTotal;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/OrderItem.cs#L141-L164' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-child-insert-update' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Setter Accessibility

The generator respects setter accessibility modifiers on partial properties:

| Declaration | Generated Setter | Setter Body | Interface Declaration |
|-------------|-----------------|-------------|----------------------|
| `public partial string Name { get; set; }` | `set` (public) | `.Value = value` | `string Name { get; set; }` |
| `public partial decimal Total { get; private set; }` | `private set` | `SetPrivateValue(value)` | `decimal Total { get; }` |
| `public partial string Data { get; protected set; }` | `protected set` | `.Value = value` | `string Data { get; }` |
| `public partial string Info { get; internal set; }` | `internal set` | `.Value = value` | `string Info { get; }` |
| `public partial string ReadOnly { get; }` | (none) | N/A | `string ReadOnly { get; }` |

Key behaviors:
- **`private set`** uses `SetPrivateValue()` which bypasses `IsReadOnly` checks. The property's `IsReadOnly` is `true` at runtime.
- **`protected set`** and **`internal set`** use `.Value = value` (same as public). `IsReadOnly` is `false` at runtime.
- Any non-public setter generates `get;` only on the interface declaration.
- EntityLazyLoad properties with `private set` use `LoadValue(value)` (same as public EntityLazyLoad), since EntityLazyLoad already bypasses `IsReadOnly`.

See [properties.md](properties.md) for runtime behavior of private-set properties.

## Suppressing Generation

`[SuppressFactory]` on a class prevents RemoteFactory from generating a factory for it. Use it on a test-only class that derives from a Neatoo base and is constructed directly with its services object.

Neatoo.BaseGenerator keys on the same `[Factory]` attribute, so a `[SuppressFactory]` class gets **no** generated partial properties; it must declare ordinary properties. Every class in a domain assembly that a factory creates is a `[Factory]` class, and every Neatoo example in this skill is one.

## Generated/ Folder

When a project sets `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` and `<CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>`, the generated code is written to `Generated/Neatoo.BaseGenerator/` and `Generated/Neatoo.Generator/` in the project folder. Exclude that folder from compilation (`<Compile Remove="Generated/**/*.cs" />`) and from source control. Without the opt-in, use the IDE's "Go to Definition" on a generated member.

## Related

- [Base Classes](base-classes.md) - Base class selection
- [Properties](properties.md) - Property declarations
- [Entities](entities.md) - Save routing and cascade patterns
