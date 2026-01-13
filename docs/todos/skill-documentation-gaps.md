# Skill Documentation Gaps

Skill documentation gaps identified during NeatooATM implementation (2026-01-11).

Source: `NeatooATM/docs/todos/neatoo-blazor-implementation-plan.md`

---

## Task List

### Client-Server Configuration (client-server.md)

- [ ] **Server Configuration API Mismatch**: Documentation shows `AddNeatooServices(NeatooFactory.Server, assembly)` but actual API in Neatoo.RemoteFactory.AspNetCore 10.6.0 is:
  - `builder.Services.AddNeatooAspNetCore(assembly)` - for DI registration
  - `app.UseNeatoo()` - for middleware/endpoint configuration

- [ ] **Client Configuration API Mismatch**: Documentation doesn't clearly show the client-side API. Actual API is:
  - `builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, assembly)` - for DI registration
  - `builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) => new HttpClient { ... })` - for HttpClient configuration

- [ ] **AddNeatooServices vs AddNeatooRemoteFactory/AddNeatooAspNetCore**: Critical gap:
  - The skill shows `AddNeatooRemoteFactory` for client and `AddNeatooAspNetCore` for server
  - **Neither registers core Neatoo services** like `IEntityBaseServices<T>`, `IRuleManager<T>`, etc.
  - **Both client AND server must use `AddNeatooServices(NeatooFactory.Remote/Server, assembly)`**
  - `AddNeatooServices` internally calls `AddNeatooRemoteFactory` AND registers all core services
  - For server, additionally register `HandleRemoteDelegateRequest` manually:
    ```csharp
    builder.Services.AddScoped<HandleRemoteDelegateRequest>(sp =>
        LocalServer.HandlePortalRequest(sp, null));
    ```
  - Without `AddNeatooServices`, domain entities that extend `EntityBase<T>` will fail with:
    `Unable to resolve service for type 'IEntityBaseServices<T>'`

### MudNeatoo Components

- [ ] **MudNeatoo Component Namespace Gap**: Documentation shows `@using Neatoo.Blazor.MudNeatoo` but components are in sub-namespaces:
  - `@using Neatoo.Blazor.MudNeatoo.Components` - for MudNeatooTextField, MudNeatooNumericField
  - `@using Neatoo.Blazor.MudNeatoo.Validation` - for NeatooValidationSummary

### Entity Operations

- [ ] **Delete Pattern Not Documented**: The skill doesn't clearly document the delete pattern:
  - `entity.Delete()` - mark entity for deletion
  - `await factory.Save(entity)` - persist the deletion
  - There is no `factory.Delete(entity)` method

### Command Pattern (factories.md, rules.md)

- [ ] **Command Properties with `private set` Don't Serialize**: Important distinction:
  - `EntityBase<T>` classes use `partial` properties -> PropertyManager handles state transfer -> `private set` works fine
  - **Command classes** (plain `[Factory]` classes without EntityBase) use standard JSON serialization -> `private set` fails

  ```csharp
  // BROKEN for Commands - client will see default value (false)
  [Factory]
  internal partial class CheckUniqueIdCommand : ICheckUniqueIdCommand
  {
      public bool IsAvailable { get; private set; }  // Won't deserialize
  }

  // WORKS for Commands - use public setter
  [Factory]
  internal partial class CheckUniqueIdCommand : ICheckUniqueIdCommand
  {
      public bool IsAvailable { get; set; }  // Deserializes correctly
  }
  ```

  **Fix:** For Command result properties, use `public set` or consider `[JsonInclude]` attribute.

- [ ] **Document generated delegate naming convention**:
  - Command method `_MethodName` generates delegate `ClassName.MethodName` (underscore stripped)
  - Example: `CheckEmailUnique._IsUnique` -> `CheckEmailUnique.IsUnique` delegate

- [ ] **Add cross-reference between factories.md and rules.md**:
  - factories.md shows Command definition
  - rules.md shows Command consumption in async rules
  - Need explicit link showing these are parts of one pattern

- [ ] **Add "Commands are NOT interfaces" callout**:
  - Commands generate delegates, not factory interfaces
  - Don't create `ICheckUniqueIdCommand` - inject `CheckUniqueId.IsUnique` delegate instead
  - Contrast with EntityBase which DOES use interface-first design

- [ ] **Add complete end-to-end example in one place**:
  - Command definition (static class with `[Execute]`)
  - What gets generated (delegate type)
  - Rule injection (constructor parameter)
  - Rule usage (await delegate call)

- [ ] **Document instance-based Command pitfall**:
  - If you create a class-based Command with properties, setters must be `public` for serialization
  - Prefer static Command pattern to avoid serialization concerns entirely

---

## Context

These gaps were discovered while implementing the NeatooATM demonstration application. Each gap required either:
- Web searching the Neatoo.RemoteFactory GitHub repo
- Examining local Neatoo source code at `~/neatoodotnet/Neatoo`
- Trial and error during implementation

The goal is to update skill documentation so future implementations don't require these workarounds.
