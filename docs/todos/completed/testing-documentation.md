# Testing Documentation Plan

## Problem Statement

A developer created a wrapper class to expose the protected `Execute` method for testing:

```csharp
// WRONG - Unnecessary boilerplate
private class TestableBuildingEditRule : BuildingEditRule
{
    public IRuleMessages TestExecute(IBuildingEdit target)
    {
        return Execute(target);
    }
}
```

When they should have used the public `RunRule` method:

```csharp
// CORRECT - Use public API
var rule = new BuildingEditRule();
var result = rule.RunRule(mockTarget);
```

**Root cause:** No documentation exists for testing Neatoo rules or domain objects.

---

## Scope Analysis

### What Needs Testing Documentation?

| Component | Test Approach | Key Methods |
|-----------|---------------|-------------|
| Sync Rules (`RuleBase<T>`) | `rule.RunRule(target)` | Returns `IRuleMessages` |
| Async Rules (`AsyncRuleBase<T>`) | `await rule.RunRule(target)` | Returns `Task<IRuleMessages>` |
| Rules with Dependencies | Mock injected services/Commands | Constructor injection |
| Rules accessing Parent | Mock target with Parent property | `target.Parent` |
| Domain Objects | Real Neatoo classes + mocked externals | `IsValid`, `IsSavable`, `PropertyMessages` |
| Factory Operations | Mock `[Service]` dependencies | `[Create]`, `[Fetch]`, etc. |
| Collections | Test child validation rollup | `EntityListBase<T>` |

### Anti-Patterns to Document

1. **Creating wrapper classes to expose `Execute`** - Use `RunRule` instead
2. **Mocking Neatoo interfaces** - Use real Neatoo classes
3. **Recreating Neatoo logic in tests** - Inherit from base classes
4. **Testing rules via domain objects** - Test rules in isolation first

---

## Deliverables

### 1. Main Documentation: `docs/testing.md`

Comprehensive reference documentation covering:

```
# Testing Neatoo Applications

## Philosophy
- Use real Neatoo classes, mock external dependencies
- Test rules in isolation using RunRule
- Integration tests for full aggregate behavior

## Testing Rules

### Sync Rules
- Pattern: rule.RunRule(mockTarget)
- Example with mock target interface
- Asserting on IRuleMessages

### Async Rules
- Pattern: await rule.RunRule(mockTarget)
- Handling CancellationToken in tests

### Rules with Injected Dependencies
- Mocking Command delegates
- Mocking service interfaces
- Example: UniqueEmailRule with mocked IsUnique command

### Rules Accessing Parent/Siblings
- Setting up mock with Parent property
- Testing cross-entity validation

## Testing Domain Objects

### Test Infrastructure Setup
- IntegrationTestBase pattern
- DI container configuration for tests

### Testing Property Behavior
- Setting properties and checking validation state
- WaitForTasks for async rules

### Testing Validation State
- IsValid, IsSavable, IsBusy
- PropertyMessages assertions

## Testing Factory Operations

### Mocking [Service] Dependencies
- Pattern for mocking injected services
- Testing Create, Fetch, Insert, Update

## Common Mistakes

| Mistake | Why It's Wrong | Correct Approach |
|---------|----------------|------------------|
| Wrapper class for Execute | Unnecessary boilerplate | Use RunRule |
| Mocking IValidateBase | Tests fake behavior | Use real ValidateBase |
| Testing validation in factory tests | Wrong layer | Test rules directly |
```

### 2. Skill File: `~/.claude/skills/neatoo/testing.md`

Concise quick-reference for Claude Code:

```
# Testing Neatoo Applications

## Rule Testing - Quick Reference

### Sync Rules
rule.RunRule(target) → IRuleMessages

### Async Rules
await rule.RunRule(target) → IRuleMessages

### With Mocked Command
var mockCommand = new Mock<CheckUnique.IsUnique>();
var rule = new UniqueRule(mockCommand.Object);

## Anti-Patterns

❌ Creating wrapper to expose Execute
❌ Mocking Neatoo interfaces
❌ Testing rules only through domain objects

## Domain Object Testing

Use real Neatoo classes + mock external deps
await entity.WaitForTasks() before assertions
```

### 3. Sample Code: Documentation Samples Project

Follow the existing documentation samples workflow (see CLAUDE.md "Documentation Samples" section):
- Add code with `#region docs:testing:snippet-id` markers
- Add tests to verify samples compile and behave correctly
- Run `.\scripts\extract-snippets.ps1 -Update` to sync to docs

New files needed:

| File | Purpose |
|------|---------|
| `Samples/Testing/SampleSyncRule.cs` | Simple sync rule for testing examples |
| `Samples/Testing/SampleAsyncRule.cs` | Async rule with Command dependency |
| `Samples/Testing/SampleEntity.cs` | Entity for domain object testing |
| `Tests/Testing/SyncRuleTests.cs` | Tests demonstrating RunRule pattern |
| `Tests/Testing/AsyncRuleTests.cs` | Tests demonstrating async RunRule |
| `Tests/Testing/DomainObjectTests.cs` | Tests demonstrating entity testing |

### 4. Updates to Existing Files

| File | Change |
|------|--------|
| `docs/index.md` | Add Testing section to TOC |
| `docs/validation-and-rules.md` | Add "See Also: Testing" link |
| `~/.claude/skills/neatoo/rules.md` | Add brief testing section with link |

