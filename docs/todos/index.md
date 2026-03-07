# Neatoo Framework Improvement Todos

*Last audited: 2026-03-07*

---

## Active Todos

### Features

| Todo | Priority | Effort | Status |
|------|----------|--------|--------|
| [C1 - Domain Events Support](C1-domain-events-support.md) | Critical | Low | Not Started |
| [M2 - Value Object Base Type](M2-value-object-base.md) | Medium | Medium | Not Started |
| [Entity Dictionary Base](entity-dictionary-base.md) | Medium | Medium | Pending |
| [Meta-State Notifier Service](meta-state-notifier-service.md) | Medium | Medium | Not Started |

### Bugs

| Todo | Priority | Effort | Status |
|------|----------|--------|--------|
| [L2 - Duplicate Event Subscriptions](L2-duplicate-event-subscriptions.md) | Low | Low | Not Started |
| [RunRulesFlag Messages Bug](runrulesflag-messages-bug.md) | Medium | Low | Not Started |

### Code Quality

| Todo | Priority | Effort | Status |
|------|----------|--------|--------|
| [L1 - Naming Consistency](L1-naming-consistency.md) | Low | Low | Not Started |
| [L5 - Internal Interface Pattern](L5-internal-interface-pattern.md) | Low | Medium | Not Started |
| [L6 - Consolidate Lifecycle Interfaces](L6-consolidate-lifecycle-interfaces.md) | Low | Low | Not Started |
| [Remove Inconsistent Locks](remove-inconsistent-locks.md) | Low | Low | Not Started |
| [Replace Quoted Identifiers with nameof](replace-quoted-identifiers-with-nameof.md) | Low | Low | Not Started |
| [Solution B - Internal Interfaces Design](solution-b-internal-interfaces-design.md) | Low | Medium | Design Document |

### Documentation

| Todo | Priority | Effort | Status |
|------|----------|--------|--------|
| [C2 - Beginner Tutorials](C2-beginner-tutorials.md) | Critical | Medium | Not Started |
| [H2 - Async Validation Patterns](H2-document-async-validation-patterns.md) | High | Low | Partially Done |
| [Advanced Documentation](advanced-documentation.md) | Medium | Medium | Partially Done |
| [Business Rules in Factory Methods Antipattern](business-rules-in-factory-methods-antipattern.md) | Medium | Low | Not Started |
| [Skill Documentation Gaps](skill-documentation-gaps.md) | Low | Low | Partially Addressed |
| [MudBlazor Skill Documentation Gaps](mudblazor-skill-documentation-gaps.md) | Low | Low | Not Started |
| [Pseudo-Block Recategorization](pseudo-block-recategorization.md) | Low | Low | Analysis Complete |

### Tooling / Infrastructure

| Todo | Priority | Effort | Status |
|------|----------|--------|--------|
| [Analyzer - Partial Class Requirement](analyzer-partial-class-requirement.md) | Medium | Medium | Not Started |
| [AOT Compliance Exploration](aot-compliance-exploration.md) | Low | Medium | Not Started |

### Design / Analysis Documents

| Todo | Priority | Notes |
|------|----------|-------|
| [Internal Interface Methods Analysis](internal-interface-methods-analysis.md) | High | API design rationale reference |
| [Reduce Design Source Scope](reduce-design-source-scope.md) | Low | Planned, not started |
| [Future Enhancements](future-enhancements.md) | Low | Backlog of enhancement ideas |

---

## Completed Todos

See `completed/` directory. Key completions include:

- **IEntityRoot Interface** - Root vs child entity distinction (issavable-intuitive-api)
- **LazyLoad\<T>** - Explicit lazy loading type (lazy-loading-v2-design, lazyload-serialization-bug, lazyload-state-propagation)
- **Design.Domain** - Authoritative API design reference (design-source-of-truth, design-source-improvements, design-interface-first-pattern)
- **Property Backing Fields** - Generated Property\<T> backing fields with DI (property-backing-fields)
- **IsValid Stale Cache Fix** - RecalculateValidity after RunRules (isvalid-stale-cache-during-runrules)
- **LoadValue/SetParent Bug** - ChangeReason enum for property change intent (loadvalue-setparent-bug)
- **Documentation Overhaul** - MarkdownSnippets integration, samples project, code block audit, snippet refactoring
- **CancellationToken Support** - Full cancellation token support with samples
- **Collapse Base Layer v10.4.0** - Simplified class hierarchy
- **Multi-Target .NET 8/9/10** - Framework multi-targeting
- **Neatoo Skill** - Created and refined with MarkdownSnippets samples

## Stale Todos

See `stale/` directory. Items superseded by newer work:

- **M5 - IAggregateRoot Marker** - Superseded by IEntityRoot interface
- **M7 - Analyze Documentation Patterns** - Patterns analyzed and resolved
- **L4 - Specification Pattern** - Deferred by design ("Do Not Implement Yet")
- **LL1 - Lazy Loading Entities** - Superseded by LazyLoad\<T> implementation
- **KnockOff Upgrade Build Errors** - KnockOff now separate repo
- **ListBase Parent Behavior** - Reference doc, not a work item

---

## Related Documents

- [Comprehensive Framework Review](completed/comprehensive-framework-review.md) - Original analysis (December 2024)

---

*Originally generated from comprehensive framework review - December 31, 2024. Audited and reorganized March 7, 2026.*
