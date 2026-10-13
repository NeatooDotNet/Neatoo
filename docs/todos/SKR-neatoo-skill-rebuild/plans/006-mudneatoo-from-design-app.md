# mudneatoo blocks sourced from Design.App

**Plan #:** 006
**Date:** 2026-10-09
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-6 (and AC-2's rule for every skill block)
**Status:** Done
**Last Updated:** 2026-10-12 (drafted from the stub after SKR-005 merged)
**Plan-review opt-in:** No — the stub authorized the Design.App pages; no ruling or API changes. The decisions are listed under Notes
**Code-review opt-in:** No — the new page and ViewModel follow the two that SKR-002 reviewed; the test-reviewer's map check is the gate
**Branch:** skr-006-mudneatoo-from-design-app — cut from the arc at Step 2
**PR:** #107
---

## Scope

Replace every non-WRONG hand-written block in `skills/mudneatoo/SKILL.md` and `references/` (36 of 49; the skill has no snippet today) with a compiled region from Design.App or Design.Domain, so every block names types that exist and compile:

- **One Employee edit page and ViewModel in Design.App** carry the component patterns Design.App has no page for: text, numeric with adornment, select over the `PayGrade` record, date picker, checkbox, a multi-line field with `UserAttributes`, a field disabled by a domain fact, the validation summary, view/edit mode, and the page structure around them. Tight regions nest inside the page region (mdsnippets strips inner markers from the outer block — verified 2026-10-12).
- **Employee gains `Notes`** (the multi-line field needs a free-text property); `PayGrade` gains the set of grades a select offers.
- **OrderEdit gains a remove-item gesture** for the child-collection block; its subscription code becomes the custom-container region.
- The manual-binding block reuses `skill-input-model-manual-binding`, changed to commit through `SetValue` and label from `DisplayName` as mudneatoo teaches.
- The two excerpts of MudNeatoo's internals are dropped; the prose states the expression inline.
- **LazyLoad comes out** (D24): the EntityLazyLoad databinding section, Anti-Pattern 9 and the frontmatter's LazyLoad triggers, with one sentence saying it is being removed.
- Prose changes only where a replaced block changes what it showed — chiefly: the page owns the subscription and the ViewModel is a plain class (Design.App's shape), and "a value object" on `ValidateBase` becomes "an input model".

Out: mudneatoo's structure; MudNeatoo component fixes (#93, #95); the audit README count (its own punchlist row, worked here only if it is a one-line edit).

---

## Intent

- A reader copying any mudneatoo block gets markup and code that compiles against Design.Domain and runs in Design.App.
- The mudneatoo skill and the neatoo skill show the same page shape — the page subscribes, the ViewModel holds the aggregate and adapts gestures, the save handler reassigns.

---

## Framework & Architectural Alignment

- `SKILL.md` (neatoo) model statement 4: the UI binds and adapts gestures; the ViewModel decides nothing. A field disabled by a domain fact binds the fact, never a composition.
- D16 (facts, not screen names), D17/D25 (record vs input model), D24 (no LazyLoad teaching).
- `.claude/rules/design-snippets.md`: Razor regions by `@* begin-snippet *@`; a page-binding region is pinned by the build, the ViewModel it binds by a test.

---

## Constraints & Invariants

- WRONG blocks stay hand-written and labelled; no new WRONG block.
- Existing regions keep their names; `skill-page-binds-facts` and `skill-page-order-edit` keep rendering the same pages (inner markers are invisible in them).
- Sacred tests unchanged; new tests only.
- `dotnet mdsnippets` clean across `skills/`, `docs/`, `README.md`.

---

## Steps

1. **Design.Domain.** `Employee.Notes` (property, interface, row, Fetch, MapTo); `PayGrade.All`.
2. **Design.App.** `EmployeeEditViewModel` (New, Load, CanSave, Save; `IsEditing`, `StartEdit`, `CancelEdit` — cancel re-fetches a saved employee) and `EmployeeEdit.razor` (`/employees/new`, `/employees/{id}`) with the nested regions; `OrderEditViewModel.RemoveItem` and the remove button inside a child-collection region in `OrderEdit.razor`; a region around OrderEdit's subscription code; a region around the MudNeatoo `@using` lines in `_Imports.razor`; `InputModelPage` commits through `SetValue`. Home links the page.
3. **Tests** (through the client/server pair): an employee filled through the ViewModel saves and fetches back with every field the page binds, including `Notes` and the grade; `CancelEdit` restores the saved values; `RemoveItem` on a fetched order puts the item in `DeletedList` and the order becomes savable.
4. **Skill.** Swap each non-WRONG block for its region or a pointer; drop the LazyLoad section and Anti-Pattern 9; drop the two internals excerpts; frontmatter without LazyLoad; version 2.0.0; prose corrected where a block changed.
5. `dotnet mdsnippets`; sweep; Test Evidence; gate.

---

## Acceptance

- [x] Every fenced block in `skills/mudneatoo/` is a filled snippet or labelled WRONG `[explicit-skip: awk sweep]` · Should
- [x] An employee edited through the page's ViewModel saves and fetches back with every bound field, `Notes` and the grade included `[integration]` · Should
- [x] Cancelling an edit restores the saved employee `[integration]` · Should
- [x] Removing an item through the ViewModel moves it to `DeletedList` and makes the order savable `[integration]` · Should
- [x] No LazyLoad teaching in `skills/mudneatoo/`; no "value object" on `ValidateBase`; the subscription is shown on the page, not the ViewModel `[explicit-skip: grep + read]` · Should

---

## Current State (Pre-Flight)

Walked 2026-10-12 on `skr-arc` at `6afac34`.

- `skills/mudneatoo/`: `SKILL.md` 469 lines, `references/` 702 lines in three files; 49 fenced blocks, 0 snippets. 13 WRONG (`SKILL.md:51`; `aggregate-reactive-vm.md:9`; the anti-pattern Wrong blocks in `anti-patterns.md`). LazyLoad: `SKILL.md:3,266–325`, `anti-patterns.md:452–495`. Internals excerpts: `SKILL.md:404`, `property-change-events.md:41`. "Value object": `SKILL.md:33`.
- `aggregate-reactive-vm.md` shows a `PlanViewModel : ObservableObject` that owns the subscription; Design.App's ViewModels are plain classes and the pages subscribe (`OrderEdit.razor`, `WorkOrderPage.razor` `Resubscribe`).
- Design.App has OrderEdit (text, numeric, date, summary, items, save) and WorkOrderPage (numeric, switch, summary, facts, verb button); no select, checkbox, multi-line, disabled-by-fact or view/edit example. `Employee` has every field type those need except free text; no enum exists in Design.Domain, so the select binds the `PayGrade` record.
- mdsnippets 27.0.2 renders nested Razor snippets with the inner markers stripped (scratch test, 2026-10-12).

---

## Punchlist

- [x] mudneatoo block count in the audit README (49, not 14) · `docs/todos/consistency-audit/README.md` · done when the line reads 49 · AC-6 · Should (pulled from the todo)

---

## Test Evidence

| Acceptance bullet (short) | Priority | Tier declared | Evidence | Confirmed |
|---|---|---|---|---|
| Every mudneatoo block a snippet or WRONG | Should | `[explicit-skip]` | awk sweep (fenced blocks outside `<!-- snippet -->` pairs) over `skills/mudneatoo/`: 11 blocks, every one opening with a WRONG label (`SKILL.md:60`; `aggregate-reactive-vm.md:9`; `anti-patterns.md:11,85,155,202,243,319,334,358,421` — the eight Anti-Pattern Wrong blocks gained the label line). 25 placeholders, all filled; `reviews/006-mdsnippets.log` exit 0 with no missing/duplicate line beyond the config echo. Nested regions render with inner markers stripped (grep for `begin-snippet` in `skills/`, `docs/` outside todos/plans: none). | skip confirmed |
| Employee round-trips every bound field | Should | `[integration]` | `Design.Tests/ViewModelTests/EmployeeEditViewModelTests.cs:60` `EditedThroughTheViewModel_SavesAndFetchesBackEveryBoundField` — through `DesignClientServer.Scopes()`: the grade options are A–D in order; FirstName, LastName, Email, HireDate, Salary, IsActive, Grade (`PayGrade("C")`) and Notes set on the client, saved (1 call), fetched (2 calls), each asserted equal; not modified after fetch; the screen returns to view mode after save. | integration |
| Cancel restores the saved employee | Should | `[integration]` | `EmployeeEditViewModelTests.cs:88` `CancelEdit_RestoresTheSavedEmployee` — after a save, edits FirstName and Notes (`IsModified` true), `CancelEdit` re-fetches through the pair (2 calls total): same id, original values, not modified, view mode. `CancelEdit_NewEmployee_DiscardsTheEditForAFreshForm` (added at the gate): a new employee's cancel gives a fresh new form, still editing, no call. | integration |
| Remove item → DeletedList, savable | Should | `[integration]` | `ViewModelTests/OrderEditViewModelTests.cs:114` `RemoveItem_FetchedOrder_MovesTheItemToDeletedListAndTheOrderIsSavable` — a saved two-item order fetched back (not savable), `RemoveItem` leaves one item and `DeletedCount == 1`, `CanSave` true; the save leaves one row in the server store. Outside the `skill-save-handler-test` region. | integration |
| No LazyLoad / value-object / VM-subscription contradictions | Should | `[explicit-skip]` | grep `skills/mudneatoo/` for "lazy": one line, `SKILL.md:460` (being removed, build nothing on it); frontmatter has no LazyLoad trigger; the EntityLazyLoad section and Anti-Pattern 9 are gone. "value object": only `SKILL.md:159`, the `record`. `aggregate-reactive-vm.md` shows Design.App's `WorkOrderViewModel` (plain class) and its Key Points 2–3 put the subscription on the page; `SKILL.md` Page Structure Key Point 3 and `property-change-events.md` say the same and render `skill-mud-page-code`. | skip confirmed |

---

## Gate Record

- **Test review, round 1 (2026-10-12): CLEAN** — `reviews/006-test-review.md`. No veto, no Must-cover. Sacred tests: one new file and one method outside the region. The reviewer confirmed each integration test fails on the breakage it names. **Should-cover** (Cancel on `/employees/new` returned early, untested — reachable, since `New()` starts in edit mode): `CancelEdit` on a new employee now gives a fresh form, pinned by `CancelEdit_NewEmployee_DiscardsTheEditForAFreshForm`; mudneatoo's view/edit prose says so. Tech-debt: `ValidateBaseTests.PropertyObject_CarriesWhatTheInputModelPageBinds` (added in SKR-005) now commits through `SetValue` as the page does — same assertions. Evidence count corrected (25 placeholders). Rebuilt: 0/0, 206 passing, mdsnippets clean.
- Code review: not opted in.

---

## Plan Amendments

### 2026-10-12 — Drafted from the stub

- **Section affected:** Scope, Steps, Acceptance
- **Original said:** replace the 37 non-WRONG blocks with Design.App/Design.Domain regions, adding pages where needed; 12 WRONG stay; internals become MudNeatoo regions or are dropped.
- **What changed:** the count is 36 non-WRONG / 13 WRONG on recount; the LazyLoad blocks are dropped (D24) rather than sourced; the internals are dropped (mdsnippets is not configured over `Neatoo.Blazor.MudNeatoo`, and the prose carries the one expression); one Employee page carries all missing component patterns; the ViewModel-owns-subscription prose follows Design.App.
- **Why:** pre-flight recon.

### 2026-10-12 — Implementation amendments

- **Section affected:** Steps 2, 4
- **Original said:** a region around OrderEdit's subscription code; the view/edit block as its own region; the select typed `PayGrade?`.
- **What changed:** the subscription region is the Employee page's `@code` (`skill-mud-page-code`), the same pattern, so OrderEdit gets none; the view/edit block is described against the page region rather than rendered twice; a region wraps `EmployeeEditViewModel` (`skill-mud-viewmodel`) so the page structure shows what it injects; the select is `T="PayGrade"` because Razor-generated code cannot carry a nullable annotation (CS8669 under TreatWarningsAsErrors), and `SKILL.md` says so; the eight Anti-Pattern Wrong blocks gained a WRONG label line; `MudForm` is no longer recommended, since Design.App's pages need no form wrapper.
- **Why:** build and render results; one complete page is shown once.

---

## Notes

Decisions made at drafting, for the user to overrule:

1. One Employee page carries every missing component pattern, with nested regions, rather than a page per component.
2. The "field disabled by a domain fact" example binds `Disabled="@(!Vm.Employee.IsActive)"` on the grade select: a stored business fact, bound as is. A derived fact would need a rule recomputed inside Create and Fetch (D18), and Employee's Create is synchronous with many callers.
3. The select binds the `PayGrade` record (Design.Domain has no enum), which also shows a record bound through MudNeatoo.
4. mudneatoo's "ViewModel owns the subscription" guidance is replaced by Design.App's shape (the page subscribes; the ViewModel is a plain class), so the two skills agree.
