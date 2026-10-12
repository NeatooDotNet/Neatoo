---
paths:
  - "src/Design/**/*"
---

- `src/Design/` (Design.Domain, Design.Infrastructure, Design.App, Design.Server and Design.Tests) is the single source of all code snippets for MarkdownSnippets in `docs/`, `skills/` and `README.md`
- In C#, snippets are `#region name` ... `#endregion` blocks. In `.razor` markup `#region` is text and renders on the page, so use Razor comments: `@* begin-snippet: name *@` ... `@* end-snippet *@` (mdsnippets parses them). Keep a region tight around the code it shows
- Region names must be globally unique; skill regions are named `skill-*`, doc-only regions `docs-*`
- Code must follow the doctrine (interface-first, `[Remote]` only on client entry points, no `LoadValue` in factory operations, rules over exceptions) and must compile; every snippet's behaviour is pinned by a test. The one exception: a Razor page-binding region is pinned by the build (the repo has no bUnit); the ViewModel or handler the page binds to is pinned by a test in its place
- After any code change: `dotnet build src/Design/Design.sln` then `dotnet test src/Design/Design.sln` then `dotnet mdsnippets` then verify no duplicate or missing snippets
- Never hand-write C# in a markdown file; a short block labelled WRONG that shows a forbidden pattern is the only exception
