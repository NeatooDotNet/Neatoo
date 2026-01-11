# Skill Samples Needed

Code samples needed to sync remaining skill files via `extract-snippets.ps1`.

Add `#region docs:{category}:{snippet-id}` markers to `docs/samples/` files.

---

## authorization.md

Needs samples for AuthorizeFactory patterns.

### Suggested Snippets

| Snippet ID | Description |
|------------|-------------|
| `docs:authorization:basic-authorize` | Basic `[Authorize]` attribute on factory |
| `docs:authorization:role-based` | Role-based authorization `[Authorize(Roles = "Admin")]` |
| `docs:authorization:policy-based` | Policy-based authorization |
| `docs:authorization:method-level` | Method-level authorization on specific operations |
| `docs:authorization:authorized-result` | Using `Authorized<T>` return type with TrySave |

### Example Code Needed

```csharp
#region docs:authorization:basic-authorize
[Factory]
[Authorize]
internal partial class SecureEntity : EntityBase<SecureEntity>, ISecureEntity
{
    // Only authenticated users can access
}
#endregion

#region docs:authorization:role-based
[Factory]
[Authorize(Roles = "Admin,Manager")]
internal partial class AdminOnlyEntity : EntityBase<AdminOnlyEntity>, IAdminOnlyEntity
{
    [Fetch]
    [Authorize(Roles = "Admin")]  // Stricter for fetch
    public void Fetch(Guid id) { }
}
#endregion

#region docs:authorization:authorized-result
// Using TrySave to get authorization result
var result = await factory.TrySave(entity);
if (!result.IsAuthorized)
{
    // Handle unauthorized: result.Message contains reason
}
else
{
    entity = result.Value;  // Success
}
#endregion
```

---

## blazor-integration.md

Needs samples for MudBlazor/Blazor binding patterns with Neatoo entities.

### Suggested Snippets

| Snippet ID | Description |
|------------|-------------|
| `docs:blazor:mudtextfield-binding` | MudTextField binding to entity property |
| `docs:blazor:validation-display` | Displaying validation errors |
| `docs:blazor:form-submit` | Form submission with IsSavable check |
| `docs:blazor:async-loading` | Loading entity with async factory |
| `docs:blazor:isbusy-handling` | Handling IsBusy state in UI |
| `docs:blazor:child-collection` | Binding to child collection (list editing) |

### Example Code Needed

```razor
#region docs:blazor:mudtextfield-binding
<MudTextField @bind-Value="Person.FirstName"
              Label="@Person[nameof(Person.FirstName)].DisplayName"
              Error="@(!Person[nameof(Person.FirstName)].IsValid)"
              ErrorText="@Person[nameof(Person.FirstName)].ErrorText" />
#endregion

#region docs:blazor:form-submit
<MudButton Disabled="@(!Person.IsSavable)"
           OnClick="SavePerson">
    Save
</MudButton>

@code {
    private async Task SavePerson()
    {
        Person = await PersonFactory.Save(Person);
    }
}
#endregion

#region docs:blazor:isbusy-handling
@if (Person.IsBusy)
{
    <MudProgressCircular Indeterminate="true" />
}
else
{
    <MudButton Disabled="@(!Person.IsSavable)" OnClick="Save">Save</MudButton>
}
#endregion
```

---

## source-generators.md

Needs samples showing what the Neatoo source generators produce.

### Suggested Snippets

| Snippet ID | Description |
|------------|-------------|
| `docs:source-gen:factory-interface` | Generated IXxxFactory interface |
| `docs:source-gen:factory-implementation` | Generated factory class |
| `docs:source-gen:property-backing` | Generated property backing fields |
| `docs:source-gen:map-modified-to` | Generated MapModifiedTo method |
| `docs:source-gen:di-registration` | Generated DI registration extension |

### Example Code Needed

Show input (what developer writes) and output (what generator produces):

