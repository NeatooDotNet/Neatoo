# Consistency Audit of Neatoo and RemoteFactory Guidance

**Date:** 2026-10-05. **Status:** audit complete; fixes not started except where section 6 says so.

Four read-only audits compared every guidance source in both repositories against the points Keith has settled (`docs/VisionAlignment.md`, section 0) and against each other. This file is the consolidated list. The four detail files beside it hold every finding with its file, line, quote and suggested edit.

## 1. Totals

| What was audited | Detail file | Findings | Contradicts a settled point | Disagrees with itself | Stale against the code | Needs a ruling |
|---|---|---|---|---|---|---|
| Neatoo skills (`neatoo`, `mudneatoo`) | `audit-neatoo-skills.md` | 91 | 36 | 28 | 17 | 10 |
| Design.Domain, the Person example, root `CLAUDE.md` and `README.md` | `audit-design-person.md` | 119 | 56 | 26 | 29 | 8 |
| Neatoo `docs/` and `src/samples` | `audit-neatoo-docs-samples.md` | 267 | 88 | 39 | 67 | 73 |
| RemoteFactory skill, docs and reference app | `audit-remotefactory.md` | 86 | 30 | 15 | 29 | 12 |
| **Total** | | **563** | **210** | **108** | **142** | **103** |

How to read the numbers:

- A finding is a pattern. One finding can hold many edit sites; "87 `LoadValue` sites" is one finding.
- Nothing was built or run. "Stale" means the text does not match the source the auditor read.
- Each audit was done by a subagent that read its files whole. I checked a sample of their claims against the files; the rest is as reported.
- Stale verdicts about Neatoo's generated code rest on `Generated` folders built against RemoteFactory 1.6.1.

## 2. Settled topics: where each is broken and what the edit is

"Source" says where the ruling comes from. **Ruled** means Keith stated it on 2026-10-05. **Recorded** means it rests on an earlier instruction or on the project's own files, and I am treating it as settled unless Keith says otherwise.

