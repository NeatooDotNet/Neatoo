## Test Review — SKR-005 — 2026-10-12 — Round 1

**Verdict: CLEAN**
**Logs:** build PASSED, 0 warnings / 0 errors (`reviews/005-build.log`); tests 202 passed, 0 failed, Design.Tests net9.0 (`reviews/005-test.log`); mdsnippets: 169 snippets, no missing or duplicate line (`reviews/005-mdsnippets.log`; `TreatMissingAsWarning: True`, so I grepped for "missing" rather than relying on the exit code, and the only hit is the config echo)

### Veto-tier
None.
- Sacred tests: `git diff 987251f..HEAD -- src/Design/Design.Tests` is one added method (`ValidateBaseTests.PropertyObject_CarriesWhatTheInputModelPageBinds`) and nothing else. No assertion was removed or changed.
- No failing tests.

### Must-cover (plan-related)
None. Each row was checked independently:
- **Fenced blocks.** I re-ran an awk sweep over `references/*.md` and `SKILL.md` for fenced blocks outside `<!-- snippet -->`/`<!-- endSnippet -->` pairs. It found exactly the 17 blocks the cell lists, at the cited lines. 11 are WRONG-labelled on their first line, and I read each one: none has a RIGHT half inside. 5 are non-C# (csproj/XML, two diagrams). The last is `lazy-loading.md:251`, which is exempt. The three `@code` anti-patterns (`domain-logic-placement.md:274,475,604`) now open with WRONG. Every RIGHT-block pointer goes to `blazor.md#a-viewmodel-over-a-live-aggregate`, which exists, and `WorkOrderPage.razor` binds `task.Cost`, `TotalCost`, `HasUnschedulableTasks`, `Vm.ShowHoldBanner` and `CanApprove`/`Approve` as claimed.
- **Move list.** I walked 004's Notes table against `git show 3877443:skills/neatoo/SKILL.md`:
  - Each section lands at the cited line: `domain-logic-placement.md:9,706,838,844,855`, `entities.md:166–171,234,249,269`, `base-classes.md:5,18,101–209`, `pitfalls.md:93–94`, `testing.md:3`, `collections.md`, `validation.md`, `rules-lifecycle.md`.
  - Each moved region (the three `skill-parent-in-*` regions, `skill-parent-in-child-method`, `skill-child-property-trigger`, `skill-parent-orchestrator`, `skill-aggregate-interfaces`, `skill-root-insert`, `skill-create-is-new-not-modified`) renders in exactly one reference.
  - The rewrites hold. "Edit, Then Save" has two phases and no Validate phase. The Shapes table reads `record` = value object and `ValidateBase` = input model. The old ladder became the Which Rule Shape table plus the two smell tables, with no rung numbers.
  - The Quick Start and partial-properties substitutions match the Implementation Amendment: `skill-entity-crud` renders in `base-classes.md`, and `skill-partial-property-class` renders at `properties.md:5`.
- **Contradiction grep.** I re-ran it over `references/*.md` (excluding `lazy-loading.md`) and `SKILL.md`:
  - "value object" appears only on the `record` (`base-classes.md:14,32,616,618,624`) and in `SKILL.md`'s record rows.
  - "rung" and "ladder" do not appear.
  - `Show[A-Z]` appears only as the ViewModel's `ShowHoldBanner` (`blazor.md:407,449`, `SKILL.md:209`) and in the "never" lines (`domain-logic-placement.md:68,95`).
  - No `D\d` ruling IDs appear.
  - The `pitfalls.md` LazyLoad rows are gone.
- **Input-model binding `[unit]`.** `InputModelPage.razor:13–29` holds region `skill-input-model-manual-binding`, and the build log shows 0/0. The test at `ValidateBaseTests.cs:72` uses a real factory from the DI scope and the same `model[nameof(IDemoInputModel.Name)]` object the page binds. It asserts invalid, "Name is required" in `PropertyMessages` and not busy for `""`, then valid with no messages for `"Ada"`. It matches the `AllBaseClasses.cs:115` rule and is not vacuous. The `[unit]` tier is correct because nothing crosses a tier.
- **Rename.** `#region skill-input-model` is at `AllBaseClasses.cs:93` and `skill-input-model-interface` at `IBaseClassInterfaces.cs:12`. `git grep skill-value-object HEAD` (excluding todos) finds only `skill-value-object-record(-test)`, the record, which is correctly kept. At `987251f` the old name rendered in four places (`base-classes.md` and the three docs), and all four now render `skill-input-model*`. 004's Notes mentions a `skill-value-object-test`, but no region by that name existed at the base commit.

### Should-cover (Should / Could bullets; plan-introduced reachable paths)
None. The `pains.md` bullet (Should) is pinned:
- It covers six defects (#103, #96, #89, #95, #93, RF#117), each with symptom, link and workaround.
- It lists three missing analyzers, including the `Save()` result.
- It notes LazyLoad as being removed.
- Its internal anchors resolve (`blazor.md` Mode 2, Neatoo on Each Tier, The Save Handler; `domain-logic-placement.md` Pattern 6; `properties.md:335`).
- `SKILL.md:303` and `:329` link to it, and the todo's #103 row is ticked.

The plan-introduced page (`/input-model`, linked from `Home.razor`) is covered by the build and the unit test above.

### Tech-debt (untiered, ≤ 5, one line each)
- Bullet 3's Evidence cell lists the LazyLoad mentions outside its file but leaves out `rules-lifecycle.md:19` (the generated `EntityLazyLoad` setter). That line is a description, not an instruction, so the bullet still holds; the cell's list is just incomplete.

### Theoretical (not triaged)
- `InputModelPage`'s `IsBusy` progress bar and its `PropertyChanged` re-render handler are untested. `DemoInputModel` has no async rule, so no input reaches either path today.

### Read report
- Read beyond the brief:
  - Old `SKILL.md` at `3877443`, lines 256–375, 535–600, 798–801, 824–827: needed to check the move-list rewrites.
  - `git grep` at `987251f`: confirmed which old region names existed.
  - The todo's Dismissed section and Discovery Log: checked before raising anything.
- Named but unused: none.
