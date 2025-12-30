# RemoteFactory Mapper Removal - Breaking Change Plan

**Status: COMPLETED** (2025-12-30)

## Summary

RemoteFactory commit `9e62dda` (2025-12-30) removed the Mapper source generator feature. This is a breaking change that affects Neatoo's domain objects which use `MapTo`/`MapFrom` partial methods.

## Impact Analysis

### Affected Files in Neatoo

| File | Usage |
|------|-------|
| `src/Examples/Person/Person.DomainModel/Person.cs` | `MapTo(PersonEntity)`, `MapFrom(PersonEntity)` |
| `src/Examples/Person/Person.DomainModel/PersonPhone.cs` | `MapTo(PersonPhoneEntity)`, `MapFrom(PersonPhoneEntity)` |
| `src/Neatoo.UnitTest/Integration/Aggregates/Person/PersonEntityBase.cs` | `MapFrom(PersonDto)` |
| `src/Neatoo.UnitTest/Integration/Aggregates/Person/PersonValidateBase.cs` | `MapFrom(PersonDto)` |
| `src/Neatoo.UnitTest.Demo/PersonObjectTests.cs` | `MapTo(PersonObjectDto)`, `MapFrom(PersonObjectDto)` |

### Generated Files That Will Disappear

After updating RemoteFactory, these generated files will no longer be produced:
- `DomainModel.PersonMapper.g.cs`
- `DomainModel.PersonPhoneMapper.g.cs`
- `Integration.Aggregates.Person.PersonEntityBaseMapper.g.cs`
- `Integration.Aggregates.Person.PersonValidateBaseMapper.g.cs`

### Build Impact

Partial method declarations without implementations will cause **CS8795** compiler errors:
```
error CS8795: Partial method 'Person.MapFrom(PersonEntity)' must have an implementation part
```

## Recommended Solution: Manual Implementation

The generated mapper code was simple property-by-property assignment. The recommended approach is to manually implement these methods.

### Example: Before (with generator)

```csharp
// Person.cs - partial declaration only
public partial void MapFrom(PersonEntity personEntity);
public partial void MapTo(PersonEntity personEntity);
```

### Example: After (manual implementation)

```csharp
// Person.cs - full implementation
public void MapFrom(PersonEntity personEntity)
{
    this.Id = personEntity.Id;
    this.FirstName = personEntity.FirstName;
    this.LastName = personEntity.LastName;
    this.Email = personEntity.Email;
    this.Notes = personEntity.Notes;
}

public void MapTo(PersonEntity personEntity)
{
    personEntity.Id = this.Id;
    personEntity.FirstName = this.FirstName ?? throw new NullReferenceException("Person.FirstName");
    personEntity.LastName = this.LastName ?? throw new NullReferenceException("Person.LastName");
    personEntity.Email = this.Email;
    personEntity.Notes = this.Notes;
}
```

## Implementation Steps

### Step 1: Update RemoteFactory NuGet Reference
- Update to latest RemoteFactory version (post `9e62dda`)
- Build will fail with CS8795 errors

### Step 2: Remove Generated Files
Delete the generated mapper files that are now orphaned:
```
src/Examples/Person/Person.DomainModel/Generated/.../DomainModel.PersonMapper.g.cs
src/Examples/Person/Person.DomainModel/Generated/.../DomainModel.PersonPhoneMapper.g.cs
src/Neatoo.UnitTest/Generated/.../Integration.Aggregates.Person.PersonEntityBaseMapper.g.cs
src/Neatoo.UnitTest/Generated/.../Integration.Aggregates.Person.PersonValidateBaseMapper.g.cs
```

### Step 3: Implement Manual Mappers

#### Person.cs
Replace partial declarations with full implementations based on generated code.

#### PersonPhone.cs
Replace partial declarations with full implementations based on generated code.

#### PersonEntityBase.cs / PersonValidateBase.cs
Replace partial declarations with full implementations.

#### PersonObjectTests.cs (Demo)
Replace partial declarations with full implementations.

### Step 4: Update Documentation
- Update `docs/mapper-methods.md` to reflect manual implementation approach
- Remove references to MapperGenerator in documentation

### Step 5: Build and Test
- Run `dotnet build` to verify no compilation errors
- Run `dotnet test` to verify all tests pass

## Alternative Solutions Considered

### Option A: Use Third-Party Mapper (AutoMapper, Mapster)
**Rejected**: Adds external dependency for simple property mapping. Overkill for Neatoo's straightforward use case.

### Option B: Create Neatoo's Own Mapper Generator
**Rejected**: Maintenance burden. The mapping is simple enough that manual implementation is cleaner.

### Option C: Manual Implementation (RECOMMENDED)
**Selected**: Simple, explicit, no dependencies. The generated code was trivial property assignments anyway.

## Benefits of Manual Implementation

1. **Explicit**: You can see exactly what's being mapped
2. **Flexible**: Easy to add custom logic (e.g., transformations, child collections)
3. **No Dependencies**: No generator complexity
4. **Debuggable**: Can step through mapping logic

## Timeline

- **Priority**: High (blocking change when RemoteFactory is updated)
- **Effort**: ~1-2 hours to implement all manual mappers
- **Risk**: Low (changes are straightforward)

## Related Commits

- RemoteFactory: `9e62dda` - Remove Mapper Functionality (2025-12-30)

## Execution Summary

**Completed: 2025-12-30**

### Files Modified

| File | Change |
|------|--------|
| `src/Examples/Person/Person.DomainModel/Person.cs` | Replaced `partial void MapFrom/MapTo` with full implementations |
| `src/Examples/Person/Person.DomainModel/PersonPhone.cs` | Replaced `partial void MapFrom/MapTo` with full implementations |
| `src/Neatoo.UnitTest/Integration/Aggregates/Person/PersonEntityBase.cs` | Replaced `partial void MapFrom` with full implementation |
| `src/Neatoo.UnitTest/Integration/Aggregates/Person/PersonValidateBase.cs` | Replaced `partial void MapFrom` with full implementation |
| `src/Neatoo.UnitTest.Demo/PersonObjectTests.cs` | Replaced `partial void MapFrom/MapTo` with full implementations |

### Files Deleted

| File | Reason |
|------|--------|
| `DomainModel.PersonMapper.g.cs` | Orphaned generated file |
| `DomainModel.PersonPhoneMapper.g.cs` | Orphaned generated file |
| `Integration.Aggregates.Person.PersonEntityBaseMapper.g.cs` | Orphaned generated file |
| `Integration.Aggregates.Person.PersonValidateBaseMapper.g.cs` | Orphaned generated file |

### Notes

- `MapModifiedTo` remains as a partial method - it's generated by Neatoo's **BaseGenerator**, not RemoteFactory
- All 1674 tests pass (54 Person.DomainModel.Tests + 1620 Neatoo.UnitTest)
- Build succeeds with only pre-existing warnings
