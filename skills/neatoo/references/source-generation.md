# Source Generation

Neatoo uses Roslyn source generators to create property implementations, factory methods, and wiring code at compile time. Understanding what gets generated helps with debugging and customization.

## What Gets Generated

### Property Implementations

For each partial property with `Getter<T>()` / `Setter()`:

<!-- snippet: api-generator-partial-property -->
<a id='snippet-api-generator-partial-property'></a>
```cs
/// <summary>
/// Entity demonstrating partial property generation.
/// The source generator completes these partial property declarations.
/// </summary>
[Factory]
public partial class SkillGenCustomer : ValidateBase<SkillGenCustomer>
{
    public SkillGenCustomer(IValidateBaseServices<SkillGenCustomer> services) : base(services) { }

    public partial string Name { get; set; }

    public partial string Email { get; set; }

    public partial DateTime BirthDate { get; set; }

    [Create]
    public void Create() { }
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/SourceGenerationSamples.cs#L15-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-api-generator-partial-property' title='Start of snippet'>anchor</a></sup>
<a id='snippet-api-generator-partial-property-1'></a>
```cs
[Factory]
public partial class ApiGeneratedCustomer : ValidateBase<ApiGeneratedCustomer>
{
    public ApiGeneratedCustomer(IValidateBaseServices<ApiGeneratedCustomer> services) : base(services) { }

    // Source generator creates:
    // - private IValidateProperty<string> _NameProperty;
    // - getter: return _NameProperty.Value;
    // - setter: _NameProperty.SetValue(value);
    public partial string Name { get; set; }

    public partial string Email { get; set; }

    [Create]
    public void Create() { }
}
```
<sup><a href='/src/docs/samples/ApiReferenceSamples.cs#L598-L615' title='Snippet source file'>snippet source</a> | <a href='#snippet-api-generator-partial-property-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Factory Methods

For classes with `[Factory]` attribute:

<!-- snippet: api-generator-factory-methods -->
<a id='snippet-api-generator-factory-methods'></a>
```cs
/// <summary>
/// Entity demonstrating factory method generation.
/// Source generator creates factory interface and implementation.
/// </summary>
[Factory]
public partial class SkillGenEntity : EntityBase<SkillGenEntity>
{
    public SkillGenEntity(IEntityBaseServices<SkillGenEntity> services) : base(services) { }

    public partial int Id { get; set; }

    public partial string Name { get; set; }

    [Create]
    public void Create()
    {
        Id = 0;
        Name = "";
    }

    [Fetch]
    public async Task FetchAsync(int id, [Service] ISkillGenRepository repository)
    {
        var data = await repository.FetchAsync(id);
        Id = data.Id;
        Name = data.Name;
    }

    [Insert]
    public Task InsertAsync([Service] ISkillGenRepository repository) =>
        repository.InsertAsync(Id, Name);

    [Update]
    public Task UpdateAsync([Service] ISkillGenRepository repository) =>
        repository.UpdateAsync(Id, Name);

    [Delete]
    public Task DeleteAsync([Service] ISkillGenRepository repository) =>
        repository.DeleteAsync(Id);
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/SourceGenerationSamples.cs#L40-L81' title='Snippet source file'>snippet source</a> | <a href='#snippet-api-generator-factory-methods' title='Start of snippet'>anchor</a></sup>
<a id='snippet-api-generator-factory-methods-1'></a>
```cs
[Factory]
public partial class ApiGeneratedEntity : EntityBase<ApiGeneratedEntity>
{
    public ApiGeneratedEntity(IEntityBaseServices<ApiGeneratedEntity> services) : base(services) { }

    public partial int Id { get; set; }

    public partial string Name { get; set; }

    // Source generator creates static factory methods from instance methods
    [Create]
    public void Create()
    {
        Id = 0;
        Name = "";
    }

    [Fetch]
    public async Task FetchAsync(int id, [Service] IApiCustomerRepository repository)
    {
        var data = await repository.FetchAsync(id);
        Id = data.Id;
        Name = data.Name;
    }
}
```
<sup><a href='/src/docs/samples/ApiReferenceSamples.cs#L620-L646' title='Snippet source file'>snippet source</a> | <a href='#snippet-api-generator-factory-methods-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Save Factory

Save routing and factory wiring:

<!-- snippet: api-generator-save-factory -->
<a id='snippet-api-generator-save-factory'></a>
```cs
/// <summary>
/// Entity demonstrating save factory generation.
/// When Insert/Update/Delete have only [Service] parameters,
/// the generator creates a unified SaveAsync method.
/// </summary>
[Factory]
public partial class SkillGenSaveEntity : EntityBase<SkillGenSaveEntity>
{
    public SkillGenSaveEntity(IEntityBaseServices<SkillGenSaveEntity> services) : base(services) { }

    public partial int Id { get; set; }

    public partial string Data { get; set; }

    public void DoMarkNew() => MarkNew();
    public void DoMarkOld() => MarkOld();

    [Create]
    public void Create()
    {
        Id = 0;
        Data = "";
    }

    [Insert]
    public Task InsertAsync([Service] ISkillGenRepository repository) =>
        repository.InsertAsync(Id, Data);

    [Update]
    public Task UpdateAsync([Service] ISkillGenRepository repository) =>
        repository.UpdateAsync(Id, Data);

    [Delete]
    public Task DeleteAsync([Service] ISkillGenRepository repository) =>
        repository.DeleteAsync(Id);
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/SourceGenerationSamples.cs#L87-L124' title='Snippet source file'>snippet source</a> | <a href='#snippet-api-generator-save-factory' title='Start of snippet'>anchor</a></sup>
<a id='snippet-api-generator-save-factory-1'></a>
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
<sup><a href='/src/docs/samples/ApiReferenceSamples.cs#L651-L691' title='Snippet source file'>snippet source</a> | <a href='#snippet-api-generator-save-factory-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Rule IDs

Stable identifiers for validation rules:

<!-- snippet: api-generator-ruleid -->
<a id='snippet-api-generator-ruleid'></a>
```cs
/// <summary>
/// Entity demonstrating RuleId generation.
/// Lambda expressions in AddRule generate stable RuleId entries.
/// </summary>
[Factory]
public partial class SkillGenRuleEntity : ValidateBase<SkillGenRuleEntity>
{
    public SkillGenRuleEntity(IValidateBaseServices<SkillGenRuleEntity> services) : base(services)
    {
        RuleManager.AddValidation(
            entity => entity.Value > 0 ? "" : "Value must be positive",
            e => e.Value);

        RuleManager.AddValidation(
            entity => entity.Value <= 100 ? "" : "Value cannot exceed 100",
            e => e.Value);
    }

