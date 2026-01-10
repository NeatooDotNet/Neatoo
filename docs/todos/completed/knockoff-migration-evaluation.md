# KnockOff Migration Evaluation

Evaluating replacement of Moq with KnockOff for all Neatoo tests.

## Final Status (2026-01-09): **COMPLETE** ✅

**Migration achieved:** 8 of 8 test files migrated to KnockOff (100%)

| Metric | Value |
|--------|-------|
| Files migrated to KnockOff | 8 |
| Files still using Moq | 1 (documentation sample) |
| Projects with Moq removed | 2 (`Neatoo.UnitTest`, `Person.DomainModel.Tests`) |
| Projects with Moq remaining | 1 (`Neatoo.Samples.DomainModel.Tests` - intentional) |

**KnockOff version:** 10.12.0

**All previous blockers resolved in 10.12.0:**
- ✅ Generic methods now supported (`IRuleManager.AddRule<T>`, `RunRule<T>`)
- ✅ Neatoo domain interfaces work (`IPersonPhoneList`, `IPersonPhone`, etc.)
- ✅ BCL interface compatibility (117 interfaces tested)

**Remaining Moq usage:**
- `TestingRuleSamplesTests.cs` - Documentation samples showing Moq patterns (intentional)

---

## Execution Log (2026-01-09) - KnockOff 10.12.0 Migration

### Upgraded to KnockOff 10.12.0

All previous blockers resolved:

| Interface | 10.11.0 | 10.12.0 | Notes |
|-----------|---------|---------|-------|
| `IRuleManager` | ❌ Generic methods | ✅ Works | Generic methods now supported |
| `IPersonDbContext` | ❌ Namespace collision | ✅ Works | Resolved |
| `IPersonPhoneListFactory` | ❌ Not tested | ✅ Works | Simple factory interface |
| `IPersonPhoneList` | ❌ Indexer shadowing | ✅ Works | Complex Neatoo domain interface |

### Completed Migrations (10.12.0)

| File | Interfaces | Status |
|------|------------|--------|
| `FluentRuleTests.cs` | `IRuleManager`, `IValidateBase` | ✅ Migrated |
| `PersonTests.cs` | `IPersonDbContext`, `IPersonPhoneListFactory`, `IPersonPhoneList` | ✅ Migrated |

### Moq Package Removed From:

| Project | Tests | Status |
|---------|-------|--------|
| `Neatoo.UnitTest` | 1594 pass, 1 skip | ✅ Moq removed |
| `Person.DomainModel.Tests` | 54 pass | ✅ Moq.EntityFrameworkCore removed |

---

## Execution Log (2026-01-08)

### KnockOff 10.9.0 Update

Upgraded from 10.8.0 to **10.9.0**. Retested complex interfaces:

| Interface | 10.8.0 | 10.9.0 | Notes |
|-----------|--------|--------|-------|
| `IValidateBase` | ❌ Failed | ✅ Works | Multiple inheritance + events now supported |
| `IRuleManager` | ❌ Failed | ❌ Still fails | Generic methods (`AddRule<T>`, `RunRule<T>`) |

### Key Finding: Generic Methods Still Block IRuleManager (RESOLVED in 10.12.0)

~~KnockOff v10.9.0 **cannot** generate stubs for interfaces with generic methods~~

**✅ RESOLVED in KnockOff 10.12.0** - Generic methods now fully supported.

| Limitation | Affected Interfaces | 10.9.0 | 10.12.0 |
|------------|---------------------|--------|---------|
| Generic methods | `IRuleManager` (`AddRule<T>`, `RunRule<T>`) | ❌ Failed | ✅ Works |

**Resolved in 10.9.0:**
- ✅ Multiple interface inheritance
- ✅ Events (`PropertyChanged`, `NeatooPropertyChanged`)

**Resolved in 10.12.0:**
- ✅ Generic methods
- ✅ BCL interface compatibility (117 interfaces tested)

### Key Finding: Neatoo Domain Interfaces Cannot Be Migrated (RESOLVED in 10.12.0)

