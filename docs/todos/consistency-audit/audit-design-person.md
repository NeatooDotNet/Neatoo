# Consistency audit: Design projects, Person example, root CLAUDE.md / README.md

Read-only audit. No repo file edited, nothing built or run.

Repo root: `C:\Users\KeithVoels\source\repos\neatoodotnet\Neatoo\`

Path prefixes used in the tables:

| Prefix | Expands to |
|---|---|
| `DD\` | `src\Design\Design.Domain\` |
| `DT\` | `src\Design\Design.Tests\` |
| `PX\` | `src\Examples\Person\` |
| `CD` | `src\Design\CLAUDE-DESIGN.md` |
| `DR` | `src\Design\README.md` |
| `CLAUDE.md`, `README.md` | repo root files |

Row id = edit site (or a group of identical sites). "Class" gives the primary class first; a second class after `+` means the same passage also fails that way.

## 0. What the STALE verdicts were checked against

| Source | Used for |
|---|---|
| `DD\Generated\Neatoo.Generator\Neatoo.Factory\*.g.cs` (written 2026-09-29, after the 1.6.1 pin of 2026-05-24) | factory interface shape, Save routing, remote transport, registration, `IsServerRuntime` guards, static-command delegates |
| `DD\Generated\Neatoo.BaseGenerator\...\Design.Domain.BaseClasses.DemoEntity.g.cs` | property backing-field shape, `InitializePropertyBackingFields` |
| `PX\Person.DomainModel\Generated\...` | bare `[Execute]` behaviour on 1.6.1, Insert return-value handling, generated partial interface |
| `src\Neatoo\` (`EntityBase.cs`, `ValidateBase.cs`, `ValidateListBase.cs`, `EntityListBase.cs`, `Exceptions.cs`, `IMetaProperties.cs`, `IValidateProperty.cs`, `IPropertyFactory.cs`, `IValidateBaseServices.cs`, `AddNeatooServices.cs`, `Rules\RuleBase.cs`, `Rules\RuleManager.cs`, `Rules\RunRulesFlag.cs`, `Internal\EntityPropertyManager.cs`) | runtime behaviour claims |
| `~\.nuget\packages\neatoo.remotefactory\1.6.1\` DLLs (string search only) | `FactoryMode` does not occur in the runtime DLLs (once in the generator DLL); `RemoteOnly` occurs nowhere in the package |
| `PX\Person.Server\Program.cs`, `PX\Person.App\Program.cs` | real endpoint and client registration |

Not verified (stated as such in the rows): anything about RemoteFactory 1.9+ behaviour. The repo pins 1.6.1 (`Directory.Packages.props:30-31`).

---

## 1. Findings against the settled points

### S1 - Tier and `[Remote]`

| Id | file:line | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S1-1 | `DD\FactoryOperations\RemoteBoundary.cs:16` | "[Remote] marks factory methods that MUST execute on the server." | CONTRADICTS S1 | comment | "[Remote] marks a client entry point: a client call to it crosses to the server. It does not mean 'runs on the server' - internal non-[Remote] operations run there too." |
| S1-2 | `DR:74` | "`[Remote]` marks methods that must execute on the server. Key rules:" | CONTRADICTS S1 | comment | Same wording as S1-1. |
| S1-3 | `README.md:20`; `README.md:178` | "Mark a method `[Remote]` and it runs on the server." / "// RemoteFactory method: Runs on server, result transferred to client" | CONTRADICTS S1 | comment (line 178 is a snippet from `src\samples\ReadmeSamples.cs`) | "Mark an aggregate-root operation `[Remote]` and client calls to it cross to the server." |
| S1-4 | 9 sites: `DD\FactoryOperations\FetchPatterns.cs:5`, `:22-23`; `DD\FactoryOperations\RemoteBoundary.cs:21`, `:37`, `:96-97`; `DD\BaseClasses\AllBaseClasses.cs:121`, `:243`; `DD\Entities\Employee.cs:119`; `DD\FactoryOperations\CreatePatterns.cs:124` | "DESIGN DECISION: [Fetch] methods almost always need [Remote]." / "Fetching requires database access, which is only on the server." (FetchPatterns 22-23) | CONTRADICTS S1 + INTERNAL (non-[Remote] `[Fetch]` that hits the repository in the same tree: `FetchPatterns.cs:285-294`, `OrderItemList.cs:58-67`, `AddressList.cs:70-79`) | comment | "A root `[Fetch]` the client calls carries `[Remote]`; child and list `[Fetch]` never do. The reason is the entry point, not database access." |
| S1-5 | 10 sites: `RemoteBoundary.cs:142`, `:145`, `:148-150`; `DD\CommonGotchas.cs:245-248`, `:258-260`, `:498-499`; `DD\DI\ServiceRegistration.cs:91`, `:172`; `DR:78`; `DT\GotchaTests\CommonGotchaTests.cs:152` | "Method [Service]: Available ONLY on server (when method has [Remote])." (RemoteBoundary 142) / "[Service] on methods needs [Remote]     \| Add [Remote] or use" (CommonGotchas 498) | CONTRADICTS S1 + INTERNAL (child operations take `[Service]` with no `[Remote]`: `OrderItem.cs:125`, `:132`; `Address.cs:133`, `:140`; `SavePatterns.cs:325`, `:331`) | comment | "A `[Service]` parameter resolves in the container of the tier the operation runs on. Server-only services go on operations that only run on the server: `[Remote]` root operations and internal child operations." Also delete "or use constructor injection" (a server-only service in a constructor breaks S6/S12). |
| S1-6 | `RemoteBoundary.cs:253-255`, `:277-281`, `:303` | "[Remote] governs / where an operation executes, and it is inert when the operation is already" (253-254) / "// Called via factory when root, or by parent when child" (303) | CONTRADICTS S1 (+S4 at 303) + INTERNAL with `RemoteBoundary.cs:232-237` ("COMMON MISTAKE: assuming a child's persistence methods are called 'by the parent's persistence code'") | comment | Drop the "inert when invoked from a parent's save flow" framing; say child operations are separate internal non-[Remote] methods. Line 303: "Called via the root factory's Save". |
| S1-7 | `DD\Generators\TwoGeneratorInteraction.cs:208-230`, `:268-270`; `ServiceRegistration.cs:81-91` | "[assembly: FactoryMode(FactoryMode.RemoteOnly)] // Client - HTTP proxies" (213) / "DID NOT DO THIS: Generate both modes and select at runtime." (219) | CONTRADICTS S1 (one domain assembly on both tiers) + STALE (generated factories carry both paths and pick at runtime: `OrderFactory.g.cs:35-48`, `ApproveEmployeeFactory.g.cs:18-39`; no `FactoryMode` type or `RemoteOnly` string in the 1.6.1 runtime DLL; no `FactoryMode` attribute anywhere in Design or Person) + INTERNAL with `ServiceRegistration.cs:23-26` (`NeatooFactory.Server/Remote/Logical`) | comment | Replace with: tier is chosen at registration, `AddNeatooServices(NeatooFactory.Server \| Remote \| Logical, assembly)`; the same domain assembly ships to both tiers. |
| S1-8 | `PX\Person.DomainModel\PersonPhoneList.cs:62-64` | "[Remote]" / "[Fetch]" / "internal async Task Fetch(Guid personId," | CONTRADICTS S1 (a child list operation carries `[Remote]`; generated `IPersonPhoneListFactory.Fetch(Guid)` is public, `PersonPhoneListFactory.g.cs:20`). It exists for the lazy loader at `Person.cs:31-34`. | code | Needs a ruling: either lazy-loaded child lists are a named exception to S1, or lazy load goes through a root-level entry point. |
| S1-9 | Public child `[Create]` (not `internal`), 12 child sites: `DD\Aggregates\OrderAggregate\OrderItem.cs:52-53`, `:58-59`; `DD\Entities\Address.cs:65-66`, `:71-72`; `CommonGotchas.cs:189-190`, `:456-457`; `CreatePatterns.cs:223-224`, `:226-227`; `FetchPatterns.cs:244-245`; `SavePatterns.cs:291-292`; `DD\PropertySystem\StateProperties.cs:210-211`; `PX\Person.DomainModel\PersonPhone.cs:25-26`. 7 list sites: `OrderItemList.cs:35-36`; `AddressList.cs:45-46`; `CommonGotchas.cs:223-224`; `CreatePatterns.cs:236-237`; `FetchPatterns.cs:277-278`; `SavePatterns.cs:342-343`; `AllBaseClasses.cs:480-481` | "[Create] stays public so the factory interface remains public for client-side creation." (`FetchPatterns.cs:255`, the stated reason) | CONTRADICTS S1 on a strict reading ("Child entity operations are `internal`"); documented as deliberate | code + comment | Needs a ruling: is child `[Create]` exempt? If not, make it internal and create children through a list/root domain method (the Person shape: `PersonPhoneList.cs:23-28`). |
| S1-10 | `CD:188`, `CD:194` | "Do NOT add `[Remote]` to Insert/Update/Delete - parent handles persistence" (194) | CONTRADICTS S1 by omission (never says child operations are `internal`; line 188 lists `[Remote]` on every operation without "root only") + S4 wording | comment | "Child Fetch/Insert/Update/Delete are `internal` and never `[Remote]`. Each child writes its own row, reached through its own factory's Save." |

### S2 - Create / Fetch / Execute

| Id | file:line | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S2-1 | `DD\ValueObjects\EmployeeList.cs:56-63`, `:78-85` | "var item = itemFactory.Create();" / "item["Id"].LoadValue(data.Id);" | CONTRADICTS S2 (+S3) + INTERNAL with `FetchPatterns.cs:186-187` ("Children load through / [Fetch], never [Create].") | code | Add an internal `[Fetch](row values)` to the item; call `itemFactory.Fetch(...)`. |
| S2-2 | `AllBaseClasses.cs:380-384` | "var item = valueObjectFactory.Create(name);" / "Add(item);" (inside a `[Remote][Fetch]`) | CONTRADICTS S2 | code | Same as S2-1. |
| S2-3 | `StateProperties.cs:140-143` | "Child = childFactory.Create();" / "Child["Value"].LoadValue(data.ChildValue);" | CONTRADICTS S2 (+S3). The comment at 143 ("After Fetch: IsNew=false, IsModified=false, IsSelfModified=false") is false for the child: it completed `[Create]`, so `IsNew=true`. `ModificationChildDemo` has no `[Fetch]`. | code + comment | Give `ModificationChildDemo` a `[Fetch]`; `Child = childFactory.Fetch(data.ChildValue)`. |
| S2-4 | `PX\Person.DomainModel\Person.cs:83` | "internal async Task<bool> Fetch([Service] IPersonDbContext personContext," | CONTRADICTS S2 (soft): no identifying parameter; `FindPerson()` returns the first row (`PersonDbContext.cs:44-47`) | code | Take `Guid id`. |

No example of "may return new or existing, so static `[Execute]`" exists in the audited files (gap, not a contradiction).

### S3 - Baseline inside a factory operation

| Id | file:line | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S3-1 | `LoadValue` inside `[Fetch]`/`[Insert]` bodies: 87 code sites in 19 files. `Order.cs:136-141`; `AllBaseClasses.cs:284-285`; `CommonGotchas.cs:163, 201, 202, 302, 431, 465`; `Employee.cs:145-151`; `DD\ErrorHandling\ErrorPatterns.cs:143-145`; `CreatePatterns.cs:144-145`; `FetchPatterns.cs:89-91, 109-111, 212-213`; `RemoteBoundary.cs:106, 107, 115, 197, 294-296, 305`; `SavePatterns.cs:88-90, 111, 205-206`; `TwoGeneratorInteraction.cs:61-62`; `DD\PropertySystem\FieldLevelAuthorization.cs:49-51`; `DD\PropertySystem\LazyLoadProperty.cs:107`; `DD\PropertySystem\PropertyBasics.cs:255-256, 316-317`; `StateProperties.cs:138, 141, 236`; `DD\Rules\AsyncRules.cs:71-72`; `DD\Rules\FluentRules.cs:163-167`; `DD\Rules\RuleBasics.cs:73-76`; `EmployeeList.cs:57-61, 79-83`; `DD\ValueObjects\EmployeeListItem.cs:70-74` | "this["Id"].LoadValue(data.Id);" | CONTRADICTS S3 + INTERNAL (plain assignment in `OrderItem.cs:84-88`, `Address.cs:106-111`, `SavePatterns.cs:319-321`, `FetchPatterns.cs:260-261`, and in the Person example throughout) | code | `Id = data.Id;` |
| S3-2 | `using (PauseAllActions())` inside a factory operation, 6 code sites: `AllBaseClasses.cs:281`; `CommonGotchas.cs:161`, `:429`; `CreatePatterns.cs:141`; `StateProperties.cs:135`; `LazyLoadProperty.cs:105`. Plus `CD:138`. | "using (PauseAllActions())  // For EntityBase - already paused during Fetch" (CD 138) | CONTRADICTS S3 + INTERNAL with `FetchPatterns.cs:115-126` ("Pattern 3 (RETIRED) ... COMMON MISTAKE"), `Order.cs:115-117`, `Employee.cs:123-125`. `LazyLoadProperty.cs:105-109` is the exact hazard described: the `using` ends, then `LazyDescription = ...` runs. | code | Delete the `using`. |
| S3-3 | `FetchPatterns.cs:74-80` | "DESIGN DECISION: Use LoadValue() for loads anyway. LoadValue() is the" / "explicit load primitive: it never marks the property modified and never" | CONTRADICTS S3 + INTERNAL with `FetchPatterns.cs:71-72` (same block) and `:260` | comment | Replace with S3: "Inside a factory operation the object is paused; assign properties. `LoadValue`, `PauseAllActions`, `MarkUnmodified` are not used here." |
| S3-4 | `Order.cs:124-126`; `Employee.cs:132-135` | "This[...].LoadValue is shown for the root's own properties — plain" / "property assignment is equally clean here because the object is paused;" | CONTRADICTS S3 | comment | Delete; assign. |
| S3-5 | `AllBaseClasses.cs:556-568`, `:279-280`; `PropertyBasics.cs:207-227` | "Name = repo.Get(id).Name;  // Sets IsModified=true!" (AllBaseClasses 561) / "Name = data.Name;  // SetValue! IsModified becomes true!" (PropertyBasics 217) | CONTRADICTS S3 + STALE (`src\Neatoo\Internal\EntityPropertyManager.cs:43-46` sets `IsSelfModified` only when `!IsPaused`; a factory operation pauses) + INTERNAL with `OrderItem.cs:70-73` | comment | Invert both "COMMON MISTAKE" blocks: plain assignment is the load. |
| S3-6 | `PropertyBasics.cs:244-246` | "// Using property setter - this marks as modified" / "// IsNew=true, IsSelfModified=true (from setter)" (inside `[Create]`) | CONTRADICTS S3 and S11 + STALE (same source) + INTERNAL with `CreatePatterns.cs:74` and `AllBaseClasses.cs:268` | comment | "IsNew=true, IsSelfModified=false: the object is paused." |
| S3-7 | `StateProperties.cs:402-404` | "DESIGN DECISION: LoadValue() works even when paused." / "This is critical for Fetch operations which pause, then load values." | CONTRADICTS S3 | comment | Drop the second sentence. |
| S3-8 | `CD:121-143`; `CD:368-377` | "// To load data without triggering rules or modification:" (134) / "**Key Point:** During `[Fetch]` operations, the factory pauses the object, so `LoadValue()` calls are safe" (377) | CONTRADICTS S3. The table at 372-375 also says `SetValue()` "Sets IsModified: Yes" with no paused exception. | comment | Rewrite section 4 and the "LoadValue vs SetValue" block to S3. |
| S3-9 | `SavePatterns.cs:110-111` | "// Use LoadValue to set Id without marking as modified" | CONTRADICTS S3 + INTERNAL with `SavePatterns.cs:230-231` and `Order.cs:163-164` ("object is paused — assignment is clean") | comment + code | `Id = generatedId;` |
| S3-10 | `EmployeeListItem.cs:68-69`; `TwoGeneratorInteraction.cs:240`; `PropertyBasics.cs:287-288` | "// Still use LoadValue for consistency" / "Factory methods use property indexer: this["Name"].LoadValue(value)" | CONTRADICTS S3 | comment | Delete or reword to "the indexer is for property metadata, not for loading". |
| S3-11 | `DT\PropertyTests\StatePropertyTests.cs:60`; `DT\PropertyTests\PropertyBasicsTests.cs:209` | "public async Task Fetch_UsesLoadValue_NotModified()" / "LoadValue on private-set property succeeds (Fetch escape hatch)" | CONTRADICTS S3 (test name and comment assert the old guidance) | test name / comment | Rename `Fetch_LoadsCleanBaseline_NotModified`; drop "(Fetch escape hatch)". |

### S4 - Self-persistence

| Id | file:line | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S4-1 | Root `[Delete]` iterates children and deletes their rows: `Order.cs:208-214`; `Employee.cs:228-237`; `SavePatterns.cs:269-275` | "// Delete items first (FK constraint). Direct repository deletes are" / "// INTENTIONAL here - deleted children get no factory operation" | CONTRADICTS S4 + INTERNAL with the COMMON MISTAKE blocks in the same files (`Order.cs:182-188`, `Employee.cs:197-202`, `SavePatterns.cs:239-245`) | code + comment | Needs a ruling for root delete: child `[Delete]` reached through the list factory, or database cascade. Either way the root stops calling `repository.DeleteItem`. |
| S4-2 | List `[Update]` deletes child rows; the child has no `[Delete]`. Code: `OrderItemList.cs:100-108`; `AddressList.cs:111-118`; `SavePatterns.cs:363-370`. Comments: `OrderItemList.cs:79-80`; `AddressList.cs:91-92`; `SavePatterns.cs:54-57`, `:149-151`, `:171-172`; `OrderItem.cs:196`; `Address.cs:206`; `AllBaseClasses.cs:423-424`; `Order.cs:174`; `Employee.cs:190` | "Deleted children are removed" / "from persistence directly; they get no factory operation." (OrderItemList 79-80) | CONTRADICTS S4. Generated child Save today throws on a deleted child (`OrderItemFactory.g.cs:249-252`: `if (target.IsDeleted) { throw new NotImplementedException(); }`). | code + comment | Add `[Delete] internal void Delete(int parentId, [Service] repo)` to the child with the same parameter list as Insert/Update; the list calls `itemFactory.Save(item, parentId)` for deleted items too. |
| S4-3 | `AllBaseClasses.cs:470-472` | "Lists are ALWAYS saved through their parent aggregate root." / "The parent's Save() method iterates the list and calls Insert/Update/Delete." | CONTRADICTS S4 + INTERNAL with `AllBaseClasses.cs:420-424` and `Order.cs:187-188` ("never as a cascade from the parent") | comment | "The root's Insert/Update calls the list factory's Save; the list's `[Update]` calls each child's factory Save." |
| S4-4 | `CommonGotchas.cs:383-404` | "if (parent.IsSelfModified) {" ... "await parent.Update(...);" / "NOTE: You typically don't write this persistence logic manually." | CONTRADICTS S4 + INTERNAL (`AllBaseClasses.cs:249` "You NEVER call Insert/Update/Delete directly"; `OrderItemList.cs:110` guards on `IsNew \|\| IsModified`, not `IsSelfModified`) | comment | Rewrite the RIGHT block to the list-factory shape; delete the NOTE. |
| S4-5 | `PX\Person.DomainModel\PersonPhoneList.cs:82-97` | "personPhoneEntity = new PersonPhoneEntity();" / "personPhoneEntities.Remove(personPhoneEntity);" | CONTRADICTS S4: the list creates, attaches and removes the child's row; the child only maps onto the entity it is handed (`PersonPhone.cs:68-78`); no child `[Delete]` | code | Child `[Insert]`/`[Delete]` receive the parent's `PersonEntity` (or its Phones collection) and add/remove their own `PersonPhoneEntity`. |
| S4-6 | `SavePatterns.cs:303-313`; `CD:194`; `DR:114` | "DID NOT DO THIS: Have child entities save themselves." / "await employee.Save();  // Parent saves children" | CONTRADICTS S4 in wording (reads as the opposite of "self-persistence") | comment | Retitle "DID NOT DO THIS: expose Save() on a child interface"; "the root's Save reaches each child's own Insert/Update/Delete". |
| S4-7 | `DT\AggregateTests\AggregateCoverageGapTests.cs:81`, `:97` | "public async Task OrderDelete_DeletesChildrenThenRoot()" | CONTRADICTS S4 (test names pin S4-1) | test name | Changes with S4-1. |

### S5 - Rules over exceptions

| Id | file:line | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S5-1 | `PX\Person.DomainModel\Person.cs:101-106` (`[Insert]`), `:129-134` (`[Update]`) | "await RunRules(token: cancellationToken);" / "if(!this.IsSavable) { return null; }" | CONTRADICTS S5 + STALE as a mechanism: the generated factory discards the return value (`PersonFactory.g.cs:248` `await cTarget.Insert(personContext, cancellationToken);`, then `FactoryComplete(Insert)` and `return new Authorized<IPerson>((IPerson)cTarget)` at 252-262; Update the same at 327). The drop-out reports a successful save, marks the object old and unmodified, and persists nothing. | code | Delete both blocks. Whether the server re-validates at all is unruled (U-3). |
| S5-2 | `PX\Person.DomainModel.Tests\UnitTests\PersonTests.cs:77`, `:93`, `:99`; `PX\Person.DomainModel.Tests\TestDoubles\TestPerson.cs:7` | "public async Task Insert_ShouldReturnNull_WhenModelIsNotSavable()" / "Test stub for Person that allows controlling IsSavable and RunRules behavior." | CONTRADICTS S5 (tests pin the drop-out) + INTERNAL with `CLAUDE.md:115` | test | Remove with S5-1. |
| S5-3 | `DD\Commands\ApproveEmployee.cs:102-116`, `:228-238`, `:128-129` | "return Task.FromResult(ApproveEmployeeResult.Failed("Employee is already approved"));" / "This is cleaner than throwing exceptions for expected failures." | CONTRADICTS S5: check-and-return business validation inside a factory method, and a third channel (result object) beside rule and exception | code + comment | Move "already approved / inactive" to rules on the Employee aggregate; the command throws only for application failure. If result objects are allowed for commands, S5 needs that sentence. |
| S5-4 | `PX\Person.DomainModel\UniquePhoneNumberRule.cs:15-16`; `UniquePhoneTypeRule.cs:16-17` | "RuleMessages.If(target.ParentPerson == null, nameof(IPersonPhone.PhoneType), "Parent is null")" | CONTRADICTS S5 (soft): a rule message shown to the user for an application state, not for user input | code | Return `None` when there is no parent (or throw). |

### S6 - Rules that call the server

| Id | file:line | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S6-1 | `ServiceRegistration.cs:293-321` | "PATTERN 2: Rules with dependencies (less common)" / "private readonly IEmployeeRepository _repo;" | CONTRADICTS S6: a rule takes a repository, and the entity constructor takes that rule (`[Service] UniqueNameRule uniqueNameRule`, 316-317), so the entity cannot be built on the client. "less common" also contradicts "allowed and intended". | comment | Replace with the Person shape: the rule takes the delegate of a `[Remote, Execute]` command (`PX\...\UniqueNameRule.cs:10-16`). |
| S6-2 | `AsyncRules.cs:100-127`, `:272-275` | "Rules can have services injected via constructor." / "private readonly IUsernameService? _usernameService;" | CONTRADICTS S6: `IUsernameService` is a plain service interface, introduced at 89-92 as a "Database Lookup"; nothing makes it a command delegate or client-safe. It is also never wired: `AsyncRules.cs:59` constructs the rule with no argument and `:139-140` simulates with `Task.Delay`. Design.Domain has no working example of S6. | code + comment | Add a static `[Factory]` class with `[Remote, Execute] private static Task<bool> _IsUsernameAvailable(string, [Service] repo)`; inject its delegate into the rule; register the rule in DI. |
| S6-3 | `CD:494-504` | "_svc = svc;  // Captured on server, not available after deserialize!" / "// Or: Use method-injected services in factory methods, not rules" | CONTRADICTS S6 + STALE (rules are constructed by DI with the entity on each tier; nothing is "captured on server" or serialized) | comment | Replace the pitfall with S6. |
| S6-4 | `AsyncRules.cs:28-30` vs `RuleBasics.cs:372-373` | "consider implementing debouncing in the rule logic itself." vs "Consider debouncing at the UI layer, not in rules." | INTERNAL (direct) + CONTRADICTS S6 (neither states "trigger on field commit, not per keystroke") | comment | Both: "Bind so the property is set on field commit, not per keystroke." |

Conforming example to copy from: `PX\Person.DomainModel\UniqueNameRule.cs:8-16` with `UniqueName.cs` (once S7-1 is fixed).

### S7 - Commands on RemoteFactory 1.9+

| Id | file:line | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S7-1 | `PX\Person.DomainModel\UniqueName.cs:9-12` | "// It is ALWAYS executed Remotely on the Server (for now)" / "[Execute]" + "internal static async Task<bool> _IsUniqueName(" | CONTRADICTS S7: bare `[Execute]`, `internal` not `private`, comment states the old behaviour. Depends on 1.6.1: `UniqueNameFactory.g.cs` registers the remote delegate under `NeatooFactory.Remote` for this bare `[Execute]`. Per S7, on 1.9+ a bare `[Execute]` runs on the calling tier; the caller is `UniqueNameRule` on the client, where `IPersonDbContext` is not registered (`PX\Person.App\Program.cs`), so the uniqueness rule would break. 1.9+ behaviour itself was not verified here. | code + comment | `[Remote, Execute] private static`; delete the comment. |
| S7-2 | `ApproveEmployee.cs:93-95`, `:154-156`, `:193-195`, `:216-218` | "[Remote]" / "[Execute]" / "public static Task<ApproveEmployeeResult> _Approve(" | CONTRADICTS S7: `public static` (4 declarations). `[Remote]` is already present, so these do not depend on the old behaviour. | code | `private static`. |
| S7-3 | `ApproveEmployee.cs:38-43` | "[Execute]" / "public static Task<ApproveEmployeeResult> _Approve(int employeeId, [Service] IRepo repo) { ... }" | CONTRADICTS S7: the "ACTUAL PATTERN" comment shows a bare `[Execute]` with a server-only service; depends on the old behaviour | comment | `[Remote, Execute] private static`. |
| S7-4 | `ApproveEmployee.cs:77-78` | "With [Remote], the client factory makes an HTTP call." / "Without [Remote], it's local execution." | Matches S7 but STALE against the pinned 1.6.1 + INTERNAL with `UniqueName.cs:10` | comment | Keep; bump the pin or add "(RemoteFactory 1.9+)". |
| S7-5 | `CLAUDE.md:77`; `DR:70`; `ApproveEmployee.cs:21` | "Static classes with `[Factory]` and `[Execute]` - Commands" | CONTRADICTS S7 by omission (no `[Remote]`, no `private static _Name`) | comment | "Static `[Factory]` class; `[Remote, Execute] private static _Name` when it needs the server." |

Full list of command declarations in the audited files: the four in `ApproveEmployee.cs` (S7-2) and `UniqueName.cs:11-12` (S7-1). Only `UniqueName` and the comment at `ApproveEmployee.cs:38-43` depend on the old behaviour.

### S8 - Read model

| Id | file:line | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S8-1 | `EmployeeListItem.cs:22-23`; `EmployeeList.cs:21-22` | "internal partial class EmployeeListItem : ValidateBase<EmployeeListItem>, IEmployeeListItem" | CONTRADICTS S8 (the only read-model example is built on `ValidateBase` / `ValidateListBase`) | code | Plain `[Factory]` class with `[Fetch]` only; no base class, no rules. |
| S8-2 | Wording, 27 sites: `CLAUDE.md:74`, `:76`; `DR:57`, `:59`; `AllBaseClasses.cs:15`, `:20`, `:52`, `:324`, `:328`, `:346`, `:351`, `:513-514`; `EmployeeList.cs:4`, `:13`, `:19`, `:35`, `:92`; `EmployeeListItem.cs:4`, `:14`, `:20`, `:28`, `:40-41`, `:59`, `:80`, `:97-99`; `DD\ValueObjects\IValueObjectInterfaces.cs:13`, `:25` | "`ValidateBase<T>` - Value objects, read models, validation-only objects" (CLAUDE.md 74) | CONTRADICTS S8 | comment | "`ValidateBase<T>`: only when the object needs rules. Read model: a plain `[Factory]` class with `[Fetch]` only." |
| S8-3 | `TwoGeneratorInteraction.cs:264-266` | "FIX: Ensure class inherits from EntityBase<T> or ValidateBase<T>" | CONTRADICTS S8 (a plain `[Factory]` class is valid) | comment | "FIX: add `[Factory]` to the class." |

### S9 - Interface-first

| Id | file:line | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S9-1 | `LazyLoadProperty.cs:84`, `:122`, `:102-103` | "public partial class LazyLoadEntityDemo : EntityBase<LazyLoadEntityDemo>" | CONTRADICTS S9: two public concretes with no interface. Line 102-103 is also a `public` non-`[Remote]` `[Fetch]`. | code | Add `ILazyLoadEntityDemo : IEntityRoot`, `ILazyLoadValidateDemo : IValidateBase`; make the classes `internal`. |
| S9-2 | `README.md:38`, `:73`, `:88`, `:144`, `:165`, `:194`, `:206` | "public partial class Employee : EntityBase<Employee>" / "public class AddressList : EntityListBase<IAddress>, IAddressList { }" | CONTRADICTS S9 (7 public concretes; `Employee`, `CustomerSearch`, `Customer` have no interface) + INTERNAL with `README.md:24` ("Interface-first design enforces aggregate boundaries at compile time") | code (snippet source `src\samples\ReadmeSamples.cs`, outside this audit) | Fix the sample, re-run MarkdownSnippets. |
| S9-3 | `RemoteBoundary.cs:33-38`; `CD:181-188` | "public class Employee : EntityBase<Employee> {" (under "ACTUAL PATTERN") / "1. Create class inheriting from `EntityBase<T>`" | CONTRADICTS S9 (public concrete in a recommended pattern; "Adding a New Entity" never mentions the interface or `internal`) | comment | Show `internal partial class Employee : EntityBase<Employee>, IEmployee`; add the interface step. |
| S9-4 | `DD\BaseClasses\IBaseClassInterfaces.cs:24`, `:38` | "public interface IDemoEntity : IEntityRoot" / "public interface IDemoEntityList : IEntityListBase<IDemoEntity>" | CONTRADICTS S9 (a list whose child interface extends `IEntityRoot`) + INTERNAL with `IOrderInterfaces.cs:29-39` ("COMMON MISTAKE: Extending IEntityRoot for child entities."). `DemoEntity` also carries `[Remote]` Insert/Update/Delete (S1). | code | Separate `IDemoChild : IEntityBase` for the list demo. |

### S10 - Waiting on rules

No passage contradicts S10. `await WaitForTasks()` before reading `IsValid` is stated consistently (`AllBaseClasses.cs:580-589`, `AsyncRules.cs:32-41`, `StateProperties.cs:256-265`, `CD:287-294`, `CD:356-366`, `ErrorPatterns.cs:256-263`). Three uses of `RunRules` that S10 does not settle are U-2, U-3 and U-4.

### S11 - IsNew / IsModified

| Id | file:line | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S11-1 | `StatePropertyTests.cs:55`; `DT\BaseClassTests\EntityListBaseTests.cs:104` | "// Assert - Note: New entities are already modified, but check the specific property" / "// Note: New items start as modified, so the list is already modified" | CONTRADICTS S11 (stale comments) | comment (test) | "A created entity is not modified" / "Adding to a live list marks the item modified". |
| S11-2 | `CD:310` | "\| `IsModified` \| EntityBase \| Has unsaved changes (includes children) \|" | CONTRADICTS S11 (soft: a new object is unsaved but not modified) | comment | "Differs from its baseline (includes children). Does not include `IsNew`." |

`PropertyBasics.cs:244-246` (row S3-6) is also an S11 site.

### S12 - Services

| Id | file:line | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S12-1 | `[Service]` parameter on a non-`[Remote]` `[Create]` that runs on the client, 13 sites: `Order.cs:102`; `Employee.cs:110`; `CommonGotchas.cs:152`, `:420`; `CreatePatterns.cs:127`, `:209`; `FetchPatterns.cs:171`; `SavePatterns.cs:192`; `StateProperties.cs:69`, `:124`; `LazyLoadProperty.cs:97`, `:134`; `PX\Person.DomainModel\Person.cs:76` | "public void Create([Service] IOrderItemListFactory itemsFactory)" | CONTRADICTS S12 as worded ("a `[Service]` parameter on a factory method (server)") + INTERNAL: `CommonGotchas.cs:247-248` and `RemoteBoundary.cs:142` say method `[Service]` is server-only, while `CreatePatterns.cs:111-124` says "Pattern 3: Create with Service (rare, but supported)". "rare" is also false: every aggregate root here gets its child list this way. | code + comment | Needs a ruling: sanction method `[Service]` of a both-tier service (child factory) on a local `[Create]`, or require constructor injection (the Person shape, `Person.cs:24`). Then fix the three comments to match. |
| S12-2 | `CommonGotchas.cs:252-261` | "employee.DoServerThing();  // Has [Service] IDbContext - THROWS!" / "public void DoServerThing([Service] IDbContext db) { ... }" | CONTRADICTS S12 (a service as a parameter of an ordinary entity method) + STALE (`[Remote]` applies to factory operations only; no proxy is generated for an arbitrary method) + INTERNAL with `CommonGotchas.cs:294-295` | comment | Replace with a factory-operation or command example. |

### S13 - Placement

No passage places business logic in a ViewModel or Razor file. `PX\Person.App\Pages\Home.razor:244-252` calls list domain methods (`AddPhoneNumber`, `RemovePhoneNumber`), which conforms. The save orchestration at `Home.razor:177-203` is U-4. None of the audited files states S13.

---

## 2. INTERNAL - two passages disagree (no single settled point decides it)

| Id | Passage A | Passage B | Kind | Minimal fix |
|---|---|---|---|---|
| I-1 | `AllBaseClasses.cs:578` "await parent.Save();  // NOW child [Delete] method called"; `CommonGotchas.cs:137` "// Save() will call [Delete] on this item"; `CD:110` "Each DeletedList item: [Delete] called" | `SavePatterns.cs:54-57` "deleted / CHILDREN never get a [Delete] factory call at all" (and the code in S4-2) | comment | A agrees with S4; fix B's code (S4-2). |
| I-2 | `DD\FactoryOperations\IFactoryInterfaces.cs:153-157` "Can serve as aggregate root or as child within another aggregate." (on `IDualUseEntity : IEntityRoot`); `RemoteBoundary.cs:258-260` | `RemoteBoundary.cs:283-285` "To ALSO serve as a child, this class would need a second set of / operations ... and a child-shaped interface"; `Address.cs:148-175` "NO STANDALONE-ROOT OPERATIONS - and why that is a hard rule" | comment | Describe `DualUseEntity` as root-only; one statement on entity duality. |
| I-3 | `CD:413` "\| Validation messages \| No \| Rules re-run on deserialization \|"; `CD:470` "Rules run fresh"; `RuleBasics.cs:394` "(rules re-run, order preserved)" | `CD:485-491` "After deserialization, rules have not run yet"; `CommonGotchas.cs:44-47` "ResumeAllActions ... does NOT run rules." | comment | A is STALE: `ValidateBase.OnDeserialized` only resumes (`src\Neatoo\ValidateBase.cs:534-563`), and rule messages are serialized (`CustomPropertyType.cs:36-37`, `serializedRuleMessages`). Correct A. |
| I-4 | `RuleBasics.cs:214` "Default is 0.", `:218`, `:403` | `RuleBasics.cs:237` "default RuleOrder (1)", `:385` | comment | Default is 1 (`src\Neatoo\Rules\RuleBase.cs:163`). |
| I-5 | `AsyncRules.cs:259-260` "DESIGN DECISION: Rules can run in parallel." | `RuleBasics.cs:225` "Async rules: Run sequentially, not in parallel"; `:339` | comment | Sequential per trigger (`src\Neatoo\Rules\RuleManager.cs:609-619`); overlap only across separate property sets. |
| I-6 | `AllBaseClasses.cs:378-379` "Note: List bases don't have PauseAllActions - items are added directly" / "Rules on individual items run as they are added"; `EmployeeList.cs:53` | `AllBaseClasses.cs:337` "PauseAllActions for batch operations"; `OrderItemList.cs:45-47` "The list is paused by its own / factory operation while items are added" | comment | Lists have no `PauseAllActions` method but are paused by `FactoryStart` (`src\Neatoo\ValidateListBase.cs:563-565`); delete line 337 and the "rules run as they are added" sentence. |
| I-7 | Save routing consults `IsModified`: `CD:172-175`; `DD\DI\ServiceContracts.cs:146-157` "if (entity.IsModified) return await Update(entity);" / "return entity;  // Nothing to save"; `TwoGeneratorInteraction.cs:169-175`; `DR:68` | `SavePatterns.cs:26` "IsModified is NEVER consulted by routing."; `AllBaseClasses.cs:305-306` | comment | B matches the generated code (`SaveDemoFactory.g.cs:265-281`); rewrite A. |
| I-8 | `SavePatterns.cs:18-31` "verbatim from the emitted factory code" ... "a created-then-deleted entity routes to / [Delete], not to a silent no-op."; `SavePatterns.cs:147-149`; `AllBaseClasses.cs:317-318` | `AllBaseClasses.cs:175-177` "If IsNew=true: No persistence (never existed)"; `StateProperties.cs:274` "Save() will call [Delete] (if not IsNew)"; `CD:173` | comment | A is STALE against the emitted code: `if (target.IsDeleted) { if (target.IsNew) { return Task.FromResult(default(ISaveDemo)); } return LocalDelete(...)` (`SaveDemoFactory.g.cs:265-273`). Correct A, or add a version note if 1.9+ changed it. |
| I-9 | `FieldLevelAuthorization.cs:10-11` "the server decides / permissions during Fetch, and the client respects them." | `FieldLevelAuthorization.cs:46` "internal void Fetch(int id, bool canEditSalary, [Service] IFieldLevelAuthRepository repository)" (the caller of a `[Remote]` Fetch supplies its own permission) | code | Resolve the permission on the server from a `[Service]`; drop the parameter. Test comments at `DT\PropertyTests\FieldLevelAuthorizationTests.cs:38`, `:53`, `:68` follow. |
| I-10 | `LazyLoadProperty.cs:5` "EntityLazyLoad<T> properties are regular C# properties (not partial properties)." | `LazyLoadProperty.cs:9-10` "EntityLazyLoad<T> is declared as a partial property"; code at `:90` | comment | Delete line 5-7. |
| I-11 | Rule classes typed on the concrete entity: `RuleBasics.cs:110`, `:146`, `:191`, `:233`; `AsyncRules.cs:98`, `:164`, `:197`, `:232`; `ErrorPatterns.cs:205`; `CD:214`; `ServiceRegistration.cs:295` | Rule classes typed on the interface: `PX\...\UniqueNameRule.cs:8` "AsyncRuleBase<IPerson>"; `UniquePhoneNumberRule.cs:6` "RuleBase<IPersonPhone>" | code | S9 names properties, parameters and list type arguments but not rule targets; pick one. |
| I-12 | Hand-written full interfaces: `IOrderInterfaces.cs:73-82` (e.g. "int Id { get; }") and every `I*Interfaces.cs` in Design.Domain | `PX\...\Person.cs:9-13` "public partial interface IPerson : IEntityRoot" / "// Not Empty - Properties auto-generated by BaseGenerator (Roslyn)" (generated member is `Guid? Id { get; set; }`, `DomainModel.Person.g.cs:17`); `PropertyBasics.cs:128-130` "Emits `get;` only on the interface" | code + comment | State which form is recommended; say in `PropertyBasics.cs` that interface members are generated only for a `partial` interface. |
| I-13 | Consumer creates the child and adds it: `OrderItem.cs:145-146` "var item = orderItemFactory.Create("Widget", 5, 10.00m);" / "order.Items.Add(item);"; `Address.cs:63` | List domain method creates it: `PX\...\PersonPhoneList.cs:23-28` "public IPersonPhone AddPhoneNumber()" | code + comment | Follows from the S1-9 ruling. |
| I-14 | `Order.cs:87-94` "DID NOT DO THIS: Use NeatooPropertyChanged event subscription." | `PX\...\PersonPhoneList.cs:36-48` "protected override async Task HandleNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)" (string-matched property names, re-runs sibling rules) | code | Say whether the list override is the sanctioned way to re-validate siblings. |
| I-15 | `CreatePatterns.cs:194-206` "COMMON MISTAKE: Creating child collection without factory." | `PX\...\Person.cs:76-78` "public void Create([Service] IPersonPhoneList personPhoneModelList)" (a list instance from DI; `PersonPhoneList` has no `[Create]`) | code | `_personPhoneListFactory.Create()` and add `[Create]` to the list. |
| I-16 | `RemoteBoundary.cs:323-349` "Keep EF Core in a separate Infrastructure project." / "Domain.csproj - References Infrastructure privately" (`PrivateAssets="all"`) | Person: DomainModel references `Person.Dal` (interfaces and POCO entities, plain reference); EF lives only in `Person.Ef`, referenced by `Person.Server` | comment | Describe the layout the flagship uses, or change the flagship. |
| I-17 | `CLAUDE.md:115` "Only mock external dependencies** - Do not mock Neatoo interfaces or classes" | `PX\...\UnitTests\UniqueNameRuleTests.cs:8-9` "[KnockOff<IPerson>]" / "[KnockOff<IEntityProperty>]"; `UniquePhoneNumberRuleTests.cs:6-8`; `TestDoubles\TestPerson.cs:21-27` (overrides `IsSavable`, `RunRules`) | test | Say whether the rule covers application tests; if so rewrite the example tests on real objects. |
| I-18 | Root `[Remote]` operations are `internal` in all code (e.g. `Order.cs:128-130`; generated interface method is public, `OrderFactory.g.cs:20`) | Comments show them `public`: `RemoteBoundary.cs:37-38`; `AllBaseClasses.cs:559-560`, `:565-566`; `PropertyBasics.cs:214-215`, `:222-223`; `CD:135-136` (also missing `[Remote]`); `CommonGotchas.cs:259-260` | comment | `internal` in every comment sample; one sentence on why (visibility on the class versus the factory interface). |
| I-19 | `ApproveEmployee.cs:49` "[Execute] methods MUST return Task or Task<T>." | `ApproveEmployee.cs:186-188` "[Execute] methods should return Task<T> rather than Task." | comment | Keep one. |
| I-20 | `[Service]` on constructor parameters: `RemoteBoundary.cs:171`; `ServiceRegistration.cs:317`; `PX\...\PersonPhone.cs:26-28`; `PersonPhoneList.cs:18`; `DR:77` "Constructor `[Service]` injection" | No attribute: `PX\...\Person.cs:22-25` | code | State when the attribute is required (a `[Create]` constructor) and when it is noise. |
| I-21 | `Employee.cs:92-100` "// Action rule to set default values" (triggered by `IsActive`, guarded by `t.IsNew`) with `Employee.cs:113` "IsActive = true;" in `[Create]` | `CommonGotchas.cs:17-18` "the object is PAUSED. Rules do NOT fire during these methods." | code | The default never applies on create; set `HireDate` in `[Create]`. |
| I-22 | `FluentRules.cs:194-197` WRONG uses `AddValidation` | `FluentRules.cs:199-203` RIGHT switches to `AddAction` setting `t.HasWarning`; `:228` "Must list ALL properties involved" above a rule triggered only on `Sum` | comment | Keep it a validation with both triggers; fix the note. |
| I-23 | `Employee.cs:72-75` "This is a regular property, not partial - not tracked by Neatoo." (`FullName`) | `README.md:42-45`, `:59` (`FullName` is a partial property set by an `AddAction` rule) | code | Pick one; a non-partial computed property raises no change notification. |
| I-24 | `AllBaseClasses.cs:98-105` "COMMON MISTAKE: Creating services manually." | `PX\...\UnitTests\PersonTests.cs:26` "new TestPerson(new EntityBaseServices<Person>(null), ..." | test | Scope the comment to production code, or build test objects from DI. |
| I-25 | `ServiceRegistration.cs:239-241` "Generated/Neatoo.Generator/Neatoo.Factory/" | `TwoGeneratorInteraction.cs:280` "Generated files are in obj/Debug/{tfm}/Generated/ folders" | comment | Both projects set `CompilerGeneratedFilesOutputPath=Generated`; say so. |
| I-26 | `AsyncRules.cs:191` "They typically return Empty since they're not validation." | `AsyncRules.cs:213` "return None;" | comment | "None". |

---

## 3. STALE - does not match current behaviour

| Id | file:line | Quote | Verified against | Kind | Minimal fix |
|---|---|---|---|---|---|
| T-1 | `RemoteBoundary.cs:48-54`; `FetchPatterns.cs:31-36`; `TwoGeneratorInteraction.cs:27`, `:180-204`; `CD:160` | "var response = await httpClient.PostAsJsonAsync("/api/Employee/Fetch", request);" / "Generates HTTP endpoints for server" | `OrderFactory.g.cs:96-99` (`MakeRemoteDelegateRequest!.ForDelegate<IOrder>(typeof(FetchDelegate), [id], cancellationToken)`); one endpoint, `PX\Person.Server\Program.cs:46` (`app.MapPost("/api/neatoo", ...)`); `README.md:13` | comment | "The client factory sends the delegate type and arguments to the single `/api/neatoo` endpoint." |
| T-2 | `CreatePatterns.cs:37-50`; `TwoGeneratorInteraction.cs:128-178`; `RemoteBoundary.cs:49`, `:57`; `FetchPatterns.cs:32`, `:39`; `ServiceContracts.cs:141-144` | "public interface IGeneratorDemoFactory : IFactorySave<GeneratorDemo>" / "CreateDemo Create();" | `OrderFactory.g.cs:17-24`, `:289-302`: methods return the entity interface, take a `CancellationToken`, include `Save(target)`; the interface does not extend `IFactorySave`; the class is `internal`; `IFactorySave<T>.Save` returns `Task<IFactorySaveMeta?>` and has `CanSave` | comment | Paste the real generated interface. |
| T-3 | `AllBaseClasses.cs:75-82`, `:208-221`; `PropertyBasics.cs:26-50`; `TwoGeneratorInteraction.cs:83-118`; `ServiceContracts.cs:105-127`; `Employee.cs:30-33`; `CD:151` | "private IEntityProperty<string?> _nameProperty = null!;" / "_nameProperty = factory.CreateProperty<string?>("Name", this);" | `DemoEntity.g.cs:14-67`: `protected IValidateProperty<string?> NameProperty => (IValidateProperty<string?>)PropertyManager[nameof(Name)]!;` (also for EntityBase), setter assigns `.Value` and tracks `Task`, `PropertyManager.Register(factory.Create<string?>(this, nameof(Name)))`; `src\Neatoo\IPropertyFactory.cs` has only `Create` and `CreateEntityLazyLoad`. INTERNAL with `PropertyBasics.cs:139-154` and `CustomPropertyType.cs:121-130`, which show the real shape. | comment | Replace all six with the shape at `PropertyBasics.cs:141-154`. |
| T-4 | Awaiting the synchronous `Create`, 7 sites: `AllBaseClasses.cs:105`, `:254`, `:258`; `CommonGotchas.cs:254`; `ServiceRegistration.cs:154`; `SavePatterns.cs:38`, `:43` | "var entity = await factory.Create();" | `OrderFactory.g.cs:19` (`IOrder Create(CancellationToken cancellationToken = default);`). INTERNAL with `ServiceContracts.cs:231`, `CD:255`. Line 105 also calls it as a static (`DemoValueObjectFactory.Create()`). | comment | Remove `await`. |
| T-5 | `ServiceContracts.cs:228`; `ServiceRegistration.cs:190`, `:204`, `:208`, `:374` | "services.AddNeatooServices(typeof(Employee).Assembly);" | `src\Neatoo\AddNeatooServices.cs:18` (`AddNeatooServices(this IServiceCollection services, NeatooFactory portalServer, params Assembly[] assemblies)`). INTERNAL with `ServiceRegistration.cs:21`, `:50`. | comment | Add the `NeatooFactory` argument. |
| T-6 | `ServiceRegistration.cs:113` | "// services.AddHttpClient<INeatooHttpClient, NeatooHttpClient>();" | `PX\Person.App\Program.cs:20-22` (`AddKeyedScoped(RemoteFactoryServices.HttpClientKey, ...)`) | comment | Show the keyed `HttpClient` registration. |
| T-7 | `ServiceRegistration.cs:122-136`, `:235-237` | "services.AddTransient<ITFactory, TFactory>();  // Generated" / "AddNeatooRemoteFactory extension method" | `OrderFactory.g.cs:10`, `:304-310` (factories `AddScoped`, via a generated static `FactoryServiceRegistrar` plus `[assembly: NeatooFactoryRegistrar]`); `AddNeatooServices.cs:63` (`IPropertyInfoList<>` singleton), `:104` (`DefaultPropertyFactory<>`) | comment | Correct lifetimes and names. |
| T-8 | `ServiceContracts.cs:49-62`, `:204-208` | "public interface IValidateBaseServices<T> where T : ValidateBase<T>" (listing) / "- IPropertyFactory<Employee>" | `src\Neatoo\IValidateBaseServices.cs` (also has `ILogger<T> Logger`); INTERNAL with `CustomPropertyType.cs:29-31` ("EntityBaseServices<T> always constructs EntityPropertyFactory<T> itself and / never resolves IPropertyFactory<T>") | comment | Add `Logger`; fix the resolution chain. |
| T-9 | `RuleBasics.cs:19-22`, `:29-45`; `AsyncRules.cs:157-158`; `CD:212-225`; `DT\RuleTests\SyncRuleTests.cs:4` | ""RuleBase<T>" in documentation refers to AsyncRuleBase<T>." / "Even synchronous rules use the async signature." | `src\Neatoo\Rules\RuleBase.cs:327` (`public abstract class RuleBase<T> : AsyncRuleBase<T>`, synchronous `Execute`); used by `PX\...\UniquePhoneNumberRule.cs:6`, `:13` | comment + code | Document `RuleBase<T>` for synchronous rules; convert the synchronous demo rules. |
| T-10 | `RuleBasics.cs:258` | "RunRules(RunRulesFlag.Children): Run children's rules only" | `src\Neatoo\Rules\RunRulesFlag.cs` (None, NoMessages, Messages, NotExecuted, Executed, Self, All) | comment | Delete; list the real members. |
| T-11 | `ErrorPatterns.cs:60-62`, `:334-335`; `DR:112-113` | "Example: Calling Save() on a child entity (IsSavable=false)" / "await employee.Addresses[0].Save();  // Throws" | `src\Neatoo\Exceptions.cs:62-72` (`SaveFailureReason`: IsInvalid, NotModified, IsBusy, NoFactoryMethod; nothing child-specific). INTERNAL with `Address.cs:220` ("Does not compile") and `StateProperties.cs:252-254`. | comment | "Does not compile: the child interface has no Save()." |
| T-12 | `ErrorPatterns.cs:64`, `:337-338` | "3. NeatooConfigurationException (and subtypes)" / "FactoryException" | `src\Neatoo\Exceptions.cs:52` (`ConfigurationException`); neither `NeatooConfigurationException` nor `FactoryException` exists in `src\Neatoo`. INTERNAL with `ErrorPatterns.cs:316`. | comment | Use the real names. |
| T-13 | `OrderItemList.cs:277-278`; `AddressList.cs:200-201` | "// "Cannot add OrderItem to list: item belongs to aggregate 'Order'," / "//  but this list belongs to aggregate 'Order'."" | `src\Neatoo\EntityListBase.cs:246-250` ("item belongs to a different 'Order' instance than this list"); `DT\AggregateTests\AggregateBoundaryTests.cs:36` | comment | Quote the current message. |
| T-14 | `AllBaseClasses.cs:208-212`; `PropertyBasics.cs:350-353` | "IEntityProperty (extends IValidateProperty):" / "- LoadValue(value): Set WITHOUT modification tracking" | `src\Neatoo\IValidateProperty.cs:80` (`LoadValue` is on `IValidateProperty`) | comment | Move `LoadValue` to the `IValidateProperty` list. |
| T-15 | `AllBaseClasses.cs:159-161` | "Checking IsModified walks all children recursively" / "For deep graphs (>100 items), consider caching if called frequently" | `src\Neatoo\EntityListBase.cs:351` (`_cachedChildrenModified`); INTERNAL with `OrderItemList.cs:85-86` ("recalculates the cached / modified state") | comment | "Modified state is cached and updated on change." |
| T-16 | `CLAUDE.md:36`, `:187`; `DR:41`; `AllBaseClasses.cs:491` | "\| `Design.Infrastructure` \| Repository interface examples \|" | `src\Design\Design.Infrastructure\` holds only `Design.Infrastructure.csproj`; every repository interface is declared in Design.Domain | comment | Remove the project or say it is empty. |
| T-17 | `DR:26-48` | "│   ├── PropertySystem/        # Partial properties, Getter/Setter, LoadValue" | Tree omits `ErrorHandling/`, `CommonGotchas.cs`, `GotchaTests/`; Getter/Setter are obsolete | comment | Regenerate the tree. |
| T-18 | `CLAUDE.md:136-138` | "var propertyInfo = typeof(TestPoco).GetProperty("Name");" / "var property = new Property<string>(wrapper);" | No `Property<T>` type in `src\Neatoo`. INTERNAL: reflection in the "DO" example against `CD:281-285` ("Do NOT Use Reflection") | comment | Replace the example. |
| T-19 | `CLAUDE.md:151` | "public string Name { get => Getter<string>(); set => Setter(value); }" | `src\Neatoo\ValidateBase.cs:431`, `:447` (`[Obsolete("Use partial properties instead...")]`). INTERNAL with `PropertyBasics.cs:20-21` | comment | `public partial string Name { get; set; }`. |
| T-20 | `CLAUDE.md:174` | "Neatoo depends on **RemoteFactory** (`C:\src\neatoodotnet\RemoteFactory`)" | The checkout is under `source\repos` (memory note); the file tracks no "analyzed commits" | comment | Fix the path or drop the section. |
| T-21 | `README.md:29` | "unit tests, and a Blazor Server UI, see the [Person Example]" | `PX\Person.App\Program.cs:10` (`WebAssemblyHostBuilder`); hosted by `Person.Server` | comment | "Blazor WebAssembly UI". |
| T-22 | `PX\README.md:7-10`, `:20-53`, `:58-59` | "- **Person.Ef** - Entity Framework Core entities and DbContext" / "- SQL Server (LocalDB or full instance)" | Entities are in `Person.Dal`; `Person.Dal` and `Person.Server` are not listed; `Person.App` is the WASM client, not the EF startup project; the database is Sqlite (`PX\Person.Ef\PersonDbContext.cs:29`), created by `EnsureCreatedAsync` (`Person.Server\Program.cs:26`); projects target net10.0 | comment | Rewrite. |
| T-23 | `README.md:252` | "**UPDATED:** 2026-01-24" | Line 23 describes 0.31 behaviour | comment | Update or remove. |
| T-24 | `DD\IGotchaInterfaces.cs:17-18`, `:26-27`, `:87-88`, `:97`, `:103` | "Exposes RunRules and WaitForTasks because this demo's purpose is" | Both are already on `IValidateBase` through `IValidateMetaProperties` (`src\Neatoo\IMetaProperties.cs:25`, `:59`, `:67`) | code + comment | Remove the redeclarations (keep `PauseAllActions`). |
| T-25 | `RemoteBoundary.cs:144`, `:188-189` | "Client assemblies have stubs that throw "not registered"" | Internal operations are guarded in the generated factory: `if (!NeatooRuntime.IsServerRuntime) throw new InvalidOperationException("Server-only method called in non-server runtime.");` (`OrderItemFactory.g.cs:129-130`) | comment | Describe the guard. |
| T-26 | `ApproveEmployee.cs:72-75`, `:89-90` | "interface IApproveEmployeeFactory {" / "factory.Approve(employeeId, approverName)" | `ApproveEmployeeFactory.g.cs:15`: a nested `public delegate ... Approve(...)`; no factory interface. INTERNAL with `ApproveEmployee.cs:86-87`. | comment | "Generates the delegate `ApproveEmployee.Approve`; inject it and call it." |
| T-27 | `CD:95` | "Constructor-injected services survive serialization round-trips (available on client and server)." | Services are never serialized; each tier resolves them from its own container | comment | "Constructor-injected services are resolved on both tiers, so they must be registered on both." |
| T-28 | `CLAUDE.md:209`; `CD:39`; `DR:34` | "- Value objects: `Design.Domain/ValueObjects/`" | The folder holds only `EmployeeList` and `EmployeeListItem`, both read models (S8-1). Design.Domain has no value-object example and no plain-`[Factory]` read model. | comment | Add `ReadModels\`; fix the three pointers. |
| T-29 | `README.md:215` | "- ValidateBase inheritance for validation, business rules, change tracking, and property metadata" | `AllBaseClasses.cs:44-53` (no modification tracking on ValidateBase) | comment | Drop "change tracking". |

---

## 4. UNRULED - normative claims the settled list does not decide

| Id | file:line | Verbatim | Why it needs a ruling | Kind |
|---|---|---|---|---|
| U-1 | `Address.cs:90-93` (same shape: `OrderItem.cs:82`, `SavePatterns.cs:317`, `FetchPatterns.cs:258`, `PX\...\PersonPhone.cs:62`) | "this [Fetch] takes already-loaded row data / and is internal and non-[Remote]" | S2 says Fetch parameters "identify it, e.g. a key". Every child `[Fetch]` here takes the row values or the EF entity, loaded by the list. | code + comment |
| U-2 | `CommonGotchas.cs:34` (code `:97-104`, table `:492-493`); `AggregateCoverageGapTests.cs:178-179` | "RIGHT: Call RunRules at the end of your factory method." / "This is why a factory / method that must not produce invalid objects calls RunRules() itself." | S3 says no rules run in a factory operation; S10 says RunRules is for forcing a re-run. Whether a `[Create]` should force rules is open. | code + comment |
| U-3 | `README.md:105` | "The same rules execute again on the server during persistence." | Nothing re-runs rules on the server automatically (I-3). The only site that does is `Person.cs:101`, `:129`, which S5 removes. Whether the server re-validates, and how a failure surfaces, is open. | comment |
| U-4 | `PX\Person.App\Pages\Home.razor:186-187` | "await Person.WaitForTasks();" / "await Person.RunRules(RunRulesFlag.NotExecuted);" | Pre-save orchestration in Razor so untouched required fields show errors. S10 and S13 do not say where this belongs. | code |
| U-5 | `ErrorPatterns.cs:97-102` | "- If expected (multiple users editing): Validation with message" | A concurrency conflict is found in `[Update]` on the server. As "validation" it can only be check-and-return, which S5 forbids. | comment |
| U-6 | `PX\...\PersonPhone.cs:25-26`; `FetchPatterns.cs:76-78` | "[Create]" / "public PersonPhone([Service] IUniquePhoneNumberRule uniquePhoneNumberRule," ; "[Create] / CONSTRUCTORS (read-style lifecycle - the constructor body runs before / the factory pause exists)" | `[Create]` on a constructor: not paused, so S3 does not hold there. The flagship uses it; Design never does. | code + comment |
| U-7 | `Home.razor:173`, `:201`; `SavePatterns.cs:27-28` | "await PersonFactory.Save(Person!, CancellationToken.None);" ; "(EntityBase.Save() won't / invoke it — IsSavable gates that — but a direct factory.Save(target) will)" | Two ways to save a root with different gating. Design says `await entity.Save()` (`AllBaseClasses.cs:260`); the flagship UI calls the factory. | code + comment |
| U-8 | `CLAUDE.md:93`; `CD:196` | "(they often need the parent entity or parent ID)" | What a child `[Insert]` receives is shown three ways: parent id plus `[Service]` repository (Design), the child's own EF entity (Person), and "the parent entity" (this text). S4 does not choose. | comment |

---

## 5. Save-cascade shape of every aggregate example

| Aggregate | Root Insert/Update | Who iterates children | Child `[Insert]` receives | Deleted children | Root `[Delete]` |
|---|---|---|---|---|---|
| Order (`DD\Aggregates\OrderAggregate\`) | `itemsFactory.Save(Items!, Id)` (`Order.cs:167`, `:201`) | The list's `[Update]`, over `this.Union(DeletedList)`, guard `IsNew \|\| IsModified` (`OrderItemList.cs:98-114`) | `(int orderId, [Service] IOrderRepository repository)` (`OrderItem.cs:125`) | List calls `repository.DeleteItem`; no child `[Delete]` | Root iterates `Items` and calls `repository.DeleteItem` (`Order.cs:211-214`) |
| Employee (`DD\Entities\`) | `addressListFactory.Save(Addresses!, Id)` (`Employee.cs:179`, `:215`) | The list (`AddressList.cs:109-124`) | `(int employeeId, [Service] IEmployeeRepository repository)` (`Address.cs:133`) | List calls `repository.DeleteAddress` | Root iterates, guard `!address.IsNew` (`Employee.cs:231-237`) |
| SaveAggregateDemo (`SavePatterns.cs`) | `itemsFactory.Save(Items!, Id)` (`:233`, `:262`) | The list (`:361-376`) | `(int parentId, [Service] ISaveAggregateRepository repository)` (`:325`) | List calls `repository.DeleteChild` | Root iterates (`:272-275`) |
| FetchWithChildrenDemo (`FetchPatterns.cs`) | Empty bodies (`:223-233`) | Nobody; the list has no `[Update]` (`:280-284` says so) | `([Service] IFetchChildRepository repository)`, no parent id (`:264-265`) | Child has a `[Delete]` (`:270-271`), never reached | Empty |
| Gotcha2Parent, Gotcha5Parent (`CommonGotchas.cs`) | Empty bodies (`:168-178`, `:436-446`) | Nobody | Parameterless `Insert()` (`:205-206`, `:468-469`) | Child has a `[Delete]` (`:211-212`, `:474-475`), never reached | Empty |
| ModificationStateDemo (`StateProperties.cs`) | Empty bodies (`:190-200`) | Nobody; the child is a single property | Child has only `[Create]` | n/a | Empty |
| DemoEntityList (`AllBaseClasses.cs:467-485`) | n/a (list only) | Nobody | Items are root-shaped (`[Remote]` Insert/Update/Delete, S9-4) | n/a | n/a |
| Person (`PX\Person.DomainModel\`) | `_personPhoneListFactory.Save(phoneList, personEntity.Phones, cancellationToken)` after `LoadAsync()`, then one `SaveChangesAsync` (`Person.cs:114-120`, `:144-150`) | The list's `[Update]`, over `this.Union(DeletedList)`, no modified guard (`PersonPhoneList.cs:80-102`) | `(PersonPhoneEntity personPhoneEntity)`: the child's EF row, already created and attached by the list; no `[Service]` (`PersonPhone.cs:68`) | List removes the EF entity from the parent's collection (`PersonPhoneList.cs:94-97`); no child `[Delete]` | `DeleteAllPersons` (`Person.cs:159`) |

Observations:
- Every working cascade has the list iterate, never the parent. That part agrees with S4.
- No working cascade reaches a child's own `[Delete]`. The three demos that declare a child `[Delete]` never call it.
- No example saves a single (non-list) child entity through its factory. `Gotcha5Parent` and `ModificationStateDemo` hold one and leave the root bodies empty.
- `[Insert]`/`[Update]` that validate and return or throw: only `Person.cs:101-106` and `:129-134` (S5-1). `Person.cs:137-140` throws `KeyNotFoundException` when the row is missing, which is an application failure and conforms. No `[Insert]`/`[Update]` in Design.Domain validates.

---

## 6. Passages that already state the settled position (templates for the fixes)

| Point | file:line |
|---|---|
| S1 | `CommonGotchas.cs:263-265` ("[Remote] means "this is an entry point from client to server.""); `OrderItem.cs:95-100`; `FetchPatterns.cs:251-254` |
| S2 | `FetchPatterns.cs:183-187`; `OrderItemList.cs:50-56`; `Address.cs:95-98` |
| S3 | `OrderItem.cs:70-73`; `FetchPatterns.cs:115-126`; `Address.cs:100-101` |
| S4 | `Order.cs:182-188`; `Employee.cs:197-202`; `RemoteBoundary.cs:232-237` |
| S5 | `ErrorPatterns.cs:18-49`; `RuleBasics.cs:183-185` |
| S6 | `PX\Person.DomainModel\UniqueNameRule.cs:8-16` |
| S9 | `IOrderInterfaces.cs:1-62`; `StateProperties.cs:284-291` |
| S10 | `AllBaseClasses.cs:580-589`; `StateProperties.cs:256-265` |
| S11 | `CreatePatterns.cs:74-81`; `StateProperties.cs:149-155`; `AllBaseClasses.cs:183-187`; `OrderItem.cs:156-159` |
| S13 | `PX\Person.DomainModel\PersonPhoneList.cs:23-34` |

---

## 7. Counts

Rows by primary class:

| Class | Rows |
|---|---|
| CONTRADICTS (S1-S13) | 56 |
| INTERNAL | 26 |
| STALE | 29 |
| UNRULED | 8 |
| Total | 119 |

CONTRADICTS rows per settled point:

| S1 | S2 | S3 | S4 | S5 | S6 | S7 | S8 | S9 | S10 | S11 | S12 | S13 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 10 | 4 | 11 | 7 | 4 | 4 | 5 | 3 | 4 | 0 | 2 | 2 | 0 |

Largest grouped rows by edit sites: S3-1 (87 code sites, 19 files), S8-2 (27 comment sites), S1-9 (19 declarations), S12-1 (13), S4-2 (3 code and 10 comment sites), S1-5 (10), S1-4 (9), T-4 (7), S9-2 (7).

Rows by kind: comment 75, code 22, code + comment 17, test 5.

---

## 8. Coverage

Read whole:
- `src\Design\CLAUDE-DESIGN.md`, `src\Design\README.md`
- All 38 hand-written `.cs` files under `src\Design\Design.Domain\` (no `.md` files exist there)
- `src\Design\Design.Infrastructure\`: contains only the `.csproj` (read); no source files
- `src\Examples\Person\`: all of `Person.DomainModel\*.cs` (9), `Person.App\Pages\Home.razor`, `App.razor`, `Layout\MainLayout.razor`, `_Imports.razor`, `Person.App\Program.cs`, `Person.Server\Program.cs`, `Person.Dal\*.cs` (3), `Person.Ef\PersonDbContext.cs`, all 9 files of `Person.DomainModel.Tests` (TestDoubles, UnitTests, Integration Tests), `README.md`
- Root `CLAUDE.md`, `README.md`

Read in part, by design:
- `src\Design\Design.Tests\` (24 files): every test method name and every comment line was extracted and read (710 lines); test bodies were opened only at `EntityListBaseTests.cs:97-110` and `StatePropertyTests.cs:47-70`. This matches the brief ("only where a test name or comment asserts guidance") but it is not a whole-file read.
- `Generated\` folders: 6 files opened for verification (`OrderFactory`, `OrderItemFactory`, `ApproveEmployeeFactory`, `SaveDemoFactory` routing block, `DemoEntity.g.cs`, Person `UniqueNameFactory` / `PersonFactory` Insert block / interface headers).

Not read:
- `src\Examples\Person\Person.Ef\Migrations\*.cs` (3 tool-generated files)
- `.csproj` files beyond their reference lists; `appsettings*.json`; `Person.Server.http`; `wwwroot`

Out of scope but implicated by a finding: `src\samples\ReadmeSamples.cs` (source of the README snippets, S9-2 and S1-3) and `Directory.Packages.props` (the 1.6.1 pin, S7).
