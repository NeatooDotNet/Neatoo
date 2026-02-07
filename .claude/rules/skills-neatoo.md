---
paths:
  - "skills/neatoo/**/*"
---

- `skills/neatoo/` contains Claude-facing framework knowledge (not user-facing)
- Code samples come from MarkdownSnippets -- use `<!-- snippet: name -->` placeholders, same as docs
- After `dotnet mdsnippets` runs, skill files are self-contained with actual code embedded
- No "see link" file references to repository source files -- the code must be in the rendered markdown
- Skills describe how Neatoo works for Claude's understanding when generating code
