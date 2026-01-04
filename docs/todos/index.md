# Neatoo Framework Improvement Todos

This directory contains detailed action items from the comprehensive framework review conducted on December 31, 2024.

---

## Quick Reference

| ID | Priority | Title | Effort | Status |
|----|----------|-------|--------|--------|
| [C1](C1-domain-events-support.md) | Critical | Add Domain Events Support | High | Not Started |
| [C2](C2-beginner-tutorials.md) | Critical | Create Beginner-Friendly Tutorials | Medium | Not Started |
| [H1](completed/H1-fix-rulemanager-assertion.md) | High | Fix RuleManager Assertion Logic | Low | **Completed** |
| [H2](H2-document-async-validation-patterns.md) | High | Document Async Validation Patterns | Medium | Not Started |
| [H3](completed/H3-add-generator-diagnostics.md) | High | Add Generator Diagnostics | Low | **Completed** |
| [H4](completed/H4-thread-safe-ismarkedbusy.md) | High | Make IsMarkedBusy Thread-Safe | Low | **Completed** |
| [M1](M1-extract-metaproperty-helper.md) | Medium | Extract Meta-Property Change Helper | Low | Not Started |
| [M2](M2-value-object-base.md) | Medium | Add Value Object Base Type | Medium | Not Started |
| [M3](M3-document-propertychanged-events.md) | Medium | Document PropertyChanged vs NeatooPropertyChanged | Low | Not Started |
| [M4](M4-resolve-todo-comments.md) | Medium | Resolve TODO Comments | Low | Not Started |
| [M4a](M4a-displayname-serialization.md) | Medium | Remove DisplayName from Serialization | Medium | **Completed** |
| [M5](M5-aggregate-root-marker.md) | Medium | Add IAggregateRoot Marker Interface | Low | Not Started |
| [M6](completed/multi-targeting-net8-9-10.md) | Medium | Multi-Target .NET 8.0/9.0/10.0 | Medium | **Completed** |
| [L1](L1-naming-consistency.md) | Low | Naming Consistency Fixes | Low | Not Started |
| [L2](L2-duplicate-event-subscriptions.md) | Low | Guard Against Duplicate Event Subscriptions | Low | Not Started |
| [L3](completed/L3-fix-waitfortasks-assert.md) | Low | Fix Debug.Assert in WaitForTasks | Low | **Completed** |
| [L4](L4-specification-pattern.md) | Low | Specification Pattern Support | High | Evaluation |
| [M7](M7-analyze-documentation-patterns.md) | Medium | Analyze Documentation Sample Patterns | Low | Not Started |

---

## By Category

### Missing DDD Features
- [C1](C1-domain-events-support.md) - Domain Events Support
- [M2](M2-value-object-base.md) - Value Object Base Type
- [M5](M5-aggregate-root-marker.md) - IAggregateRoot Marker Interface
- [L4](L4-specification-pattern.md) - Specification Pattern Support

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
- [Snippet Refactoring](snippet-refactoring-plan.md) - Refactor full examples to focused micro-snippets
- ~~[Documentation Feedback Review](completed/documentation-feedback-review.md) - Inline feedback cleanup~~ **DONE**

### Code Quality
- [M1](M1-extract-metaproperty-helper.md) - Meta-Property Change Helper
- [M4](M4-resolve-todo-comments.md) - TODO Comments
- ~~[M4a](M4a-displayname-serialization.md) - DisplayName Serialization Performance~~ **DONE**
- [M7](M7-analyze-documentation-patterns.md) - Analyze Documentation Sample Patterns
- [L1](L1-naming-consistency.md) - Naming Consistency

### Infrastructure
- ~~[M6](completed/multi-targeting-net8-9-10.md) - Multi-Target .NET 8.0/9.0/10.0~~ **DONE**

---

## Suggested Sprint Planning

### Sprint 1 (Quick Wins)
- ~~[H1](completed/H1-fix-rulemanager-assertion.md) - Fix RuleManager assertion (Low effort)~~ **DONE**
- ~~[H3](completed/H3-add-generator-diagnostics.md) - Add generator diagnostics (Low effort)~~ **DONE**
- ~~[H4](completed/H4-thread-safe-ismarkedbusy.md) - Thread-safe IsMarkedBusy (Low effort)~~ **DONE**
- [M4](M4-resolve-todo-comments.md) - Resolve TODO comments (Decision items)
- [M7](M7-analyze-documentation-patterns.md) - Analyze patterns from doc samples (Low effort)

### Sprint 2 (Documentation)
- [C2](C2-beginner-tutorials.md) - Beginner tutorials (Reduces adoption barrier)
- [H2](H2-document-async-validation-patterns.md) - Async validation docs
- [M3](M3-document-propertychanged-events.md) - Event documentation

### Sprint 3 (Code Quality)
- [M1](M1-extract-metaproperty-helper.md) - Extract helper method
- [L1](L1-naming-consistency.md) - Naming consistency
- [L2](L2-duplicate-event-subscriptions.md) - Event subscription guards
- ~~[L3](completed/L3-fix-waitfortasks-assert.md) - WaitForTasks cleanup~~ **DONE**

### Sprint 4+ (Features)
- [C1](C1-domain-events-support.md) - Domain Events (Major feature)
- [M2](M2-value-object-base.md) - Value Object support
- [M5](M5-aggregate-root-marker.md) - IAggregateRoot marker

---

## Related Documents

- [Comprehensive Framework Review](completed/comprehensive-framework-review.md) - Full analysis (completed)
- [RemoteFactory Mapper Removal Plan](remotefactory-mapper-removal-plan.md) - Migration plan

---

*Generated from comprehensive framework review - December 31, 2024*
