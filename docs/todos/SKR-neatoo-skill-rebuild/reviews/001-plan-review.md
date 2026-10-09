# SKR-001 plan review — 2026-10-09

**Reviewer:** plan-reviewer (tight budget). **Verdict: CONCERNS.** One veto-tier finding, six callouts, all on AC-3 (Must).

## Veto-tier

**V1 — the context's verb.** Step 1 and Acceptance bullet 1 describe an instance `[Remote, Execute]` on the context. RemoteFactory 1.9.0 has no instance `[Execute]` on a class factory: with no matching interface it is NF0103 (error); with one, an instance `[Execute]` returning the target is NF0201 (warning, which the 0-warning gate fails). Shapes that exist: a class-level `public static [Remote, Execute]` taking the context or its key and returning the context; or a plain instance method that calls the held root's own verb and persists through that root's `Save()`. The user picks, because the choice is doctrine.

## Callouts

| # | Finding | Where |
|---|---|---|
| C1 | A static `[Execute]` with no `[Remote]` is a local-only delegate: registered unguarded on the client, body not trimmed. A server-only `[Service]` on the job breaks the plan's own constraint and model statement 4. Keep `[Remote]` (on the server it does nothing extra) or take only aggregate factories. | v1.9.0 `StaticFactoryRenderer.cs:144-156` |
| C2 | Notes question: yes, the job runs `RunRules(All)` and checks `IsValid`, because the root's `[Update]` gate does the same and would throw otherwise; catching it is ruled out by D2. The Notes premise is wrong: `IOrder` and `IWorkOrder` extend `IEntityRoot` and expose `Save()`. `Save()` also throws `NotModified` on an unchanged entity, so the job must skip those too. | `Order.cs:246-254`, `EntityBase.cs:476-495` |
| C3 | Scope under-admits aggregate changes. Step 2 needs a business-key Fetch, a repository method and a mock change (Order's Fetch is by row id; a created Order gets a fresh Guid at Insert). Step 4 needs a verb that can break a validation rule: WorkOrder has only action rules; Order has a breakable rule but no verb. | `Order.cs:58-62,107,142,203`, `WorkOrder.cs:51-134` |
| C4 | Bullet 3 is not observable in Server mode: the registrar does not register the interface, so the rule receives the implementation directly. "Carries no `[Factory]`" is a type-shape claim needing reflection. Word the bullet as behaviour; leave the shape to code review. | v1.9.0 `InterfaceFactoryRenderer.cs:484-498` |
| C5 | The rename re-renders five regions into four docs and three skill files; two prose sites name `DemoValueObject`; the region names themselves say `value-object` (8 placeholders go missing if renamed); `ServiceContracts.cs:68` calls a `ValidateBase` `MyValueObject` in a comment. Scope and Step 8 contradict this churn. | `AllBaseClasses.cs:91,342`, `TestInfrastructure.cs:35,48` |
| C6 | `AsyncRules.cs:103` says a rule takes "a command delegate, never a server-only service"; a second canonical rule taking an interface factory needs a header saying when each applies. The implementation's registration is server-only and must not go in `AddDesignDomainRules` (on a Remote client it would replace the generated proxy). | `AsyncRules.cs:103-104`, `DomainRegistration.cs:22-29` |

## Theoretical (not triaged)

- A context holding `IOrder` across `[Remote, Fetch]` relies on the interface JSON converter; no Design example does that yet.
- WorkOrder's `Create` ends in `RunRules(All)`, which plan 003 removes; picking Order avoids the coupling.
- RemoteFactory tags v1.10.0/1.10.1 exist locally; NuGet not checked.
- Stale `Generated/*ValueObjects*` files would give false grep hits for bullet 5.

## Read report

Beyond the brief: RemoteFactory generator at tag v1.9.0 (Transform, Types, both renderers, diagnostics), its Design `CtorInjectionExample.cs` and `ClassFactoryWithExecute.cs`, `EntityBase.cs` Save guard, repo-wide grep of `DemoValueObject`. Lightly used: `static-factory.md`, `interface-factory.md`, `EmployeeDirectory.cs`, `ApproveEmployee.cs`.
