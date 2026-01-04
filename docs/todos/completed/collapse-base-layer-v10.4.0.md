# Plan: Collapse Base Layer Inheritance Chains

## Overview

Remove the "Base" layer from all Neatoo inheritance chains, making "Validate" the new foundation. This simplifies the framework by eliminating an unused abstraction layer.

**Pattern:** `Entity → Validate → Base` becomes `Entity → Validate`

---

## Phase 1: Core Framework Classes

### 1.1 Domain Object Classes

| DELETE | MERGE INTO | Location |
|--------|------------|----------|
| `Base<T>` | `ValidateBase<T>` | `src/Neatoo/Base.cs` → `src/Neatoo/ValidateBase.cs` |
| `IBase` | `IValidateBase` | Same files |
| `ListBase<I>` | `ValidateListBase<I>` | `src/Neatoo/ListBase.cs` → `src/Neatoo/ValidateListBase.cs` |
| `IListBase` / `IListBase<I>` | `IValidateListBase` / `IValidateListBase<I>` | Same files |

**Merge steps:**
1. Copy all code from `Base<T>` into `ValidateBase<T>` (Getter/Setter, Parent tracking, AsyncTasks, PropertyManager, etc.)
2. Update `ValidateBase<T>` to no longer inherit from `Base<T>`
3. Copy `IBase` members into `IValidateBase`
4. Repeat for ListBase → ValidateListBase
5. Delete `Base.cs` and `ListBase.cs`

### 1.2 Services Classes

| DELETE | MERGE INTO | Location |
|--------|------------|----------|
| `IBaseServices<T>` | `IValidateBaseServices<T>` | `src/Neatoo/IBaseServices.cs` |
| `BaseServices<T>` | `ValidateBaseServices<T>` | `src/Neatoo/Internal/BaseServices.cs` |

**Merge steps:**
1. Move `IPropertyManager<IProperty> PropertyManager` and `IPropertyInfoList<T> PropertyInfoList` from `IBaseServices<T>` into `IValidateBaseServices<T>`
2. Remove `: IBaseServices<T>` inheritance from `IValidateBaseServices<T>`
3. Update `ValidateBaseServices<T>` constraint from `where T : ValidateBase<T>` (will still work)
4. Delete `IBaseServices.cs` and `Internal/BaseServices.cs`

### 1.3 Property Classes

| DELETE | MERGE INTO | Location |
|--------|------------|----------|
| `IProperty` | `IValidateProperty` | `src/Neatoo/IProperty.cs` → `src/Neatoo/IValidateProperty.cs` |
| `IProperty<T>` | `IValidateProperty<T>` | Same files |
| `Property<T>` | `ValidateProperty<T>` | `src/Neatoo/Internal/Property.cs` → `src/Neatoo/Internal/ValidateProperty.cs` |

**Merge steps:**
1. Copy all `IProperty` members (Name, Value, SetValue, Task, IsBusy, IsReadOnly, etc.) into `IValidateProperty`
2. Remove `: IProperty` inheritance from `IValidateProperty`
3. Copy all `Property<T>` implementation into `ValidateProperty<T>`
4. Update `ValidateProperty<T>` to no longer inherit from `Property<T>`
5. Delete `IProperty.cs` and `Internal/Property.cs`

### 1.4 PropertyManager Classes

| DELETE | MERGE INTO | Location |
|--------|------------|----------|
| `IPropertyManager<P>` | `IValidatePropertyManager<P>` | `src/Neatoo/IPropertyManager.cs` → `src/Neatoo/IValidatePropertyManager.cs` |
| `PropertyManager<P>` | `ValidatePropertyManager<P>` | `src/Neatoo/Internal/PropertyManager.cs` → `src/Neatoo/Internal/ValidatePropertyManager.cs` |

**Merge steps:**
1. Copy all `IPropertyManager<P>` members into `IValidatePropertyManager<P>`
2. Remove `: IPropertyManager<P>` inheritance
3. Copy all `PropertyManager<P>` implementation into `ValidatePropertyManager<P>`
4. Update `ValidatePropertyManager<P>` to no longer inherit from `PropertyManager<P>`
5. Delete `IPropertyManager.cs` and `Internal/PropertyManager.cs`

### 1.5 Internal Interfaces (in InternalInterfaces.cs)

| DELETE | MERGE INTO |
|--------|------------|
| `IBaseInternal` | `IValidateBaseInternal` |
| `IPropertyInternal` | `IValidatePropertyInternal` |

### 1.6 Meta Properties Interfaces (in IMetaProperties.cs)

| DELETE | MERGE INTO |
|--------|------------|
| `IBaseMetaProperties` | `IValidateMetaProperties` |

