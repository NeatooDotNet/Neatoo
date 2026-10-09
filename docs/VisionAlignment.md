# Vision Alignment: RemoteFactory and Neatoo

**Status:** Draft 2, 2026-10-05. Written by Claude (Fable 5.1) for Keith to correct. Nothing here is doctrine until Keith confirms it. This is a working document, not user-facing documentation.

**How to use it:** Sections 2 to 6 are my statement of the vision. Mark what is wrong. Section 7 is my questions, each with the answer I would guess; reply by number.

**What I read:** both skills; the Design.Domain files on rules and factory operations; the Person example; the August assessment (`docs/FableFeedback.md`); zTreatment's ARCH registry and `CLAUDE.md`; the Claude memory notes from zTreatment sessions; grep counts over `zTreatment.DomainModels` at `c05c24d0`; zCRM's solution strategy and building blocks. A research subagent swept zTreatment's decision records, review calibration log and todos for recorded incidents; Appendix A has its results and its coverage limits. I have not read session transcripts.

---

## 0. Decided on 2026-10-05

Keith's rulings on the first seven questions. Where a ruling conflicts with text further down this document, the ruling wins; the sections below have not been rewritten yet.

| # | Ruling | Supersedes |
|---|---|---|
| D1 | A rule may call the server. "This is a big driver for async rules and async rules are a big reason I wrote Neatoo." The rule takes an injected command that encapsulates the server call. A rule never takes a server-only DI service, such as an Entity Framework service or any other server concept. Triggering on field commit is highly recommended. | Question N3. The Neatoo skill's Pattern 4, "Do not use a rule to fetch". |
| D2 | Rules are preferred, so that the block happens on the client, whether or not the user can recover. "Exceptions should NEVER be caught and used for validation messages or warnings." An exception is an application failure, not a try-again. A factory method contains no logic that checks data and drops out for the user to try again. | Question N4. The silent-return gate in `PatientEdit`. |
| D3 | "[Create] is the equivalent of 'new' and [Fetch] is to get existing data." Create takes what a constructor would take. Fetch takes a key. | Question R1. |
| D4 | A Neatoo entity can be right with no person involved. A background job often covers a use case the screens also cover, so the entity is reused for consistency. Validation still matters there, and listing the broken rules is often easier than throwing. | Question R5, and the test under statement 7 ("a person edits it"), which was too narrow. |
| D5 | The inconsistencies are fixed now. A third skill is explored after that. | Questions C1 and C2. |
| D6 | The framework does not run the rules on the server itself. Running them there before the write is highly recommended in the skill and the user documentation. | Question N2. The proposal in section 0.1 is feasible but not adopted. |
| D7 | Design.Domain is the one compiled example set for Neatoo. | `src/samples` as the source of skill and doc snippets. |
| D8 | Neatoo uses the latest RemoteFactory. On 2026-10-05 the newest version listed on NuGet is 1.9.0; the RemoteFactory repository's source is at 1.10.0, which is not published. | The 1.6.1 pin. |
| D9 | A child is added by calling `itemFactory.Create(...)` and then `Add`, "for the cases the itemFactory.Create has parameters". | The first half of the child-creation question. |
| D10 | Neatoo's examples use the Person example's save cascade: "I think it handles the most use cases." The root makes its own row and hands the row's child collection to the list factory's `Save`. The list loops: it makes a row for each new child, finds the row of each existing child, removes the row of each deleted child, and passes each live child its row through the child factory's `Save`. The child maps itself into the row it is handed. The root flushes once. Everything in Neatoo is consistent with this. Applications may vary, which is why the framework does not enforce a shape; zTreatment's step aggregate, where the child makes its own row, is a specialised case. | The three other shapes in Neatoo's examples and published samples. |

Rulings of 2026-10-09, on the questions the third skill needs:

| # | Ruling | Supersedes |
|---|---|---|
| D11 | An operation that may return a new object or an existing one is an `[Execute]` that calls Create or Fetch inside it. This holds for every open, start and ensure seam. | Question R2. |
| D12 | A static command is for a command: it usually returns nothing or a simple result. A class-level static `[Execute]` is an aggregate factory method or verb with logic before or after the standard operations (Create, Fetch, Save), which ties it to D11. An interface factory is a DDD domain service the client calls. A repository is never an interface factory; the RemoteFactory examples that expose one are filed as NeatooDotNet/RemoteFactory#112. | Question R3. |
| D13 | A context (a plain `[Factory]` instance) when the screen binds to the result and later verbs act on it; a static command when the call returns and is done. | Question R4. |
| D14 | A root `[Create]` is local. It is `[Remote, Create]` only when creation needs something only the server has, such as tenant defaults or a sequence. | Question R6. |
| D15 | RemoteFactory's point: when the library holding the object definitions is loaded on both tiers, the client-server seam needs no DTOs and no controllers. Persistence routing is one use of that, not the headline. | Question R7. |
| D16 | An entity exposes business facts (`CanApprove`). A property named for a screen element (`ShowTreatmentPanel`) is a code smell; it belongs in a ViewModel or read model. | Question N5. |
| D17 | `ValidateBase` is an input model: edited and validated, not persisted as itself. It is not a value object; a value object is a `record`. | Question N6. |
| D18 | Of the known framework pains: analyzers are written for the silent failures (a trigger on a collection reference; `LoadValue` inside a factory operation), and an analyzer flags a discarded `Save()` result. The framework does not run rules at the end of an operation, and the guidance does not tell users to: loaded data is assumed valid, because persisted data is valid. Running rules at the end of a `[Create]` or `[Fetch]` is the user's decision and rare, for the case where the returned object should already show its broken rules. A derived value an operation needs is assigned inside it. D6 stands: the framework does not run the server gate. `[Remote]` on a child operation is not an analyzer: once on the server, `[Remote]` does nothing different, so it causes no extra round trips. A fetched aggregate reporting `IsModified == false` is a test recommended in the user's project, not an analyzer. | Question N7. |
| D19 | Interface-first (a public interface per entity, an `internal` concrete, references through the interface) is a strongly recommended architecture, not a requirement. Nothing blocks using the concretes, and no analyzer enforces it. | The "central pillar" and "The Rules" wording in `CLAUDE.md` and the skill, which state it as required. |

