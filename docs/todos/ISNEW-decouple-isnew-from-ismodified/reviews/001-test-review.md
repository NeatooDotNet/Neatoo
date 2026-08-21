# Test Review Record — ISNEW-001 — 2026-08-21

**Reviewer:** test-reviewer agent (two passes). **Closing verdict: coverage gate PASSES.**

## Closing tier picture

| Source | must-cover | should-cover | nice-to-have |
|---|---|---|---|
| Plan-related | 0 open (2 closed) | 0 open (3 closed) | 0 open (1 closed) |
| Pre-existing tech debt | 0 | 5 queued to ISNEW-006 | 3 queued to ISNEW-006, 1 fixed inline |

## What the first pass found (and what was done)

1. **must-cover — `SaveAggregateDemo` reworked with zero execution.** Closed: new
   `SaveAggregateLifecycleTests.cs` (2 integration tests: insert path; fetch/modify/add/
   remove/save with every route pinned by exact-match assertions).
   `MockSaveAggregateRepository` upgraded to scoped + recording; `_nextChildId` seeded at
   200 so inserted-child Ids can never collide with fetched Ids 101/102 — **load-bearing,
   keep it** (exact-match routing assertions could otherwise pass by coincidence).
2. **must-cover — generated-Id writeback unasserted** (regression would silently route the
   next save to `UpdateItem(0)`). Closed in all four places (Order root/child,
   SaveAggregateDemo root/child).
3. **should-cover** — unmodified-child skip pinned by exact count; `FetchWithChildrenDemo`
   post-fetch child state asserted; post-save count guards added (no assertion loop can run
   empty); second save asserts surviving-item delegation.
4. **nice-to-have** — `!item.IsNew` delete-branch guard marked defensive in both files.
5. **Tech debt queued as ISNEW-006:** Order.Update header-guard positive direction (skip
   direction now pinned free via `UpdatedParentIds.Count == 0`), SaveTests never calling
   `Save()`, cross-aggregate boundary exception + its duplicated-type-name message (possible
   library fix), root delete paths, item-count validation rule. `GetItems`-keyed-by-orderId
   infrastructure note recorded in the ISNEW-002 stub. Stale `DeletedListTests` header fixed.

## Sacred tests

None gutted in either pass. `DeletedListTests.cs` change comment-only; `FetchTests.cs`
strictly additive; all other pre-existing test files byte-identical to HEAD.

## Logs

`001-build.log` / `001-test.log` — full src/Neatoo.sln (0 errors; 42+254+55+1793 passed,
2 pre-existing skips). `001-design-build.log` / `001-design-test.log` — Design.Tests
(not part of Neatoo.sln), final state 110/110 passed. Design logs were overwritten once
after the review-loop additions (re-run noted per gate protocol).

## Explicit accepts

None — no `MISSING` rows, no accepted-open findings. The prose-accuracy bullet
(`[explicit-skip]`) rides on the opted-in code review (001-code-review.md).
