# Neatoo Testing Reference

## Overview

Testing Neatoo entities and rules focuses on verifying business logic in isolation. The framework provides patterns for testing validation rules, async operations, and parent-child relationships.

## Testing Sync Rules

### Rule Definition

<!-- snippet: sync-rule-definition -->
```csharp
/// <summary>
/// Interface for entities validated by NameValidationRule.
/// </summary>
public interface INamedEntity : IValidateBase
{
    string? Name { get; set; }
    int? Id { get; set; }
}

/// <summary>
/// Simple sync rule that validates Name and Id fields.
/// Demonstrates basic RuleBase pattern for testing documentation.
/// </summary>
public class NameValidationRule : RuleBase<INamedEntity>
{
    public NameValidationRule() : base(e => e.Name, e => e.Id) { }

    protected override IRuleMessages Execute(INamedEntity target)
    {
        // Use RuleMessages.If for simple conditional checks
        // For multiple potential errors, collect and return
        return RuleMessages
            .If(string.IsNullOrWhiteSpace(target.Name), nameof(target.Name), "Name is required")
            .ElseIf(() => target.Id is null or <= 0, nameof(target.Id), "Id must be a positive number");
    }
}
```
<!-- /snippet -->

### Test Implementation

<!-- snippet: sync-rule-test -->
```csharp
[TestMethod]
    public void RunRule_WhenNameIsEmpty_ReturnsError()
    {
        // Arrange
        var rule = new NameValidationRule();

        var mockTarget = new Mock<INamedEntity>();
        mockTarget.Setup(e => e.Name).Returns(string.Empty);
        mockTarget.Setup(e => e.Id).Returns(1);

        // Act - Use RunRule directly, NOT a wrapper class
        var result = rule.RunRule(mockTarget.Object);

        // Assert
        Assert.IsNotNull(result);
        var messages = result.Result.ToList();
        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual("Name", messages[0].PropertyName);
        Assert.IsTrue(messages[0].Message.Contains("required"));
    }
```
<!-- /snippet -->

## Testing Async Rules

### Async Rule Definition

<!-- snippet: async-rule-definition -->
```csharp
/// <summary>
/// Interface for uniqueness checking - typically generated from a [Factory] Command.
/// </summary>
public interface ICheckNameUnique
{
    Task<bool> IsUnique(string name, int? excludeId);
}

/// <summary>
/// Interface for entities with IsModified tracking (extends IEntityBase).
/// </summary>
public interface INamedEntityWithTracking : IEntityBase
{
    string? Name { get; set; }
    int? Id { get; set; }
}

/// <summary>
/// Async rule with injected dependency for database validation.
/// Demonstrates AsyncRuleBase pattern for testing documentation.
/// </summary>
public class UniqueNameAsyncRule : AsyncRuleBase<INamedEntityWithTracking>
{
    private readonly ICheckNameUnique _checkUnique;

    public UniqueNameAsyncRule(ICheckNameUnique checkUnique) : base(e => e.Name)
    {
        _checkUnique = checkUnique;
    }

    protected override async Task<IRuleMessages> Execute(INamedEntityWithTracking target, CancellationToken? token = null)
    {
        if (string.IsNullOrWhiteSpace(target.Name))
            return None;

        // Skip check if Name hasn't been modified (optimization for existing entities)
        if (!target[nameof(target.Name)].IsModified)
            return None;

        var isUnique = await _checkUnique.IsUnique(target.Name, target.Id);

        return isUnique
            ? None
            : (nameof(target.Name), "Name already exists").AsRuleMessages();
    }
}
```
<!-- /snippet -->

### Async Test Implementation

<!-- snippet: async-rule-test -->
```csharp
[TestMethod]
    public async Task RunRule_WhenNameNotUnique_ReturnsError()
    {
        // Arrange - Mock the uniqueness check dependency
        var mockCheckUnique = new Mock<ICheckNameUnique>();
        mockCheckUnique
            .Setup(c => c.IsUnique(It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(false); // Name is NOT unique

        var rule = new UniqueNameAsyncRule(mockCheckUnique.Object);

        // Mock the target entity (IEntityBase for IsModified support)
        var mockTarget = new Mock<INamedEntityWithTracking>();
        mockTarget.Setup(e => e.Name).Returns("Duplicate Name");
        mockTarget.Setup(e => e.Id).Returns(1);

        // Mock the property accessor for IsModified check
        var mockProperty = new Mock<IEntityProperty>();
        mockProperty.Setup(p => p.IsModified).Returns(true);
        mockTarget.Setup(e => e[nameof(INamedEntityWithTracking.Name)]).Returns(mockProperty.Object);

        // Act - Use RunRule with await for async rules
        var result = await rule.RunRule(mockTarget.Object);

        // Assert
        var messages = result.ToList();
        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual("Name", messages[0].PropertyName);
        Assert.IsTrue(messages[0].Message.Contains("already exists"));

        // Verify the dependency was called
        mockCheckUnique.Verify(c => c.IsUnique("Duplicate Name", 1), Times.Once);
    }
```
<!-- /snippet -->