| # | Ruling | Source | Where it is broken | The edit |
|---|---|---|---|---|
| T1 | A rule may call the server through an injected command. It never takes a server-only service. Trigger on field commit. | Ruled (D1) | The skill forbids it in four places (`domain-logic-placement.md` Pattern 4 and decision tree, `SKILL.md` ladder). Every async-rule example injects a service into the rule or the entity constructor: 7 sites in the skills, 23 in `src/samples` across 13 published regions, and a repository-in-a-rule in Design.Domain. No file in either repository shows the rule-plus-command form. | Rewrite the four skill passages. Add one compiled rule-plus-command sample and publish it everywhere the service-injecting samples are published now. |
| T2 | Validation is a rule. Exceptions are application failures. A factory method does not check data and drop out. | Ruled (D2) | `if (!IsSavable) return` in `[Insert]`/`[Update]`: the Person example (a test pins it), 3 sample sites, 2 doc passages. RemoteFactory docs and samples: 5 findings. | Depends on the server-gate decision (section 3, Q1). Removing these guards before the framework gate exists removes the only server-side check. |
| T3 | `[Create]` is `new`; `[Fetch]` gets existing data by key. A loaded child is built with its own `[Fetch]`. | Ruled (D3) | Design.Domain's read-model list loads items with `Create()` plus `LoadValue`. 24 sample root `[Fetch]` declarations take the row data as parameters instead of a key. | Fix the Design example. Give sample roots `Fetch(id, [Service] repo)`. |
| T4 | `[Remote]` marks a client entry point. | Recorded (RemoteFactory docs) | About 22 Design.Domain sites and the root `README.md` say it means "must execute on the server". 69 sample root persistence methods take a `[Service]` repository with no `[Remote]`. The skill's `entities.md:733` states RemoteFactory's NF0105 rule backwards. | Reword. Add `[Remote]` and make those methods `internal`. |
| T5 | Inside a factory operation, assign properties directly. No `LoadValue`, `PauseAllActions` or `MarkUnmodified` there. | Recorded (Keith, 2026-09-06; zTreatment ARCH-124) | Design.Domain: 87 `LoadValue` sites in 19 files, 6 `PauseAllActions`, a "DESIGN DECISION" saying to use `LoadValue` anyway, and two comments claiming plain assignment in a `[Fetch]` marks the entity modified. Skills: 8 sites. Docs: about 27 prose sites and 4 code sites. | Plain assignment everywhere. Delete the false comments. |
| T6 | A read model is a plain `[Factory]` class with `[Fetch]`. | Recorded (`SKILL.md`, zCRM strategy) | Root `CLAUDE.md` lines 74 and 76, 27 Design.Domain wording sites, the only Design read-model example, the skill's `base-classes.md` and `pitfalls.md`, and about 9 docs and sample findings all use `ValidateBase`. | Reword. Replace the examples with plain classes. |
| T7 | Interface-first: public interface, `internal` concrete, references typed on the interface. | Recorded (root `CLAUDE.md`) | `src/samples`: 152 of 194 entity and list classes are public with no interface, in 25 of 27 files, plus about 80 references typed on a concrete. The skills publish 12 of them. `README.md` shows 7 public concretes under an "interface-first" heading. RemoteFactory's reference app: 160 public concretes, 5 with an interface. Neither skill states the rule. | See section 5. This one is too large to patch site by site. |
| T8 | On RemoteFactory 1.9 and later, a command that needs the server is `[Remote, Execute]`. | Recorded (RemoteFactory 1.9 release notes) | The Person example's `UniqueName` and the only sample command are bare `[Execute]`, commented "always on the server". Four skill prose sites say the same. | Needs the RemoteFactory pin moved off 1.6.1 (section 3, Q3). |
| T9 | Await `WaitForTasks()` before reading `IsValid`. `RunRules` is for forcing a re-run. | Recorded (`SKILL.md`; zTreatment ARCH-063) | 25 sample call sites and 4 prose sites show `RunRules()` as the routine step. 3 Blazor snippets read `IsValid` with an async rule still running. `pitfalls.md` says "always `RunRules()`". | Replace the calls. Fix the three snippets. |
| T10 | `IsNew` does not imply `IsModified` (Neatoo 0.31). | Settled by the code | About 15 doc findings still state the old behaviour; `blazor.md:380` binds Save to `IsModified`. Two each in the skills and Design.Domain. | Reword to `(IsModified \|\| IsNew) && IsValid && !IsBusy`. |
| T11 | Logic does not live in a ViewModel. | Recorded (skill ladder; ARCH-013) | The MudNeatoo reference `aggregate-reactive-vm.md` is entirely a "ViewModel computes derived values" pattern. | Rewrite or delete the reference. |
| T12 | An interface factory is a remote service proxy, not a repository. | Proposed in `VisionAlignment.md`; not yet ruled | RemoteFactory names interface factories as repositories in 20 lines across 4 files. `IEmployeeRepository` is both the server repository and a client-exposed factory interface. | Rename the examples. |
| T13 | RemoteFactory is for any class, not only persisted entities. | Proposed; not yet ruled | "Persistence routing engine" framing in 8 RemoteFactory files. Its decision tree gives read models and orchestration classes no home. | Reword once Keith answers question R7. |

## 3. Rulings needed

The audits found 103 normative claims that the settled points do not decide. These are the ones that block the most edit sites. The full registers are in the detail files.

**Decided on 2026-10-05, after this table was written:**

- **Q1:** No. The framework does not run the gate. It is highly recommended in the skill and the user documentation.
- **Q2:** Design.Domain is the one example set.
- **Q3:** Use the latest RemoteFactory. The newest version listed on NuGet is 1.9.0.
- **Q4, adding a child:** `itemFactory.Create(...)` then `Add`, where the child's `Create` takes parameters. Who creates the list is taken from Design.Domain: the list factory inside the parent's `[Create]`.
- **Q5:** The Person example's shape (the list loops, makes and removes rows, and hands each child its own row). It applies to everything in Neatoo; applications may vary. Recorded as D10 in `VisionAlignment.md`.