Two details Keith confirmed the same day:

- **D6, the recommended form.** The root's `[Insert]` and `[Update]` begin with `await RunRules(RunRulesFlag.All)` and throw `SaveOperationException(SaveFailureReason.IsInvalid)` if `IsValid` is false. That is the exception `entity.Save()` throws on the client for the same condition. A `return` is ruled out by D2 and leaves the entity marked as saved. Keith approved changing the Person example's `Insert_ShouldReturnNull_WhenModelIsNotSavable` test to expect the exception.
- **D9, the other case.** A child whose `[Create]` takes no parameters is added by a method on the list that creates and adds, as the Person example's `AddPhoneNumber()` does.

Section 0.1 records the feasibility answer Keith asked for before deciding D6.

### 0.1 Can Neatoo run the rules on the server with RemoteFactory staying rules-agnostic?

Yes, as far as I can tell from the source. I have not built it.

- RemoteFactory already has the plug-in point: `IFactoryOnStart` and `IFactoryOnStartAsync`. Generated code calls them on the target before the operation body. It knows nothing about what they do.
- Neatoo's `ValidateBase` already implements `IFactoryOnStart` and `IFactoryOnComplete`. That is how the pause and resume around every operation work today.
- `EntityBase` could implement `IFactoryOnStartAsync`. For `Insert` and `Update` on a root, it would run `RunRules(RunRulesFlag.All)` and throw when the graph is not valid. One call on the root covers the children, because `RunRules` walks the child properties.
- No tier check is needed. The hooks run only where the operation body runs, and for a `[Remote]` operation that is the server.
- A throw happens before the body, so nothing is written and the entity is not marked as saved.

Three facts from the audit make the case for doing it in the framework.

- The Neatoo `README.md` already says the same rules execute again on the server during persistence. Nothing does that automatically today.
- The Person example hand-writes the gate as `await RunRules(); if (!this.IsSavable) return null;` in `Insert` and `Update`, and a test pins the null return. That is the drop-out form D2 rules out.
- `factory.Save(target)` does not check `IsSavable`. Only `entity.Save()` does, as Design.Domain's `SavePatterns.cs` notes. For a caller that uses the factory, the server gate would be the only gate.

Two things need a decision.

- **Synchronous operations skip the async hook.** The generator awaits `IFactoryOnStartAsync` only when the domain method returns `Task`. A `void` `[Insert]` or `[Update]` would skip the gate with no warning. Either the generator always awaits it for these operations, or an analyzer requires them to return `Task`.
- **An override point.** zTreatment's two treatment aggregates deliberately validate `Self` only, with a comment warning not to simplify it. A gate that always validates the whole graph needs a way for a root to narrow it, or those two need review.

### 0.2 The 200 throws in zTreatment, read against D2

I read all 200 sites, one line each (method, guard, message), and sorted them. The sorting is mine and approximate.

| Kind | Count | Fits D2? |
|---|---|---|
| A row, id or successor that should exist does not | about 67 | Yes. Application failure. |
| Engine, closed-set and contract guards in pure functions and infrastructure | about 71 | Yes. Application failure. |
| Hand-written "is the aggregate valid" gates that throw | 9 | They are the manual form of the server gate in 0.1. |
| Guards at the top of a verb on a Neatoo entity | 16 | Fits when the entity also exposes a `CanX` the UI reads. Two do not fit: `EndEarly` throws when the reason is blank, and `ExtendPlan` throws when the count is outside 1 to 4. Both are user input. |
| Refusals in static commands, plain `[Factory]` classes and handlers | about 37 | See below. |

Three situations where a client-side rule cannot be the whole answer:

1. **A fact that changes between the client's check and the write.** About 12 throws re-check such a fact on the server: the visit was archived since the screen loaded, the protocol changed since the steps were built, therapy was already started. The client cannot know. For an entity, a rule that the server re-runs (0.1) covers it.
2. **Code with no Neatoo object.** A static command or a plain `[Factory]` class has no rule engine. About 37 refusals are here, 25 of them in `TherapySession`. The client-side block then needs another form: a verdict or flag the UI reads before the user acts, as zTreatment does for starting therapy.
3. **No client at all.** Event handlers and jobs. D4 covers these when the job uses the entity.

One throw is validation in a factory method by any reading: `PatientEdit.Delete` throws "Cannot delete patient with existing plans. Deactivate instead."

---

## 1. What I found before answering

### 1.1 zTreatment confirms your read on RemoteFactory versus Neatoo

| In `zTreatment.DomainModels` | Count |
|---|---|
| `[Factory]` types | 64 |
| on a Neatoo base class (entities and lists) | about 28 |
| plain classes with no Neatoo base | about 19 |
| static command classes | 12 |
| interface factories | 1 |
| `[Execute]` operations | 60 |
| `AddAction` registrations | 20 |
| class-based rules (`AddRule`) | 10 |
| `AddValidation` registrations | 2 |
| validation attributes | 3 |
| `throw new` statements | about 200 |

These are grep counts and approximate. Four of the 64 factory types are unclassified.

RemoteFactory carries every type in the application. Neatoo carries the edit graphs, which is under half of them. Within Neatoo, the parts that earned their place were the aggregated state (`IsModified`, `IsValid`, `IsSavable`) and derived values through `AddAction`. The validation engine was hardly used: two `AddValidation` calls in the whole application.

