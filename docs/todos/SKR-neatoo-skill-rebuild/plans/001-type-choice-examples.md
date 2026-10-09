# Design.Domain example per type choice

**Plan #:** 001
**Date:** 2026-10-09
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3
**Status:** Draft
**Last Updated:** 2026-10-09
**Plan-review opt-in:** Yes — these examples become doctrine that `SKILL.md` points at and that gets copied; `plan-reviewer`, Pass A against D1–D19 and the RemoteFactory skill
**Code-review opt-in:** Yes — examples are what gets copied, so the shape matters more than in ordinary code
**Branch:** skr-001-type-choice-examples — cut from the arc at Step 2
**PR:** —

---

## Scope

Add to Design.Domain the type choices the rebuilt `SKILL.md` will point at and that it does not yet have: a context (a plain `[Factory]` instance a screen binds to, with `[Fetch]` and an `[Execute]` verb, D13), a class-level static `[Execute]` that gets-or-makes an aggregate (D11, D12), an interface factory that is a domain service the client calls (D12; never named or shaped as a repository), and a Neatoo entity reused by a server job with no person involved (D4). Rename `DemoValueObject` and its list to an input model, since `ValidateBase` is not a value object (D17), and make sure no Design.Domain text calls `ValidateBase` a value object. Each new type gets a Design.Tests test and `skill-*` regions. It does not touch the skill files, the user docs, or the aggregates already there beyond adding the one static `[Execute]`.

---

## Intent

- A reader of the rebuilt `SKILL.md` who is told "that is a context" or "that is a class-level `[Execute]`" can open one compiled, tested file and copy its shape.
- The four shapes that zTreatment got wrong by habit (an instance where a command belonged, a `[Fetch]` that created, a repository exposed to the client, a job rebuilt as plain EF because "no person edits it") each have one correct example in the canonical set.
- The example set stops calling `ValidateBase` a value object, so the next session does not re-learn that mapping.

---

## Framework & Architectural Alignment

- Rulings D3 (Create = new, Fetch = existing), D11 (get-or-make is an `[Execute]`), D12 (static command / class-level `[Execute]` / interface factory split), D13 (context vs command), D14 (local Create by default), D4 (an entity is right with no person involved; list the broken rules rather than throw), D17 (`ValidateBase` is an input model), D19 (interface-first strongly recommended; Design.Domain follows it).
- RemoteFactory skill: class-level `[Execute]` is `public static` and returns the containing type or its interface (`references/class-factory.md`); an interface factory's implementation carries no `[Factory]` and its methods carry no operation attributes, NF0106 (`references/interface-factory.md`); `[Execute]` returns `Task<T>`; a static command is `[Remote, Execute] private static _Name`.
- Neatoo doctrine already in Design.Domain: `[Remote]` only on client entry points; child operations `internal`; no `LoadValue` inside an operation; the D6 server gate on root `[Insert]`/`[Update]`; the D10 save cascade.
- `.claude/rules/design-snippets.md`: regions named `skill-*`, globally unique, tight around the code shown, each pinned by a test.
- RemoteFactory#112: the new interface factory is the counter-example to the repository-shaped ones; it is named and documented as a domain service.

---

## Constraints & Invariants

- Existing Design.Tests stay green and unchanged except for the `DemoValueObject` rename.
- No new type exposes a repository, a `DbContext` or any server-only service to the client: server-only services arrive only by `[Service]` on an operation that is `[Remote]` or `internal`.
- The job example raises no exception for an invalid entity; it reports which entities were skipped and why, and saves the rest.
- The get-or-make `[Execute]` never leaves an object claiming the wrong persistence state: an existing aggregate comes back `IsNew == false`, a new one `IsNew == true`.
- The context holds interfaces, never concretes, and has no Neatoo base class.
- `Design.sln` builds with 0 warnings; `dotnet mdsnippets` reports no duplicate region names.

---

## Steps

