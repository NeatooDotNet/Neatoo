# Remote Factory Integration

[← Properties](properties.md) | [↑ Guides](index.md) | [Validation →](validation.md)

Neatoo entities integrate with [RemoteFactory](https://github.com/NeatooDotNet/RemoteFactory) for factory generation and client-server execution. This guide covers how Neatoo entity state interacts with factory operations.

For RemoteFactory documentation (factory attributes, service injection, remote execution, authorization, setup), see:
- **GitHub**: [NeatooDotNet/RemoteFactory](https://github.com/NeatooDotNet/RemoteFactory)
- **Claude Code**: `/RemoteFactory` skill

## Save Routing Based on Entity State

When `Save()` is called, the factory routes to the appropriate method based on Neatoo entity state:

| Entity State | Factory Routes To | After Completion |
|--------------|-------------------|------------------|
| `IsNew == true` | `[Insert]` method | `IsNew = false`, `IsModified = false` |
| `IsNew == false && IsModified == true` | `[Update]` method | `IsModified = false` |
| `IsDeleted == true` | `[Delete]` method | Entity cannot be modified further |

This routing is determined by Neatoo's state properties, not RemoteFactory configuration.

## Entity State During Factory Operations

### Create Operations

<!-- snippet: remote-factory-create -->
<a id='snippet-remote-factory-create'></a>
```cs
[Create]
public void Create()
{
    // After Create completes:
    // - IsNew = true (entity not yet persisted)
    // - IsModified = false (initial state is clean)
    // - IsPaused = false (validation rules active)
    Id = 0;
    Name = "";
    Department = "";
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/RemoteFactoryIntegrationSamples.cs#L41-L53' title='Snippet source file'>snippet source</a> | <a href='#snippet-remote-factory-create' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Fetch Operations

<!-- snippet: remote-factory-fetch -->
<a id='snippet-remote-factory-fetch'></a>
```cs
[Remote, Fetch]
public async Task Fetch(int id, [Service] ISkillRemoteFactoryRepository repo)
{
    // During Fetch:
    // - IsPaused = true (validation and modification tracking suspended)
    // - Property assignments use LoadValue semantics (no IsModified change)

    var data = await repo.FetchAsync(id);
    Id = data.Id;
    Name = data.Name;
    Department = data.Department;

    // After Fetch completes:
    // - IsNew = false (entity was loaded from persistence)
    // - IsModified = false (loaded state is considered clean)
    // - IsPaused = false (validation resumes)
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/RemoteFactoryIntegrationSamples.cs#L55-L73' title='Snippet source file'>snippet source</a> | <a href='#snippet-remote-factory-fetch' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Save Operations

Before save executes, check `IsSavable`:

<!-- snippet: remote-factory-issavable-check -->
<a id='snippet-remote-factory-issavable-check'></a>
```cs
/// <summary>
/// IsSavable combines multiple state checks before persistence.
/// </summary>
public static async Task<bool> CheckSavableBeforeSave(SkillRfIntegrationRoot entity)
{
    // IsSavable = IsModified && IsValid && !IsBusy && !IsChild
    if (!entity.IsSavable)
    {
        // Don't persist - one or more conditions failed:
        // - !IsModified: No changes to save
        // - !IsValid: Validation failed
        // - IsBusy: Async rules still running
        // - IsChild: Must save through parent aggregate
        return false;
    }

    // Safe to persist
    return true;
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/RemoteFactoryIntegrationSamples.cs#L179-L199' title='Snippet source file'>snippet source</a> | <a href='#snippet-remote-factory-issavable-check' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

After `[Insert]` or `[Update]` completes:
- `IsNew = false` (after Insert)
- `IsModified = false` - Changes have been persisted

## Child Entity State Cascade

Child entities within an aggregate have their state cascade to the parent:

| Child State | Effect on Parent |
|-------------|------------------|
| `IsModified = true` | Parent `IsModified = true` |
| `IsValid = false` | Parent `IsValid = false` |
| `IsBusy = true` | Parent `IsBusy = true` |

Child entities have `IsChild = true` and `IsSavable = false` - they must save through the aggregate root.

<!-- snippet: remote-factory-child-no-remote -->
<a id='snippet-remote-factory-child-no-remote'></a>
```cs
// Child entities do NOT use [Remote] - they persist through the aggregate root
[Create]
public void Create()
{
    // IsChild = true (set when added to parent collection)
    // IsSavable = false (must save through aggregate root)
}

[Fetch]
public void Fetch(int id, string value)
{
    Id = id;
    Value = value;
}

// Insert/Update/Delete called by parent's Save() - no [Remote] needed
[Insert]
public void Insert() { /* Persist through aggregate root */ }

[Update]
public void Update() { /* Persist through aggregate root */ }

[Delete]
public void Delete() { /* Persist through aggregate root */ }
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/RemoteFactoryIntegrationSamples.cs#L131-L156' title='Snippet source file'>snippet source</a> | <a href='#snippet-remote-factory-child-no-remote' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## DeletedList Lifecycle

When items are removed from an `EntityListBase`:

1. **New items** (`IsNew = true`): Discarded entirely (never persisted)
2. **Existing items** (`IsNew = false`):
   - `MarkDeleted()` called, `IsDeleted = true`
   - Added to `DeletedList` for persistence during Save
   - `[Delete]` method called for each during aggregate Save

After Save completes, `DeletedList` is cleared.

<!-- snippet: remote-factory-deletedlist-lifecycle -->
<a id='snippet-remote-factory-deletedlist-lifecycle'></a>
```cs
/// <summary>
/// DeletedList lifecycle for removed items.
/// </summary>
public static void DeletedListLifecycle(
    SkillRfIntegrationRoot parent,
    ISkillRfIntegrationChildFactory childFactory)
{
    // Step 1: New items are discarded when removed (never persisted)
    var newChild = childFactory.Create();
    parent.Children.Add(newChild);
    parent.Children.Remove(newChild);  // Discarded - never goes to DeletedList

    // Step 2: Existing items go to DeletedList when removed
    var existingChild = childFactory.Fetch(1, "existing");
    parent.Children.Add(existingChild);
    parent.Children.Remove(existingChild);
    // Now: existingChild.IsDeleted = true
    // Now: parent.Children.DeletedCount = 1

    // Step 3: During Save(), [Delete] called for each DeletedList item
    // Step 4: After Save(), DeletedList is cleared
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/RemoteFactoryIntegrationSamples.cs#L224-L247' title='Snippet source file'>snippet source</a> | <a href='#snippet-remote-factory-deletedlist-lifecycle' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Serialization State Transfer

When entities cross client-server boundaries:

| State | Serialized? | Notes |
|-------|-------------|-------|
| Property values | Yes | All registered properties |
| `IsNew` | Yes | Preserved across boundary |
| `IsDeleted` | Yes | Preserved across boundary |
| `IsModified` | Yes | Preserved across boundary |
| `IsChild` | Yes | Preserved across boundary |
| `DeletedList` items | Yes | For pending deletes |
| Validation messages | No | Rules re-run after deserialization |
| `IsBusy` | No | Reset on deserialization |

After deserialization, validation rules execute to establish `IsValid` state on the client.

---

**See also:**
- [RemoteFactory Documentation](https://github.com/NeatooDotNet/RemoteFactory) - Factory attributes, service injection, remote execution
- [Entities](entities.md) - EntityBase lifecycle and state properties
- [Collections](collections.md) - EntityListBase and DeletedList behavior

---

**UPDATED:** 2026-02-01