~~KnockOff v10.11.0 **cannot** generate stubs for Neatoo domain interfaces~~

**✅ RESOLVED in KnockOff 10.12.0** - All Neatoo domain interfaces now work.

| Interface | 10.11.0 | 10.12.0 |
|-----------|---------|---------|
| `IPersonPhoneList` | ❌ Indexer shadowing | ✅ Works |
| `IPersonPhone` | ❌ Internal members | ✅ Works |
| `IPerson` | ❌ Complex hierarchy | ✅ Works |
| `IPersonDbContext` | ❌ Namespace collision | ✅ Works |

**Previously identified blockers (now resolved):**
- ~~Indexer shadowing (`new IEntityProperty this[string]`)~~
- ~~Internal interface members~~
- ~~Namespace/type name collisions~~

**All Neatoo domain interfaces successfully migrated to KnockOff.**

### Completed Migrations

| File | Interface/Delegate | Status | Notes |
|------|-----------|--------|-------|
| `PersonAuthTests.cs` | `IUser` | ✅ Migrated | Simple interface with one property |
| `UniqueNameRuleTests.cs` | `UniqueName.IsUniqueName`, `IPerson`, `IEntityProperty` | ✅ Migrated | Delegate stub with call verification |
| `RuleProxyTests.cs` | `IValidateBase` | ✅ Migrated | Complex interface (10.9.0) |
| `FluentRuleTests.cs` | `IRuleManager`, `IValidateBase` | ✅ Migrated | Generic methods (10.12.0) |
| `PersonTests.cs` | `IPersonDbContext`, `IPersonPhoneListFactory`, `IPersonPhoneList` | ✅ Migrated | Neatoo domain interfaces (10.12.0) |
| `UniquePhoneTypeRuleTests.cs` | `IPerson`, `IPersonPhoneList`, `IPersonPhone` | ✅ Migrated | Complex hierarchy |
| `UniquePhoneNumberRuleTests.cs` | `IPerson`, `IPersonPhoneList`, `IPersonPhone` | ✅ Migrated | Complex hierarchy |

### Migration Pattern for Simple Interfaces

```csharp
// Before (Moq)
var mockUser = new Mock<IUser>();
mockUser.SetupGet(u => u.Role).Returns(userRole);
var auth = new PersonAuth(mockUser.Object);

// After (KnockOff inline stub)
[KnockOff<IUser>]
public partial class PersonAuthTests
{
    var userStub = new Stubs.IUser();
    IUser user = userStub;
    user.Role = userRole;
    var auth = new PersonAuth(user);
}
```

### Migration Pattern for Delegates

```csharp
// Before (Moq)
var mockIsUniqueName = new Mock<UniqueName.IsUniqueName>();
mockIsUniqueName
    .Setup(x => x(It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()))
    .ReturnsAsync(true);
var rule = new UniqueNameRule(mockIsUniqueName.Object);
// Verification
mockIsUniqueName.Verify(x => x(...), Times.Never);

// After (KnockOff inline stub)
[KnockOff<UniqueName.IsUniqueName>]
public partial class UniqueNameRuleTests
{
    var isUniqueStub = new Stubs.IsUniqueName();
    isUniqueStub.Interceptor.OnCall = (ko, id, firstName, lastName) => Task.FromResult(true);
    var rule = new UniqueNameRule(isUniqueStub);  // Implicit conversion to delegate

    // Verification
    Assert.False(isUniqueStub.Interceptor.WasCalled);
    Assert.Equal(0, isUniqueStub.Interceptor.CallCount);
}
```

### Not Migrating (Intentional Moq Usage)

| File | Reason |
|------|--------|
| `TestingRuleSamplesTests.cs` | Documentation samples showing Moq testing patterns |

**Previously blocked, now migrated with 10.12.0:**