So zTreatment does not tell us how Neatoo's validation holds up under form-heavy input. zCRM will be its first real test.

I have not read the 200 throws. Some are legitimate broken-state failures (ARCH-045). I cannot say how many are validation written as a guard.

### 1.2 The guidance contradicts itself on the topics you listed

I verified each of these today.

| Topic | One source says | Another source says |
|---|---|---|
| A rule that needs the server (uniqueness, overlap) | Design.Domain `Rules/AsyncRules.cs`: async rules "are crucial for database lookups (uniqueness checks)". The Person example's `UniqueNameRule` calls a command. `docs/todos/business-rules-in-factory-methods-antipattern.md`: "almost always yes". | Neatoo skill, `domain-logic-placement.md` Pattern 4, added 2026-09-29: "Do not use a rule to fetch." Its decision tree: "it would round-trip: NOT a rule." |
| `LoadValue` inside `[Fetch]` | Design.Domain `FetchPatterns.cs`, added 2026-08-21: "DESIGN DECISION: Use LoadValue() for loads anyway." | Your instruction of 2026-09-06 and zTreatment ARCH-124: nothing inside the operation uses `LoadValue`. The child `[Fetch]` in that same Design file uses plain assignment. |
| Read model | `SKILL.md`: a plain `[Factory]` class with `[Fetch]` only. | `base-classes.md` and `pitfalls.md`: `ValidateBase` with `[Fetch]` only. |
| What `[Remote]` means | Design.Domain `RemoteBoundary.cs`: it "marks factory methods that MUST execute on the server". | RemoteFactory docs: "`[Remote]` is not 'this method runs on the server.'" It marks a client entry point. |
| Save cascade shape | Neatoo skill `entities.md`: the parent loops its items and calls `itemFactory.SaveAsync(item, parentId)`. | Design.Domain: the list's `[Update]` coordinates. zTreatment ARCH-029: a child receives the parent's EF entity, never a parent id. |
| Declaring a command | Neatoo skill `base-classes.md` and the Person example: a bare `[Execute] internal static`, commented "always executes on the server". | RemoteFactory since v1.9.0 (2026-09-08): `[Execute]` obeys `[Remote]`, so a bare one is no longer always remote, and its skill requires `private static`. The Neatoo repo still pins RemoteFactory 1.6.1. I have not run these samples on 1.9. |
| Interface-first | Neatoo `CLAUDE.md`: the "central pillar"; concretes are `internal`. | The Neatoo skill's quick start and most of its samples: `public partial class X : EntityBase<X>` with no interface. |
| Before reading `IsValid` | `pitfalls.md`: "always `await entity.RunRules()`". | `SKILL.md`: `await entity.WaitForTasks()`. |

A session that loads every skill, as ARCH-093 requires, still gets two answers and uses whichever it read last. Loading the skills more reliably cannot fix that.

My inference about the cause: each of these was written or revised in its own session to fix the problem at hand, and nobody reconciled it against the other sources. Rationale written in one session, such as a "DESIGN DECISION" comment, then reads to the next session as your doctrine. I cannot tell from git who drafted each line.

### 1.3 The guidance is long on prohibitions and short on the model

zTreatment has 127 ARCH entries. Most are "do not X, because Y", and each was earned from an incident. The skills add ladders, tests and tables. All of that stops a repeat. None of it tells me what to do in the case nobody has hit yet, and there I fall back on mainstream .NET habit.

| The struggle you named | The habit that fills the gap |
|---|---|
| When to use a rule | Validation is a guard at the boundary that throws |
| When to use static factory methods | Logic that is not on an entity goes in an injected application service |
| Create versus Fetch | The difference between constructing and loading is where the data came from |
| Parent updating its children | The root's repository persists the whole graph |
| Client-side and server-side rules | Client validation is a courtesy copy; the real validation is written separately on the server |

My recommendation: the rewrite leads with one page of model that the rules follow from, and the prohibitions become an appendix of known traps. Sections 2 to 6 are my attempt at that page.

### 1.4 What zTreatment's written record adds

The sweep found 50 recorded incidents (Appendix A). Section 2.1 counts them by cluster. Four things in the record change how I read the struggles.

- **The answer to "when Neatoo" changed during the project.** In December 2025, `SOURCE_GENERATOR_QUESTIONS.md` asks "Should all my domain models inherit from it?" about `EntityBase<T>`, and your answer is "Yes". By August 2026 the design you adopted for the therapy session says "Neatoo's value is **user input**" and "Neatoo appears only where a person edits." The workflow classes and `TherapySession` were both built on `EntityBase` before the second statement existed. That part was the first answer being followed, and the skills were never rewritten around the second.
- **Examples outrank prose.** Three incidents are recorded as a nearby violation copied as the pattern. The calibration log's words: the "orchestrator was following local violations as patterns". The samples, Design.Domain and the Person example are what gets copied, so they matter more than the skill text.
- **Four plans relied on a framework behaviour nobody had verified.** Examples: `AddAction` firing inside a factory operation, and `IsChild` being set on a path where it never was.
- **About a dozen items are recorded as framework bugs or skill gaps, not author error** (Appendix A.2). Several are open issues.

---

## 2. The model, as I understand it

1. **One domain assembly, loaded on both tiers.** The browser and the server run the same compiled classes. A domain object can exist on either side, and its code runs wherever the object is. Each tier has its own container. The tiers differ in which services are registered, not in which classes exist.

2. **The factory operation is the seam.** An object is created, loaded, sent across the wire and written only through a factory operation. That operation is where services are injected, authorization is checked, the transaction lives and events are raised. Code outside an operation holds no server service. (ARCH-006, ARCH-007, ARCH-011.)

