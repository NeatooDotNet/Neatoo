---
name: RemoteFactory
description: |
  This skill should be used when the user mentions "RemoteFactory", "Neatoo.RemoteFactory", "[Factory] attribute", "[Remote] attribute", "[Execute] attribute", "[Event] attribute", "IFactorySaveMeta", "[AspAuthorize]", "[AuthorizeFactory]", "Save routing", "fire-and-forget events", "client-server factory", or asks about factory patterns for 3-tier .NET applications. Provides guidance for building enterprise line-of-business applications using RemoteFactory's source-generated factory patterns.
version: 1.0.0
---

# RemoteFactory Development Guide

RemoteFactory is a Roslyn Source Generator-powered Data Mapper Factory for 3-tier .NET applications. It eliminates the need for DTOs, manual factories, and API controllers by generating everything at compile time.

## What RemoteFactory Does

- **Generates factory interfaces and implementations** from attributed classes
- **Handles serialization automatically** - objects cross client/server boundary without DTOs
- **Generates ASP.NET Core endpoints** - no manual controller code
- **Supports three factory patterns** for different use cases

## The Three Factory Patterns

| Pattern | Use When | Example | Reference |
|---------|----------|---------|-----------|
| **Class Factory** | Aggregate roots with lifecycle | `Order`, `Customer` | `references/class-factory.md` |
| **Interface Factory** | Remote services without entity identity | `IOrderRepository` | `references/interface-factory.md` |
| **Static Factory** | Stateless commands and events | `EmailCommands` | `references/static-factory.md` |

## Quick Decisions Reference

| Question | Answer |
|----------|--------|
| Should this method be [Remote]? | Only aggregate root entry points |
| Can I use private setters? | No - breaks serialization |
| Should interface methods have attributes? | No - interface IS the boundary |
| Do I need `partial` keyword? | Yes, always |
| Should child entities have [Remote]? | No - causes N+1 remote calls |
| Can [Execute] return void? | No, must return Task<T> |
| Do [Event] methods need CancellationToken? | Yes, as final parameter |
| Where does business logic go? | In the entity, not the factory |

## Reference Files

Consult these files for detailed patterns and examples:

### Core Patterns
- **`references/class-factory.md`** - Aggregate roots, IFactorySaveMeta, lifecycle hooks
- **`references/interface-factory.md`** - Remote service proxies
- **`references/static-factory.md`** - Execute commands and Event handlers

### Implementation Details
- **`references/service-injection.md`** - Constructor vs method injection, child entities
- **`references/setup.md`** - Server and client configuration
- **`references/anti-patterns.md`** - Common mistakes to avoid

### Advanced Topics
- **`references/advanced-patterns.md`** - Authorization, correlation context, complex aggregates, testing

---

## Code Sample Sources

This skill contains two types of code examples:

### Compiled Code (via MarkdownSnippets)
Code blocks marked with `<!-- snippet: name -->` are extracted from the reference application and are guaranteed to compile and work. These include:
- Complete class/interface examples
- Full working implementations
- Configuration samples

### Hand-Written Code
The following code blocks are intentionally hand-written and NOT extracted from compiled source:

1. **Anti-pattern examples** (`references/anti-patterns.md`)
   - All code in this file shows intentionally wrong patterns
   - Marked with `// WRONG` comments
   - Cannot come from compiled source because they demonstrate errors

2. **Partial/illustrative snippets** (various files)
   - Short excerpts showing just properties or method signatures
   - Usage examples that aren't full compilable units
   - Inline comparisons (WRONG vs RIGHT patterns)

When updating this skill, edit anti-patterns and partial examples directly in the markdown files. For compiled examples, update the reference application code and run `mdsnippets` to re-extract.
