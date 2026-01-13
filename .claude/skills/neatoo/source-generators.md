# Neatoo Source Generators Reference

## Overview

Neatoo uses Roslyn source generators to create factory interfaces, implementations, and property infrastructure at compile time. This eliminates boilerplate while maintaining type safety.

## What Gets Generated

### For [Factory] Classes

| Generated | Purpose |
|-----------|---------|
| `IProductFactory` | Factory interface with Create, Fetch, Save methods |
| `ProductFactory` | Factory implementation |
| Property implementations | Backing fields, change notification, meta-properties |

### For Partial Properties

| Generated | Purpose |
|-----------|---------|
| Backing field | Property value storage |
| Getter/Setter | With change notification and rule triggering |
| Meta-property access | IsModified, IsBusy, PropertyMessages |

## Complete Entity Example

What the developer writes - generators create factory and property implementations:

<!-- snippet: complete-entity -->
```csharp
/// <summary>
/// Complete entity example - what the developer writes.
/// The source generators create:
/// - IProductFactory interface with Create, Fetch, Save methods
/// - ProductFactory implementation
/// - Property backing fields and Getter/Setter implementations
/// - Meta-property access (IsModified, IsBusy, PropertyMessages)
/// </summary>
public partial interface IProduct : IEntityBase
{
    int Id { get; }
    string? Name { get; set; }
    decimal Price { get; set; }
    int StockCount { get; set; }
}

// [Factory] - Triggers factory interface and implementation generation
[Factory]
internal partial class Product : EntityBase<Product>, IProduct
{
    public Product(IEntityBaseServices<Product> services) : base(services) { }

    // partial keyword triggers property implementation generation
    public partial int Id { get; set; }

    [DisplayName("Product Name")]
    [Required(ErrorMessage = "Name is required")]
    public partial string? Name { get; set; }

    [Range(0.01, 10000, ErrorMessage = "Price must be between $0.01 and $10,000")]
    public partial decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Stock count cannot be negative")]
    public partial int StockCount { get; set; }

    [Create]
    public void Create()
    {
        // Initialize defaults
    }

    [Fetch]
    public void Fetch(int id, string name, decimal price, int stockCount)
    {
        Id = id;
        Name = name;
        Price = price;
        StockCount = stockCount;
    }

    [Insert]
    public Task Insert()
    {
        // Persist new product to database
        return Task.CompletedTask;
    }

    [Update]
    public Task Update()
    {
        // Update existing product in database
        return Task.CompletedTask;
    }

    [Delete]
    public Task Delete()
    {
        // Remove product from database
        return Task.CompletedTask;
    }
}
```
<!-- /snippet -->

## Minimal Entity Requirements

The minimum required for source generation:

<!-- snippet: entity-input -->
```csharp
/// <summary>
/// Minimal entity showing required elements for source generation.
/// </summary>
public partial interface IMinimalEntity : IEntityBase
{
    // Interface properties are optional - generated from partial class
}

[Factory]
internal partial class MinimalEntity : EntityBase<MinimalEntity>, IMinimalEntity
{
    // Required: Constructor with IEntityBaseServices<T>
    public MinimalEntity(IEntityBaseServices<MinimalEntity> services)
        : base(services) { }

    // Required: At least one factory method with attribute
    [Create]
    public void Create() { }
}
```
<!-- /snippet -->

## [Factory] Attribute

<!-- snippet: factory-attribute -->
```csharp
// The [Factory] attribute marks a class for factory generation.
// Place it on the class declaration:
//
// [Factory]
// internal partial class MyEntity : EntityBase<MyEntity>, IMyEntity
//
// This generates:
// - IMyEntityFactory interface
// - MyEntityFactory implementation class
// - DI registration extension methods
```
<!-- /snippet -->

## Partial Properties

<!-- snippet: partial-property -->
```csharp
// The 'partial' keyword on properties triggers implementation generation:
//
// public partial string? Name { get; set; }
//
// Generator creates:
// - private string? _name;  (backing field)
// - get => Getter<string?>(); (with change notification)
// - set => Setter(value);     (with rule triggering)
//
// The generated code integrates with Neatoo's:
// - Change tracking (IsModified)
// - Validation system (PropertyMessages)
// - Busy state (IsBusy during async rules)
```
<!-- /snippet -->

## Generator Requirements

1. **Classes must be `partial`** - Generator adds to the class
2. **Properties must be `partial`** - Generator provides implementation
3. **Classes should be `internal`** - Interfaces are public
4. **Namespace required** - Avoid `MissingNamespace` issues

## Common Generator Errors

| Error | Cause | Fix |
|-------|-------|-----|
| NF0001 | Non-partial class | Add `partial` keyword |
| NF0002 | Non-partial property | Add `partial` keyword |
| NF0206 | Record struct not supported | Use `record class` instead |

## FactoryHintNameLength Attribute

RemoteFactory (the source generator powering Neatoo factories) enforces a **50-character limit** on fully qualified type names by default. When your namespace + class name exceeds this limit, you'll see build errors.

### Symptoms

- Build errors mentioning hint name length
- Errors for types with long namespaces like `MyCompany.MyProduct.Domain.Entities.SomeEntity`

### Solution

Add the assembly attribute to increase the limit:

<!-- pseudo:hint-name-length -->
```csharp
// In AssemblyAttributes.cs or any .cs file in your project
[assembly: FactoryHintNameLength(100)]
```
<!-- /snippet -->

Choose a value that accommodates your longest fully qualified type name. Common values:
- `100` - suitable for most projects
- `150` - for deeply nested namespaces

### Note

This is a **RemoteFactory** feature (v9.20.1+), not a Neatoo feature. The limit exists to keep generated source file names reasonable across different operating systems.

## Viewing Generated Code

In Visual Studio:
1. Expand Dependencies > Analyzers > Neatoo.Generators
2. View generated `.g.cs` files

## Best Practices

1. **Keep entities internal** - Expose via interfaces
2. **Use partial everywhere** - Classes and properties
3. **Include namespace** - Avoid generation issues
4. **Check for errors** - Generator errors appear as build errors
