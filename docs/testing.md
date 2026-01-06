# Testing Neatoo Applications

This guide covers unit testing patterns for Neatoo rules and domain objects.

## Contents

| Section | Description |
|---------|-------------|
| [Testing Philosophy](#testing-philosophy) | Use real Neatoo classes, mock external dependencies |
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

## Testing Rules

### The Key Pattern: Use `RunRule`

Every rule class (`RuleBase<T>`, `AsyncRuleBase<T>`) provides a **public `RunRule` method** for testing:

```csharp
// Sync rule
var result = rule.RunRule(target);           // Returns Task<IRuleMessages>

// Async rule
var result = await rule.RunRule(target);     // Returns IRuleMessages
```

### Why `RunRule` Exists

| Method | Visibility | Purpose |
|--------|------------|---------|
| `Execute` | `protected` | Implementation - subclasses override this |
| `RunRule` | `public` | Testing and framework use |

`RunRule` properly sets the `Executed` flag and returns `IRuleMessages` for assertion.

---

## Sync Rule Testing

### Basic Pattern

<!-- snippet: docs:testing:sync-rule-test -->
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

### What to Assert

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

---

## Async Rule Testing

### Pattern with Mocked Dependency

<!-- snippet: docs:testing:async-rule-test -->
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

### Mocking `IsModified`

When your rule checks `IsModified` (common optimization for async rules):

```csharp
// Mock the property with IsModified = true
var mockProperty = new Mock<IEntityProperty>();
mockProperty.Setup(p => p.IsModified).Returns(true);
mockTarget.Setup(e => e[nameof(IEntity.PropertyName)]).Returns(mockProperty.Object);
```

### Verifying Dependency Calls

```csharp
// Verify called
mockService.Verify(s => s.CheckAsync(email), Times.Once);

// Verify NOT called (e.g., when property not modified)
mockService.Verify(s => s.CheckAsync(It.IsAny<string>()), Times.Never);
```

---

## Cross-Entity Rule Testing

### Testing Rules that Access Parent

<!-- snippet: docs:testing:rule-with-parent-test -->
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

### Testing Null Parent Handling

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

---

## Anti-Patterns

### Don't: Create Wrapper Classes to Expose Execute

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

```csharp
// CORRECT - Use RunRule directly
var rule = new BuildingEditRule();
var result = rule.RunRule(mockTarget.Object);  // Public API
```

### Don't: Mock Neatoo Interfaces

```csharp
// WRONG - Mocking framework internals
var mockValidateBase = new Mock<IValidateBase>();
// ... manually setting up PropertyMessages, IsValid, etc.
```

```csharp
// CORRECT - Use real Neatoo classes for integration tests
public class TestPerson : ValidateBase<TestPerson>
{
    public string Name { get => Getter<string>(); set => Setter(value); }
}
```

### Don't: Test Rules Only Through Domain Objects

For unit testing rules, test the rule in isolation first:

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

---

## See Also

- [Validation and Rules](validation-and-rules.md) - Writing rules
- [Database-Dependent Validation](database-dependent-validation.md) - Async rules with Commands