| File | Interface | Status |
|------|-----------|--------|
| `FluentRuleTests.cs` | `IRuleManager` | ✅ Migrated (generic methods now supported) |
| `PersonTests.cs` | `IPersonDbContext`, `IPersonPhoneListFactory`, `IPersonPhoneList` | ✅ Migrated |
| `UniquePhoneTypeRuleTests.cs` | `IPerson`, `IPersonPhoneList`, `IPersonPhone` | ✅ Migrated |
| `UniquePhoneNumberRuleTests.cs` | `IPerson`, `IPersonPhoneList`, `IPersonPhone` | ✅ Migrated |

---

## Task List

### Setup
- [x] Verify KnockOff v10.8.0 API syntax for delegate and class stubs
- [x] Add KnockOff 10.8.0 package to:
  - [x] `src/Examples/Person/Person.DomainModel.Tests/`
  - [x] `src/Neatoo.UnitTest/`
  - [x] `docs/samples/Neatoo.Samples.DomainModel.Tests/`
- [x] **Upgrade to KnockOff 10.9.0** (2026-01-08)

### Category Migrations
- [x] **Category 1** - Simple interface stubs:
  - [x] `PersonAuthTests.cs` - Migrated to KnockOff
  - [x] `RuleProxyTests.cs` - **Migrated to KnockOff** (IValidateBase works in 10.9.0!)
- [x] Run tests, verify pass for PersonAuthTests (30 tests pass)
- [x] **Category 4** - Delegate mock: `UniqueNameRuleTests.cs` (`UniqueName.IsUniqueName` → `[KnockOff<TDelegate>]`)
- [x] Run tests, verify pass (3 tests pass)
- [x] **Category 5** - Class stubs: Keep manual `TestPerson` and `TestUniqueNameRule`
- [x] Run tests, verify pass (54 tests pass for entire test project)
- [x] **10.9.0 Retest** - `FluentRuleTests.cs` with `IRuleManager` - Still fails (generic methods)

### Cleanup
- [x] Remove Moq from projects where no longer needed
- [x] Run full test suite, verify all pass

#### Cleanup Conclusion (2026-01-09) - Updated with KnockOff 10.12.0

**Moq removed from 2 of 3 projects:**

| Project | Status | Notes |
|---------|--------|-------|
| `Neatoo.UnitTest` | ✅ **Moq removed** | 1594 pass, 1 skip |
| `Person.DomainModel.Tests` | ✅ **Moq.EntityFrameworkCore removed** | 54 pass |
| `Neatoo.Samples.DomainModel.Tests` | 📝 **Moq kept** | Documentation samples (intentional) |

### Rollback
If migration fails or KnockOff has issues:
- Revert to last working commit
- Keep Moq as fallback until issues resolved
- Document specific failure in this file

## Files Using Moq (Current State - Updated 2026-01-09)

| File | Location | Status | Notes |
|------|----------|--------|-------|
| RuleProxyTests.cs | `src/Neatoo.UnitTest/Unit/Rules/` | ✅ **Migrated** | Uses KnockOff for `IValidateBase` |
| FluentRuleTests.cs | `src/Neatoo.UnitTest/Unit/Rules/` | ✅ **Migrated** | Uses KnockOff for `IRuleManager`, `IValidateBase` |
| TestingRuleSamplesTests.cs | `docs/samples/.../Testing/` | 📝 **Intentional Moq** | Documentation samples |
| UniquePhoneTypeRuleTests.cs | `src/Examples/.../UnitTests/` | ✅ **Migrated** | Uses KnockOff |
| UniquePhoneNumberRuleTests.cs | `src/Examples/.../UnitTests/` | ✅ **Migrated** | Uses KnockOff |
| UniqueNameRuleTests.cs | `src/Examples/.../UnitTests/` | ✅ **Migrated** | Uses KnockOff delegate stub |
| PersonTests.cs | `src/Examples/.../UnitTests/` | ✅ **Migrated** | Uses KnockOff for all interfaces |
| PersonAuthTests.cs | `src/Examples/.../UnitTests/` | ✅ **Migrated** | Uses KnockOff for `IUser` |