    public partial int Value { get; set; }

    [Create]
    public void Create() { }
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/SourceGenerationSamples.cs#L130-L154' title='Snippet source file'>snippet source</a> | <a href='#snippet-api-generator-ruleid' title='Start of snippet'>anchor</a></sup>
<a id='snippet-api-generator-ruleid-1'></a>
```cs
[Factory]
public partial class ApiRuleIdEntity : ValidateBase<ApiRuleIdEntity>
{
    public ApiRuleIdEntity(IValidateBaseServices<ApiRuleIdEntity> services) : base(services)
    {
        // Lambda expressions in AddRule generate stable RuleId entries
        // in RuleIdRegistry for consistent rule identification
        RuleManager.AddValidation(
            entity => entity.Value > 0 ? "" : "Value must be positive",
            e => e.Value);
    }

    public partial int Value { get; set; }

    [Create]
    public void Create() { }
}
```
<sup><a href='/src/docs/samples/ApiReferenceSamples.cs#L696-L714' title='Snippet source file'>snippet source</a> | <a href='#snippet-api-generator-ruleid-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Generated/ Folder

Generated code is output to a `Generated/` folder within each project:

```
MyProject/
├── Domain/
│   └── Employee.cs              # Your code
├── Generated/
│   └── Neatoo.BaseGenerator/
│       └── Employee.g.cs        # Generated code
└── MyProject.csproj
```

**Note:** The `Generated/` folder is excluded from git (see `.gitignore`).

## Viewing Generated Code

To inspect generated code:

1. Build the project
2. Look in `Generated/Neatoo.BaseGenerator/`
3. Or use IDE "Go to Definition" on generated members

## Suppressing Generation

Use `[SuppressFactory]` to prevent factory generation:

<!-- snippet: api-attributes-suppressfactory -->
<a id='snippet-api-attributes-suppressfactory'></a>
```cs
/// <summary>
/// [SuppressFactory] prevents factory generation.
/// Used for test classes, abstract bases, or manual factory implementations.
/// </summary>
[SuppressFactory]
public class SkillGenTestObject : ValidateBase<SkillGenTestObject>
{
    public SkillGenTestObject(IValidateBaseServices<SkillGenTestObject> services) : base(services) { }

    // Using traditional Getter/Setter instead of partial properties
    // (partial properties also work, but this shows the alternative)
    public string Name { get => Getter<string>(); set => Setter(value); }

    public int Amount { get => Getter<int>(); set => Setter(value); }
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/SourceGenerationSamples.cs#L160-L176' title='Snippet source file'>snippet source</a> | <a href='#snippet-api-attributes-suppressfactory' title='Start of snippet'>anchor</a></sup>
<a id='snippet-api-attributes-suppressfactory-1'></a>
```cs
[SuppressFactory]
public class ApiTestObject : ValidateBase<ApiTestObject>
{
    public ApiTestObject(IValidateBaseServices<ApiTestObject> services) : base(services) { }

    public string Name { get => Getter<string>(); set => Setter(value); }
}
```
<sup><a href='/src/docs/samples/ApiReferenceSamples.cs#L585-L593' title='Snippet source file'>snippet source</a> | <a href='#snippet-api-attributes-suppressfactory-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Use cases:
- Abstract base classes
- Test classes that shouldn't have factories
- Manual factory implementation needed

## Generator Diagnostics

The generator produces warnings and errors:

| Code | Meaning |
|------|---------|
| NEATOO001 | Class must be partial |
| NEATOO002 | Invalid factory method signature |
| NEATOO003 | Missing required attribute |

## Incremental Generation

Neatoo uses incremental source generation for fast builds:
- Only regenerates when source files change
- Caches intermediate results
- Minimal impact on build times

## Customizing Generated Code

### Partial Methods

Implement partial methods to hook into generated code:

```csharp
partial void OnPropertyChanged(string propertyName)
{
    // Custom logic after any property change
}
```

### Virtual Methods

Override virtual methods in base classes:

```csharp
protected override void AddRules()
{
    base.AddRules();
    // Add custom rules
}
```

## Debugging Generated Code

1. **Enable source maps** - Generated code includes `#line` directives
2. **Step through** - Debugger can step into generated code
3. **Inspect generated files** - Check `Generated/` folder for issues

## Build Troubleshooting

**Generator not running:**
- Ensure NuGet package is referenced correctly
- Check for analyzer errors in build output
- Rebuild the project (not just build)

**Generated code not updating:**
- Clean solution and rebuild
- Check that source file timestamps are updating
- Restart IDE if caching issues persist

**Duplicate member errors:**
- Ensure class is marked `partial`
- Check for manual implementations conflicting with generated code

## Related

- [Factory](factory.md) - Factory attributes
- [Properties](properties.md) - Property declarations
- [Base Classes](base-classes.md) - Base class selection
