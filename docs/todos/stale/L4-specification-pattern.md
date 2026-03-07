# L4: Specification Pattern Support

**Priority:** Low
**Category:** Future Feature
**Effort:** High
**Status:** Not Started / Evaluation

---

## Overview

The Specification pattern is commonly used in DDD for:
1. Encapsulating query criteria
2. Combining criteria with AND/OR/NOT
3. Checking if an object satisfies criteria
4. Building repository queries

---

## Current State

Neatoo does not include specification pattern support. Queries are handled directly in factory methods or repository implementations.

---

## Potential Value

### Pros
- Encapsulates business query logic
- Reusable across queries
- Composable (AND, OR, NOT)
- Testable in isolation

### Cons
- Additional abstraction layer
- Complexity for simple queries
- Learning curve
- Not needed for basic CRUD

---

## If Implemented

### Core Interfaces

```csharp
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T entity);
    Expression<Func<T, bool>> ToExpression();
}

public abstract class Specification<T> : ISpecification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T entity)
    {
        var predicate = ToExpression().Compile();
        return predicate(entity);
    }

    public Specification<T> And(Specification<T> other) =>
        new AndSpecification<T>(this, other);

    public Specification<T> Or(Specification<T> other) =>
        new OrSpecification<T>(this, other);

    public Specification<T> Not() =>
        new NotSpecification<T>(this);
}
```

### Composite Specifications

```csharp
internal class AndSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public AndSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr = _left.ToExpression();
        var rightExpr = _right.ToExpression();

        var param = Expression.Parameter(typeof(T));
        var body = Expression.AndAlso(
            Expression.Invoke(leftExpr, param),
            Expression.Invoke(rightExpr, param));

        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}
```

### Usage Example

```csharp
// Define specifications
public class ActiveCustomerSpec : Specification<Customer>
{
    public override Expression<Func<Customer, bool>> ToExpression()
        => c => c.IsActive;
}

public class PremiumCustomerSpec : Specification<Customer>
{
    public override Expression<Func<Customer, bool>> ToExpression()
        => c => c.TotalPurchases > 10000;
}

// Compose
var activePremium = new ActiveCustomerSpec()
    .And(new PremiumCustomerSpec());

// Use in repository
public async Task<List<Customer>> FindAsync(ISpecification<Customer> spec)
{
    return await _context.Customers
        .Where(spec.ToExpression())
        .ToListAsync();
}
```

---

## Recommendation

**Do Not Implement Yet**

Reasons:
1. Neatoo focuses on aggregate behavior, not querying
2. EF Core and LINQ already provide query composition
3. Adds complexity for limited benefit in the target use case
4. Can be added by developers who need it

---

## Alternative: Documentation

Instead of building in, document how to use specifications with Neatoo:

```markdown
## Using Specification Pattern with Neatoo

Neatoo focuses on aggregate behavior and validation. For query specifications,
consider using a separate library like:

- [Ardalis.Specification](https://github.com/ardalis/Specification)
- Custom implementation as shown below

### Integration Pattern

```csharp
[Fetch]
public static async Task<ICustomerList> FetchBySpec(
    ISpecification<CustomerEntity> spec,
    [Service] ICustomerRepository repo)
{
    var entities = await repo.FindAsync(spec);
    // Map to Neatoo domain objects...
}
```
```

---

## If Demand Exists

If users request this feature, consider:

1. **Separate NuGet package:** `Neatoo.Specifications`
2. **Integration hooks:** Allow specifications in factory methods
3. **Query factories:** Generate specification-aware fetch methods

---

## Decision Log

| Date | Decision | Reason |
|------|----------|--------|
| 2024-12-31 | Evaluate only | Low priority, high effort |

---

## Files to Create (If Implemented)

| File | Description |
|------|-------------|
| `src/Neatoo.Specifications/ISpecification.cs` | Core interface |
| `src/Neatoo.Specifications/Specification.cs` | Base class |
| `src/Neatoo.Specifications/AndSpecification.cs` | Composite |
| `src/Neatoo.Specifications/OrSpecification.cs` | Composite |
| `src/Neatoo.Specifications/NotSpecification.cs` | Composite |
| `docs/specifications.md` | Documentation |