3. **The operation attribute is a claim about persistence.**
   - `[Create]`: this object is not in the store yet. Its first save inserts.
   - `[Fetch]`: this object reflects what the store holds, and is null when the store holds nothing.
   - `[Insert]`, `[Update]`, `[Delete]`: write this object's own row. They are never called by name; `Save` routes by state.
   - `[Execute]`: anything else. Decide, orchestrate, compute. An operation that might return a new object or an existing one is an `[Execute]` that calls Create or Fetch inside it (ARCH-119).

   The claim is what matters, not what the body does. A `[Create]` may read the database for defaults and is still a Create. Building a loaded child with `Create` is wrong even when every value is right, because the child then claims to be new and the next save inserts it again.

4. **`[Remote]` marks where a call crosses, not where code lives.** It goes on the operations the client calls; that is usually a root's operations, and also a child list's `[Fetch]` when the client lazy-loads it. A `[Service]` parameter is resolved on the tier where its method runs, so a server-only service belongs only on a method that runs on the server: one that is `[Remote]` or `internal`. A child's persistence operations (`[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`) are `internal` and never `[Remote]`, because the call is already on the server when they run. A child's `[Create]` is the exception: the skill and Design.Domain both keep it public and local, so the client can build a child before adding it to a list.

5. **What an operation sets is baseline. What happens afterwards is the user's work.** Inside an operation the object is paused, so an assignment does not mark it modified and does not run rules. Everything derived at open is therefore written inside the operation that produces the object, and each child hydrates inside its own operation. `LoadValue` is for the rare write that must be baseline after the operation has ended. (ARCH-124.)

6. **Every object writes itself, and only itself.** The root's operation opens the transaction and commits. Each child's own `[Insert]`, `[Update]` or `[Delete]` maps and writes its own row, reached through the child factory's `Save`. A parent never maps a child's row. An operation touches only its own aggregate's rows; another aggregate's fact comes through that aggregate's domain service. (ARCH-023, ARCH-027, ARCH-028, ARCH-125.)

7. **Neatoo is the editing session.** Between the load and the save, a person changes the graph over many interactions. Neatoo makes the graph answer, continuously and per property: what changed, is it valid, is work in flight, can it be saved, and what else must change because of this edit. The answers aggregate to the root and bind to the UI. Because of statement 1, all of this runs in the browser with no request, and the same rules can run again on the server before the write.

