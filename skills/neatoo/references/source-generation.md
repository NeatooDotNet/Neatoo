# Source Generation

Neatoo uses Roslyn source generators at compile time. **Understanding source generation is not required to use Neatoo** — declare `partial` properties and factory methods, and the generators handle the rest. This page is for curiosity and debugging.

## What Gets Generated

For each `partial` property, the generator creates a backing `IValidateProperty<T>` field with change tracking, validation triggering, and `PropertyChanged` notifications wired in.

For each class with `[Factory]`, the generator creates a factory interface (`IMyEntityFactory`) with methods matching your `[Create]`, `[Fetch]`, etc. methods.

## IFactorySave — How entity.Save() Works

When an entity defines `[Insert]`, `[Update]`, and `[Delete]` methods with **no non-service parameters**, the generator creates an `IFactorySave<T>` implementation. This is automatically injected into the entity's `Factory` property via `IEntityBaseServices<T>`, enabling `entity.Save()` to route to the correct method based on state.

<!-- snippet: api-generator-save-factory -->
<a id='snippet-api-generator-save-factory'></a>
```cs
[Factory]
public partial class ApiGeneratedSaveEntity : EntityBase<ApiGeneratedSaveEntity>
{
    public ApiGeneratedSaveEntity(IEntityBaseServices<ApiGeneratedSaveEntity> services) : base(services) { }

    public partial int Id { get; set; }

    public partial string Name { get; set; }

    public void DoMarkNew() => MarkNew();
    public void DoMarkOld() => MarkOld();

    [Create]
    public void Create()
    {
        Id = 0;
        Name = "";
    }

    // Insert, Update, Delete with no non-service parameters
    // generates IFactorySave<T> implementation
    [Insert]
    public async Task InsertAsync([Service] IApiCustomerRepository repository)
    {
        await repository.InsertAsync(Id, Name, "");
    }

    [Update]
    public async Task UpdateAsync([Service] IApiCustomerRepository repository)
    {
        await repository.UpdateAsync(Id, Name, "");
    }

    [Delete]
    public async Task DeleteAsync([Service] IApiCustomerRepository repository)
    {
        await repository.DeleteAsync(Id);
    }
}
```
<sup><a href='/src/samples/ApiReferenceSamples.cs#L651-L691' title='Snippet source file'>snippet source</a> | <a href='#snippet-api-generator-save-factory' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

If `[Insert]`/`[Update]`/`[Delete]` methods have non-service parameters (like a parent ID), `IFactorySave<T>` is not generated. The parent must call `factory.SaveAsync(child, parentId)` explicitly — this is the cascade save pattern described in [entities.md](entities.md).

## Suppressing Generation

Use `[SuppressFactory]` to prevent factory generation for abstract base classes, test classes, or when manual factory implementation is needed:

<!-- snippet: api-attributes-suppressfactory -->
<a id='snippet-api-attributes-suppressfactory'></a>
```cs
[SuppressFactory]
public class ApiTestObject : ValidateBase<ApiTestObject>
{
    public ApiTestObject(IValidateBaseServices<ApiTestObject> services) : base(services) { }

    public string Name { get => Getter<string>(); set => Setter(value); }
}
```
<sup><a href='/src/samples/ApiReferenceSamples.cs#L585-L593' title='Snippet source file'>snippet source</a> | <a href='#snippet-api-attributes-suppressfactory' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Generated/ Folder

Generated code is output to `Generated/Neatoo.BaseGenerator/` within each project. This folder is excluded from git. To inspect generated code, build the project and look there, or use IDE "Go to Definition" on generated members.

## Related

- [Base Classes](base-classes.md) - Base class selection
- [Properties](properties.md) - Property declarations
- [Entities](entities.md) - Save routing and cascade patterns