| # | Question | What the sources say | My recommendation |
|---|---|---|---|
| Q1 | Does the framework run the rules on the server before `Insert` and `Update`? | `README.md` says they run. Nothing does it automatically. Hand-written gates exist in three forms. Feasibility is in `VisionAlignment.md` section 0.1. | Yes, in `EntityBase`, throwing on failure. Then T2's guards can be deleted. |
| Q2 | Which compiled example set is the one source for every snippet? | Neatoo has three: Design.Domain, `src/samples` and the Person example. Section 5. | One set per repository. |
| Q3 | Move the Neatoo repository off RemoteFactory 1.6.1? | Blocks T8 and any current `[Execute]` sample. | Yes, as its own step with a build and tests. |
| Q4 | Who adds a child, and who creates the list? | Adding: client code calls `itemFactory.Create()` then `Add` (about 70 sample sites), or the list exposes `AddItem()`. Creating the list: `new` plus `LoadValue` in the parent's constructor (16 sample constructors, and `collections.md` says to), or `listFactory.Create()` inside the parent's `[Create]` (Design.Domain calls the first a common mistake). | `AddItem()` on the list, and the list factory inside `[Create]`. The UI then never holds a child factory. |
| Q5 | What is the save cascade's shape? | Who iterates: the list's `[Update]` in every working Design and Person example; the parent in the one published sample. What the child receives: a parent id (Design), its own EF entity made by the list (Person), or the parent's EF entity (zTreatment). Deletes: the list deletes rows directly in every working example; the skill routes them through the child's `[Delete]`. | The list iterates and each child maps its own row. I have no basis to choose on deletes or on what the child receives. |
| Q6 | Are `PauseAllActions`, `LoadValue` and `MarkUnmodified` ever for application code outside a factory operation? | The docs teach all three to consumers. By source reading, an edit made while paused is not tracked as modified, so it is not saved. | Not application API, except the one `LoadValue` case zTreatment ARCH-124 names. |
| Q7 | Is "call `RunRules` at the end of a factory operation" doctrine, or a framework gap? | Design.Domain and the skill both teach it; zTreatment has 24 such calls. | Doctrine for now, stated once. |
| Q8 | What happens when a row is not found? | `[Fetch]` returns null in most places. `[Update]` on a missing row is skipped silently in RemoteFactory's samples and throws in its README. | `[Fetch]` returns null. `[Update]` and `[Delete]` throw, per D2. |
| Q9 | How does an expected conflict surface, such as a record another user changed or archived? | Design.Domain's `ErrorPatterns.cs` says "validation with message". zTreatment throws. | No recommendation; this is the case in `VisionAlignment.md` section 0.2 where a client-side rule cannot be the whole answer. |
| Q10 | What form does a client-side block take where there is no Neatoo object? | Static commands and plain `[Factory]` classes have no rule engine. zTreatment uses a verdict the UI reads. | Document the verdict form. |
| Q11 | Are `MarkInvalid` from outside code, and a rule that throws, still guidance? | Docs present both as validation channels. | Drop both under D2. |
| Q12 | Are rule classes `internal` and typed on the concrete class, or public and typed on the interface? | Design.Domain does the first; the Person example the second; 30 or more sample rules are public on the concrete. | Typed on the interface, so a rule can be shared and stubbed. |
| Q13 | When does a `[Create]` go to the server? | About 25 RemoteFactory sites show `[Remote, Create]` with a repository it never uses. Design.Domain says Create is local by default. | Local unless it needs server data. |
| Q14 | Can one class be a root in one place and a child in another? | The skill says such a class keeps every method public with `[Remote]`. Design.Domain says not to add a root role to a child type. | Follow Design.Domain. |
| Q15 | Private setters on RemoteFactory classes | The skill says no. The docs say yes with `[JsonInclude]`. The samples use them 212 times with neither. | Test first; see section 4. |
| Q16 | Should this work run as an iterative todo? | It is multi-session and spans two repositories. | Yes. |

## 4. Possible defects found along the way

These are leads from reading, not confirmed failures, except the first.

- **A drop-out gate reports a save that did not happen.** `EntityBase.FactoryComplete` marks the entity unmodified and not new after every `Insert` or `Update`. The Person example and zTreatment's `PatientEdit` return early without writing. I confirmed the framework code and both call sites.
- **RemoteFactory may drop private-setter properties.** The audit reports that the default serialization format omits them and that the generator never reads `[JsonInclude]`.
- **A lazy-loaded value may lose its loader on the client.** The audit reports the generated deserializer builds `LazyLoad<T>` with no loader, so `LoadAsync()` would throw. The docs say the loader is re-created.
- **The one published cascade sample never clears its `DeletedList`.**
- **A published `FactoryComplete` override leaves a created object modified.** It sets a default after calling `base`.
- **A skill example does not compile.** `domain-logic-placement.md` lines 248 to 258 pass two triggers to `AddValidation`.
- **A field-authorization example trusts the client.** `FieldLevelAuthorization.cs:46` takes `bool canEditSalary` as a parameter of a `[Remote]` fetch.
- **RemoteFactory's "client/server" tests serialize nothing.** They use one Logical container.
- **A non-partial class silently loses ordinal serialization.** The skill says it will not compile.
- **An `[Execute]` returning plain `Task` produces generated code that does not compile, with no diagnostic (confirmed on RemoteFactory 1.9.0).** The generated delegate returns `Task<Task>` (CS0266 in the generated file). The skill documents "must return `Task<T>`", but NF0102 only catches non-Task returns, so the user sees an error in generated code instead of a rule violation.
- **Two installed skills are behind their repositories.** `~/.claude/skills/RemoteFactory` is the pre-1.9 copy (8 of 12 files differ). `~/.claude/skills/mudneatoo/SKILL.md` still refers to `IsChild`.

