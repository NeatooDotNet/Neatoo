# TODO: Verify and Document Rules Paused During Fetch

**Status: COMPLETED** (2025-12-30)

## Background

There's documentation suggesting "Use LoadProperty() in fetch to prevent the rules from triggering." However, rules are actually **paused during fetch operations** by the framework, making this guidance potentially misleading or outdated.

## Investigation Complete ✓

### Confirmed: Rules ARE Automatically Paused During Factory Operations

#### The Mechanism

1. **`FactoryStart()` automatically called by RemoteFactory**
   - Location: `ValidateBase.cs:519-521`
   - Code: `this.PauseAllActions();`
   - Called before any factory method (`[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`)

2. **`PauseAllActions()` pauses everything**
   - Rules don't trigger (ValidateBase.IsPaused = true)
   - Properties are NOT marked as modified (EntityPropertyManager.IsPaused = true)
   - PropertyChanged events are suppressed

3. **`FactoryComplete()` resumes everything**
   - Location: `ValidateBase.cs:531-533`
   - Code: `this.ResumeAllActions();`
   - Called after factory method completes

4. **Verification in EntityProperty.OnPropertyChanged**
   ```csharp
   if (propertyName == nameof(Value))
   {
       if (!this.IsPaused)  // Only marks modified when NOT paused
       {
           this.IsSelfModified = true && this.EntityChild == null;
       }
   }
   ```

### LoadValue() vs Regular Setters

| Scenario | Regular Setter | LoadValue() |
|----------|----------------|-------------|
| During Factory Operation | Rules paused, NOT marked modified | Rules paused, NOT marked modified |
| Outside Factory Operation | Rules trigger, marked modified | Rules DON'T trigger, NOT marked modified |

**Conclusion**: `LoadValue()` is NOT needed in factory methods (`[Fetch]`, `[Create]`, etc.) because `FactoryStart()` already pauses everything.

### When to Use LoadValue()

1. **In rules** - Use `LoadProperty()` (which calls `LoadValue()`) to set side-effect properties without triggering cascading rules
2. **Outside factory methods** - When explicitly loading values without modification tracking
3. **Manual fetch helpers** - If calling a fetch method directly (not through factory), the helper might need explicit pausing

### Redundant Patterns Found

In `PersonEntityBase.cs`:
```csharp
public void FromDto(PersonDto dto)
{
    using var pause = this.PauseAllActions();  // REDUNDANT when called from [Fetch]
    this[nameof(Id)].LoadValue(dto.PersonId);  // LoadValue not needed during paused state
    MapFrom(dto);
}
```

The explicit `PauseAllActions()` is redundant when `FromDto` is called from a factory method like `FillFromDto`. However, it's a safety measure if `FromDto` is called directly in tests.

### Correct Pattern (Person.cs)

The Person example already uses the correct pattern:
```csharp
[Fetch]
public async Task<bool> Fetch([Service] IPersonDbContext personContext, ...)
{
    var personEntity = await personContext.FindPerson();
    if (personEntity == null) return false;

    this.MapFrom(personEntity);  // Regular setters are fine - rules paused automatically
    this.PersonPhoneList = personPhoneModelListFactory.Fetch(personEntity.Phones);
    return true;
}
```

## Tasks

### 1. Confirm Rules Are Paused During Fetch ✓

- [x] Find the code that pauses rules during fetch operations
  - `ValidateBase.FactoryStart()` calls `PauseAllActions()`
- [x] Verify the mechanism (PauseAllActions, specific flag, etc.)
  - `IsPaused` flag on both object and property manager
- [x] Document exactly when rules are paused vs. active
  - During all factory operations: Create, Fetch, Insert, Update, Delete
- [x] Confirm this applies to all fetch scenarios (Create, Fetch, remote operations)
  - Yes, `IFactoryOnStart` interface triggers `FactoryStart()` for all factory operations

### 2. Update Tests ⏳

Future improvement - add explicit tests for rule pausing behavior:
- [ ] Add/update tests that verify rules don't trigger during fetch
- [ ] Test that rules DO trigger after fetch completes and properties are modified
- [ ] Ensure test coverage for edge cases (nested fetches, list fetches, etc.)

### 3. Update Examples ✓

- [x] Review Person example for unnecessary LoadProperty() calls in fetch methods
  - Person example is CORRECT - uses regular setters in fetch
- [x] Review test aggregates for unnecessary patterns
  - PersonEntityBase.FromDto has explicit PauseAllActions (defensive programming for direct calls in tests)
  - Left as-is since it's test infrastructure

### 4. Update Code Comments ⏳

Future improvement - code comments are minimal but accurate:
- [ ] Search for comments mentioning LoadProperty() preventing rule execution
- [ ] Add comments explaining that rules are paused during fetch (if helpful)

### 5. Update Documentation ✓

Files updated:
- [x] `docs/validation-and-rules.md` - Added note clarifying LoadValue not needed in fetch methods
- [x] `docs/property-system.md` - Updated LoadValue guidance, added factory operations note
- [x] `docs/mapper-methods.md` - Updated to reflect manual MapFrom/MapTo implementation

Documentation updates completed:
- [x] Explained that rules are automatically paused during factory operations
- [x] Clarified when to use LoadValue() vs regular property setters
- [x] Documented that MapFrom/MapTo are now manually implemented (not generated)

## Key Documentation Points

### For Users

1. **You don't need LoadProperty() in fetch methods** - Regular property setters are fine because rules are automatically paused during factory operations.

2. **Use LoadProperty() only in rules** - When a rule needs to set a property value as a side-effect, use `LoadProperty()` to avoid triggering cascading rules.

3. **The lifecycle is automatic**:
   - Factory method starts → rules paused, modification tracking off
   - Factory method completes → rules resume, modification tracking on
   - User modifies property → rules execute, property marked as modified

### Technical Details

- `IFactoryOnStart` interface implementation triggers `FactoryStart()`
- `IFactoryOnComplete` interface implementation triggers `FactoryComplete()`
- Both `ValidateBase` and `EntityBase` implement these interfaces
- RemoteFactory's generated code automatically calls these lifecycle methods
