# Documentation TODO: Save() Reassignment Pattern

## Problem

Developers are forgetting to reassign the aggregate after calling `Save()`. For example:

```csharp
// WRONG - loses the new instance
await this.Book.Save();

// CORRECT - captures the deserialized instance
this.Book = await this.Book.Save();
```

This is critical because `Save()` serializes the object to the server, performs persistence, and returns a **new deserialized instance**. The original object is stale.

## Current Documentation Gaps

1. **No explanation of WHY** - Docs show the pattern but don't explain serialization/deserialization
2. **Limited consequences** - Only mentions losing database-generated IDs
3. **No Blazor-specific guidance** - Missing warnings about UI binding and event subscriptions
4. **Not in troubleshooting** - No "Common Mistakes" entry for this anti-pattern

---

## Documentation Improvements

### 1. Add to `docs/factory-operations.md` (expand existing section at line 271)

Replace the current "Important: Capture Return Value" section with:

```markdown
### Critical: Always Reassign After Save()

When you call `Save()`, the aggregate is **serialized to the server**, persisted, and a **new instance is returned** via deserialization. You MUST capture this return value:

```csharp
// CORRECT - captures the new deserialized instance
person = await person.Save();
```

```csharp
// WRONG - original object is now stale!
await person.Save();
// person still has old state, no database-generated values
```

#### Why This Happens

The Remote Factory pattern transfers your object across the client-server boundary:

1. **Client**: Your aggregate is serialized to JSON/binary
2. **Server**: A new instance is created from that data, persistence runs
3. **Server**: The updated aggregate is serialized back
4. **Client**: A NEW instance is deserialized and returned

The object you started with is **not the same object** that comes back. They are two different instances in memory.

#### Consequences of Forgetting

| What You Lose | Example |
|---------------|---------|
| Database-generated IDs | `person.Id` remains `Guid.Empty` or `0` |
| Server-computed values | Timestamps, calculated fields |
| Updated validation state | `IsValid`, `IsSavable` reflect old state |
| Property modification flags | `IsModified` doesn't reflect saved state |
| Concurrency tokens | RowVersion/ETag for optimistic concurrency |
```

---

### 2. Add to `docs/blazor-binding.md` (new section after Save button example ~line 230)

```markdown
### Critical: Reassign After Save() in Blazor Components

In Blazor, failing to reassign after `Save()` causes the UI to display stale data:

```csharp
@code {
    private IPerson? Person { get; set; }

    private async Task HandleSave()
    {
        if (Person is null || !Person.IsSavable) return;

        // CRITICAL: Reassign to get the new deserialized instance
        Person = await Person.Save();

        // The UI will now show:
        // - Database-generated ID
        // - Server-computed values
        // - Reset modification state (IsModified = false)
    }
}
```

#### Why This Matters for Blazor

When `Save()` completes, a **completely new object instance** is returned. If you don't reassign:

1. **UI shows stale data** - The bound `Person` object has old values
2. **ID fields are wrong** - Database-generated IDs won't appear
3. **State is incorrect** - `IsModified`, `IsDirty` reflect pre-save state
4. **Subsequent saves may fail** - Concurrency tokens are outdated

#### Common Mistake Pattern

```csharp
// DON'T DO THIS
private async Task HandleSave()
{
    await Person.Save();  // Return value discarded!

    // Person still shows:
    // - Id = Guid.Empty (if new)
    // - IsModified = true (should be false)
    // - Old property values if server modified them

    NavigationManager.NavigateTo($"/person/{Person.Id}");  // Navigates to empty GUID!
}
```

#### Correct Pattern

```csharp
// DO THIS
private async Task HandleSave()
{
    Person = await Person.Save();  // Capture new instance

    // Person now shows correct state
    NavigationManager.NavigateTo($"/person/{Person.Id}");  // Works correctly
}
```
```

---

### 3. Add to `docs/troubleshooting.md` (new section in Common Issues)

