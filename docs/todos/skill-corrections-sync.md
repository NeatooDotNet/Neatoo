# Skill Corrections Sync

Sync Neatoo documentation with corrections discovered in `~/.claude/skills/neatoo` skill files.

## Summary of Key Corrections

### 1. Rules Paused in Factory Methods (ALREADY CORRECT IN DOCS)

The skill was updated to clarify that **rules are automatically paused** in factory methods (`[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`). Property setters work directly - no need for `LoadProperty()`.

**Old (wrong) understanding:**
```csharp
[Fetch]
public async Task<bool> Fetch([Service] IDbContext db)
{
    var entity = await db.Persons.FindAsync(Id);
    if (entity == null) return false;

    // OLD - Use LoadProperty to avoid triggering rules
    LoadProperty(nameof(Id), entity.Id);
    LoadProperty(nameof(FirstName), entity.FirstName);
}
```

**Correct understanding:**
```csharp
[Fetch]
public async Task<bool> Fetch([Service] IDbContext db)
{
    var entity = await db.Persons.FindAsync(Id);
    if (entity == null) return false;

    // Rules are paused in factory methods - setters work directly
    Id = entity.Id;
    FirstName = entity.FirstName;
}
```

**Status:** Neatoo docs already correct (factory-operations.md, property-system.md, validation-and-rules.md)

### 2. Cascading Rules Are a Feature (NEEDS DOC UPDATE)

**Key insight:** When a rule sets a property, dependent rules run automatically. This cascading behavior is **intentional and essential** for maintaining domain consistency.

**Only use LoadProperty() for rare cases:**
- Breaking circular references (Rule A triggers Rule B triggers Rule A)

**Do NOT use LoadProperty() for:**
- Factory methods (rules already paused)
- Normal rule execution (cascading is correct behavior)

**Action:** Add guidance to validation-and-rules.md about cascading being a feature.

### 3. EntityListBase Constructor Pattern (NEEDS REVIEW)

The skill shows EntityListBase using parameterless base constructor:

```csharp
public OrderLineList(IOrderLineFactory lineFactory) : base()
{
    _lineFactory = lineFactory;
}
```

Current docs show `[Service]` on constructor:
```csharp
public PhoneList([Service] IPhoneFactory phoneFactory)
{
    _phoneFactory = phoneFactory;
}
```

**Action:** Review if collections.md needs updating for constructor pattern.

### 4. DI Patterns - Constructor vs [Service] Injection

**Constructor Injection** - For dependencies needed throughout entity's lifetime:
```csharp
public Person(
    IEntityBaseServices<Person> services,
    IEmailValidator emailValidator) : base(services)
{
    _emailValidator = emailValidator;
    RuleManager.AddRule(new EmailValidationRule(_emailValidator));
}
```

**[Service] Injection** - For dependencies only needed in factory methods:
```csharp
[Create]
public void Create([Service] IPersonPhoneListFactory phoneListFactory)
{
    PersonPhoneList = phoneListFactory.Create();  // One-time initialization
}
```

**Anti-Pattern - Factory as Method Parameter:**
```csharp
// WRONG - forces caller to inject and pass factory
public IOrderLine AddLine(IOrderLineFactory lineFactory)
{
    var line = lineFactory.Create();
    Add(line);
    return line;
}
```

Services should be constructor-injected, not passed by callers.

### 5. Common Pitfalls Updated

**Removed pitfall:**
- "Using property setters in Fetch - Use LoadProperty() instead"

**Added pitfall:**
- "Overusing LoadProperty - Cascading rules are a feature; LoadProperty only for circular references"

## Tasks

- [x] **validation-and-rules.md** - Add section clarifying cascading rules as a feature
- [x] **collections.md** - Reviewed - constructor pattern with `[Service]` is correct
- [x] **aggregates-and-entities.md** - Added DI patterns section
- [ ] **CLAUDE.md** - Review if project instructions need updates (optional)

## Source Files Changed in Skill

| File | Type of Changes |
|------|-----------------|
| SKILL.md | LoadProperty guidance, pitfalls list |
| rules.md | Cascading rules section, LoadProperty guidance |
| entities.md | EntityListBase constructor, DI patterns |
| properties.md | LoadProperty guidance, pitfalls |
| data-mapping.md | Factory method guidance throughout |

## Cross-Reference

- Skill location: `~/.claude/skills/neatoo/`
- Doc location: `c:\src\neatoodotnet\Neatoo\docs\`
