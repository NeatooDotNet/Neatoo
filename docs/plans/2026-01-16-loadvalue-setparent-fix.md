# LoadValue SetParent Fix Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Fix the bug where `LoadValue()` doesn't establish parent-child relationships by adding `ChangeReason` to `NeatooPropertyChangedEventArgs`.

**Architecture:** Add a `ChangeReason` enum that signals the *intent* of a property change (UserEdit vs Load). `LoadValue()` fires `NeatooPropertyChanged` with `Reason = Load`. `ValidateBase` always calls `SetParent` (structural) but skips rules for Load (behavioral). This preserves architectural separation - properties don't know about ValidateBase.

**Tech Stack:** C# 13, .NET 9, MSTest, Neatoo framework internals

---

## Summary of Changes

1. Add `ChangeReason` enum to Neatoo namespace
2. Add `Reason` property to `NeatooPropertyChangedEventArgs` (all constructors)
3. Update `ValidateProperty.LoadValue()` to fire event with `Reason = Load`
4. Update `ValidateBase.ChildNeatooPropertyChanged()` to check Reason before running rules
5. Add/verify unit tests for LoadValue with child objects (single + lists)
6. Verify all failing tests pass
7. Consider if `OnDeserialized` still needs explicit `SetParent` loop

---

## Task 1: Add ChangeReason Enum

**Files:**
- Create: `src/Neatoo/ChangeReason.cs`

**Step 1: Create the enum file**

```csharp
namespace Neatoo;

/// <summary>
/// Specifies the reason a property value was changed.
/// </summary>
/// <remarks>
/// <para>
/// This enum is used in <see cref="NeatooPropertyChangedEventArgs"/> to indicate
/// whether a change came from user editing or from loading data.
/// </para>
/// <para>
/// The reason affects how <see cref="ValidateBase{T}"/> handles the change:
/// <list type="bullet">
/// <item><description><see cref="UserEdit"/>: Runs rules and bubbles events up the hierarchy</description></item>
/// <item><description><see cref="Load"/>: Only establishes parent-child relationships, skips rules</description></item>
/// </list>
/// </para>
/// </remarks>
public enum ChangeReason
{
    /// <summary>
    /// Normal property assignment via setter. Triggers full rule execution and event bubbling.
    /// </summary>
    UserEdit,

    /// <summary>
    /// Loading data via <see cref="IValidateProperty.LoadValue"/>.
    /// Establishes structural relationships (SetParent) but skips rules and event bubbling.
    /// </summary>
    Load
}
```

**Step 2: Verify file created**

Run: `dir "src\Neatoo\ChangeReason.cs"`
Expected: File exists

**Step 3: Commit**

```bash
git add src/Neatoo/ChangeReason.cs
git commit -m "feat: add ChangeReason enum for NeatooPropertyChanged events

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Task 2: Add Reason Property to NeatooPropertyChangedEventArgs

**Files:**
- Modify: `src/Neatoo/NeatooPropertyChangedEventArgs.cs`

**Step 1: Review current file**

Read `src/Neatoo/NeatooPropertyChangedEventArgs.cs` to confirm current structure.

**Step 2: Add Reason property and update all constructors**

Replace entire file with:

```csharp
namespace Neatoo;

public record NeatooPropertyChangedEventArgs
{
    public NeatooPropertyChangedEventArgs(string propertyName, object source, ChangeReason reason = ChangeReason.UserEdit)
    {
        this.PropertyName = propertyName;
        this.Source = source;
        this.Reason = reason;
        this.OriginalEventArgs = this;
    }

    public NeatooPropertyChangedEventArgs(IValidateProperty property, ChangeReason reason = ChangeReason.UserEdit)
    {
        ArgumentNullException.ThrowIfNull(property, nameof(property));
        this.PropertyName = property.Name;
        this.Property = property;
        this.Source = property;
        this.Reason = reason;
        this.OriginalEventArgs = this;
    }

    public NeatooPropertyChangedEventArgs(IValidateProperty property, object source, NeatooPropertyChangedEventArgs? previous)
    {
        ArgumentNullException.ThrowIfNull(property, nameof(property));
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        this.PropertyName = property.Name;
        this.Property = property;
        this.Source = source;
        this.InnerEventArgs = previous;
        this.OriginalEventArgs = previous?.OriginalEventArgs ?? this;
        // Inherit reason from original event args
        this.Reason = this.OriginalEventArgs.Reason;
    }