---

## Phase 2: Factory Infrastructure

### 2.1 IFactory Interface (`src/Neatoo/Internal/IFactory.cs`)

**Before:**
```csharp
public interface IFactory
{
    Property<P> CreateProperty<P>(IPropertyInfo propertyInfo);
    ValidateProperty<P> CreateValidateProperty<P>(IPropertyInfo propertyInfo);
    EntityProperty<P> CreateEntityProperty<P>(IPropertyInfo propertyInfo);
}
```

**After:**
```csharp
public interface IFactory
{
    ValidateProperty<P> CreateValidateProperty<P>(IPropertyInfo propertyInfo);
    EntityProperty<P> CreateEntityProperty<P>(IPropertyInfo propertyInfo);
}
```

### 2.2 DefaultFactory (`src/Neatoo/Internal/PropertyFactory.cs`)

Remove `CreateProperty<P>()` method.

### 2.3 Delegates to Remove

- `CreatePropertyManager` delegate in `PropertyManager.cs` → DELETE (file deleted)

---

## Phase 3: Update References

### 3.1 PropertyInfoList (`src/Neatoo/Internal/PropertyInfoList.cs`)

**Line 25 - Update neatooTypes array:**

**Before:**
```csharp
private static Type[] neatooTypes = new Type[] {
    typeof(Base<>), typeof(ListBase<>),
    typeof(ValidateBase<>), typeof(ValidateListBase<>),
    typeof(EntityBase<>), typeof(EntityListBase<>)
};
```

**After:**
```csharp
private static Type[] neatooTypes = new Type[] {
    typeof(ValidateBase<>), typeof(ValidateListBase<>),
    typeof(EntityBase<>), typeof(EntityListBase<>)
};
```

### 3.2 Source Generator (`src/Neatoo.BaseGenerator/BaseGenerator.cs`)

Update `ClassOrBaseClassIsNeatooBaseClass` method to check for `ValidateBase` instead of `Base`.

### 3.3 JSON Converters

- `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs`:
  - Rename to `NeatooValidateBaseJsonTypeConverter.cs`
  - Update constraint `where T : IBase` → `where T : IValidateBase`
- `src/Neatoo/RemoteFactory/Internal/NeatooListBaseJsonTypeConverter.cs`:
  - Rename to `NeatooValidateListBaseJsonTypeConverter.cs`
  - Update constraint `where I : IBase` → `where I : IValidateBase`

---

## Phase 4: Test Infrastructure Changes

### 4.1 DELETE Entire Folder: `src/Neatoo.UnitTest/Integration/Concepts/BaseClass/`

Delete the entire folder and all contents:
- `Objects/BaseObject.cs`
- `Objects/BaseObjectList.cs`
- `BasePropertyChangedTests.cs`
- `FatClientBaseTests.cs`
- `BaseObjectTests.cs`
- `BaseSerializationTests.cs`

ValidateBase tests already cover the same functionality.

### 4.2 Unit Test Files to Update

| File | Change |
|------|--------|
| `Unit/Core/BaseServicesTests.cs` | DELETE (no longer exists) |
| `Unit/Core/PropertyTests.cs` | Rename to ValidatePropertyTests or merge |
| `Unit/Core/ValidatePropertyTests.cs` | Absorb Property tests |
| `Unit/Core/ValidatePropertyManagerTests.cs` | Absorb PropertyManager tests |
| `Unit/Core/ListBaseTests.cs` | Rename/merge into ValidateListBaseTests |
| `Unit/Core/DefaultFactoryTests.cs` | Remove CreateProperty test |

### 4.3 Test Classes to Update (change Base<T> → ValidateBase<T>)

These test classes inherit directly from `Base<T>` and need updating:

| File | Class | Change |
|------|-------|--------|
| `Neatoo.UnitTest.Demo/Sandbox.cs` | `BaseObject` | Change to `ValidateBase<BaseObject>` |

---

## Phase 5: Documentation & Generated Files

### 5.1 Documentation Updates

| File | Change |
|------|--------|
| `docs/aggregates-and-entities.md` | Remove Base<T> from hierarchy diagram |
| `docs/index.md` | Update any Base references |

### 5.2 Regenerate All Generated Files

After all changes, run build to regenerate:
- All factory files in `Generated/` folders
- Source generator outputs

---

## Phase 6: Update Neatoo Skill

