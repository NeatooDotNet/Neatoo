# Testing Neatoo Applications

This guide covers unit testing patterns for Neatoo rules and domain objects.

## Contents

| Section | Description |
|---------|-------------|
| [Testing Philosophy](#testing-philosophy) | Use real Neatoo classes, mock external dependencies |
| [Unit Testing Entities](#unit-testing-entities) | Test entities directly without factories |
| [Testing Rules](#testing-rules) | Use `RunRule` - do NOT expose `Execute` |
| [Sync Rule Testing](#sync-rule-testing) | Testing `RuleBase<T>` rules |
| [Async Rule Testing](#async-rule-testing) | Testing `AsyncRuleBase<T>` with mocked dependencies |
| [Cross-Entity Rule Testing](#cross-entity-rule-testing) | Testing rules that access Parent |
| [Anti-Patterns](#anti-patterns) | Common mistakes to avoid |

---

## Testing Philosophy

| Principle | Do | Don't |
|-----------|-----|-------|
| Mock external dependencies | Mock `IEmailService`, database access | Mock `IValidateBase`, `IProperty` |
| Test rules in isolation | Call `rule.RunRule(target)` | Create wrapper classes to expose `Execute` |
| Use real Neatoo classes | Inherit from `ValidateBase<T>` | Manually implement Neatoo interfaces |

---

## Unit Testing Entities

For unit testing individual entities without DI or factory setup, use the parameterless `EntityBaseServices<T>()` constructor:

<!-- snippet: entity-unit-test-class -->
```cs
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
<!-- endSnippet -->

> **Caution: Unit Testing Only**
>
> Using `new EntityBaseServices<T>()` creates an entity with:
> - **No DI container** - `[Service]` attributes won't inject dependencies
> - **No factory** - `Save()` operations will fail
> - **No parent tracking** - Parent/child relationships won't work
>
> This pattern is acceptable for testing entity state tracking, property changes, calculated properties, and business logic that doesn't require persistence. Do not use in production code.

### What You Can Test

| Testable | Not Testable |
|----------|--------------|
| Property get/set | Save operations |
| IsModified, IsNew, IsDeleted | Parent-child relationships |
| Calculated properties | Factory methods |
| Business logic methods | `[Service]` injected dependencies |
| State transitions (MarkNew, etc.) | Remote operations |

### Example Test

<!-- pseudo:entity-unit-test-example -->
```csharp
[TestMethod]
public void TestableProduct_WhenPropertyChanged_IsModifiedTrue()
{
    // Arrange - Create entity without factory
    var product = new TestableProduct();

    // Act
    product.Name = "Widget";

    // Assert
    Assert.IsTrue(product.IsModified);
}
```
<!-- /snippet -->

---

## Testing Rules

### The Key Pattern: Use `RunRule`

Every rule class (`RuleBase<T>`, `AsyncRuleBase<T>`) provides a **public `RunRule` method** for testing:

<!-- pseudo:runrule-methods -->
```csharp
// Sync rule
var result = rule.RunRule(target);           // Returns Task<IRuleMessages>

// Async rule
var result = await rule.RunRule(target);     // Returns IRuleMessages
```
<!-- /snippet -->

### Why `RunRule` Exists

| Method | Visibility | Purpose |
|--------|------------|---------|
| `Execute` | `protected` | Implementation - subclasses override this |
| `RunRule` | `public` | Testing and framework use |

`RunRule` properly sets the `Executed` flag and returns `IRuleMessages` for assertion.

---

## Sync Rule Testing

### Basic Pattern

<!-- snippet: sync-rule-test -->
```cs
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
<!-- endSnippet -->

### What to Assert

<!-- pseudo:what-to-assert -->
```csharp
var result = rule.RunRule(mockTarget.Object);
var messages = result.Result.ToList();

// No errors
Assert.IsFalse(messages.Any());

// Has specific error
Assert.IsTrue(messages.Any(m => m.PropertyName == "Email"));
Assert.IsTrue(messages.Any(m => m.Message.Contains("required")));

// Error count
Assert.AreEqual(2, messages.Count);
```
<!-- /snippet -->

---

## Async Rule Testing

### Pattern with Mocked Dependency

<!-- snippet: async-rule-test -->
```cs
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
<!-- endSnippet -->

### Mocking `IsModified`

When your rule checks `IsModified` (common optimization for async rules):

<!-- pseudo:mocking-ismodified -->
```csharp
// Mock the property with IsModified = true
var mockProperty = new Mock<IEntityProperty>();
mockProperty.Setup(p => p.IsModified).Returns(true);
mockTarget.Setup(e => e[nameof(IEntity.PropertyName)]).Returns(mockProperty.Object);
```
<!-- /snippet -->

### Verifying Dependency Calls

<!-- pseudo:verifying-dependency-calls -->
```csharp
// Verify called
mockService.Verify(s => s.CheckAsync(email), Times.Once);

// Verify NOT called (e.g., when property not modified)
mockService.Verify(s => s.CheckAsync(It.IsAny<string>()), Times.Never);
```
<!-- /snippet -->

---

## Cross-Entity Rule Testing

### Testing Rules that Access Parent

<!-- snippet: rule-with-parent-test -->
```cs
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
<!-- endSnippet -->

### Testing Null Parent Handling

<!-- pseudo:testing-null-parent -->
```csharp
[TestMethod]
public void RunRule_WhenNoParent_ReturnsNone()
{
    var rule = new QuantityLimitRule();

    var mockTarget = new Mock<ILineItem>();
    mockTarget.Setup(l => l.Quantity).Returns(1000);
    mockTarget.Setup(l => l.Parent).Returns((IOrderHeader?)null);

    var result = rule.RunRule(mockTarget.Object);

    // Rule should handle null parent gracefully
    Assert.IsFalse(result.Result.Any());
}
```
<!-- /snippet -->

---

## Anti-Patterns

### Don't: Create Wrapper Classes to Expose Execute

<!-- invalid:wrapper-class-antipattern -->
```csharp
// WRONG - Unnecessary boilerplate
private class TestableBuildingEditRule : BuildingEditRule
{
    public IRuleMessages TestExecute(IBuildingEdit target)
    {
        return Execute(target);  // Exposing protected method
    }
}

var rule = new TestableBuildingEditRule();
var result = rule.TestExecute(mockTarget);  // Don't do this
```
<!-- /snippet -->

<!-- pseudo:correct-runrule-usage -->
```csharp
// CORRECT - Use RunRule directly
var rule = new BuildingEditRule();
var result = rule.RunRule(mockTarget.Object);  // Public API
```
<!-- /snippet -->

### Don't: Mock Neatoo Interfaces

<!-- invalid:mock-neatoo-antipattern -->
```csharp
// WRONG - Mocking framework internals
var mockValidateBase = new Mock<IValidateBase>();
// ... manually setting up PropertyMessages, IsValid, etc.
```
<!-- /snippet -->

<!-- pseudo:correct-real-neatoo-class -->
```csharp
// CORRECT - Use real Neatoo classes for integration tests
public class TestPerson : ValidateBase<TestPerson>
{
    public string Name { get => Getter<string>(); set => Setter(value); }
}
```
<!-- /snippet -->

### Don't: Test Rules Only Through Domain Objects

For unit testing rules, test the rule in isolation first:

<!-- pseudo:rule-testing-approach -->
```csharp
// Unit test - tests rule logic in isolation
var rule = new EmailValidationRule();
var result = rule.RunRule(mockTarget.Object);
Assert.IsTrue(result.Any());

// Integration test - tests rule integration with entity
var person = await personFactory.Create();
person.Email = "invalid";
await person.WaitForTasks();
Assert.IsFalse(person.IsValid);
```
<!-- /snippet -->

---

## See Also

- [Validation and Rules](validation-and-rules.md) - Writing rules
- [Database-Dependent Validation](database-dependent-validation.md) - Async rules with Commands
