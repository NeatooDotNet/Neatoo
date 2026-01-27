# Best Practices and Gotchas

This document captures important patterns and behaviors when working with Neatoo.

## Commands

Commands are static partial classes with `[Factory]` and `[Execute]` methods:

```csharp
[Factory]
public static partial class SendEmailCommand
{
    [Execute]
    internal static async Task<bool> _SendEmail(
        string to,
        string subject,
        string body,
        [Service] IEmailService emailService)
    {
        await emailService.SendAsync(to, subject, body);
        return true;
    }
}
```

The source generator creates a delegate (`SendEmailCommand.SendEmail`) that you inject via DI.

---

## Read-Only Patterns

Use `ValidateBase` with only `[Fetch]` methods (no `[Insert]`/`[Update]`/`[Delete]`):

```csharp
[Factory]
public partial class EmployeeSummary : ValidateBase<EmployeeSummary>
{
    public EmployeeSummary(IValidateBaseServices<EmployeeSummary> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string Name { get; set; }

    // Fetch-only = effectively read-only
    [Fetch]
    public void Fetch(int id, string name)
    {
        Id = id;
        Name = name;
    }
}

public class EmployeeSummaryList : ValidateListBase<EmployeeSummary> { }
```

---

## Authorization

Use a single `[AuthorizeFactory<>]` attribute with a combined interface:

```csharp
public interface ISecureEntityAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetch();

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanSave();
}

public class SecureEntityAuthorization : ISecureEntityAuthorization
{
    private readonly IPrincipal _principal;
    private readonly IFeatureFlagService _featureFlags;

    public SecureEntityAuthorization(IPrincipal principal, IFeatureFlagService featureFlags)
    {
        _principal = principal;
        _featureFlags = featureFlags;
    }

    // Combine multiple checks in one method
    public bool CanFetch()
    {
        var isAuthenticated = _principal.Identity?.IsAuthenticated ?? false;
        var featureEnabled = _featureFlags.IsEnabled("SecureEntityAccess");
        return isAuthenticated && featureEnabled;
    }

    public bool CanSave() => _principal.IsInRole("Admin");
}

[Factory]
[AuthorizeFactory<ISecureEntityAuthorization>]
public partial class SecureEntity : EntityBase<SecureEntity>
```

---

## NeatooPropertyChanged Event

Use single argument (event args) and return Task:

```csharp
entity.NeatooPropertyChanged += (e) =>
{
    Console.WriteLine(e.PropertyName);
    return Task.CompletedTask;
};
```

---

## ChangeReason

Use `Reason` property with `Load` or `UserEdit` values:

```csharp
var reason = e.Reason;
Assert.AreEqual(ChangeReason.Load, reason);
```

The `ChangeReason` enum has two values:
- `ChangeReason.UserEdit` - Normal property setter assignment
- `ChangeReason.Load` - Data loading via `LoadValue()`

---

## Project Configuration

Include generator references with proper configuration:

```xml
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>

<ItemGroup>
    <!-- Analyzers as separate references -->
    <ProjectReference Include="path/to/Neatoo.BaseGenerator.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
    <ProjectReference Include="path/to/Neatoo.Analyzers.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />

    <!-- Main Neatoo reference -->
    <ProjectReference Include="path/to/Neatoo.csproj" />

    <!-- Exclude generated files from compilation (already included by generator) -->
    <Compile Remove="$(CompilerGeneratedFilesOutputPath)/**/*.cs" />
</ItemGroup>
```

---

## Validation Rules Are Async

Await `RunRules()` before checking validity:

```csharp
employee.Name = "";
await employee.RunRules(RunRulesFlag.All);
Assert.IsFalse(employee["Name"].IsValid);
```

---

## Testing

Use real Neatoo factories and mock only external dependencies:

```csharp
services.AddNeatooServices(NeatooFactory.Logical, typeof(Employee).Assembly);
services.AddScoped<IEmployeeRepository, MockEmployeeRepository>();

var factory = serviceProvider.GetRequiredService<IEmployeeFactory>();
var employee = factory.Create();  // Real Neatoo object
```

---

## Quick Reference

| Pattern | How To |
|---------|--------|
| Commands | Static class with `[Factory]` and `[Execute]` |
| Read-only models | `ValidateBase` with only `[Fetch]` methods |
| Authorization | Single `[AuthorizeFactory<IInterface>]` attribute |
| Property change events | `(e) => { return Task.CompletedTask; }` |
| Change reason property | `e.Reason` |
| Change reason for loads | `ChangeReason.Load` |
| Check validity | `await RunRules()` first |
| Testing | Use real factories, mock external deps |