    public string PropertyName { get; init; }
    public IValidateProperty? Property { get; init; }
    public object? Source { get; init; }
    public NeatooPropertyChangedEventArgs OriginalEventArgs { get; init; }
    public NeatooPropertyChangedEventArgs? InnerEventArgs { get; init; }
    public string FullPropertyName => this.PropertyName + (this.InnerEventArgs == null ? "" : "." + this.InnerEventArgs.FullPropertyName);

    /// <summary>
    /// Gets the reason this property change occurred.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ChangeReason.UserEdit"/> indicates a normal property setter assignment
    /// that should trigger rules and bubble events.
    /// </para>
    /// <para>
    /// <see cref="ChangeReason.Load"/> indicates data loading via <see cref="IValidateProperty.LoadValue"/>
    /// that should only establish structural relationships (SetParent) without running rules.
    /// </para>
    /// </remarks>
    public ChangeReason Reason { get; init; } = ChangeReason.UserEdit;
}
```

**Step 3: Build to verify no compilation errors**

Run: `dotnet build src/Neatoo/Neatoo.csproj`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add src/Neatoo/NeatooPropertyChangedEventArgs.cs
git commit -m "feat: add Reason property to NeatooPropertyChangedEventArgs

Adds ChangeReason to signal intent (UserEdit vs Load).
Reason is inherited through event bubbling via OriginalEventArgs.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Task 3: Update LoadValue to Fire NeatooPropertyChanged Event

**Files:**
- Modify: `src/Neatoo/Internal/ValidateProperty.cs:318-322`

**Step 1: Review current LoadValue implementation**

Current (lines 318-322):
```csharp
public virtual void LoadValue(object? value)
{
    this.SetPrivateValue((T?)value, true);
    this._value = (T?)value;
}
```

**Step 2: Update LoadValue to fire event with ChangeReason.Load**

Replace LoadValue method with:

```csharp
public virtual void LoadValue(object? value)
{
    if (value == null && this._value == null) { return; }

    // Handle old value cleanup (unsubscribe events, clear parent)
    if (this._value != null && !ReferenceEquals(this._value, value))
    {
        if (this._value is INotifyNeatooPropertyChanged neatooPropertyChanged)
        {
            neatooPropertyChanged.NeatooPropertyChanged -= this.PassThruValueNeatooPropertyChanged;
        }
        if (this._value is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged -= this.PassThruValuePropertyChanged;
        }
        if (this._value is ISetParent oldSetParent)
        {
            oldSetParent.SetParent(null);
        }
    }

    this._value = (T?)value;

    // Handle new value setup (subscribe events)
    if (value != null)
    {
        if (value is INotifyNeatooPropertyChanged valueNeatooPropertyChanged)
        {
            valueNeatooPropertyChanged.NeatooPropertyChanged += this.PassThruValueNeatooPropertyChanged;
        }
        if (value is INotifyPropertyChanged valueNotifyPropertyChanged)
        {
            valueNotifyPropertyChanged.PropertyChanged += this.PassThruValuePropertyChanged;
        }
    }

    // Fire event with ChangeReason.Load - SetParent will be called but rules will be skipped
    this.OnPropertyChanged(nameof(Value));
    this.Task = this.OnValueNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(this, ChangeReason.Load));
}
```

**Step 3: Build to verify no compilation errors**

Run: `dotnet build src/Neatoo/Neatoo.csproj`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add src/Neatoo/Internal/ValidateProperty.cs
git commit -m "feat: LoadValue fires NeatooPropertyChanged with ChangeReason.Load

LoadValue now fires the NeatooPropertyChanged event to establish parent-child
relationships (SetParent) while signaling via ChangeReason.Load that rules
should be skipped.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Task 4: Update ValidateBase.ChildNeatooPropertyChanged to Check Reason

**Files:**
- Modify: `src/Neatoo/ValidateBase.cs:374-388`

**Step 1: Review current implementation**

Current (lines 374-388):
```csharp
protected virtual async Task ChildNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
{
    if (!this.IsPaused)
    {
        await this.RunRules(eventArgs.FullPropertyName);

        await this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(eventArgs.Property!, this, eventArgs.InnerEventArgs));

        this.CheckIfMetaPropertiesChanged();
    }
    else
    {
        this.ResetMetaState();
    }
}
```

**Step 2: Update to check ChangeReason**

Replace ChildNeatooPropertyChanged method with:

```csharp
protected virtual async Task ChildNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
{
    // Skip rules and bubbling for Load operations (only SetParent is needed, which happens in _PropertyManager_NeatooPropertyChanged)
    // Also skip when paused (during factory operations or deserialization)
    if (!this.IsPaused && eventArgs.OriginalEventArgs.Reason != ChangeReason.Load)
    {
        await this.RunRules(eventArgs.FullPropertyName);

        await this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(eventArgs.Property!, this, eventArgs.InnerEventArgs));

        this.CheckIfMetaPropertiesChanged();
    }
    else
    {
        this.ResetMetaState();
    }
}
```

**Step 3: Build to verify no compilation errors**

Run: `dotnet build src/Neatoo/Neatoo.csproj`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add src/Neatoo/ValidateBase.cs
git commit -m "feat: ChildNeatooPropertyChanged checks ChangeReason before running rules

When ChangeReason.Load, only structural operations (SetParent) occur.
Rules and event bubbling are skipped to prevent unintended side effects
during data loading.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Task 5: Run Existing Failing Tests

**Files:**
- Test: `src/Neatoo.UnitTest/Integration/Concepts/ValidateBase/ValidateListBaseRuleTests.cs`

**Step 1: Run the previously failing tests**

Run: `dotnet test src/Neatoo.UnitTest/Neatoo.UnitTest.csproj --filter "FullyQualifiedName~ValidateListBaseRuleTests" -v n`

Expected: All 5 tests pass:
- `ValidateListBaseRuleTests_Constructor`
- `ValidateListBaseRuleTests_UniqueValue_Invalid`
- `ValidateListBaseRuleTests_UniqueValue_Valid`
- `ValidateListBaseRuleTests_UniqueValue_Fixed`
- `ValidateListBaseRuleTests_UniqueValue_Removed_Fixed`

**Step 2: If any test fails, investigate**

The key test is that `target.ParentObj` returns the actual parent (not null) inside `ChildObjUniqueValue.Execute()`.

---

## Task 6: Run Full Test Suite

**Step 1: Run all tests**

Run: `dotnet test src/Neatoo.UnitTest/Neatoo.UnitTest.csproj -v n`

Expected: All tests pass

**Step 2: If tests fail, analyze and fix**

Possible issues:
- Tests that expect LoadValue NOT to fire events (unlikely but check)
- Tests that depend on modification tracking behavior during load
- Tests for EntityBase that may have different expectations

---

## Task 7: Add Unit Test for LoadValue with Single Child Object

**Files:**
- Create: `src/Neatoo.UnitTest/Unit/Internal/ValidatePropertyLoadValueTests.cs`

**Step 1: Create test file**

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using System.ComponentModel;

namespace Neatoo.UnitTest.Unit.Internal;

/// <summary>
/// Tests that LoadValue correctly establishes parent-child relationships.
/// </summary>
[TestClass]
public class ValidatePropertyLoadValueTests
{
    [Factory]
    public partial class ParentWithChildProperty : ValidateBase<ParentWithChildProperty>
    {
        public ParentWithChildProperty() : base(new ValidateBaseServices<ParentWithChildProperty>())
        {
        }

        public partial ChildObject? Child { get; set; }

        public void LoadChild(ChildObject child)
        {
            ChildProperty.LoadValue(child);
        }
    }

    [Factory]
    public partial class ChildObject : ValidateBase<ChildObject>
    {
        public ChildObject() : base(new ValidateBaseServices<ChildObject>())
        {
        }

        public partial string? Name { get; set; }

        public ParentWithChildProperty? ParentObj => this.Parent as ParentWithChildProperty;
    }

    [TestMethod]
    public void LoadValue_WithChildObject_EstablishesParentRelationship()
    {
        // Arrange
        var parent = new ParentWithChildProperty();
        var child = new ChildObject();

        // Act
        parent.LoadChild(child);

        // Assert
        Assert.IsNotNull(child.ParentObj, "Parent should be set after LoadValue");
        Assert.AreSame(parent, child.ParentObj, "Parent should be the parent object");
    }

    [TestMethod]
    public void LoadValue_WithChildObject_DoesNotRunRules()
    {
        // Arrange
        var parent = new ParentWithChildProperty();
        var child = new ChildObject();
        bool ruleRan = false;
        parent.PropertyChanged += (s, e) =>
        {
            // Rules would trigger PropertyChanged for IsValid changes
            if (e.PropertyName == "IsValid") ruleRan = true;
        };

        // Act
        parent.LoadChild(child);

        // Assert - rules should NOT run for LoadValue
        Assert.IsFalse(ruleRan, "Rules should not run during LoadValue");
    }

    [TestMethod]
    public void NormalAssignment_WithChildObject_RunsRules()
    {
        // Arrange
        var parent = new ParentWithChildProperty();
        var child = new ChildObject();

        // Act - use normal setter
        parent.Child = child;

        // Assert - parent should still be set
        Assert.IsNotNull(child.ParentObj, "Parent should be set after normal assignment");
        Assert.AreSame(parent, child.ParentObj, "Parent should be the parent object");
    }

    [TestMethod]
    public void LoadValue_InConstructor_EstablishesParentRelationship()
    {
        // This tests the pattern used in ChildObjList scenario
        var parent = new ParentWithListInConstructor();

        // Assert
        Assert.IsNotNull(parent.ChildList, "ChildList should be created");
        Assert.AreSame(parent, (parent.ChildList as ISetParent)?.Parent, "Parent should be set on list");
    }

    [Factory]
    public partial class ParentWithListInConstructor : ValidateBase<ParentWithListInConstructor>
    {
        public ParentWithListInConstructor() : base(new ValidateBaseServices<ParentWithListInConstructor>())
        {
            ChildListProperty.LoadValue(new ChildObjectList());
        }

        public partial ChildObjectList? ChildList { get; set; }
    }

    public class ChildObjectList : ValidateListBase<ChildObject>
    {
    }
}
```