## Testing Parent-Child Rules

### Rule with Parent Definition

<!-- snippet: rule-with-parent-definition -->
```csharp
/// <summary>
/// Interface for a line item that has a parent order.
/// </summary>
public interface ILineItem : IValidateBase
{
    int Quantity { get; set; }
    IOrderHeader? Parent { get; }
}

/// <summary>
/// Interface for the parent order with a quantity limit.
/// </summary>
public interface IOrderHeader
{
    int MaxQuantityPerLine { get; }
}

/// <summary>
/// Rule that validates against parent entity properties.
/// Demonstrates cross-entity validation for testing documentation.
/// </summary>
public class QuantityLimitRule : RuleBase<ILineItem>
{
    public QuantityLimitRule() : base(l => l.Quantity) { }

    protected override IRuleMessages Execute(ILineItem target)
    {
        if (target.Parent is null)
            return None;

        if (target.Quantity > target.Parent.MaxQuantityPerLine)
        {
            return (nameof(target.Quantity),
                $"Quantity cannot exceed {target.Parent.MaxQuantityPerLine}").AsRuleMessages();
        }

        return None;
    }
}
```
<!-- /snippet -->

### Parent-Child Test Implementation

<!-- snippet: rule-with-parent-test -->
```csharp
[TestMethod]
    public void RunRule_WhenQuantityExceedsParentLimit_ReturnsError()
    {
        // Arrange
        var rule = new QuantityLimitRule();

        // Mock the parent with a quantity limit
        var mockParent = new Mock<IOrderHeader>();
        mockParent.Setup(p => p.MaxQuantityPerLine).Returns(100);

        // Mock the line item with quantity exceeding the limit
        var mockTarget = new Mock<ILineItem>();
        mockTarget.Setup(l => l.Quantity).Returns(150);
        mockTarget.Setup(l => l.Parent).Returns(mockParent.Object);

        // Act
        var result = rule.RunRule(mockTarget.Object);

        // Assert
        var messages = result.Result.ToList();
        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual("Quantity", messages[0].PropertyName);
        Assert.IsTrue(messages[0].Message.Contains("100"));
    }
```
<!-- /snippet -->

## Unit Testing Entities Directly

For testing entity behavior without factory setup, use `EntityBaseServices<T>()`:

<!-- snippet: entity-unit-test-class -->
```csharp
/// <summary>
/// Entity class designed for direct unit testing.
/// Uses [SuppressFactory] to prevent factory generation.
/// </summary>
[SuppressFactory]
public class TestableProduct : EntityBase<TestableProduct>
{
    /// <summary>
    /// Parameterless constructor using EntityBaseServices for unit testing.
    /// WARNING: This bypasses DI and disables Save() functionality.
    /// </summary>
    public TestableProduct() : base(new EntityBaseServices<TestableProduct>())
    {
    }

    public string? Name { get => Getter<string>(); set => Setter(value); }
    public decimal Price { get => Getter<decimal>(); set => Setter(value); }
    public int Quantity { get => Getter<int>(); set => Setter(value); }

    /// <summary>
    /// Calculated property - tests business logic without needing factories.
    /// </summary>
    public decimal TotalValue => Price * Quantity;

    /// <summary>
    /// Expose MarkNew for testing state transitions.
    /// </summary>
    public void SetAsNew() => MarkNew();

    /// <summary>
    /// Expose MarkOld for testing existing entity scenarios.
    /// </summary>
    public void SetAsExisting() => MarkOld();

    /// <summary>
    /// Expose MarkAsChild for testing child entity behavior.
    /// </summary>
    public void SetAsChild() => MarkAsChild();
}
```
<!-- /snippet -->

### Caution: Unit Testing Only

Using `new EntityBaseServices<T>()` creates an entity with:

| Works | Broken |
|-------|--------|
| Property get/set | Save operations |
| IsModified, IsNew, IsDeleted | Parent-child relationships |
| Calculated properties | Factory methods |
| Business logic methods | `[Service]` dependencies |
| State transitions | Remote operations |

**Do not use in production code** - the factory is `null` and Save() will fail.

### Test Example

```csharp
[TestMethod]
public void Product_WhenQuantityChanged_IsModifiedTrue()
{
    // Arrange - no factory needed
    var product = new TestableProduct();

    // Act
    product.Quantity = 10;

    // Assert
    Assert.IsTrue(product.IsModified);
}
```

## Testing Patterns

### Key Concepts

1. **Isolate rule logic** - Test rules independently from entities
2. **Mock dependencies** - Use KnockOff or similar for service mocks
3. **Test both valid and invalid cases** - Verify error messages
4. **Async testing** - Use `await` and verify `IsBusy` states

### Best Practices

1. **Name tests descriptively** - `RuleName_Condition_ExpectedResult`
2. **Arrange-Act-Assert** - Clear test structure
3. **Test edge cases** - Null values, empty strings, boundaries
4. **Verify message text** - Ensure user-friendly messages