## 5. Why patching 563 findings in place will drift again

The same rule is written down in up to six places in Neatoo: skill prose, skill code generated from `src/samples`, docs prose, Design.Domain comments, the Person example, and `README.md` or `CLAUDE.md`. Each was edited in its own session. That is how they came to disagree.

Neatoo also has three compiled example sets:

| Example set | Size | State |
|---|---|---|
| Design.Domain | 38 files, about 8,700 lines | Interface-first, and its cascade is the list-iterates form. 119 findings, mostly `LoadValue` and stale comments. Feeds no snippet. |
| `src/samples` | 27 files, 194 entity and list classes | 152 classes are not interface-first. Feeds every snippet in the skills and the docs. 267 findings. 48 of its regions are published nowhere. |
| Person example | one application | Uses the drop-out gate and a pre-1.9 command. |

The skills therefore take their code from the set that is furthest from the doctrine. Keith's todo `reduce-design-source-scope.md` (February, in progress) already names the parallel-codebase problem and proposes snippets from tested code only.

My recommendation:

1. **One doctrine page.** Each rule is stated once in prose. Other pages link to it.
2. **One compiled example set per repository** feeds every snippet in the skills, docs and README. The other sets are retired region by region instead of being fixed. Which set survives is Q2.
3. **Comments that describe generated code are deleted, not corrected.** They are stale across the tree, and the real output is on disk.
4. **Installed skills are copied from the repositories as a release step.**

## 6. Order of work

