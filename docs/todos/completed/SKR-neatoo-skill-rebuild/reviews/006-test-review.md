## Test Review — SKR-006 — [2026-10-12] — Round 1

**Verdict: CLEAN**
**Logs:** build PASSED, 0 warnings / 0 errors — `reviews/006-build.log`; tests 205 passed, 0 failed, 0 skipped (Design.Tests, net9.0) — `reviews/006-test.log`; mdsnippets ran clean (no missing, duplicate or warning lines; `TreatMissingAsWarning: True` produced none) — `reviews/006-mdsnippets.log`

Every Acceptance bullet carries `Should`. Every Test Evidence row checks out:

- **Employee round-trip** (`EmployeeEditViewModelTests.cs:60`). This is integration through `DesignClientServer.Scopes()`. It asserts all eight fields the page binds after a real save and fetch (1 call, then 2), plus `Grades` A–D in order, `IsModified` false, and the return to view mode. It fails if `Notes` is dropped from `MapTo`, from `Fetch` or from `EmployeeRow`, because `fetched.Notes` comes back null. It also fails if `PayGrade.All` changes.
- **Cancel restores** (`:88`). It edits FirstName and Notes, then `CancelEdit`. It asserts the original values, `IsModified` false, view mode, and `_link.Calls == 2`. A `CancelEdit` that only flipped `IsEditing` would fail on FirstName ("Augusta") and on the call count.
- **RemoveItem** (`OrderEditViewModelTests.cs:114`). Integration: a fetched two-item order, not savable before. `RemoveItem` leaves Count 1 and `DeletedCount` 1, and `CanSave` is true. After the save the server store holds one row. A no-op `RemoveItem` would fail all three assertions.
- **Explicit-skip, fenced-block sweep.** I ran it myself with awk over fences outside `<!-- snippet -->` / `<!-- endSnippet -->` pairs. It finds 11 blocks, each opening with a WRONG label, at exactly the cited lines (`SKILL.md:60`; `aggregate-reactive-vm.md:9`; `anti-patterns.md:11,85,155,202,243,319,334,358,421`). Every placeholder is filled, and `Could not find snippet` appears nowhere. One correction: there are **25** placeholders (SKILL.md 13, aggregate-reactive-vm 3, anti-patterns 8, property-change-events 1), not the 22 the cell says. The claim the bullet depends on still holds.
- **Explicit-skip, LazyLoad / value object / subscription.** "lazy" appears only at `SKILL.md:460`, the removal sentence. The frontmatter has no LazyLoad trigger. "value object" appears only at `SKILL.md:159`, the `PayGrade` record. `aggregate-reactive-vm.md:137` is `WorkOrderViewModel`, Key Points 2–3 are at lines 213–214, and `SKILL.md:454` is Page Structure Key Point 3. All put the subscription on the page, and `property-change-events.md:54` renders `skill-mud-page-code`.
- **Razor regions.** EmployeeEdit, OrderEdit's child collection and `_Imports` are pinned by the build, per `.claude/rules/design-snippets.md:9`. The ViewModels behind them are pinned by the tests above.

**Rendering checks:**

- `begin-snippet` / `end-snippet` appear nowhere in `skills/`, `README.md` or `docs/` (outside todos and plans). The nested `skill-mud-page` renders at `SKILL.md:224` with its inner markers stripped.
- The regions re-rendered elsewhere read correctly:
  - `skill-save-handler` in `skills/neatoo/SKILL.md` and `references/blazor.md` gains `RemoveItem`.
  - `skill-client-composition` gains `EmployeeEditViewModel`.
  - `skill-value-object-record` in `base-classes.md` gains `All`.
  - `skill-input-model-manual-binding` in `blazor.md` shows the `SetValue` commit.
  - `skill-plain-computed-getter` in `docs/guides/properties.md` and `references/properties.md` has only its line range shifted.

### Veto-tier
None. `git diff 6afac34..HEAD -- src/Design/Design.Tests` only adds `EmployeeEditViewModelTests.cs` and the one new `[TestMethod]` in `OrderEditViewModelTests`, after the `#endregion` of `skill-save-handler-test`. No existing line was changed or removed.

### Must-cover (plan-related)
None. The plan has no Must bullets.

### Should-cover (Should / Could bullets; plan-introduced reachable paths)
- `CancelEdit` on a new employee does nothing (early return), and the screen stays in edit mode. No test covers this path. It is in `src/Design/Design.App/ViewModels/EmployeeEditViewModel.cs:CancelEdit`. `Affects: AC-6 (Should)`. `Reachable by:` the Cancel button on `/employees/new` (`EmployeeEdit.razor:95` → `CancelAsync`), because `New()` starts in edit mode. Suggested tier: integration, a one-line addition to `EmployeeEditViewModelTests` (`New`, `CancelEdit`, then assert the same instance, `IsEditing` still true, `_link.Calls == 0`).

### Tech-debt (untiered, ≤ 5, one line each)
- `ValidateBaseTests.PropertyObject_CarriesWhatTheInputModelPageBinds` drives `model.Name = …`, but `InputModelPage` now commits through `name.SetValue(v)`. No test drives the property object's `SetValue` the way the page does. The Razor region itself is build-pinned under the rule.

### Theoretical (not triaged)
None.

### Read report
- Read beyond the brief: `ValidateBaseTests.cs:70–90` (to see whether the `SetValue` change to the input-model region had a test behind it); `.claude/rules/design-snippets.md:9` (the exact wording of the build-pinned exception).
- Named but unused: none.
