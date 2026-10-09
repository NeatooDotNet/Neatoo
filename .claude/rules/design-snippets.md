---
paths:
  - "src/Design/**/*"
---

- `src/Design/` (Design.Domain and Design.Tests) is the single source of all code snippets for MarkdownSnippets in `docs/`, `skills/` and `README.md`
- Snippets are `#region name` ... `#endregion` blocks; keep a region tight around the code it shows
- Region names must be globally unique; skill regions are named `skill-*`, doc-only regions `docs-*`
- Code must follow the doctrine (interface-first, `[Remote]` only on client entry points, no `LoadValue` in factory operations, rules over exceptions) and must compile; every snippet's behaviour is pinned by a test
- After any code change: `dotnet build src/Design/Design.sln` then `dotnet test src/Design/Design.sln` then `dotnet mdsnippets` then verify no duplicate or missing snippets
- Never hand-write C# in a markdown file; a short block labelled WRONG that shows a forbidden pattern is the only exception