| Step | Work | Depends on | Status |
|---|---|---|---|
| 1 | Skill prose that contradicts a ruling made on 2026-10-05 (T1) | Nothing | Hand-written passages done on branch `docs/consistency-fixes`, uncommitted: `SKILL.md`, `domain-logic-placement.md`, `validation.md`, `shared-rules.md`. The generated samples that inject a service are step 3. |
| 2 | Prose and comments for T4, T6, T9, T10: no compiled code changes | Keith not objecting to the "recorded" rulings | Not started |
| 3 | The canonical example set brought to doctrine, with build and tests | Q2, Q4, Q5 | In progress. Done: T5 in Design.Domain (69 `LoadValue` sites converted to assignment, 6 `PauseAllActions` wrappers removed, the comments that taught them rewritten); one child loaded with `Create` now uses its own `[Fetch]`; `CLAUDE-DESIGN.md` sections 2, 4 and 5, save routing and "Adding a New Entity". `Design.sln` builds, 133 tests pass. Also done since: T4 wording in `RemoteBoundary.cs`, `FetchPatterns.cs`, `CreatePatterns.cs`; the stale generator descriptions in `TwoGeneratorInteraction.cs` (the nonexistent `FactoryMode` attribute, per-entity HTTP endpoints, routing on `IsModified`) and in `DI/ServiceRegistration.cs` and `DI/ServiceContracts.cs`, including a rule example that took a repository; the repo `CLAUDE.md` (read-model wording, a reflection example using a type that no longer exists, obsolete property syntax, the RemoteFactory path, the retired todo skill). Done: the cascade (D10) in Order, Employee and SaveAggregateDemo (Guid keys, row classes, one flush per root save, root delete removes its row with its child rows), with their mocks and tests rewritten to assert on rows; the recommended server-side gate in those three roots' `Insert` and `Update`, with a new test proving it refuses an invalid order on a direct `factory.Save`. `Design.sln`: 0 errors, 135 tests pass. Done since: the read model (T6) is `ReadModels/EmployeeDirectory.cs`, a plain `[Factory]` class; the command examples are `[Remote, Execute] private static`, return data, and throw instead of returning a failure result (S5-3, S7-2; the Person `UniqueName` is now `private static` too); CommonGotchas gotcha 3 rewritten (S1-5, S12-2) and gotcha 4 warns that paused edits are not saved; AllBaseClasses read-model wording, the entity list now holds a child interface (`IDemoChild : IEntityBase`, S9-4) owned by a `DemoParent` root, the value-object list loads items with `[Fetch]` (S2-2); the LazyLoad demos are interface-first (S9-1); AsyncRules has the compiled rule-plus-command example (`UsernameAvailability` plus `CheckUsernameAvailabilityRule`, registered by `DI/DomainRegistration.cs`) with two tests (S6-2); the debounce passages agree (S6-4); `CLAUDE-DESIGN.md`'s serialization table and pitfalls corrected (rule messages are serialized; rules are not re-run on arrival; S6-3); FieldLevelAuthorization takes the permission from a server service instead of a client parameter (I-9). `Design.sln`: 0 errors, 143 tests pass. Then the STALE comment register: synchronous demo rules on `RuleBase<T>` (T-9), rule order and sequencing (I-4, I-5), `RunRulesFlag` members (T-10), generated property shape quoted from the real output (T-3), `IValidateBaseServices` listing (T-8), `LoadValue` placement (T-14), cached modified state (T-15), interface redeclarations removed (T-24), exception names (T-11, T-12), boundary messages (T-13), awaited synchronous `Create` (T-4), FluentRules' multi-property validation (I-22), dual-use wording (I-2), Employee defaults in `[Create]` (I-21), `FullName` notification caveat (I-23). Person's `[Create]` builds its phone list through the list factory (I-15). Left for Keith, all soft: Person's `Fetch` takes no id (S2-4); Person's rules return "Parent is null" as a user message (S5-4); `PersonPhoneList`'s `[Remote]` fetch for the lazy loader (S1-8). |
| 4 | Skills and docs re-pointed at that set; retired regions removed | Step 3 | Done except retirement. Skills (`ec3b080`) and README, getting-started, guides and API reference (`5c28e98`) render only regions from compiled `src/Design` code; hand-typed C# is 4 labelled WRONG blocks in the skill and 0 in the docs. `Design.sln`: 183 tests. `src/samples` and its 254 tests deleted on 2026-10-09 (`c9af08c`) with Keith's approval; `Design.sln` now builds and tests in CI. mudneatoo keeps 14 Blazor blocks Design.Domain cannot supply. |
| 5 | The recommended server-side rules gate documented; drop-out gates replaced with it (T2) | Nothing | In progress. Done: the Person example's `Insert` and `Update` use the throwing gate; its test now expects `SaveOperationException`; the happy-path test configures its phone-list stub to report valid. 55 tests pass. `CLAUDE-DESIGN.md` and the Neatoo skill (`SKILL.md`, "Re-run the Rules on the Server Before the Write") document the form. Still to do: the user docs, which need a compiled snippet from Design.Domain. |
| 6 | RemoteFactory: pin moved in Neatoo, reference app, skill and docs (T8, T12, T13) | Nothing | Pin moved to 1.9.0 in `Directory.Packages.props`, uncommitted. `Neatoo.sln`: 0 errors, 2,206 tests pass, 2 skipped. `Design.sln`: 0 errors, 133 tests pass. The Person example's `UniqueName` command now carries `[Remote]`. Still to do: the sample command in `src/samples`, four skill prose sites, and the RemoteFactory repository's own findings. |
| 7 | Installed skills synced | Steps 1 to 6 | neatoo and mudneatoo copied to `~/.claude/skills/` on 2026-10-06; RemoteFactory not yet (its repo work is step 6). |

## 7. Rulings of 2026-10-09

- `MarkInvalid` does not clear under `RunRules(All)`: filed as bug #96.
- Lazy-loaded child lists need a `[Remote]` child fetch: filed as techdebt #97.
- A root `[Fetch]` must not `RunRules(All)` when root rules write children: filed as documentation #98.
- `MarkInvalid` from application code: dropped from all guidance.
- Housekeeping done: `.claude/rules/design-snippets.md`, `.gitattributes` (markdown LF), `Design.sln` in CI.
- RemoteFactory repository work: deferred; ask Keith again before starting.
