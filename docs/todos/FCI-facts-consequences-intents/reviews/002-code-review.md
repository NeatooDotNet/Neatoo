# FCI-002 code review

**2026-10-16: APPROVED.** No veto. Semantics match the setter: `SetValue` for public, protected and internal, `SetPrivateValue` for private, the same task tracking, the cascade's Task returned. The model stays equatable.

- **Callout, Medium, AC-2, fixed:** a partial interface that declares the property get-only still gained `Task SetX(T)`, reopening a write its author left out. Now skipped (`InterfaceDeclaresReadOnly`), pinned by `AwaitableSetterGenerationTests.InterfaceDeclaresPropertyGetOnly_NoSetterOnInterface`.
- Could not verify: CS0628 for a `protected set` accessor on a sealed class.
- Theoretical, not triaged: sealed class with `protected set`; `Set{Name}` on a base interface hidden; hand-written doubles of a partial interface need the new members (follows from D28); a nested type named `Set{Name}`.