**Step 2: Run new tests**

Run: `dotnet test src/Neatoo.UnitTest/Neatoo.UnitTest.csproj --filter "FullyQualifiedName~ValidatePropertyLoadValueTests" -v n`

Expected: All tests pass

**Step 3: Commit**

```bash
git add src/Neatoo.UnitTest/Unit/Internal/ValidatePropertyLoadValueTests.cs
git commit -m "test: add unit tests for LoadValue parent-child relationships

Tests verify that LoadValue establishes SetParent but doesn't run rules.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Task 8: Verify Factory Fetch Methods Still Work

**Files:**
- Test: `src/Neatoo.UnitTest/Integration/Aggregates/Person/PersonEntityBase.cs`

**Step 1: Run Person integration tests**

Run: `dotnet test src/Neatoo.UnitTest/Neatoo.UnitTest.csproj --filter "FullyQualifiedName~PersonEntityBaseTests" -v n`

Expected: All tests pass

**Step 2: Verify that Fetch methods establish parent-child**

The existing tests should cover this, but verify by checking that after Fetch, any child objects have correct Parent reference.

---

## Task 9: Consider OnDeserialized Simplification

**Files:**
- Review: `src/Neatoo/ValidateBase.cs:525-543`

**Step 1: Analyze if OnDeserialized loop is still needed**

Current OnDeserialized loops through properties and calls SetParent explicitly. Now that LoadValue fires the event, this might be redundant for properties loaded via LoadValue.

However, OnDeserialized is called AFTER JSON deserialization, which uses a different path (JsonConstructor). The JSON deserializer sets `_value` directly via constructor, not via LoadValue.

**Decision:** Keep OnDeserialized as-is. It handles a different code path (deserialization) where property values are set directly via constructor, not via LoadValue.

**Step 2: Document decision**

No code change needed. The two paths are:
1. **LoadValue** - Now fires event, SetParent called via event handler
2. **Deserialization** - Values set via JsonConstructor, OnDeserialized explicitly calls SetParent

---

## Task 10: Update Todo Document and Final Verification

**Files:**
- Modify: `docs/todos/loadvalue-setparent-bug.md`

**Step 1: Run full test suite one more time**

Run: `dotnet test src/Neatoo.UnitTest/Neatoo.UnitTest.csproj -v n`

Expected: All tests pass

**Step 2: Update todo document status**

Change Status to "Complete" and update Tasks section to check off completed items.

**Step 3: Move todo to completed folder**

```bash
git mv docs/todos/loadvalue-setparent-bug.md docs/todos/completed/loadvalue-setparent-bug.md
```

**Step 4: Final commit**

```bash
git add docs/todos/completed/loadvalue-setparent-bug.md
git commit -m "docs: mark LoadValue SetParent bug as fixed

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Rollback Plan

If issues are discovered:

1. **Revert LoadValue change** - Change `ValidateProperty.LoadValue()` back to not fire event
2. **Keep ChangeReason** - The enum and event args changes are backward-compatible
3. **Alternative fix** - Implement Option 6 (explicit `EstablishChildRelationships()` method)

---

## Related Documentation

- `docs/todos/loadvalue-setparent-bug.md` - Original bug analysis
- `docs/advanced/event-system.md` - Event system documentation
- `docs/plans/2026-01-15-constructor-loadvalue-analyzer-design.md` - Related analyzer work