```markdown
## Save() Not Updating the UI / Stale Data After Save

### Symptom

After calling `Save()`:
- Database-generated ID is still empty/zero
- UI shows old values
- `IsModified` is still `true` when it should be `false`
- Navigation to `/{id}` routes fail with empty ID

### Cause

You forgot to reassign the return value from `Save()`:

```csharp
// WRONG
await person.Save();
// person is now stale - it's the PRE-save instance

// CORRECT
person = await person.Save();
// person is now the POST-save instance with updated state
```

### Why This Happens

`Save()` uses the Remote Factory pattern which serializes your object to the server and deserializes a NEW instance back. The original object in memory is unchanged - it's a completely different object from what the server returns.

Think of it like mailing a letter:
1. You write a letter (your aggregate)
2. You mail it (serialize to server)
3. Someone adds information and mails it back (server persistence + serialize back)
4. You receive a NEW letter (deserialized instance)
5. Your original draft is still on your desk unchanged (original object)

### Solution

Always capture the return value:

```csharp
// In a Blazor component
this.Person = await this.Person.Save();

// In a service/handler
var savedPerson = await person.Save();
return savedPerson;

// Chained operations
person = await person.Save();
var id = person.Id;  // Now has the database-generated ID
```
```

---

### 4. Add to `docs/remote-factory.md` (new section ~line 145)

```markdown
### Object Identity After Remote Operations

When using Remote Factory, understand that **remote operations return new object instances**:

```csharp
var person = await createPerson("John");
var originalReference = person;

person = await person.Save();

// These are DIFFERENT objects
Console.WriteLine(ReferenceEquals(originalReference, person));  // false
```

This occurs because:
1. The object is serialized (converted to data)
2. Transmitted to the server
3. A new instance is created on the server
4. Server performs the operation
5. The result is serialized back
6. A NEW instance is deserialized on the client

#### Implications

| Operation | Returns New Instance? | Must Reassign? |
|-----------|----------------------|----------------|
| `Create()` | Yes | Yes (to variable) |
| `Fetch()` | Yes | Yes (to variable) |
| `Save()` | Yes | **Yes - Critical!** |
| `Delete()` | N/A | N/A |

Always treat remote factory operations as returning fresh instances that must be captured.
```

---

## Implementation Checklist

- [x] Update `docs/factory-operations.md` with expanded "Critical: Always Reassign" section (completed 2025-12-29)
- [x] Add Blazor-specific section to `docs/blazor-binding.md` (completed 2025-12-29)
- [x] Add troubleshooting entry to `docs/troubleshooting.md` (completed 2025-12-29)
- [x] Add "Object Identity" section to `docs/remote-factory.md` (completed 2025-12-29)
- [ ] Consider adding a code analyzer rule to detect `await x.Save();` without assignment
- [ ] Add example showing the issue in `src/Examples/` if helpful

## Completed Changes Summary (2025-12-29)

### factory-operations.md
- Expanded "Important: Capture Return Value" to "Critical: Always Reassign After Save()"
- Added "Why This Happens" section explaining serialization/deserialization
- Added "Consequences of Forgetting" table with specific examples
- Added cross-reference to Blazor Binding documentation

### blazor-binding.md
- Added new section "Critical: Reassign After Save() in Blazor Components"
- Included Blazor-specific consequences (UI stale data, navigation breaks)
- Added "Common Mistake Pattern" with NavigationManager example
- Added "Correct Pattern" showing proper reassignment

### troubleshooting.md
- Added new section "Stale Data After Save / UI Not Updating"
- Listed specific symptoms developers will see
- Added "mailing a document" analogy for understanding
- Included cross-references to other documentation

### remote-factory.md
- Added "Object Identity After Remote Operations" section
- Included `ReferenceEquals` code example proving objects are different
- Added implications table showing which operations return new instances
- Added "Common Mistake" section with correct vs incorrect code