Update the Neatoo skill at `C:\Users\KeithVoels\.claude\skills\neatoo\` to reflect the new architecture:

### 6.1 Files to Update

| File | Changes |
|------|---------|
| `neatoo.md` (main skill) | Update inheritance hierarchy references |
| Any base class documentation | Remove Base<T>/ListBase<I> references |
| Code examples | Update to show ValidateBase as the foundation |

### 6.2 Key Changes

- Remove all references to `Base<T>` as a base class option
- Remove all references to `ListBase<I>` as a base class option
- Update inheritance diagrams to show `ValidateBase<T>` as the foundation
- Update `IBase` references to `IValidateBase`
- Update property system documentation (no more `Property<T>`, just `ValidateProperty<T>`)
- Update any code snippets showing the old hierarchy

---

## Files Summary

### Files to DELETE (10 files)

1. `src/Neatoo/Base.cs`
2. `src/Neatoo/ListBase.cs`
3. `src/Neatoo/IBaseServices.cs`
4. `src/Neatoo/Internal/BaseServices.cs`
5. `src/Neatoo/IProperty.cs`
6. `src/Neatoo/Internal/Property.cs`
7. `src/Neatoo/IPropertyManager.cs`
8. `src/Neatoo/Internal/PropertyManager.cs`
9. `src/Neatoo.UnitTest/Integration/Concepts/BaseClass/Objects/BaseObject.cs`
10. `src/Neatoo.UnitTest/Integration/Concepts/BaseClass/Objects/BaseObjectList.cs`

### Test Files/Folders to DELETE (4 items)

1. `src/Neatoo.UnitTest/Integration/Concepts/BaseClass/` (entire folder)
2. `src/Neatoo.UnitTest/Unit/Core/BaseServicesTests.cs`

### Files to MODIFY (major changes)

1. `src/Neatoo/ValidateBase.cs` - Absorb Base<T>
2. `src/Neatoo/ValidateListBase.cs` - Absorb ListBase<I>
3. `src/Neatoo/IValidateBaseServices.cs` - Absorb IBaseServices
4. `src/Neatoo/Internal/ValidateBaseServices.cs` - Absorb BaseServices
5. `src/Neatoo/IValidateProperty.cs` - Absorb IProperty
6. `src/Neatoo/Internal/ValidateProperty.cs` - Absorb Property<T>
7. `src/Neatoo/IValidatePropertyManager.cs` - Absorb IPropertyManager
8. `src/Neatoo/Internal/ValidatePropertyManager.cs` - Absorb PropertyManager<P>
9. `src/Neatoo/IMetaProperties.cs` - Remove IBaseMetaProperties
10. `src/Neatoo/InternalInterfaces.cs` - Remove IBaseInternal, IPropertyInternal
11. `src/Neatoo/Internal/IFactory.cs` - Remove CreateProperty method
12. `src/Neatoo/Internal/PropertyFactory.cs` - Remove CreateProperty method
13. `src/Neatoo/Internal/PropertyInfoList.cs` - Update neatooTypes array
14. `src/Neatoo.BaseGenerator/BaseGenerator.cs` - Check for ValidateBase instead of Base

### Test Files to UPDATE (update inheritance/references)

1. `src/Neatoo.UnitTest/Unit/Core/PropertyTests.cs` - Merge into ValidateProperty tests
2. `src/Neatoo.UnitTest/Unit/Core/ValidatePropertyManagerTests.cs` - Update references
3. `src/Neatoo.UnitTest/Unit/Core/ListBaseTests.cs` - Rename/update
4. `src/Neatoo.UnitTest/Unit/Core/DefaultFactoryTests.cs` - Remove CreateProperty tests
5. `src/Neatoo.UnitTest.Demo/Sandbox.cs` - Update BaseObject class

---

## Execution Order

1. **Backup/Branch** - Create feature branch
2. **Phase 1.3** - Property classes (lowest dependency)
3. **Phase 1.4** - PropertyManager classes
4. **Phase 2** - Factory infrastructure
5. **Phase 1.5-1.6** - Internal interfaces
6. **Phase 1.2** - Services classes
7. **Phase 1.1** - Domain object classes (highest dependency)
8. **Phase 3** - Update all references
9. **Phase 4** - Test infrastructure
10. **Phase 5** - Documentation
11. **Build & Test** - Verify everything compiles and tests pass
12. **Regenerate** - Clean rebuild to regenerate all generated files

---

## Risk Assessment

| Risk | Mitigation |
|------|------------|
| Breaking external code | This is internal refactoring; public API unchanged |
| Missing references | Use `dotnet build` after each phase to catch errors |
| Generated file issues | Clean rebuild after completion |
| Test failures | Run tests after each phase |

---

## Estimated Impact

- **Files deleted:** 12
- **Files modified (major):** 14
- **Test files affected:** ~20
- **Lines of code moved:** ~1500
- **Net lines removed:** ~800 (eliminating duplication)
