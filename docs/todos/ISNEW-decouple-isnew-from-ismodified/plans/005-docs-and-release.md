# Docs, Skill, README, Release Notes, 0.29.0

**Plan #:** 005
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md)
**Status:** Done (doc-only; verified by mdsnippets regeneration + full-suite runs)
**Last Updated:** 2026-08-21
**Plan-review opt-in:** TBD at draft
**Code-review opt-in:** TBD at draft (doc-only — expect No)

---

## Scope

_Stub — Scope only; flesh out at Step 2._

Carry The Why (design.md canonical block) and the new semantics to every documentation
surface per design.md's Doc/Skill Touchpoints: the neatoo skill (Key Properties table at
`SKILL.md:100` and the quick-check line at `:241` — both verified stale 2026-08-21; the Why;
and the COMMON MISTAKE about `MarkModified()` not being needed for savability — copied to
`~/.claude/skills/neatoo/`), **repo-root `CLAUDE.md`** (its State Properties section defines
the pre-flip `IsSavable`; it loads into every agent session here, so staleness mis-teaches
future work), `docs/guides/change-tracking.md` (full treatment) and the other
guides, `docs/reference/api.md` + `src/samples/` snippets, Design.Domain semantics
commentary, a **brief** README mention linking to the change-tracking guide, release notes
`docs/release-notes/v0.29.0.md` with the migration guide, and the version bump to 0.29.0 in
`Directory.Build.props`. MarkdownSnippets rerun per repo rules.

---

## Outcome (2026-08-21)

- **`docs/guides/change-tracking.md`** — removed `IsNew` from the documented `IsModified`
  terms; added a **"Why IsNew is not part of IsModified"** section (the two-questions table,
  the guard payoff, the `MarkModified()` opt-in, and the COMMON MISTAKE); corrected the
  `IsSavable` definition to `(IsModified || IsNew) && …`; rewrote the IsNew paragraph and the
  Architecture Note, which had claimed `IsNew` and `IsDeleted` behave alike — they
  deliberately do not (deleting is user work; creating is not).
- **`skills/neatoo/SKILL.md`** — Key Properties table rows for `IsModified`/`IsSavable`/`IsNew`
  corrected (the verified-stale `:100` and `:241`), plus a new **IsNew vs IsModified** section
  with the COMMON MISTAKE and the "IsNew never aggregates" rule.
  `references/collections.md` — attach-marking explained, including why it is load-bearing for
  new items and why paused adds are exempt. Copied to `~/.claude/skills/neatoo/` per repo rule.
- **Repo-root `CLAUDE.md`** — State Properties corrected and an IsNew-vs-IsModified paragraph
  added. This file loads into every agent session here, so staleness would actively mis-teach
  future work (added to the touchpoint list by the ISNEW-004 plan review).
- **`README.md`** — one-sentence mention on the change-tracking bullet, linking to the guide
  section (brief, per Keith's instruction).
- **`docs/reference/api.md` + `src/samples/`** — sample assertions updated; `dotnet mdsnippets`
  re-run and the regenerated blocks verified (the "New entity is considered modified" lines
  are gone). `ChangeTrackingSamples.IsNew_IndicatesUnpersistedEntity` was rewritten to teach
  the new rule rather than merely flip a boolean.
- **`docs/release-notes/v0.29.0.md`** — summary, the why, core semantics, opt-in create,
  attach-marking, the five bundled defect fixes, the Design.Domain documentation correction,
  and a five-point migration guide (starting with the silent-skip risk on
  `if (IsModified) Save()`).
- **`Directory.Build.props`** — `<Version>` 0.28.1 → 0.29.0.

Publishing (tag, push, NuGet) is deliberately out of scope per the todo and the repo's CI
standards — it is user-initiated.
