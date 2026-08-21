# Plan Review Record — ISNEW-004 — 2026-08-21

**Reviewer:** plan-reviewer (deep budget, pre-implementation). **Verdict: CONCERNS** — 2
veto-tier, both Pass B. Direction judged correct and faithful to design.md.

## Veto-tier findings and dispositions

**B1 — The plan (and design.md itself) were factually wrong about the child-property
channel.** Both claimed that assigning a Neatoo child to a parent property "never dirties the
parent at all — a pre-existing quirk this fixes". False for **new** children, which is the
common case: the property never self-dirties, but the parent is dirtied *through the child*,
because `EntityProperty.IsModified => IsSelfModified || EntityChild?.IsModified` and a new
child's `IsModified` was true **only via the weld**. So this channel was mandatory — the same
silent-data-loss shape as the list channel — not a bonus. The error made the work look safer
than it was, and would have shipped verbatim in the 0.31.0 release notes.
**Fixed:** design.md corrected at both the attach-marking section and the migration bullet;
the plan's Current State rewritten; the channel promoted into Constraints; and a
characterization test (`ChildPropertyAttachTests`) written **before** the library edit as a
parity anchor, since red on that path would mean regression rather than expected new state.

**B2 — Step 4 silently decided a persistence question design.md left open.** "A swap behaves
like a remove plus an attach" means the displaced item starts going to `DeletedList` and
being deleted on the next save — a new observable save-side behavior, inside a plan whose
Constraints open with "factory save routing is untouched", with no Acceptance bullet and no
test anywhere exercising list-item replacement.
**Fixed:** `SetItem` was narrowed out of ISNEW-004 entirely and carved to **ISNEW-009**, with
its real defects recorded (the displaced item is silently orphaned; the incoming item gets no
identity; none of `Add`'s guards run). *Note:* the later code review found that removing
`SetItem` entirely went one step too far — replacement *did* dirty the graph for new items via
the weld — so the incoming-item mark came back into ISNEW-004 as a regression fix, and
ISNEW-009 retains everything save-side. See `004-code-review.md` V2.

## Callout-tier findings and dispositions

- **Repo-root `CLAUDE.md` documented the pre-flip `IsSavable` and was on no touchpoint list.**
  It loads into every agent session in this repo, so staleness would actively mis-teach future
  work. **Added to ISNEW-005**, and corrected there.
- **design.md cited a SKILL.md string that does not exist** ("True after Create (because
  IsNew)"). The genuinely stale lines are `SKILL.md:100` and `:241`; both verified and
  corrected in ISNEW-005.
- **Acceptance dropped "add-then-remove returns parent to clean"**, which design.md names —
  the behavior that proves attach-marking is reversible rather than sticky. **Added**, and it
  promptly exposed a real pre-existing bug (see the ISNEW-004 `RemoteItem` Discovery Log entry).
- **Lazy insulation is incidental, not structural** — `LazyLoadEntityProperty` *calls*
  `base.OnPropertyChanged` and undoes `IsSelfModified`, which would not undo a mark on the
  child; the real protections are that the generated lazy setter uses `LoadValue` (no `Value`
  notification) and that `EntityLazyLoad` does not implement `IEntityBaseInternal`. **Recorded
  as a placement constraint** in both design.md and the code comment.
- **`EntityChild` is `IEntityMetaProperties`, which lists also implement** — so list-valued
  properties need deliberate handling. **Recorded**; the code review later showed the chosen
  handling was wrong and it was fixed (V1).
- **`MapperGenerator.cs:47` consumes property-level `IsModified`** — unchanged for scalars;
  recorded in Current State.
- **Blast radius exceeds design.md's named list** —
  `Design.Tests DeletedListTests.IsModified_TrueWhenNewItemRemoved` is a weld pin whose intent
  the flip deletes. **Acceptance bullet reworded** away from "the tests named in design.md",
  and that test was rewritten.
- **Ordering/re-entrancy is bounded**, with one newly reachable case (an item that already has
  a `Parent` now fires an upward notification for new items too). Recorded.
- **Verified clean and worth recording:** every reference cascade already guards with
  `IsNew || IsModified` or saves unconditionally, so no sample or example silently skips a
  clean new child's insert after the flip.
