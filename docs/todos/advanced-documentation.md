# Advanced Documentation

Documentation tasks for advanced topics identified during code review.

**Created:** 2026-01-10
**Status:** Not Started
**Origin:** code-smells-review.md

---

## Extensibility Principle

**Draft created:** `docs/architecture/extensibility-principle.md`

Add extensibility principle to main documentation (advanced section). This principle should be prominent so users understand Neatoo's philosophy of full DI extensibility.

- [ ] Add "Architecture" or "Advanced" section to main docs
- [ ] Move/incorporate extensibility-principle.md content
- [ ] Add examples of customizing services
- [ ] Document which services can be replaced and why users might want to

---

## Threading Model

Document Neatoo's threading assumptions and safe usage patterns.

**Key points:**
- Neatoo assumes single async flow per entity instance
- Entities must not be shared across concurrent operations
- Blazor Server has sync context per circuit (safe)
- Blazor Server background work requires `InvokeAsync`
- ASP.NET Core server has no sync context but safe if single async flow

**Reference:** [ASP.NET Core Blazor synchronization context](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/synchronization-context)

- [ ] Create threading model documentation
- [ ] Add examples of safe vs unsafe usage patterns
- [ ] Document Blazor Server `InvokeAsync` requirement for background work

---

## Rule Index Serialization Contract

Document the fragile assumption that rules are added in the same order on client and server.

**How rule indexing works:**
1. `RuleManager._ruleIndex` is a sequential counter starting at 1
2. Each rule gets assigned `UniqueIndex` when added
3. `RuleMessage.RuleIndex` references which rule produced the message
4. Messages are serialized with their `RuleIndex`
5. After deserialization, rules clear/update messages by matching `RuleIndex`

**Scenarios that break this:**
- Different code versions on client vs server
- Conditional rule registration
- Custom rules added in different order
- Plugin-based rules

- [ ] Document rule index serialization contract in advanced docs
- [ ] Document that rules MUST be added in identical order on client/server
- [ ] Consider if there's a more robust identification scheme (rule type + property hash?)
