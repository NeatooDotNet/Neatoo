# Neatoo Framework Improvement Todos

This directory contains detailed action items from the comprehensive framework review conducted on December 31, 2024.

---

## Quick Reference

| ID | Priority | Title | Effort | Status |
|----|----------|-------|--------|--------|
| [C1](C1-domain-events-support.md) | Critical | Add Domain Events Support | Low | Not Started |
| [C2](C2-beginner-tutorials.md) | Critical | Create Beginner-Friendly Tutorials | Medium | Not Started |
| [H1](completed/H1-fix-rulemanager-assertion.md) | High | Fix RuleManager Assertion Logic | Low | **Completed** |
| [H2](H2-document-async-validation-patterns.md) | High | Document Async Validation Patterns | Medium | Not Started |
| [H3](completed/H3-add-generator-diagnostics.md) | High | Add Generator Diagnostics | Low | **Completed** |
| [H4](completed/H4-thread-safe-ismarkedbusy.md) | High | Make IsMarkedBusy Thread-Safe | Low | **Completed** |
| [M1](completed/M1-extract-metaproperty-helper.md) | Medium | Extract Meta-Property Change Helper | Low | **Completed** |
| [M2](M2-value-object-base.md) | Medium | Add Value Object Base Type | Medium | Not Started |
| [M3](M3-document-propertychanged-events.md) | Medium | Document PropertyChanged vs NeatooPropertyChanged | Low | Not Started |
| [M4](completed/M4-resolve-todo-comments.md) | Medium | Resolve TODO Comments | Low | **Completed** |
| [M4a](completed/M4a-displayname-serialization.md) | Medium | Remove DisplayName from Serialization | Medium | **Completed** |
| [M5](M5-aggregate-root-marker.md) | Medium | Add IAggregateRoot Marker Interface | Low | Not Started |
| [M6](completed/multi-targeting-net8-9-10.md) | Medium | Multi-Target .NET 8.0/9.0/10.0 | Medium | **Completed** |
| [L1](L1-naming-consistency.md) | Low | Naming Consistency Fixes | Low | Not Started |
| [L2](L2-duplicate-event-subscriptions.md) | Low | Guard Against Duplicate Event Subscriptions | Low | Not Started |
| [L3](completed/L3-fix-waitfortasks-assert.md) | Low | Fix Debug.Assert in WaitForTasks | Low | **Completed** |
| [L4](L4-specification-pattern.md) | Low | Specification Pattern Support | High | Evaluation |
| [L5](L5-internal-interface-pattern.md) | Low | Internal Interface Pattern - Document & Simplify | Medium | Not Started |
| [L6](L6-consolidate-lifecycle-interfaces.md) | Low | Consolidate Lifecycle Interfaces | Low | Not Started |
| [LL1](lazy-loading-entities.md) | Medium | Add Lazy Loading for Child Entities | High | Not Started |
| [M7](M7-analyze-documentation-patterns.md) | Medium | Analyze Documentation Sample Patterns | Low | Not Started |
| [M8](M8-document-child-property-cascade.md) | Medium | Document Child Property Cascade Behavior | Low | Not Started |
| [CT1](completed/cancellation-token-support.md) | Medium | Add CancellationToken Support | High | **Completed** |

---

## By Category

### Missing DDD Features
- [C1](C1-domain-events-support.md) - Domain Events Support
- [M2](M2-value-object-base.md) - Value Object Base Type
- [M5](M5-aggregate-root-marker.md) - IAggregateRoot Marker Interface
- [L4](L4-specification-pattern.md) - Specification Pattern Support
- [LL1](lazy-loading-entities.md) - Lazy Loading for Child Entities

### Code Bugs
- ~~[H1](completed/H1-fix-rulemanager-assertion.md) - RuleManager Assertion Logic~~ **DONE**
- ~~[H3](completed/H3-add-generator-diagnostics.md) - Generator Diagnostics~~ **DONE**
- ~~[H4](completed/H4-thread-safe-ismarkedbusy.md) - Thread-Safe IsMarkedBusy~~ **DONE**
- [L2](L2-duplicate-event-subscriptions.md) - Duplicate Event Subscriptions
- ~~[L3](completed/L3-fix-waitfortasks-assert.md) - WaitForTasks Assert~~ **DONE**