### Files Currently Using `using Moq` (Verified 2026-01-09)

| Project | File | Reason |
|---------|------|--------|
| Neatoo.Samples.DomainModel.Tests | `TestingRuleSamplesTests.cs` | Documentation samples (intentional) |

### Stub Pattern Used

All migrations use **inline stubs** (`[KnockOff<T>]` on test class → generates `Stubs.T` nested class). No standalone stubs are used.

## Category Analysis

### Category 1: Simple Interface Property Stubs ✅ Easy

| File | Mock Type | Pattern |
|------|-----------|---------|
| `RuleProxyTests.cs` | `Mock<IValidateBase>` | Just needs object implementing interface |
| `PersonAuthTests.cs` | `Mock<IUser>` | Single property `Role` with different values |

**KnockOff approach:**
```csharp
[KnockOff]
public partial class ValidateBaseKnockOff : IValidateBase { }

[KnockOff]
public partial class UserKnockOff : IUser { }
```

---

### Category 2: Interface with Multiple Property/Method Setups ✅ Medium

| File | Mock Types | Complexity |
|------|------------|------------|
| `UniquePhoneTypeRuleTests.cs` | `IPerson`, `IPersonPhoneList`, `IPersonPhone` | Multiple properties + `GetEnumerator()` |
| `UniquePhoneNumberRuleTests.cs` | Same as above | Same pattern |
| `TestingRuleSamplesTests.cs` | `INamedEntity`, `IOrderHeader`, `ILineItem`, `IEntityProperty` | Multiple properties + parent navigation |

**KnockOff approach:**
```csharp
[KnockOff]
public partial class PersonPhoneKnockOff : IPersonPhone
{
    // Properties auto-backed by KnockOff
}

[KnockOff]
public partial class PersonPhoneListKnockOff : IPersonPhoneList
{
    private List<IPersonPhone> _items = new();

    // User method for GetEnumerator
    protected IEnumerator<IPersonPhone> GetEnumerator() => _items.GetEnumerator();
}
```

---

### Category 3: Verification/Call Tracking ✅ Medium

| File | Verification Pattern |
|------|---------------------|
| `UniqueNameRuleTests.cs` | `Verify(x => x(...), Times.Never)` |
| `TestingRuleSamplesTests.cs` | `Verify(c => c.IsUnique(...), Times.Once)` |
| `PersonTests.cs` | `Verify(x => x.AddPerson(...), Times.Once)` |

**Migration pattern:**
```csharp
// Moq
mockCheckUnique.Verify(c => c.IsUnique("Name", 1), Times.Once);

// KnockOff
Assert.Equal(1, knockOff.ICheckNameUnique.IsUnique.CallCount);
Assert.Equal(("Name", 1), knockOff.ICheckNameUnique.IsUnique.LastCallArgs);
```

---

### Category 4: Delegate Mocking ✅ Native Support (v10.8.0)

| File | Pattern |
|------|---------|
| `UniqueNameRuleTests.cs` | `new Mock<UniqueName.IsUniqueName>()` |

**Issue:** Moq can mock delegates directly. KnockOff is interface-only.

**Workaround:** Create wrapper interface or use simple test implementation:
```csharp
// Option 1: Wrapper interface
public interface IIsUniqueName
{
    Task<bool> Check(Guid? id, string firstName, string lastName);
}

// Option 2: Simple implementation
public class TestIsUniqueName
{
    public bool ReturnValue { get; set; } = true;
    public int CallCount { get; private set; }

    public Task<bool> Invoke(Guid? id, string firstName, string lastName)
    {
        CallCount++;
        return Task.FromResult(ReturnValue);
    }
}
```

---

### Category 5: Concrete Class Mocking with CallBase ⏳ Partial

| File | Original Pattern | Resolution |
|------|------------------|------------|
| `PersonTests.cs` | `new Mock<Person>(...) { CallBase = true }` | Manual `TestPerson` stub |
| `PersonTests.cs` | `new Mock<AsyncRuleBase<IPerson>>().As<IUniqueNameRule>() { CallBase = true }` | Manual `TestUniqueNameRule` stub |

