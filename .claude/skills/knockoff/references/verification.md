# Verification Reference

KnockOff provides three verification approaches: direct verification on interceptors, batch verification via `Verifiable()`, and `VerifyAll()` for all configured members.

---

## Called Constraints

| Constraint | Description |
|------------|-------------|
| `Called.Never` | Must not be called (0 times) |
| `Called.Once` | Exactly 1 call |
| `Called.Twice` | Exactly 2 calls |
| `Called.AtLeastOnce` | 1 or more calls |
| `Called.Exactly(n)` | Exactly n calls |
| `Called.AtLeast(n)` | n or more calls |
| `Called.AtMost(n)` | n or fewer calls |

**`Called.Between()` does NOT exist.** Use separate constraints:

```csharp
stub.Save.Verify(Called.AtLeast(1));
stub.Save.Verify(Called.AtMost(5));
```

---

## Direct Verification

Call `Verify()` directly on any interceptor for immediate checking:

<!-- snippet: verification-ref-direct -->
```cs
stub.GetById.Verify();                  // At least once (default)
stub.GetById.Verify(Called.Exactly(2)); // Exactly twice
stub.Refresh.Verify(Called.Never);       // Never called
```
<!-- endSnippet -->

Throws `VerificationException` immediately if the constraint is not met.

---

## Batch Verification -- Verifiable() + stub.Verify()

Mark interceptors during configuration, then verify all at once:

<!-- snippet: verification-ref-batch -->
```cs
// Step 1: Mark during setup
stub.GetById.Call((id) => new User { Id = id }).Verifiable();
stub.Save.Call((u) => { }).Verifiable(Called.Exactly(2));
stub.Refresh.Call(() => { }).Verifiable(Called.Never);

// Step 2: Exercise code
IRepoVerify repository = stub;
repository.GetById(1);
repository.Save(new User { Id = 1 });
repository.Save(new User { Id = 2 });

// Step 3: Verify all marked interceptors
stub.Verify();  // Checks GetById (AtLeastOnce), Save (Exactly(2)), Refresh (Never)
```
<!-- endSnippet -->

`stub.Verify()` only checks members marked with `.Verifiable()`. Unconfigured or unmarked members are ignored.

---

## VerifyAll() -- All Configured Members

`stub.VerifyAll()` checks ALL members that were configured (Return, Call, Get, Set, When), not just those marked Verifiable. Expects each to be called at least once.

<!-- snippet: verification-ref-verifyall -->
```cs
stub.GetById.Call((id) => new User { Id = id });
stub.Save.Call((user) => { });

IRepoVerify repo = stub;
repo.GetById(1);
repo.Save(new User { Id = 1 });

stub.VerifyAll(); // Checks all configured members were called at least once
```
<!-- endSnippet -->

<!-- snippet: verification-ref-verifyall-throws -->
```cs
// VerifyAll THROWS if any configured member was not called
Assert.Throws<VerificationException>(() => stub.VerifyAll());
```
<!-- endSnippet -->

---

## Per-Member Verification

### Methods

```csharp
stub.Add.Verify(Called.Once);
stub.Save.Verify(Called.AtLeast(2));
```

### Properties

```csharp
stub.Name.VerifyGet(Called.Exactly(3));  // Getter call count
stub.Name.VerifySet(Called.Once);         // Setter call count
stub.Name.Verify(Called.Exactly(4));      // Total (get + set)
```

### Indexers

```csharp
stub.Indexer.VerifyGet(Called.Exactly(2)); // All getter calls (any key)
stub.Indexer.VerifySet(Called.Once);        // All setter calls (any key)
```

### Events

```csharp
stub.Started.VerifyAdd(Called.Once);       // Subscription count
stub.Started.VerifyRemove(Called.Never);   // Unsubscription count
stub.Started.Verify();                     // Alias for VerifyAdd(AtLeastOnce)
```

### Delegates

```csharp
stub.Interceptor.Verify(Called.Exactly(3));
```

### Generic Methods

```csharp
stub.GetById.Of<User>().Verify(Called.Once);
stub.GetById.Of<Product>().Verify(Called.Never);
```

---

## Sequence Verification

Sequences have their own `Verify()` that checks if the entire sequence was consumed:

<!-- snippet: verification-ref-sequence -->
```cs
var sequence = stub.GetById.Return(
    new User { Id = 1 },
    new User { Id = 2 },
    new User { Id = 3 });

IRepoVerify repo = stub;
repo.GetById(1);
repo.GetById(2);
repo.GetById(3);

sequence.Verify(); // Passes -- all 3 consumed
```
<!-- endSnippet -->

### When Chain Verification

<!-- snippet: verification-ref-when-chain -->
```cs
var chain = stub.GetById
    .When(1).Return(new User { Id = 1, Name = "Alice" })
    .ThenWhen(2).Return(new User { Id = 2, Name = "Bob" })
    .ThenCall((id) => new User { Id = id });

IRepoVerify repo = stub;
repo.GetById(1);
repo.GetById(2);
repo.GetById(3);

chain.Verify(); // Passes -- all matchers consumed
```
<!-- endSnippet -->

---

## VerificationException

When verification fails, `VerificationException` collects ALL failures and reports them together:

<!-- snippet: verification-ref-exception -->
```cs
stub.GetById.Call((id) => new User { Id = id }).Verifiable();
stub.Save.Call((user) => { }).Verifiable();
stub.Refresh.Call(() => { }).Verifiable();

IRepoVerify repo = stub;
repo.GetById(1); // Only GetById called

try { stub.Verify(); }
catch (VerificationException ex)
{
    // ex.Failures contains Save AND Refresh failures
    Assert.True(ex.Failures.Count >= 2);
}
```
<!-- endSnippet -->

---

## Verify() vs VerifyAll()

| Feature | `stub.Verify()` | `stub.VerifyAll()` |
|---------|-----------------|-------------------|
| Scope | Only `.Verifiable()` marked members | ALL configured members |
| Default constraint | As specified in `Verifiable(Called)` | `Called.AtLeastOnce` |
| Unconfigured members | Ignored | Ignored |
| Use case | Explicit expectations | Ensure all configs were used |

Choose based on testing philosophy:
- **Verify()**: Only verify what you explicitly mark (recommended)
- **VerifyAll()**: Catch accidentally unused configurations

---

## Verifiable() Chaining

`Verifiable()` returns the interceptor for fluent chaining:

<!-- snippet: verification-ref-verifiable-chaining -->
```cs
// Chain with Return
stub.GetById.Call((id) => new User { Id = id }).Verifiable(Called.Exactly(2));

// Chain with Call
stub.Save.Call((u) => { }).Verifiable(Called.Once);
```
<!-- endSnippet -->

---

## Reset Preserves Verifiable

`Reset()` clears tracking (counts) but preserves the Verifiable marking:

<!-- snippet: verification-ref-reset -->
```cs
stub.GetById.Call((id) => new User { Id = id }).Verifiable();
IRepoVerify repo = stub;
repo.GetById(1);
stub.Verify(); // Passes

stub.GetById.Reset();
// stub.Verify(); // Would FAIL -- count reset to 0

repo.GetById(2);
stub.Verify(); // Passes again
```
<!-- endSnippet -->