1. Add a context under a new `Contexts/` folder: a plain `[Factory]` class whose `[Remote, Fetch]` bundles one existing aggregate with one read-model answer for a screen, and whose `[Remote, Execute]` verb acts on the bundle it holds. Its header comment states D13: an instance because the screen binds to it and later verbs act on it; a static command when the call returns and is done.
2. Add a class-level static `[Remote, Execute]` to an existing aggregate (Order or WorkOrder, chosen at pre-flight) that returns the open one for a key or creates it. Its comment states D11 and D3: neither `[Fetch]` nor `[Create]` can make this claim, so it is an `[Execute]` that calls both.
3. Add a domain-service interface factory under `Services/`: a `[Factory]` interface the client calls, with a server implementation that has no `[Factory]`, injected into a Neatoo rule or verb so the example shows why the client needs it. Its comment says what an interface factory is for and that a repository is never one (RemoteFactory#112).
4. Add a job example under `Jobs/`: a static command with no `[Remote]`, run on the server, that fetches a set of an existing aggregate, applies a verb, runs the rules, saves the valid ones through the factory and returns a report naming each skipped entity and its rule messages. Its comment states D4.
5. Rename `DemoValueObject` / `IDemoValueObject` / `DemoValueObjectList` / `IDemoValueObjectList` to input-model names, re-point every reference including `DesignTestServices` and the `skill-test-services` region, and rewrite the surrounding comments so `ValidateBase` is described as an input model (D17). Grep Design.Domain and Design.Tests for "value object" and fix any remaining use that means `ValidateBase`.
6. Register the new services (the interface-factory implementation, the mock repository for the context and the job) in `DesignTestServices` and, where both tiers need it, in `AddDesignDomainRules` or a sibling extension that the skill can show.
7. Write one Design.Tests class per new type, pinning the Acceptance bullets below; wrap each shape in a `skill-*` region sized for the skill.
8. Build, test, run `dotnet mdsnippets`, confirm no duplicate or missing regions and that existing snippets are unchanged.

---

## Acceptance

- [ ] A context's `[Fetch]` returns the bundle, and its `[Execute]` verb changes the aggregate it holds `[integration]` · Must
- [ ] The class-level `[Execute]` returns the existing aggregate with `IsNew == false` when one exists and a new one with `IsNew == true` when none does `[integration]` · Must
- [ ] A rule or verb reaches the domain service through the generated factory, and the implementation type carries no `[Factory]` `[integration]` · Must
- [ ] The job saves every valid entity, skips every invalid one, reports each skipped entity with its rule messages, and throws nothing `[integration]` · Must
- [ ] No type named `*ValueObject*` derives from `ValidateBase`, and no Design.Domain or Design.Tests comment calls `ValidateBase` a value object `[explicit-skip: rename and prose, verified by grep]` · Must
- [ ] Every new shape has a `skill-*` region and `dotnet mdsnippets` reports no duplicate or missing regions `[explicit-skip: tooling]` · Must
- [ ] `Design.sln` builds with 0 errors and 0 warnings and every Design.Tests test passes `[explicit-skip: meta-bullet]` · Must

---

## Current State (Pre-Flight)

-

---

## Punchlist

- (todo-level rows triaged at Step 2: none lie in this plan's path; the `CLAUDE.md` wording, #98 and the audit count stay on the todo)

---

## Test Evidence

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| Context fetch and verb | Must | `[integration]` | | |
| Get-or-make `IsNew` | Must | `[integration]` | | |
| Domain service via factory | Must | `[integration]` | | |
| Job saves valid, reports invalid | Must | `[integration]` | | |
| No `ValidateBase` value object | Must | `[explicit-skip]` | | |
| Regions and mdsnippets | Must | `[explicit-skip]` | | |
| Build and tests green | Must | `[explicit-skip]` | | |

---

## Gate Record

-

---

## Plan Amendments

-

---

## Notes

- Skills consulted at drafting: the RemoteFactory skill's `class-factory.md`, `interface-factory.md`, `static-factory.md`, `service-injection.md`; `skills/neatoo` read from the repo (not loaded via the Skill tool, per `CLAUDE.md`).
- Open for the user: should the job example run every rule (`RunRulesFlag.All`) before deciding an entity is invalid, given D18 says loaded data is assumed valid? My reading: yes, because the job's verb changed the entity and the job is the server, so this is the D6 gate applied by a caller that has no `Save()` on a root interface to do it for it. Confirm at review.