**Solution:** Created test subclasses that inherit from the real classes and override virtual members.

**Location:** `src/Examples/Person/Person.DomainModel.Tests/TestDoubles/`

**TestPerson.cs:**
```csharp
internal class TestPerson : Person
{
    public bool IsSavableOverride { get; set; } = true;
    public int RunRulesCallCount { get; private set; }

    public TestPerson(IEntityBaseServices<Person> services, IUniqueNameRule rule)
        : base(services, rule)
    {
    }

    public override bool IsSavable => IsSavableOverride;

    public override Task RunRules(RunRulesFlag runRules = RunRulesFlag.All, CancellationToken? token = null)
    {
        RunRulesCallCount++;
        return Task.CompletedTask;
    }
}
```

**TestUniqueNameRule.cs:**
```csharp
internal class TestUniqueNameRule : AsyncRuleBase<IPerson>, IUniqueNameRule
{
    public int OnRuleAddedCallCount { get; private set; }
    public IRuleManager? LastRuleManager { get; private set; }
    public uint LastUniqueIndex { get; private set; }

    protected override Task<IRuleMessages> Execute(IPerson t, CancellationToken? token = null)
        => Task.FromResult<IRuleMessages>(None);

    public override void OnRuleAdded(IRuleManager ruleManager, uint uniqueIndex)
    {
        OnRuleAddedCallCount++;
        LastRuleManager = ruleManager;
        LastUniqueIndex = uniqueIndex;
        base.OnRuleAdded(ruleManager, uniqueIndex);
    }
}
```

**Why this is better than Moq:**
- Debuggable (real code, not proxy)
- Compile-time errors if signature changes
- No reflection/Castle.Core dependency
- Explicit about what's being controlled
- **Zero `CallBase` usage remaining in codebase**

**Remaining Moq in PersonTests.cs:** Interface mocks for `IPersonDbContext`, `IPersonPhoneListFactory`, `IPersonPhoneList` - these can be migrated to KnockOff later.

---

### Category 6: Nested Property Access with It.Is<> ✅ Simple with Direct Implementation

| File | Pattern |
|------|---------|
| `UniqueNameRuleTests.cs` | `mockPerson.Setup(x => x[It.Is<string>(...)].IsModified)` |

**Issue:** Moq's `It.Is<T>()` with chained property access looks like magic, but it's just returning a stub from an indexer.

**KnockOff approach - Direct implementation in partial class:**
```csharp
[KnockOff]
public partial class EntityPropertyKnockOff : IEntityProperty
{
    public bool IsModifiedValue { get; set; } = true;  // Configurable per-test
    public bool IsModified => IsModifiedValue;
}

[KnockOff]
public partial class PersonKnockOff : IPerson
{
    private EntityPropertyKnockOff _propertyStub = new();

    public IEntityProperty this[string propertyName] => _propertyStub;
}
```

**Test usage:**
```csharp
// For IsModified = true (default)
var personKnockOff = new PersonKnockOff();
IPerson person = personKnockOff;

// For IsModified = false
var personKnockOff = new PersonKnockOff();
personKnockOff._propertyStub.IsModifiedValue = false;
```

**Why this works:** KnockOff generates a partial class - you can add your own members that implement interface members directly. No callbacks needed.

---

## Summary Assessment

| Category | Files | KnockOff Feasibility | Effort | Status |
|----------|-------|---------------------|--------|--------|
| Simple property stubs | 2 | ✅ Easy | Low | Pending |
| Multi-property interfaces | 3 | ✅ Medium | Medium | Pending |
| Call verification | 3 | ✅ Medium | Medium | Pending |
| Delegate mocking | 1 | ✅ Native support | Medium | Pending |
| Concrete class + CallBase | 2 | ✅ Manual stub | Low | **Partial** |
| Nested property matching | 1 | ✅ Direct implementation | Low | Pending |

## Recommendation