```csharp
#region docs:source-gen:input-entity
// Developer writes:
[Factory]
internal partial class Product : EntityBase<Product>, IProduct
{
    public Product(IEntityBaseServices<Product> services) : base(services) { }

    public partial string? Name { get; set; }
    public partial decimal Price { get; set; }

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(int id) { }
}
#endregion

#region docs:source-gen:generated-factory
// Generator produces:
public interface IProductFactory
{
    IProduct Create();
    IProduct? Fetch(int id);
    Task<IProduct?> Save(IProduct product);
}

internal class ProductFactory : IProductFactory
{
    // Implementation delegates to entity methods
}
#endregion

#region docs:source-gen:generated-property
// Generator produces for partial properties:
private string? _name;
public partial string? Name
{
    get => Getter(ref _name);
    set => Setter(ref _name, value);
}
#endregion
```

---

## migration.md

Needs samples showing version migration patterns.

### Suggested Snippets

| Snippet ID | Description |
|------------|-------------|
| `docs:migration:save-reassignment` | v10.5 Save() reassignment pattern |
| `docs:migration:breaking-change-example` | Example of adapting to breaking change |

### Example Code Needed

```csharp
#region docs:migration:save-reassignment
// Before v10.5 (OLD - don't do this)
// await personFactory.Save(person);

// v10.5+ (CORRECT)
person = await personFactory.Save(person);  // Always reassign!
#endregion
```

---

## Priority

1. **authorization.md** - Common need, security-critical
2. **blazor-integration.md** - Most users use Blazor
3. **source-generators.md** - Helps debugging
4. **migration.md** - Only needed for specific versions

---

## Implementation Plan

### Overview

Create compiled, tested sample code for four skill files. All samples go in `docs/samples/Neatoo.Samples.DomainModel/` with corresponding tests in `docs/samples/Neatoo.Samples.DomainModel.Tests/`.

### Phase 1: Authorization Samples (HIGH PRIORITY)

**New files to create:**
- `docs/samples/Neatoo.Samples.DomainModel/Authorization/AuthorizationSamples.cs`
- `docs/samples/Neatoo.Samples.DomainModel.Tests/Authorization/AuthorizationSamplesTests.cs`

**Snippets to implement:**

| Snippet ID | What to Create |
|------------|----------------|
| `docs:authorization:basic-authorize` | Entity with `[Authorize]` on factory |
| `docs:authorization:role-based` | Entity with `[Authorize(Roles = "...")]` |
| `docs:authorization:method-level` | Different auth per operation (Fetch vs Insert) |
| `docs:authorization:authorized-result` | Using `TrySave()` and checking `Authorized<T>` result |

**Dependencies:** Need to verify RemoteFactory's authorization attribute support. May need mock `IAuthorizationService` or test auth handler.

### Phase 2: Blazor Integration Samples (HIGH PRIORITY)

**Modify existing files:**
- `docs/samples/Neatoo.Samples.BlazorClient/Pages/` - Add region markers to existing pages

**New files if needed:**
- `docs/samples/Neatoo.Samples.BlazorClient/Pages/SampleForm.razor` - Dedicated sample component

**Snippets to implement:**

| Snippet ID | What to Create |
|------------|----------------|
| `docs:blazor-integration:mudtextfield-binding` | MudTextField bound to entity property |
| `docs:blazor-integration:validation-display` | Error display from property metadata |
| `docs:blazor-integration:form-submit` | Save button with IsSavable check |
| `docs:blazor-integration:isbusy-handling` | IsBusy spinner pattern |
| `docs:blazor-integration:async-loading` | OnInitializedAsync with factory.Fetch |

**Note:** Razor files support `@* #region docs:... *@` comment syntax for extraction.

### Phase 3: Source Generators Samples (MEDIUM PRIORITY)

**Strategy:** Don't duplicate generated code. Instead, show the INPUT pattern and reference generated output.

**Modify existing files:**
- `docs/samples/Neatoo.Samples.DomainModel/AggregatesAndEntities/EntityBaseSamples.cs` - Add generator-focused regions

**Snippets to implement:**

| Snippet ID | What to Create |
|------------|----------------|
| `docs:source-generators:entity-input` | Complete entity showing what dev writes |
| `docs:source-generators:partial-property` | Partial property declaration pattern |
| `docs:source-generators:factory-attribute` | [Factory] attribute usage |

**Note:** Generated output examples should be inline comments showing what gets generated, not actual .g.cs files (those change with generator versions).

### Phase 4: Migration Samples (LOW PRIORITY)