### Documentation
- [C2](C2-beginner-tutorials.md) - Beginner Tutorials
- [H2](H2-document-async-validation-patterns.md) - Async Validation Patterns
- [M3](M3-document-propertychanged-events.md) - PropertyChanged Events
- [M8](M8-document-child-property-cascade.md) - Child Property Cascade Behavior
- ~~[Snippet Refactoring](completed/snippet-refactoring-plan.md) - Refactor full examples to focused micro-snippets~~ **DONE**
- ~~[Documentation Feedback Review](completed/documentation-feedback-review.md) - Inline feedback cleanup~~ **DONE**

### Code Quality
- ~~[M1](completed/M1-extract-metaproperty-helper.md) - Meta-Property Change Helper~~ **DONE**
- ~~[M4](completed/M4-resolve-todo-comments.md) - TODO Comments~~ **DONE**
- ~~[M4a](completed/M4a-displayname-serialization.md) - DisplayName Serialization Performance~~ **DONE**
- [M7](M7-analyze-documentation-patterns.md) - Analyze Documentation Sample Patterns
- [L1](L1-naming-consistency.md) - Naming Consistency
- [L5](L5-internal-interface-pattern.md) - Internal Interface Pattern (document rationale, consider InternalsVisibleTo simplification)
- [L6](L6-consolidate-lifecycle-interfaces.md) - Consolidate Lifecycle Interfaces (cosmetic)

### Infrastructure
- ~~[M6](completed/multi-targeting-net8-9-10.md) - Multi-Target .NET 8.0/9.0/10.0~~ **DONE**
- ~~[CT1](completed/cancellation-token-support.md) - CancellationToken Support~~ **DONE**
- ~~[RemoteFactory Upgrade](completed/remotefactory-upgrade-blocked.md) - Upgraded to 10.5.0~~ **DONE**
- ~~[Skill Samples](completed/skill-samples-needed.md) - Added 17 snippets for skill files~~ **DONE**

---

## Suggested Sprint Planning

### Sprint 1 (Quick Wins)
- ~~[H1](completed/H1-fix-rulemanager-assertion.md) - Fix RuleManager assertion (Low effort)~~ **DONE**
- ~~[H3](completed/H3-add-generator-diagnostics.md) - Add generator diagnostics (Low effort)~~ **DONE**
- ~~[H4](completed/H4-thread-safe-ismarkedbusy.md) - Thread-safe IsMarkedBusy (Low effort)~~ **DONE**
- ~~[M4](completed/M4-resolve-todo-comments.md) - Resolve TODO comments (Decision items)~~ **DONE**
- [M7](M7-analyze-documentation-patterns.md) - Analyze patterns from doc samples (Low effort)

### Sprint 2 (Documentation)
- [C2](C2-beginner-tutorials.md) - Beginner tutorials (Reduces adoption barrier)
- [H2](H2-document-async-validation-patterns.md) - Async validation docs
- [M3](M3-document-propertychanged-events.md) - Event documentation
- [M8](M8-document-child-property-cascade.md) - Child property cascade behavior

### Sprint 3 (Code Quality)
- ~~[M1](completed/M1-extract-metaproperty-helper.md) - Extract helper method~~ **DONE**
- [L1](L1-naming-consistency.md) - Naming consistency
- [L2](L2-duplicate-event-subscriptions.md) - Event subscription guards
- ~~[L3](completed/L3-fix-waitfortasks-assert.md) - WaitForTasks cleanup~~ **DONE**

### Sprint 4+ (Features)
- [C1](C1-domain-events-support.md) - Domain Events (Low effort with RemoteFactory 10.6.0)
- [M2](M2-value-object-base.md) - Value Object support
- [M5](M5-aggregate-root-marker.md) - IAggregateRoot marker
- [LL1](lazy-loading-entities.md) - Lazy Loading for Child Entities (High effort)

---

### Entity List Enhancements
- ~~[ContainingList Property](completed/containinglist-property-implementation.md) - Track entity's containing list~~ **DONE**
- ~~[Delete/Remove Consistency](completed/entity-delete-vs-list-remove.md) - entity.Delete() and list.Remove() now consistent~~ **DONE**
- ~~[EntityListBase Add Use Cases](completed/entitylistbase-add-use-cases.md) - Add item use case analysis~~ **DONE**

---

## Related Documents

- [Comprehensive Framework Review](completed/comprehensive-framework-review.md) - Full analysis (completed)
- ~~[RemoteFactory Mapper Removal Plan](completed/remotefactory-mapper-removal-plan.md) - Migration plan~~ **DONE**

---

*Generated from comprehensive framework review - December 31, 2024*