**KnockOff can replace most Moq usage, but requires different patterns for some cases.**

### Resolved Blockers
- **Category 5 (Concrete class mocking):** Solved with manual test subclasses
  - `TestPerson` - inherits from `Person`, overrides `IsSavable` and `RunRules`
  - `TestUniqueNameRule` - inherits from `AsyncRuleBase<IPerson>`, tracks `OnRuleAdded` calls
  - **Zero `CallBase` usage remaining in codebase**

### Path Forward
1. ✅ Category 5 resolved with manual stubs (TestPerson, TestUniqueNameRule)
2. ✅ Category 6 simplified with direct implementation in partial class
3. Migrate Categories 1-3 to KnockOff (straightforward)
4. Migrate Category 4 with native delegate support
5. Migrate Category 6 with direct implementation
6. Remove Moq package dependency

## Decision

**Use KnockOff for all mocking, including base class stubs.** Neatoo will showcase KnockOff capabilities since both projects are by the same author.

---

## Deep Analysis: KnockOff v10.8.0 Features (2026-01-07)

### Key Finding: Evaluation Gaps Are Now Resolved

KnockOff v10.8.0 (released 2026-01-07) adds features that address the identified gaps:

| Gap | Original Assessment | v10.8.0 Status |
|-----|---------------------|----------------|
| Category 4 (Delegate mocking) | "Needs Wrapper" | **Native support via `[KnockOff<TDelegate>]`** |
| Category 5 (Base class mocking) | "Resolved with manual stub" | **Native support via `[KnockOff<TClass>]`** |

---

### Delegate Stub Scenario (Category 4)

#### Current Moq Pattern (UniqueNameRuleTests.cs)

```csharp
var mockIsUniqueName = new Mock<UniqueName.IsUniqueName>();
mockIsUniqueName
    .Setup(x => x(It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()))
    .ReturnsAsync(true);

var rule = new UniqueNameRule(mockIsUniqueName.Object);

// Verification
mockIsUniqueName.Verify(x => x(It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
```

#### Delegate Signature

```csharp
public delegate Task<bool> IsUniqueName(Guid? id, string firstName, string lastName);
```

#### KnockOff Solution

> ⚠️ **Syntax needs verification** - The API shown below is hypothetical based on expected patterns. Verify against actual KnockOff v10.8.0 API before migration.

```csharp
[KnockOff<UniqueName.IsUniqueName>]
public partial class UniqueNameRuleTests
{
    [Fact]
    public async Task Execute_ShouldReturnNone_WhenNameIsUnique()
    {
        // Arrange
        var isUniqueStub = new Stubs.IsUniqueName();
        isUniqueStub.Interceptor.OnCall = (ko, id, firstName, lastName) =>
            Task.FromResult(true);

        var rule = new UniqueNameRule(isUniqueStub);  // Implicit conversion

        var personKnockOff = new PersonKnockOff();  // Interface stub
        // ... configure person stub ...

        // Act
        var result = await rule.RunRule(personKnockOff);

        // Assert
        Assert.Equal(RuleMessages.None, result);
        Assert.True(isUniqueStub.Interceptor.WasCalled);
    }

    [Fact]
    public async Task Execute_ShouldReturnNone_WhenNameIsNotModified()
    {
        // Arrange
        var isUniqueStub = new Stubs.IsUniqueName();
        // No OnCall configured - won't be called

        // ... test setup ...

        // Assert - equivalent to Times.Never
        Assert.False(isUniqueStub.Interceptor.WasCalled);
        Assert.Equal(0, isUniqueStub.Interceptor.CallCount);
    }
}
```

#### Benefits

| Feature | Moq | KnockOff |
|---------|-----|----------|
| Delegate stubbing | Runtime proxy | Compile-time generated |
| Type safety | Partial (reflection) | Full (generated code) |
| Verification | `Verify(x => ..., Times.Once)` | `Assert.Equal(1, stub.Interceptor.CallCount)` |
| Arg capture | `It.Is<>()` callbacks | `stub.Interceptor.LastCallArgs` with named tuples |
| Debuggability | Proxy objects | Real code |

