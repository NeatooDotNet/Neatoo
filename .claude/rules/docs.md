---
paths:
  - "docs/**/*.md"
---

- `docs/` contains user-facing documentation read by developers evaluating and using Neatoo
- Use `<!-- snippet: name -->` / `<!-- endSnippet -->` placeholders for all code samples -- MarkdownSnippets fills them from `#region` blocks in `src/Design/` (Design.Domain and Design.Tests)
- Do not write inline C# code blocks for framework features; use snippet placeholders instead
- Target audience: expert .NET/C# developers familiar with DDD
- Use DDD terminology freely without explaining it
- After adding or changing snippet placeholders: run `dotnet mdsnippets` then verify the placeholder was filled
- Excluded from MarkdownSnippets processing: `docs/todos/`, `docs/plans/`, `docs/release-notes/`