**Modify existing files:**
- `docs/samples/Neatoo.Samples.DomainModel/FactoryOperations/SaveUsageSamples.cs` - Add migration regions

**Snippets to implement:**

| Snippet ID | What to Create |
|------------|----------------|
| `docs:migration:save-reassignment` | `entity = await factory.Save(entity)` pattern |
| `docs:migration:cancellation-token` | Async operations with CancellationToken |

### Phase 5: Update Skill Files

After samples compile and tests pass:

1. Add `<!-- snippet: docs:xxx:yyy -->` markers to skill files
2. Run `.\scripts\extract-snippets.ps1 -Update -SkillPath "$env:USERPROFILE\.claude\skills\neatoo"`
3. Verify skill files updated correctly

### Files to Modify/Create Summary

| Action | Path |
|--------|------|
| CREATE | `docs/samples/Neatoo.Samples.DomainModel/Authorization/AuthorizationSamples.cs` |
| CREATE | `docs/samples/Neatoo.Samples.DomainModel.Tests/Authorization/AuthorizationSamplesTests.cs` |
| MODIFY | `docs/samples/Neatoo.Samples.BlazorClient/Pages/` (add regions) |
| MODIFY | `docs/samples/Neatoo.Samples.DomainModel/AggregatesAndEntities/EntityBaseSamples.cs` |
| MODIFY | `docs/samples/Neatoo.Samples.DomainModel/FactoryOperations/SaveUsageSamples.cs` |
| MODIFY | `~/.claude/skills/neatoo/authorization.md` (add snippet markers) |
| MODIFY | `~/.claude/skills/neatoo/blazor-integration.md` (add snippet markers) |
| MODIFY | `~/.claude/skills/neatoo/source-generators.md` (add snippet markers) |
| MODIFY | `~/.claude/skills/neatoo/migration.md` (add snippet markers) |

### Verification Steps

1. `dotnet build docs/samples/Neatoo.Samples.sln`
2. `dotnet test docs/samples/Neatoo.Samples.DomainModel.Tests`
3. `.\scripts\extract-snippets.ps1 -Verify -SkillPath "$env:USERPROFILE\.claude\skills\neatoo"`

### Progress Tracking

- [x] Phase 1: Authorization samples
- [x] Phase 2: Blazor integration samples
- [x] Phase 3: Source generator samples
- [x] Phase 4: Migration samples
- [x] Phase 5: Update skill files with snippet markers
- [x] Final verification

### Completion Summary (2026-01-05)

**Files Created:**
- `docs/samples/Neatoo.Samples.DomainModel/Authorization/AuthorizationSamples.cs`
- `docs/samples/Neatoo.Samples.DomainModel.Tests/Authorization/AuthorizationSamplesTests.cs`
- `docs/samples/Neatoo.Samples.DomainModel/SourceGenerators/SourceGeneratorSamples.cs`
- `docs/samples/Neatoo.Samples.DomainModel.Tests/SourceGenerators/SourceGeneratorSamplesTests.cs`
- `docs/samples/Neatoo.Samples.BlazorClient/Pages/PersonForm.razor`

**Files Modified:**
- `docs/samples/Neatoo.Samples.DomainModel/FactoryOperations/SaveUsageSamples.cs` (migration snippets)
- `docs/samples/Neatoo.Samples.DomainModel/TestInfrastructure/SampleServiceProvider.cs` (DI registration)
- `~/.claude/skills/neatoo/authorization.md`
- `~/.claude/skills/neatoo/blazor-integration.md`
- `~/.claude/skills/neatoo/source-generators.md`
- `~/.claude/skills/neatoo/migration.md`

**Snippets Added:**
- 5 authorization snippets (auth-interface, auth-implementation, entity-with-auth, operation-specific, role-based)
- 6 blazor-integration snippets (property-binding, validation-display, issavable-button, isbusy-handling, async-loading, save-pattern)
- 4 source-generators snippets (complete-entity, entity-input, factory-attribute, partial-property)
- 2 migration snippets (save-reassignment, cancellation-token)

**Verification:**
- Build: PASSED
- Tests: 175 passed
- extract-snippets: 4 skill files updated, 123 total snippets available