---

### Base Class Stub Scenario (Category 5)

#### Current Manual Stub

See [Category 5](#category-5-concrete-class-mocking-with-callbase--partial) for `TestPerson.cs` and `TestUniqueNameRule.cs` implementations.

#### KnockOff Class Stub Alternative

> ⚠️ **Syntax needs verification** - The API shown below is hypothetical based on expected patterns. Verify against actual KnockOff v10.8.0 API before migration.

```csharp
[KnockOff<Person>]
public partial class PersonTests
{
    [Fact]
    public async Task Insert_ShouldReturnPersonEntity_WhenModelIsSavable()
    {
        // Arrange
        var personStub = new Stubs.Person(new EntityBaseServices<Person>(null), testUniqueNameRule);

        // Configure IsSavable to return true
        personStub.Interceptor.IsSavable.OnGet = (ko) => true;

        // Configure RunRules to complete immediately
        personStub.Interceptor.RunRules.OnCall = (ko, runRules, token) => Task.CompletedTask;

        // ... rest of test ...

        // Assert
        Assert.Equal(1, personStub.Interceptor.RunRules.CallCount);
    }

    [Fact]
    public async Task Insert_ShouldReturnNull_WhenModelIsNotSavable()
    {
        var personStub = new Stubs.Person(new EntityBaseServices<Person>(null), testUniqueNameRule);

        // Configure IsSavable to return false
        personStub.Interceptor.IsSavable.OnGet = (ko) => false;

        // ... test ...
    }
}
```

#### Trade-off Analysis

| Aspect | Manual Stub (TestPerson) | KnockOff Class Stub |
|--------|--------------------------|---------------------|
| **Boilerplate** | More code to write | Less boilerplate |
| **Call tracking** | Manual (`CallCount++`) | Automatic (`Interceptor.CallCount`) |
| **Arg tracking** | Manual (if needed) | Automatic (`LastCallArg`, `LastCallArgs`) |
| **Flexibility** | Full control | OnCall callbacks |
| **Dependencies** | None | KnockOff package |
| **Discoverability** | Self-documenting | Requires API knowledge |
| **Existing code** | Already written | Would need migration |

#### Decision: Use KnockOff Class Stubs

**Migrate TestPerson and TestUniqueNameRule to KnockOff class stubs.**

Rationale:
- Neatoo showcases KnockOff capabilities (same author)
- Demonstrates real-world class stub usage
- Provides automatic call tracking without manual boilerplate
- Consistent pattern across all stub types (interface, delegate, class)
- Serves as documentation/example for KnockOff users

---

### Validated KnockOff Repository

**Repository:** `c:\src\neatoodotnet\KnockOff`
**Version:** 10.8.0 (released 2026-01-07)

**Features Verified:**
- [x] Delegate stubs with `[KnockOff<TDelegate>]` - working
- [x] Class stubs with `[KnockOff<TClass>]` - working
- [x] Multi-parameter delegate support - working
- [x] Async delegate return types - working
- [x] Constructor chaining for class stubs - working

**Skill Gap Found:**
The KnockOff Claude skill (`~/.claude/skills/knockoff/SKILL.md`) is missing documentation for class stubs. Tracked in: `c:\src\neatoodotnet\KnockOff\docs\todos\skill-documentation-gaps.md`

---

### Migration Path

**Prerequisites:**
1. Add KnockOff package to `Person.DomainModel.Tests`

**Migration Steps:**
1. **Category 5 (Base class):** Migrate to `[KnockOff<TClass>]` - replace TestPerson, TestUniqueNameRule
2. **Category 4 (Delegates):** Migrate using `[KnockOff<TDelegate>]` - no wrapper needed
3. **Category 1-3:** Migrate to KnockOff interface stubs
4. **Category 6:** Migrate with direct implementation in partial class
5. Remove Moq package dependency
6. Delete `TestDoubles/` folder
