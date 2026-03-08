# CSLA and DDD: Same Destination, Different Motivation

CSLA predates Eric Evans' 2003 DDD book. It arrived at aggregate-like patterns from a completely different direction.

## Different starting questions

**DDD asks:** "What is the right model for this domain?"

**CSLA asks:** "How do I build a smart business object that binds to a UI?"

They happen to agree that the answer involves self-validating objects with save boundaries and encapsulated business logic. But CSLA is a framework that gives you the plumbing; DDD is a design discipline that tells you what to put in the pipes.

## Where they converge

| Concept | DDD | CSLA | Same instinct? |
|---------|-----|------|:-:|
| Business logic lives in domain objects | Core principle | Core principle — not in UI, not in services | Yes |
| Objects enforce their own invariants | Aggregate invariants | `BusinessRules` / `IsValid` | Yes |
| Save boundary = root object | Aggregate root | Editable root (children can't self-save) | Yes |
| Identity tracking | Entity has identity | `BusinessBase` tracks `IsNew`, state | Yes |
| Persistence separated from domain | Repository pattern | DataPortal abstracts persistence | Yes |
| Children can't be saved independently | Aggregate consistency | `IsChild` prevents independent save | Yes |

## Where they diverge

### CSLA doesn't have

- **Ubiquitous language** — CSLA uses technical vocabulary (`BusinessBase`, `DataPortal`), not domain vocabulary
- **Bounded contexts** — No concept of context boundaries, context mapping, anti-corruption layers
- **Value objects** — Everything is a "business object." No immutability distinction.
- **Domain events** — No event-driven communication between aggregates
- **The "why"** — CSLA tells you *how* to structure objects. DDD tells you *why* you're structuring them that way and *how to discover* the right structure.

### CSLA has things DDD doesn't concern itself with

- **UI data binding** — `INotifyPropertyChanged`, `INotifyCollectionChanged`, validation messages formatted for UI display
- **Client-server state transfer** — Serialization, DataPortal channels, mobile objects
- **N-level undo** — `BeginEdit`/`CancelEdit` for UI cancel buttons
- **Property-level metadata** — `IsDirty`, `IsValid`, `IsBusy` per property, designed for binding save buttons and validation displays

## Where Neatoo sits

Neatoo bridges this gap. It takes CSLA's proven plumbing — self-tracking, self-validating, UI-bindable objects with save boundaries — and puts it in DDD vocabulary with DDD-first design:

| CSLA (pre-DDD) | Neatoo (DDD) |
|----------------|-------------|
| `BusinessBase` | `EntityBase` |
| "Editable root" | `IEntityRoot` (aggregate root interface) |
| "Editable child" | `IEntityBase` (child entity interface) |
| "Business object" (everything) | Entities, value objects, commands — distinct types |
| Technical framework vocabulary | Domain-aligned vocabulary |
| Interface-optional, concrete-first | Interface-first: concretes are `internal`, all references use interfaces |

The practical patterns are CSLA's. The conceptual framing is DDD's. Neatoo makes the DDD vocabulary the native language of the framework rather than an afterthought.