---

## Implementation Plan

### Phase 1: Sample Code Infrastructure

- [ ] Create `Samples/Testing/` directory structure
- [ ] Implement `SampleSyncRule` - simple rule validating Name/Id
- [ ] Implement `SampleAsyncRule` - rule with Command dependency
- [ ] Implement `ISampleEntity` interface and `SampleEntity` class

### Phase 2: Test Examples

- [ ] Write `SyncRuleTests.cs` demonstrating:
  - Basic RunRule usage
  - Mocking target interface
  - Asserting on IRuleMessages (None, Error, multiple errors)
- [ ] Write `AsyncRuleTests.cs` demonstrating:
  - Async RunRule with await
  - Mocking Command delegates
  - CancellationToken handling
- [ ] Write `DomainObjectTests.cs` demonstrating:
  - Real entity with validation
  - WaitForTasks pattern
  - Checking IsValid/IsSavable/PropertyMessages

### Phase 3: Main Documentation

- [ ] Create `docs/testing.md` with full content
- [ ] Add region markers to sample code for snippet extraction
- [ ] Run `extract-snippets.ps1 -Update` to sync snippets
- [ ] Update `docs/index.md` TOC

### Phase 4: Skill File

- [ ] Create `~/.claude/skills/neatoo/testing.md`
- [ ] Add testing section to `~/.claude/skills/neatoo/rules.md`

### Phase 5: Verification

- [ ] Run all tests to ensure samples work
- [ ] Run `extract-snippets.ps1 -Verify` to confirm sync
- [ ] Review documentation for completeness

---

## Key Code Examples to Include

### Example 1: Basic Sync Rule Test (The Fix for Original Issue)

```csharp
#region docs:testing:sync-rule-test
[TestMethod]
public void RunRule_WhenNameIsEmpty_ReturnsError()
{
    // Arrange
    var rule = new BuildingEditRule();
    var mockTarget = new Mock<IBuildingEdit>();
    mockTarget.Setup(b => b.Name).Returns(string.Empty);
    mockTarget.Setup(b => b.Id).Returns(1);

    // Act - Use RunRule, NOT a wrapper class
    var result = rule.RunRule(mockTarget.Object);

    // Assert
    result.Should().ContainSingle(m => m.PropertyName == "Name");
}
#endregion
```

### Example 2: Async Rule with Command Dependency

```csharp
#region docs:testing:async-rule-with-command
[TestMethod]
public async Task RunRule_WhenEmailExists_ReturnsError()
{
    // Arrange - Mock the Command delegate
    var mockIsUnique = new Mock<CheckEmailUnique.IsUnique>();
    mockIsUnique
        .Setup(x => x(It.IsAny<string>(), It.IsAny<Guid?>()))
        .ReturnsAsync(false);  // Email exists

    var rule = new UniqueEmailRule(mockIsUnique.Object);

    var mockTarget = new Mock<IPerson>();
    mockTarget.Setup(p => p.Email).Returns("taken@example.com");
    mockTarget.Setup(p => p[nameof(IPerson.Email)].IsModified).Returns(true);

    // Act
    var result = await rule.RunRule(mockTarget.Object);

    // Assert
    result.Should().ContainSingle(m =>
        m.PropertyName == "Email" &&
        m.Message.Contains("already in use"));
}
#endregion
```

### Example 3: Rule Accessing Parent

```csharp
#region docs:testing:rule-with-parent
[TestMethod]
public void RunRule_WhenExceedsParentLimit_ReturnsError()
{
    // Arrange
    var mockParent = new Mock<IOrder>();
    mockParent.Setup(o => o.MaxLineQuantity).Returns(100);

    var mockTarget = new Mock<IOrderLine>();
    mockTarget.Setup(l => l.Quantity).Returns(150);
    mockTarget.Setup(l => l.Parent).Returns(mockParent.Object);

    var rule = new QuantityLimitRule();

    // Act
    var result = rule.RunRule(mockTarget.Object);

    // Assert
    result.Should().ContainSingle(m => m.PropertyName == "Quantity");
}
#endregion
```

### Example 4: Domain Object Integration Test

```csharp
#region docs:testing:domain-object-validation
[TestMethod]
public async Task Person_WhenEmailInvalid_IsNotValid()
{
    // Arrange - Use real Neatoo classes
    var person = await _personFactory.Create();

    // Act
    person.Email = "not-an-email";
    await person.WaitForTasks();  // Wait for async rules

    // Assert
    person.IsValid.Should().BeFalse();
    person[nameof(person.Email)].PropertyMessages
        .Should().ContainSingle(m => m.Message.Contains("email"));
}
#endregion
```

---

## Success Criteria

1. **Developer can find testing guidance** - Clear path from rules.md to testing.md
2. **RunRule pattern is obvious** - First example shows correct approach
3. **Anti-patterns are explicit** - "Don't do this" section prevents wrapper classes
4. **Samples compile and pass** - Verified by CI
5. **Snippets stay in sync** - extract-snippets.ps1 -Verify passes

---

## Notes

- Follow existing documentation style (code-first, minimal prose)
- Use MSTest + FluentAssertions (matches existing test style)
- Moq for mocking (already in test projects)
- Keep skill file concise - it's a quick reference, not a tutorial
