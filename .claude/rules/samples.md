---
paths:
  - "src/samples/**/*"
---

- `src/samples/` is the single source of all code snippets for MarkdownSnippets
- All `#region snippet-name` markers in this directory are extracted by MarkdownSnippets into markdown files
- Snippet names must be globally unique -- no two `#region` markers across the repo may share a name
- Code must compile and all tests must pass
- After any code change: `dotnet build src/samples/` then `dotnet test src/samples/` then `dotnet mdsnippets` then verify no duplicate or missing snippets
- No other directory in the repo should contain `#region` markers intended for MarkdownSnippets