8. **Logic is placed by who is driving it.** A property change drives a rule. A user's gesture on one aggregate drives a verb on that entity. A load, another aggregate, or anything that needs server services drives a seam, which is an `[Execute]` or a `[Fetch]`. A screen's need for server truth drives a read model. (The ladder in the Neatoo skill; ARCH-015. The TVR decision's words: "Split the verbs by who's driving.")

9. **The UI binds and adapts gestures. It decides nothing.** A ViewModel turns a gesture into a call on the domain and reads the domain's answers. It never originates a mutation, re-derives a gate, chooses what to load, or holds a transaction. (ARCH-013, ARCH-014, ARCH-017.)

Statements 1 to 4 and 6 are RemoteFactory. Statements 5 and 7 are Neatoo. Statements 8 and 9 are the application architecture over both.

**When Neatoo comes into play** is statement 7. The test: does a person edit this object over more than one interaction before saving it? If yes, it is a Neatoo entity, and so are its children. If no, it is plain RemoteFactory. The record already holds this from you. The TSR todo records your realization about `TherapySession`: "nothing can fail due to user input, so why a Neatoo entity graph?"

### 2.1 The model checked against the record

Counts are recorded incidents and are floors. One incident can sit in two clusters.

| Cluster in zTreatment's record | Incidents | Statement that covers it |
|---|---|---|
| Business logic in a ViewModel or Razor | 8 | 8 and 9 |
| Wrong choice among rule, verb, seam and read model | 8 | 8 |
| Baseline versus modified state (`LoadValue`, pause, `MarkUnmodified`) | 8 | 5 |
| Wrong tier (registration, client stubs, a round-tripping rule, trimming, `[Remote]`) | 7 | 1 and 4 |
| Services as parameters; static business methods | 6 | 2 |
| Create versus Fetch versus Execute | 5 | 3 |
| Parent writes child rows | 5 | 6 |
| Neatoo entity where a plain class belonged | 3 | 7 |
| Plain class where Neatoo belonged | 1 | 7 |
| Reaching into another aggregate | 3 | 6 |
| `Save` result not reassigned | 2 | None. A mechanism trap, and a framework question (N7). |
| Tests that hid framework behaviour | 4 | None. Process. |
| Plan relied on unverified framework behaviour | 4 | None. Process. |
| Nearby violation copied as the pattern | 3 | None. Process. |

The largest clusters are logic in the UI layer, placement among rule, verb, seam and read model, and baseline state. Create versus Fetch and parent-written children, which you named, are real but smaller. Baseline versus modified state was not on your list and is as large as anything on it, and wrong-tier placement is nearly as large. Section 8 addresses the three process clusters.

---

## 3. Each DDD concept, with RemoteFactory, and what Neatoo adds

| DDD concept | With RemoteFactory | What Neatoo adds |
|---|---|---|
| Aggregate root | A `[Factory]` class behind a public interface. `[Remote]` on its operations. Its Insert and Update own the transaction and start the cascade. `IFactorySaveMeta` gives `Save` routing. | `EntityBase<T>`, interface extends `IEntityRoot`. `IsValid`, `IsModified` and `IsBusy` aggregated over the graph. `IsSavable` and `Save()`. |
| Child entity | A `[Factory]` `internal` class. Its persistence operations are `internal` and not `[Remote]`; its `[Create]` is public and local. It writes its own row; the parent reaches it through the child factory's `Save`. | `EntityBase<T>`, interface extends `IEntityBase`, so no `Save` and no `IsSavable`. `Parent`. Its state flows up to the root. |
| Child collection | A list you manage yourself, removed items included. | `EntityListBase<I>`: `DeletedList`, attaching marks the child modified, list-level `[Fetch]` and `[Update]`. |
| Value object | An immutable `record`. It needs no factory and crosses the wire as a parameter or a property. | Nothing. `ValidateBase<T>` is an editable validated object, not a value object (question N6). |
| Factory | The generated `IXxxFactory`. Nothing is constructed with `new`. | Nothing. |
| Repository | Not supplied. It is an application interface, injected by `[Service]` into persistence operations, server-only. An interface factory is not a repository. | Nothing. |
| Invariant | A guard in a verb or an operation, which throws. | A rule. A violation is a message on a property, `IsValid` goes false, the save is blocked, and the user sees it while editing. Exceptions are left for broken state (ARCH-044, ARCH-045). |
| Derived value | A computed property, or a value set in the operation. | `AddAction`: recomputed when a trigger changes, on this object or on a child. |
| Behaviour (a verb) | A public method on the entity's interface. It sets state and never persists. It may call a factory seam (ARCH-011). | Rules run after the verb. `CanX` is a property a rule maintains. |
| Domain service | An interface with an `internal` implementation in DI. Reached by a `[Service]` parameter on an operation, resolved on the tier where that operation runs, or by constructor injection, which needs it registered on both tiers. | A rule can take one in its constructor. |
| Command or use case | A static `[Factory]` class with `[Execute]`, plus `[Remote]` when it needs the server. A class-level static `[Execute]` when the result is that aggregate. | Nothing. |
| Cross-aggregate orchestration and load-time policy | `[Execute]` or `[Fetch]` on a plain `[Factory]` class (a context), or a static command. | Nothing. |
| Read model | A plain `[Factory]` class with `[Fetch]` only. | Nothing. |
| Domain event | `IFactoryEvents.Raise` inside an operation, handled by `[FactoryEventHandler<T>]`. The dispatch phase chooses transactional or after-commit. Events relay to the client. | Nothing. |
| Authorization | `[AuthorizeFactory<T>]` or `[AspAuthorize]` on the factory. | `MarkReadOnly` per property. |
| Consistency boundary | One root `Save` is one transaction. Across roots, an `[Execute]` seam or an event. | `IsValid` over the graph is the same boundary held in memory. |

Read down the right-hand column. Neatoo adds nothing for value objects, repositories, commands, orchestration, read models or events. Its whole contribution is to entities while a person edits them. That is the split zTreatment arrived at.

Two labels in the current skills disagree with this table. The RemoteFactory skill names its interface-factory examples `IOrderRepository` and `IEmployeeRepository`, which teaches exposing a repository to the client. The Neatoo skill maps "value object" to `ValidateBase`.

---

## 4. Why use RemoteFactory

- **It removes the tier boundary as something you write.** There are no DTOs, controllers, mapping code or client API layer. There is one class, and its operations are the API.
- **It makes placement a declaration.** `[Remote]`, `internal`, and constructor versus method injection state where code runs and with which services. The trimmer removes server bodies from the client.
- **It gives every domain type the same small vocabulary of operations.** A reviewer knows where loading, writing and authorization are for any type. When an AI writes the code, that uniform review surface is the main value.
- **It puts cross-cutting concerns on one seam.** Authorization, the transaction, events, logging and the version check sit on one endpoint.
- **It asks for no DDD commitment.** Read models, commands, service proxies and plain classes use it the same way entities do. In zTreatment every type does.

Limits I would state plainly in the documentation:

- It needs .NET on both tiers.
- The wire contract is your classes. Client and server deploy together, and zTreatment needed a version handshake (ARCH-050). Serialization constraints reach into the model: public setters, `partial`, `[JsonIgnore]` on computed members, linker roots (ARCH-101, ARCH-102).
- Entities that carry their own persistence operations are the CSLA lineage, not Evans. The documentation should say so instead of defending the DDD label.

---

## 5. Why use Neatoo

Neatoo is for the editing session. It gives three things.

- **State that describes the edit.** Per property and aggregated to the root: modified, valid, busy, savable. This drives the Save button, the unsaved-changes prompt and the navigation guard.
- **Rules that react in the browser.** Change a property, and the values and validations that depend on it update, with no request.
- **Persistence diffing for a graph.** `IsNew`, `IsModified` and `IsDeleted` on every node, plus `DeletedList`, let the cascade write only what changed.

What it costs: a base class and a services constructor, partial properties, pause semantics, a new instance from every `Save`, about 23 concepts before one correct aggregate (the August count), and failure modes that are silent.

Where it does not belong: anything a person does not edit. zTreatment moved two things off Neatoo for that reason. The four workflow classes were recorded as "they're viewmodels, not entities". On `TherapySession` the record says change tracking was "not unused but **fought**".

What zTreatment proved and did not prove: it proved the first and third items, and the second for derived values. It did not exercise validation. zCRM is forms, so expect it to find rough edges zTreatment never reached: single-trigger `AddValidation`, trigger paths that silently never fire, async rules with no debounce, rules paused inside operations. I would plan time for framework fixes there, not only documentation.

---

## 6. Client-side and server-side rules

You said this concept may be lost on me. Here is what I think it is, so you can correct it.

In most web stacks the browser runs JavaScript and the server runs something else. Validation therefore exists twice, the browser copy is a courtesy, and the two drift. That is the only model general training teaches.

Blazor WebAssembly loads the same compiled domain assembly on both tiers. Three things follow that are not general web concepts.

1. **A rule runs in the browser at the moment of the edit.** There is no request. This is the reason to write logic as a rule at all: the form reacts at once, and `IsValid` and `IsSavable` are always current.
2. **The same rule runs again on the server before the write.** The root's `[Insert]` and `[Update]` begin by running all rules and refusing an invalid aggregate. The server does not trust what the browser reported.
3. **A rule that needs server truth is still one rule.** It takes a command delegate in its constructor. In the browser that delegate is a remote call. On the server the same delegate resolves to the local method.

For an author this means: rule code ships to the browser; anything a rule or an entity constructor takes must be registered on both tiers; method-injected `[Service]` parameters are the server-only channel.

Where this stands today:

- **Point 2 is written down in zTreatment but not in the framework or the skill.** zTreatment's review calibration says: "`[Remote]` methods validate the entire aggregate server-side. Run **all** rules and throw if `!IsValid`." The generated `LocalSave` routes straight to `Insert` or `Update` with no validity check. A hand-written gate appears in 7 of about 19 files that declare a remote `[Insert]` or `[Update]` (grep count), in three forms: `IsSavable`, `IsValid` and `IsSelfValid`. `PatientEdit` returns silently where the calibration says throw. The silent return looks unsafe: `EntityBase.FactoryComplete` marks the entity unmodified and not new after every `Insert` or `Update`, so an operation that returns early without writing still reports a save. I read that in the code and have not run it. The TSR todo records you asking whether the pattern was defined anywhere, and the answer: "live precedent, never propagated".
- **Point 3 has a real failure behind the skill's prohibition.** A rule on the Treatment aggregate ran the engine and two fetches on every edit: "three HTTP calls per keystroke". The decision at the time, in a todo later superseded, kept local rules reactive and moved the engine to a verb invoked on debounced commit. The skill then generalised that to "do not use a rule to fetch", which also forbids the Person example's uniqueness rule. That is question N3.
- **The Neatoo samples that show point 3** were written for RemoteFactory 1.6 semantics, before `[Execute]` obeyed `[Remote]` (section 1.2).
- **What I do instead, from habit:** I treat a rule as either UI validation or server validation, and I reach for a guard in a factory method or a ViewModel. The record has throw-guards corrected into rules twice: in Plan, and in `TherapySession`'s `[Insert]`.

Tier placement is wider than rules. The record has seven tier incidents and only one involves a rule. The others include a database-backed service in a constructor the client runs, null stubs registered on the client to hide a wrong dependency, a plan for a ViewModel to hold one transaction across several saves, and a static call that kept a server engine in the client bundle. I think the rewrite needs a short chapter on tier placement in its own right: for any piece of code, where can it run and what decides.

---

## 7. Questions

### On RemoteFactory

**R1. Create versus Fetch on a class with no persistence state.** For a read model or a context there is no `IsNew`. My guess: `[Fetch]` means "hydrate from the store, null if absent" and `[Create]` means "construct without the store". Is that right, or is `[Create]` meaningless on those classes?

**R2. Is "may return new or existing, so `[Execute]`" the general rule?** ARCH-119 states it for one seam, and the record has the fetch-or-create mistake twice. My guess: it applies to every open, start and ensure seam.

**R3. Static command, class-level `[Execute]`, or interface factory.** zTreatment has 12 static command classes and 1 interface factory. The sweep reports an interface factory used twice as the fix when the client needed to reach a server-only domain service. My guess: a static command is the default; class-level `[Execute]` when the result is that aggregate; an interface factory when the client needs a server-only service with several related calls. Is that your rule?

**R4. When does orchestration deserve an instance?** `TreatmentContext` is a plain `[Factory]` class; other seams are static commands. My guess: a context when the result is a bundle the screen binds to and later verbs act on; a static command when the call returns and is done.

**R5. Persisted but not edited by a person.** zCRM has feed-written, job-written and reference data (`CareSummary`, `Notification`, `Zipcode`). The record gives two shapes. `TherapySession` became a plain `[Factory]` root with record children, under "children own their persistence, Neatoo or not". The TVR decision made server-driven events "commands operating on EF directly", with no domain class at all; that todo was later superseded, and I do not know whether the shape survived. Which shape is the default for zCRM's care feed and jobs, and what decides between them?

**R6. When does a Create go to the server?** Design.Domain says Create is local by default. The main example in the RemoteFactory skill's class-factory reference is `[Remote, Create]`. For "New Contact" in zCRM, is it a local Create, or remote to seed tenant defaults?

**R7. How should "why RemoteFactory" be positioned?** `the-problem.md` leads with "a persistence routing engine". Half of zTreatment's use is not persistence. My guess: lead with "one class on both tiers, and the factory operation is the seam", and present persistence routing as one use of it.

### On Neatoo

**N1. Is "a person edits it" the whole entry test?** The record supports it (section 2). What I cannot tell is whether anything else qualifies. One candidate: an aggregate with children that only server code mutates, where the per-node state would still drive the cascade.

**N2. Is section 6 right, and should the framework generate the server gate?** zTreatment's calibration already says a remote operation runs all rules and throws if invalid. It is hand-written in three forms, and my grep finds it in 7 of about 19 files. If it is doctrine, should generated code do it so it cannot be forgotten? And is the right response a throw, or a silent return as `PatientEdit` does?

**N3. Validation that needs the server.** Uniqueness, overlap, and zCRM's duplicate-contact check. The prohibition in the skill came from a rule that ran an engine and two fetches per keystroke. Does that lesson cover a validation rule that makes one cheap call, as the Person example does, or only heavy derivation? If such a rule is allowed, what stops a request per keystroke?

**N4. Where does a guard remain right?** The record corrects throw-guards into rules twice, so guards used as validation were drift. My guess at the remaining line: a rule for any state a user can reach; a named exception only for state that is already broken (ARCH-045). What should a verb do when it is called while its `CanX` is false: throw, do nothing, or set a message?

**N5. Screen-shaped properties on entities.** Pattern 2 in the skill puts `ShowTreatmentPanel` on `Visit`. ARCH-016 says an aggregate knows nothing of screens. Which wins? My guess: the entity exposes the business fact (`CanApprove`), and anything named for a panel belongs in a read model.

**N6. What is `ValidateBase` for?** zTreatment used none. My guess: an input model that is edited and validated but not itself persisted, such as a search form or a "log an activity" dialog. Should the documentation stop calling it a value object?

**N7. Which pains are framework fixes, not documentation?** Candidates: the server gate is manual; `RunRules` must be called by hand at the end of an operation (24 calls in zTreatment); the silent failures have no analyzers; `Save` returns a new instance, and dropping it is on record twice. Does the rewrite document these as they are, or do some get fixed first?

### On the rewrite itself

**C1. Which source wins when two conflict?** My recommendation: one short doctrine page that you own. Design.Domain, both skills, the samples and each application's ARCH entries are derived from it and checked against it. Rationale I write is marked as unratified until you confirm it.

**C2. Where does "how to build an application" live?** The placement ladder is in the Neatoo skill, but two of its four rungs (seams and read models) are pure RemoteFactory, so a RemoteFactory-only project never sees it. My recommendation: a third skill above the two, holding the model, the DDD map, tier placement and the ladder. The RemoteFactory and Neatoo skills become mechanism references under it.

**C3. Do you want the session transcripts mined?** They hold your corrections in your own words. It is the most direct evidence of where I went wrong, and it is a large read.

---

## 8. Proposed next steps

1. You correct sections 2 to 6 and answer section 7.
2. I turn the result into the doctrine page for your sign-off.
3. I audit both skills, Design.Domain, the samples and the Person example against that page and list every contradiction. The eight in section 1.2 are a start, not the full set.
4. The examples are fixed before the prose, because the record shows examples are what gets copied.
5. Each statement on the doctrine page names what enforces it: an analyzer, a test, or review. A mechanism claim with no test behind it is marked as unverified, which answers the four plans that relied on behaviour nobody had checked.
6. The skills and documentation are restructured from the page.

This is multi-session work. I would run it as an iterative todo if you want it tracked that way.

---

## Appendix A. Incidents in zTreatment's written record

Compiled by a research subagent from zTreatment's documents, not from source code.

**Coverage.** It read the review calibration log and the root question files whole. It read 13 of about 103 decision records in full and keyword-scanned about 12 more. It read about 30 archived todos by their opening sections, and about 45 of 529 discovery-log findings in active todos. Counts are therefore floors.

**Checking.** I checked 18 of its quotations against the files and all 18 matched. Every quotation from zTreatment's todos, decision records and calibration log in this document is one I checked. Everything else in this appendix is as the subagent reported it, paraphrased.

### A.1 Incidents

| # | Date | What went wrong | Recorded in |
|---|---|---|---|
| 1 | before 2025-12-25 | Called `[Insert]` directly on the entity and constructed it with `new`. | `NEATOO_QUESTIONS.md` |
| 2 | same | Proposed static Create, Fetch and Insert, and a static remote search returning entities. Keith: "No, don't use static methods." | `SOURCE_GENERATOR_QUESTIONS.md` |
| 3 | undated | 23 `Save()` calls discarded the returned instance. | `neatoo-best-practices-review` |
| 4 | 2026-02-07 | A workflow class on `EntityBase` with no `[Insert]` or `[Update]`; 21 scattered saves; verbs calling `this.Save()`. | `workflow-aggregate-save-refactor` |
| 5 | 2026-02-21 | A visit created as a side effect inside a `[Fetch]`, unsaved. The recorded fix: "Fetch should be pure data loading". | `visit-creation-atomic-save` |
| 6 | 2026-02-24 | An assessment's `Update` hand-mapped child rows and called the repository. | `assessment-child-save-cascade` |
| 7 | 2026-03-04 | A parent's `Insert` created the child's EF rows, then needed a hack to clear modified state. Keith: "Every domain model saves through its own factory. Always." | `single-saveasync-implementation` |
| 8 | undated | A child created with parent id 0, then cast to its concrete type to set the key. | `parent-child-persistence-patterns` |
| 9 | 2026-03-06 | Workflow orchestration written in Razor pages; five bugs traced to it. | `extract-ui-logic-to-domain` |
| 10 | 2026-03-13 and 2026-04-26 | A `[Fetch]` that sometimes created, so `IsNew` was false with no row and the first save routed to Update. It recurred six weeks later. | `fix-fetch-or-create-antipattern`; TLD todo |
| 11 | 2026-03-12 | Workflow classes loaded Visit's children, and the UI was given "Ensure" methods to call first. | `visit-owns-lazy-load` |
| 12 | 2026-03-14 | A computed getter turned into an `AddAction` property, which is not populated after a Fetch. | `addaction-fetch-timing` |
| 13 | 2026-03-14 | A class the client constructs took a database-backed service in its constructor; WASM startup failed. | `fix-client-featureflag-di` |
| 14 | 2026-03-19 | A server-side domain service registered on both tiers and injected into Razor. | `serverside-treatment-generation` |
| 15 | 2026-03-20 | A lazy-load property declared non-partial. | `limb-geometry-anti-pattern` |
| 16 | 2026-03-20 | Lazy load wrapped around data that is regenerated on every entry. | `fix-therapy-tab-hang` |
| 17 | 2026-03-26 | Server-only services constructor-injected into classes the client deserializes, so null stubs were registered on the client. | `move-server-services-to-method-injection` |
| 18 | 2026-03-28 | Four workflow classes on `EntityBase`, needing `MarkModified` and `LoadValue` workarounds. Recorded: "they're viewmodels, not entities". | `rearchitect-flow-layer` |
| 19 | 2026-04-06 | About 12 throw-guards in place of validation rules; a ViewModel seeding values and choosing the operation. | `plan-rules-based-redesign` |
| 20 | 2026-04-26 | A factory method hand-built another entity's EF rows, and review passed it as a design trade-off. | `code-review-calibration.md` |
| 21 | 2026-04-27 | Entity fetches injected another aggregate's repository, copied from nearby code. | `code-review-calibration.md` |
| 22 | 2026-04-29 | ViewModels using mirrored server flags for control flow. | `vm-server-truth-audit` |
| 23 | 2026-05-06 | Three ViewModels dropped the reassignment after `Save`. | `code-review-calibration.md` |
| 24 | 2026-05-06 | Aggregate verbs taking services as parameters, one of them taking seven. | `aggregate-encapsulation-cleanup` |
| 25 | 2026-05-07 | A static engine call from an aggregate that lives on the client kept the engine in the WASM bundle. | `SBM-static-business-method-audit` |
| 26 | 2026-05-07 | Tests claimed wire coverage but used `JsonSerializer` directly. | WDCR todo |
| 27 | 2026-05-10 | A rule ran the engine and two fetches on each edit: "three HTTP calls per keystroke". The rule was deleted. | TVR todo |
| 28 | 2026-05 | Seeding inside a factory operation assumed `AddAction` fires there. | WDCR plans |
| 29 | 2026-05-23 | A static resolver whose output the caller then constructed by hand. | TLF plan 009 |
| 30 | date not read | A plan for a ViewModel to wrap several saves in one transaction. Keith: "We can't have atomicity across server calls." | `repository-transaction-deferred-flush` |
| 31 | 2026-06-16 | Save logic branched on `IsChild`, which was never set on that path. | OAS todo |
| 32 | 2026-08-20 | A parent wrote child rows and bound generated ids back by position. Keith: "Is this a consequence of the parent managing the children?" | LWP todo |
| 33 | 2026-08-25 | A request builder outside the aggregate, fed ids by the ViewModel. | TSR todo |
| 34 | 2026-08-26 | `[Insert]` and `[Update]` opened with throw-guards instead of a rules gate. The gate existed elsewhere as "live precedent, never propagated". | TSR todo |
| 35 | 2026-08-27 | `TherapySession` built as a Neatoo graph that no user input could invalidate. | TSR todo; TSP design |
| 36 | 2026-08-29 | A design put an `[Execute]` on a class factory where it could not return the containing type. | TSP todo |
| 37 | 2026-09-06 | Derivation ran after the factory operation, as tracked writes, and ViewModels compensated. | UCG design |
| 38 | 2026-09-09 | A `public static` guard that took a service as a parameter. | UCG todo |
| 39 | 2026-09-11 | A flag set on the live entity before the save gate. | UCG todo |
| 40 | 2026-09-14 | A child built its own children after its `[Create]` had returned. | UCG todo |
| 41 | 2026-09-08 | A context saved one aggregate only when it was savable, a half-persist; a read-side re-validation was then added and later removed. | RSC todo |
| 42 | 2026-09-23 | A ViewModel open-then-bind step. Recorded: a `[Remote]` call made inside a non-remote method "crosses the wire at that call". | UCG plan 006 |
| 43 | 2026-09-24 | Cancel wrote the old values back, leaving the object modified. Keith: "On cancel re-read from the server. Don't add fancy footwork to get ismodified back to false." | decision record of 2026-09-24 |
| 44 | 2026-09-25 | Open wrote an engine recommendation as a tracked change, so an untouched tab prompted. | decision record of 2026-09-25 |
| 45 | 2026-09-28 | Private helpers handed services; factory methods injecting other aggregates' repositories. | decision record of 2026-09-28 |
| 46 | date not read | Tablet code validators planned as `EntityBase`. | `tablet-implementation` plan |
| 47 | 2026-05-05 | A v3 framing that would have replaced Neatoo's edit loop; Keith pushed back. | `v3-architecture-planning` |
| 48 | 2026-05 | SQL-seeded test state that production never produces; the tests passed and the bug shipped. | `orchestrator-philosophy.md` |
| 49 | 2026-05 | Tests deleted on a false claim that they were covered elsewhere, in three plans. | `v1-test-migration` |
| 50 | 2026-09-12 | A reachable unsavable state that no test could catch, because every test stubbed `IsSavable`. | UCG todo |

### A.2 Recorded as framework bugs or skill gaps

As reported. I did not check these against the issue trackers.

- `RuleMessages.If()` evaluated its message before its condition.
- A test-host crash when an `AddActionAsync` handler awaited a lazy-loaded child, fixed in Neatoo 0.20.1.
- Generated factory code failed 27 database tests after an upgrade (2026-03-21).
- `AddActionAsync` was not covered by the skill when it was first needed.
- `[Execute]` was rejected on non-static factory classes at the time.
- `MudNeatooNumericField` 0.30.1 clamped non-nullable values to 0.
- `IsNew` was part of `IsModified` until Neatoo 0.31.0.
- A validation message that targets a child-list property never arrives. Two more undocumented behaviours were flagged in the same finding (TSR, 2026-08-26).
- The trimming registry never walked dictionary value types, so an upgrade can silently remove types the client needs.
- RemoteFactory has no way to declare a factory internal.
- The `AreSame` string-equality bug; two tests were skipped until it was fixed.
- Open issues: Neatoo#87 and Neatoo#88 (skill gaps), Neatoo#89 (`RunRules(Self)` returning while a child is busy), Neatoo#90 (no member answers `IsModified || IsNew`), KnockOff#87.
