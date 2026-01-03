# Documentation Samples Project

**Status:** Complete
**Priority:** High
**Created:** 2026-01-02

---

## Problem

Documentation code snippets are incorrect - bad syntax, outdated APIs, or referencing internal types. Developers trying to use Neatoo encounter compilation errors when following the docs (e.g., `RuleBase<>` shown as usable but reported as internal).

## Solution

Create a separate `Neatoo.Documentation.Samples` project where every code snippet in documentation is:
1. Compiled (syntax verified)
2. Tested (behavior verified)
3. Extracted into docs via region markers

---

## Project Structure

```
src/
  Neatoo.Documentation.Samples/
    Neatoo.Documentation.Samples.csproj

    # Organized by documentation file
    GettingStarted/
      QuickStartSamples.cs              # docs/quick-start.md
      InstallationSamples.cs            # docs/installation.md

    ValidationAndRules/
      RuleBaseSamples.cs                # Sync rules
      AsyncRuleBaseSamples.cs           # Async rules
      FluentRuleSamples.cs              # Inline rules
      DataAnnotationSamples.cs          # Attribute validation

    AggregatesAndEntities/
      EntityBaseSamples.cs              # EntityBase examples
      ValidateBaseSamples.cs            # ValidateBase examples
      ChildEntitySamples.cs             # Parent-child patterns

    FactoryOperations/
      CreateOperationSamples.cs
      FetchOperationSamples.cs
      InsertUpdateSamples.cs
      DeleteOperationSamples.cs

    Collections/
      EntityListBaseSamples.cs
      CrossItemValidationSamples.cs

    PropertySystem/
      PropertyAccessSamples.cs
      MetaPropertiesSamples.cs
      PauseResumeSamples.cs

    BlazorBinding/
      MudNeatooSamples.razor            # Blazor component examples
      ManualBindingSamples.razor

    DatabaseValidation/
      CommandPatternSamples.cs
      AsyncValidationSamples.cs

    MapperMethods/
      MapFromToSamples.cs

    # Shared domain objects for samples
    SampleDomain/
      IPerson.cs
      Person.cs
      IPersonPhone.cs
      PersonPhone.cs
      PersonPhoneList.cs

    # Test infrastructure
    TestInfrastructure/
      SamplesTestBase.cs
      MockServices/
        MockRepository.cs

  Neatoo.Documentation.Samples.Tests/
    Neatoo.Documentation.Samples.Tests.csproj

    # Mirror samples structure
    ValidationAndRules/
      RuleBaseSamplesTests.cs
      AsyncRuleBaseSamplesTests.cs
    # ... etc
```

---

## Snippet Extraction Approach

### Region Markers

Use `#region` with naming convention: `docs:{doc-file}:{snippet-id}`

```csharp
// In ValidationAndRules/RuleBaseSamples.cs

#region docs:validation-and-rules:age-validation-rule
public class AgeValidationRule : RuleBase<IPerson>
{
    public AgeValidationRule() : base(p => p.Age) { }

    protected override IRuleMessages Execute(IPerson target)
    {
        if (target.Age < 0)
            return (nameof(target.Age), "Age cannot be negative").AsRuleMessages();
        return None;
    }
}
#endregion
```

### Markdown Reference

In docs, use markers for extraction:

```markdown
<!-- snippet: docs:validation-and-rules:age-validation-rule -->
```csharp
// ... extracted content appears here
```
<!-- /snippet -->
```

### Extraction Script

PowerShell script to extract snippets and update docs:

```powershell
# scripts/extract-snippets.ps1
# Extracts #region docs:* content and updates markdown files
```

---

## Test Strategy

### Test Base Class

Adapt from existing `IntegrationTestBase`:

```csharp
public abstract class SamplesTestBase
{
    private IServiceScope? _scope;

    protected void InitializeScope()
    {
        var services = new ServiceCollection();
        services.AddNeatooServices();
        services.AddSampleDomainServices();
        _scope = services.BuildServiceProvider().CreateScope();
    }

    protected T GetRequiredService<T>() where T : notnull
        => _scope!.ServiceProvider.GetRequiredService<T>();
}
```

### Test Organization

- One test class per sample file
- Tests verify samples actually work as documented
- Use `[TestCategory("Documentation")]` for filtering

---

## Implementation Steps

### Phase 1: Project Setup ✅
- [x] Create `Neatoo.Documentation.Samples.csproj`
- [x] Create `Neatoo.Documentation.Samples.Tests.csproj`
- [x] Add to solution
- [x] Set up project references (Neatoo, generators)
- [x] Create test base class (`SamplesTestBase.cs`)

