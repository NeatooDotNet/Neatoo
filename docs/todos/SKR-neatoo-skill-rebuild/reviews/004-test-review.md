# Test Review — SKR-004 — SKILL.md: model, habit traps, decisions, UI contract

**Reviewer:** test-reviewer
**Logs:** `004-build.log` (Build succeeded, 0 warnings, 0 errors), `004-test.log` (Design.Tests net9.0: 201 passed, 0 failed, 0 skipped)

---

## Round 1 — 2026-10-12 (HEAD `fea5eb8`, 201 tests)

**Verdict: CONCERNS** (no veto; one Must-cover; nothing at Should-cover)

### Veto-tier

None.

- Logs green.
- Sacred tests intact. `git diff 3877443..HEAD -- src/Design/Design.Tests` was walked line by line, with the counts reconciled. 17 `public void` removed / 18 `public async Task` added: 16 flipped, one method removed, two new tests. 23 `Create()` removed / 22 `await …Create()` added: the 23rd is in the removed method. 6 `Assert.` lines removed: 1 is the lifecycle Arrange, 5 are in the moved test. 18 added: 1 is the lifecycle Arrange, 5 are in the moved test, 4 are in the new ViewModel test and 8 are in `PayGradeTests`. Nothing else changed.
  - (a) `AggregateLifecycleTests.InvalidOrder_DirectFactorySave_ServerRulesRefuseBeforeWriting`: only the Arrange changed (`IsTrue(order.IsValid)` → `IsFalse(order.IsValid, "Create ran the rules…")`), plus a comment clause. Act and Assert are unchanged.
  - (b) `DeletedListTests.AddThenRemoveNewItem_LeavesOrderCleanButSavable` was re-homed as `SaveAggregateLifecycleTests.AddThenRemoveNewItem_LeavesAggregateCleanButSavable`. Its arrange assertion and four assertions are identical, apart from `order`→`demo` and the message wording "order"→"aggregate". Supporting change: `ISaveDemoItemList.DeletedCount`, `SaveDemoItemList.DeletedCount => DeletedList.Count`.
  - (c) The remaining edits are only the `async Task` / `await` changes.

### Must-cover (plan-related)

1. **Bullet 4 (RF rows once, pointer only), Must: the Evidence cell does not hold on two checkable points.**
   - The cell says the block at `SKILL.md:42` names T11-RF. It does not. The T11-RF pointer is at `:37`, inside the Neatoo table row ("The RemoteFactory skill owns 'every object writes itself'"), and the model at `:23` also mentions "every object writing its own row".
   - The cell says each entry has "a parenthetical pointer". Every parenthetical actually restates RemoteFactory's answer. Examples: "(no; Create is new, Fetch is existing)", "(it marks where a call crosses)", "(no; `[Remote, Execute]`)", "(no; the library is on both tiers)". That is what the bullet's "no restated answer" excludes, and what Scope's "names the RF skill's row and nothing more" excludes. For T18–T20 the todo itself leaves the answer to the RF skill, which is being rebuilt (plan 007 drift risk).
   - Reachable by: any reader of `SKILL.md` (and 007's fresh session). Affects: AC-1 (Must).
   - Shape of the fix (`[explicit-skip]`, re-grep):
     - (i) Cut `:42` down to the questions/topics only, add the T11-RF question to it, and correct the Evidence cell; or
     - (ii) The user accepts one-clause answers as the pointer form. Then reword the bullet and the cell to say so, and to say T11-RF is pointed at from row `:37`.

### Should-cover

None.

Bullet-by-bullet:

- **Bullet 1** `[integration]`: honest.
  - `New_EmptyForm_IsNotSavableUntilRequiredFieldsAreFilled` runs through `DesignClientServer.Scopes()` with a ViewModel resolved from the client scope. It asserts `IsValid` false, `CanSave` false, and a `CustomerName` message present. It then sets the field, calls `WaitForTasks`, and asserts `CanSave` true.
  - `Order.Create` (`Order.cs:106-120`) runs `RunRules(RunRulesFlag.All)`, so the test has teeth: before this plan, the first assertion would fail.
  - The lifecycle Arrange agrees.
- **Bullet 2** `[integration]`: honest.
  - `Record_ComparesByValue_AndRejectsAnUnknownCode` checks value equality and inequality, and that `"Z"` throws exactly `ArgumentException`.
  - `Employee_CarriesTheRecord_AcrossTheClientServerPair` checks the `Create` default `Entry`. It then sets `C`, saves and fetches over the pair with `link.Calls == 2`, and checks `fetched.Grade == new PayGrade("C")` and `MaxSalary 90_000m`.
  - A failed constructor binding on the wire would null `Grade` and fail at `MapTo`'s `Grade!.Code`, or at the equality check.
  - `Employee.cs:116` (Create), `:163` (Fetch) and `:303` (MapTo) confirmed. No mocked Neatoo types.
- **Bullet 3** (Neatoo rows): lines 31–40 map to T1, T2, T7, T8, T9, T10, T11, T12, T16, T17 as stated.
  - All five anchors resolve to headings: blazor.md `## The Save Handler`, base-classes.md `## A Job Over Entities` / `## A Value Object Is a Record`, validation.md `## A Rule That Takes a Domain Service`, entities.md `## One Class in Both Roles`.
  - The unanchored links render the named regions: `skill-entity-verb` and `skill-can-x-rule`, `skill-derived-facts`, `skill-rule-command` and `skill-rule-with-command` (domain-logic-placement.md); `skill-validation-attributes-and-rules`; `skill-list-update` and `skill-fetch-starts-clean` (entities.md).
- **Bullet 5** (C# blocks): exactly four fences (`:56`, `:115`, `:133`, `:203`), each the first line after an `<a id=…>` inside a snippet pair. No WRONG block.
  - No mdsnippets log was supplied, so this was checked independently.
  - No "Could not find snippet" text is present in `skills/`, `docs/` or `README.md`.
  - Every `<!-- snippet: -->` in `SKILL.md` and `references/` is filled.
  - No duplicate region name in `src/Design`.
- **Bullet 6** (Should): `## The model` is at `:13`, the table at `:25`, and `wc -l` = 329.
- **Bullet 7**: the Dn/Tn grep is empty. `RunRules` appears at `:31`, `:36`, `:87–93` (Fetch block) and `:299`; `:3` and `:319` are the description and the index. "lazy" (case-insensitive) appears only at `:308` and `:327`.
- **Bullet 8**: the description is 938 characters (952 with the `description: ` key and newline, which is the cell's figure). It leads with nine habit questions and has no LazyLoad words.
- **Bullet 9** (Should): Neatoo#103 is at `:197` and `:303`, RemoteFactory#117 at `:307`, and the three analyzers at `:304–306`.
- **Bullet 10**: every section in the Notes move list has a pointer, either in `:329` or in the index lines `:314–327` (testing, rules-don't-fire and factory methods are carried by the index, not the closing paragraph). `domain-logic-placement.md:3` and `:5` now point at the third and fourth model statements.

### Tech-debt (untiered)

- The `dotnet mdsnippets` run that bullet 5's cell cites left no log in `reviews/`. The claim was checkable only by re-deriving it, so future explicit-skip cells that cite tool output should keep the output beside the build and test logs.

### Theoretical (not triaged)

None.

### Read report

- Read beyond the brief:
  - `Order.cs` `[Create]` region: to confirm bullet 1's test has teeth.
  - The `Employee.cs`, `SavePatterns.cs`, `IFactoryInterfaces.cs`, `OrderEditViewModel.cs` and `OrderEdit.razor` diffs: the plan-introduced paths, and the moved test's supporting member.
  - Reference headings and placeholders: to verify the anchors and the rendered regions.
- Named but unused: `CompositionTests/` (`DesignClientServer`, used through the two tests rather than read). The todo's AC list beyond AC-1 was used only for the Affects line.