### Phase 2: Sample Domain ✅
- [x] Create `SampleDomain/IPerson.cs` interface
- [x] Create `SampleDomain/Person.cs` entity
- [x] Create `SampleDomain/IEvent.cs` and `Event.cs` for cross-property validation
- [x] Create mock services (`MockEmailService`)

### Phase 3: Migrate Snippets ✅

**62 snippets migrated, 122 tests passing**

| Priority | Document | Snippets | Status |
|----------|----------|----------|--------|
| 1 | validation-and-rules.md | 21 | ✅ Complete |
| 2 | aggregates-and-entities.md | 11 | ✅ Complete |
| 3 | factory-operations.md | 10 | ✅ Complete |
| 4 | collections.md | 6 | ✅ Complete |
| 5 | property-system.md | 5 | ✅ Complete |
| 6 | database-dependent-validation.md | 3 | ✅ Complete |
| 7 | mapper-methods.md | 6 | ✅ Complete |
| 8 | blazor-binding.md | - | Skipped (Razor) |

**Sample files created:**
- `ValidationAndRules/` - RuleBaseSamples, FluentRuleSamples, DataAnnotationSamples
- `AggregatesAndEntities/` - EntityBaseSamples, ValidateBaseSamples, ValueObjectSamples
- `FactoryOperations/` - CreateOperationSamples, FetchOperationSamples, SaveOperationSamples, ChildEntitySamples, SaveUsageSamples
- `Collections/` - EntityListSamples, CrossItemValidationSamples
- `PropertySystem/` - PropertyAccessSamples, PauseActionsSamples
- `DatabaseValidation/` - AsyncValidationSamples
- `MapperMethods/` - MapperMethodsSamples

### Phase 4: CI Verification ✅

**Approach:** CI verification only (no auto-update)
- CI checks that snippet markers in docs match samples project
- Fails build if snippets are out of sync
- Developer runs extraction script locally, commits updated docs

- [x] Create `scripts/extract-snippets.ps1`
- [x] Add verification step to GitHub Actions workflow

### Phase 5: Add Snippet Markers to Docs ✅

Replaced existing code blocks in docs with snippet markers so `extract-snippets.ps1 -Update` can inject compiled code.

**Marker format:**
```markdown
<!-- snippet: docs:validation-and-rules:required-attribute -->
```csharp
// Compiled code injected here by extract-snippets.ps1
```
<!-- /snippet -->
```

**Files updated:**

| Document | Snippets Marked |
|----------|-----------------|
| validation-and-rules.md | 19 |
| aggregates-and-entities.md | 10 |
| factory-operations.md | 8 |
| collections.md | 5 |
| property-system.md | 5 |
| database-dependent-validation.md | 3 |
| mapper-methods.md | 6 |

**Total: 56 snippets injected into 7 documentation files**

**Steps:**
1. [x] Add markers to validation-and-rules.md
2. [x] Add markers to aggregates-and-entities.md
3. [x] Add markers to factory-operations.md
4. [x] Add markers to collections.md
5. [x] Add markers to property-system.md
6. [x] Add markers to database-dependent-validation.md
7. [x] Add markers to mapper-methods.md
8. [x] Run `.\scripts\extract-snippets.ps1 -Update` to inject snippets
9. [x] Verify docs render correctly

---

## Key Files to Reference

| File | Purpose |
|------|---------|
| `src/Neatoo.UnitTest/TestInfrastructure/IntegrationTestBase.cs` | Test base class pattern |
| `src/Examples/Person/Person.DomainModel/Person.cs` | Domain object example |
| `src/Examples/Person/Person.DomainModel/UniqueNameRule.cs` | Rule example |
| `docs/validation-and-rules.md` | Largest doc to migrate first |

---

## Notes

- **RuleBase<T> is public** - Exploration confirmed. If developers see it as internal, likely a namespace/reference issue in their project.
- **Blazor samples** compile but need special handling - Razor files verify syntax, Playwright tests (future) verify runtime.
- **Existing Examples/ folder** can inform patterns but samples project should be simpler/focused on docs.

---

## Success Criteria

1. ✅ All documentation code snippets compile without errors (62 snippets)
2. ✅ All snippets have corresponding unit tests (122 tests passing)
3. ✅ CI fails if snippets out of sync (verification step in GitHub Actions)
4. ✅ Easy workflow to update docs from samples (56 snippets auto-injected into 7 docs)
