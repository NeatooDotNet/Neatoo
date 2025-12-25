# Neatoo.RemoteFactory Coding Standards Documentation

This document provides comprehensive coding standards for the Neatoo.RemoteFactory solution, derived from analysis of configuration files, project structures, and actual code patterns.

---

## Table of Contents

1. [Solution-Wide Build Properties](#solution-wide-build-properties)
2. [Code Style Rules (.editorconfig)](#code-style-rules-editorconfig)
3. [Analyzer Packages and Rules](#analyzer-packages-and-rules)
4. [Suppressed Warnings and Exceptions](#suppressed-warnings-and-exceptions)
5. [Project-Specific Configurations](#project-specific-configurations)
6. [Project Structure Patterns](#project-structure-patterns)
7. [Naming Conventions](#naming-conventions)
8. [Code Patterns and Best Practices](#code-patterns-and-best-practices)
9. [Areas for Improvement](#areas-for-improvement)
10. [Applying These Standards to New Projects](#applying-these-standards-to-new-projects)

---

## Solution-Wide Build Properties

Located in `src/Directory.Build.props`, these settings apply to ALL projects in the solution:

### Core Settings

| Property | Value | Description |
|----------|-------|-------------|
| `AnalysisMode` | `all` | Enables all .NET code analysis rules by default |
| `LangVersion` | `preview` | Uses the latest C# preview features |
| `Nullable` | `enable` | Nullable reference types are enabled solution-wide |
| `ImplicitUsings` | `enable` | Enables implicit global usings |
| `TargetFrameworks` | `net9.0` | Targets .NET 9.0 |
| `TreatWarningsAsErrors` | `True` | All warnings are treated as errors (strict quality) |
| `ManagePackageVersionsCentrally` | `true` | Uses Central Package Management |

### NuGet Security Auditing

```xml
<NuGetAudit>true</NuGetAudit>
<NuGetAuditMode>all</NuGetAuditMode>
<NuGetAuditLevel>critical</NuGetAuditLevel>
```

The solution enforces critical-level security auditing for all NuGet packages.

### Package Metadata

```xml
<Authors>Keith Voels</Authors>
<Copyright>Copyright 2025</Copyright>
<FileVersion>9.19.1</FileVersion>
<PackageVersion>9.19.1</PackageVersion>
```

---

## Code Style Rules (.editorconfig)

Located in `src/.editorconfig`, defining formatting and style enforcement:

### Indentation and Formatting

```ini
[*.cs]
indent_style = tab
indent_size = 3
end_of_line = crlf
```

**Key Style Decision:** Uses **tabs** (not spaces) with a tab width of 3 spaces.

### Expression-Bodied Members (All Enforced as Errors)

```ini
csharp_style_expression_bodied_accessors = true:error
csharp_style_expression_bodied_constructors = true:error
csharp_style_expression_bodied_indexers = true:error
csharp_style_expression_bodied_local_functions = true:error
csharp_style_expression_bodied_methods = true:error
csharp_style_expression_bodied_operators = true:error
csharp_style_expression_bodied_properties = true:error
```

**Standard:** Expression-bodied members are REQUIRED where applicable.

### Var Usage (Enforced as Errors)

```ini
csharp_style_var_elsewhere = true:error
csharp_style_var_for_built_in_types = true:error
csharp_style_var_when_type_is_apparent = true:error
```

**Standard:** Always use `var` - explicit type declarations are prohibited.

### Namespace Style

```ini
csharp_style_namespace_declarations = file_scoped:error
```

**Standard:** File-scoped namespaces are REQUIRED.

### This Qualification (Enforced as Errors)

```ini
dotnet_style_qualification_for_event = true:error
dotnet_style_qualification_for_field = true:error
dotnet_style_qualification_for_method = true:error
dotnet_style_qualification_for_property = true:error
```

**Standard:** `this.` prefix is REQUIRED for all member access.

### Pattern Matching (Enforced as Errors)

```ini
csharp_style_pattern_matching_over_as_with_null_check = true:error
csharp_style_pattern_matching_over_is_with_cast_check = true:error
csharp_style_throw_expression = true:error
csharp_style_inlined_variable_declaration = true:error
csharp_style_conditional_delegate_call = true:error
```

### Collection and Object Initialization

```ini
dotnet_style_collection_initializer = true:error
dotnet_style_object_initializer = true:error
dotnet_style_explicit_tuple_names = true:error
dotnet_style_null_propagation = true:error
dotnet_style_predefined_type_for_locals_parameters_members = true:error
```

### Async Method Naming Convention (NOT ENFORCED - Exception to Standard)

The Microsoft/.NET standard recommends async methods end with "Async" suffix. **This solution intentionally does NOT enforce this convention.**

The `.editorconfig` explicitly sets this rule to `none`:

```ini
# Async methods - "Async" suffix is NOT enforced (exception to Microsoft standard)
# Rationale: Async suffix adds noise in modern codebases where async is pervasive
dotnet_naming_rule.async_methods_end_in_async.severity = none
```

**Rationale:** The async suffix adds noise without significant value in modern codebases where async is pervasive.

Additionally, `IDE1006` in `NoWarn` (Directory.Build.props) helps suppress any remaining naming rule violations.

---

## Analyzer Packages and Rules

### Primary Analyzers

From `Directory.Packages.props`:

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.CodeAnalysis.Analyzers` | 4.14.0 | Roslyn analyzer development |
| `Microsoft.CodeAnalysis.NetAnalyzers` | 9.0.0 | .NET code quality analyzers |

### NUnit Analyzers (All Elevated to Error)

The solution elevates all NUnit analyzer warnings to errors:

```ini
dotnet_diagnostic.NUnit2001.severity=error  # through NUnit2043
```

This ensures test code quality is enforced strictly.

---

## Suppressed Warnings and Exceptions

### Solution-Wide Suppressions (Directory.Build.props)

```xml
<NoWarn>CA1861, CA1865, CA1510, IDE0021, IDE0022, IDE0023, IDE1006, CA1050, CA1822</NoWarn>
```

| Rule | Description | Reason |
|------|-------------|--------|
| CA1861 | Avoid constant arrays as arguments | Performance optimization conflicts |
| CA1865 | Use char overload | String/char overload preference |
| CA1510 | Use ArgumentNullException.ThrowIfNull | Legacy compatibility |
| IDE0021 | Use expression body for constructors | Conflicts with expression-bodied rule |
| IDE0022 | Use expression body for methods | Conflicts with expression-bodied rule |
| IDE0023 | Use expression body for operators | Conflicts with expression-bodied rule |
| IDE1006 | Naming rule violation | Custom naming conventions (includes async suffix) |
| CA1050 | Declare types in namespaces | Source generator output |
| CA1822 | Mark members as static | Flexibility for future changes |

**Special Note on Async Suffix (IDE1006):** The `IDE1006` suppression covers general naming rule violations. The `.editorconfig` also explicitly sets the async suffix naming rule to `severity = none` (lines 107-112).

### EditorConfig Suppressions

```ini
# CA1002: Do not expose generic lists
# TODO: I really should NOT suppress this, but it's too much of a PITA
dotnet_diagnostic.CA1002.severity = none

# CA1014: Mark assemblies with CLSCompliant
dotnet_diagnostic.CA1014.severity = none

# CA1030: Use events where appropriate
dotnet_diagnostic.CA1030.severity = none

# CA1303: Do not pass literals as localized parameters
dotnet_diagnostic.CA1303.severity = none

# CA1515: Consider making public types internal
dotnet_diagnostic.CA1515.severity = none

# CA1812: Internal class never instantiated (Program class issue)
dotnet_diagnostic.CA1812.severity = none

# CA2007: Consider calling ConfigureAwait
dotnet_diagnostic.CA2007.severity = none

# RS1024: Compare symbols correctly (Source generator specific)
dotnet_diagnostic.RS1024.severity = none

# IDE0055: Formatting rule (VS 2022 Preview issue)
dotnet_diagnostic.IDE0055.severity = none

# IDE0130: Namespace doesn't match folder structure
dotnet_diagnostic.IDE0130.severity = none
```

---

## Project-Specific Configurations

### Library Projects (Core NuGet Packages)

**Projects:** `RemoteFactory.csproj`, `RemoteFactory.AspNetCore.csproj`

```xml
<AllowUnsafeBlocks>true</AllowUnsafeBlocks>
<ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
<DebugType>embedded</DebugType>
<EmbedUntrackedSources>true</EmbedUntrackedSources>
<EnforceExtendedAnalyzerRules>false</EnforceExtendedAnalyzerRules>
<GenerateDocumentationFile>false</GenerateDocumentationFile>
<GeneratePackageOnBuild>true</GeneratePackageOnBuild>
```

### Source Generator Project

**Project:** `RemoteFactory.FactoryGenerator.csproj`

```xml
<TargetFramework>netstandard2.0</TargetFramework>
<EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
```

**Note:** Source generators target `netstandard2.0` for maximum compatibility and have stricter analyzer rules.

### Test Projects

**Projects:** `FactoryGeneratorTests.csproj`, `RemoteFactory.AspNet.Tests.csproj`

```xml
<IsPackable>false</IsPackable>
<IsTestProject>true</IsTestProject>
<AnalysisMode>default</AnalysisMode>  <!-- Relaxed from "all" -->
<NoWarn>..., IDE0044, CS4014</NoWarn>  <!-- Additional suppressions -->
<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
<CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
```

Test projects have relaxed analysis (`default` instead of `all`) and additional warning suppressions:

| Rule | Description | Reason |
|------|-------------|--------|
| IDE0044 | Add readonly modifier | Test flexibility |
| CS4014 | Unawaited async call | Test patterns |
| CS0051 | Inconsistent accessibility | Test infrastructure |

### Example/Demo Projects

**Projects:** `Person.Server.csproj`, `PersonApp.csproj`, `Person.Ef.csproj`

```xml
<TargetFramework>net8.0</TargetFramework>  <!-- May use older TFM -->
<NoWarn>CA1062, IDE0022</NoWarn>  <!-- EF-specific suppressions -->
```

Example projects may target older frameworks and have domain-specific suppressions.

---

## Project Structure Patterns

### Solution Organization

```
RemoteFactory/
├── .github/                    # GitHub workflows and templates
├── src/
│   ├── Directory.Build.props   # Solution-wide build settings
│   ├── Directory.Packages.props # Central Package Management
│   ├── .editorconfig           # Code style rules
│   ├── Neatoo.RemoteFactory.sln
│   │
│   ├── RemoteFactory/          # Core library
│   ├── RemoteFactory.AspNetCore/ # ASP.NET Core integration
│   ├── RemoteFactory.FactoryGenerator/ # Roslyn source generator
│   │
│   ├── Tests/
│   │   ├── FactoryGeneratorTests/      # Unit tests
│   │   ├── FactoryGeneratorSandbox/    # Development sandbox
│   │   ├── RemoteFactory.AspNet.Tests/ # Integration tests
│   │   └── RemoteFactory.AspNetCore.TestServer/ # Test server
│   │
│   └── Examples/
│       ├── Person/             # Full example application
│       │   ├── Person.DomainModel/
│       │   ├── Person.Ef/
│       │   ├── Person.Server/
│       │   └── PersonApp/      # Blazor client
│       └── HorseFarm/          # Additional example
│
├── LICENSE
├── README.md
└── neatoo_icon.png
```

### Naming Patterns

| Type | Pattern | Example |
|------|---------|---------|
| Core Library | `{ProductName}` | `RemoteFactory` |
| Integration Library | `{ProductName}.{Platform}` | `RemoteFactory.AspNetCore` |
| Source Generator | `{ProductName}.{Generator}Generator` | `RemoteFactory.FactoryGenerator` |
| Test Projects | `{LibraryName}Tests` or `{LibraryName}.Tests` | `FactoryGeneratorTests` |
| Domain Model | `{Domain}.DomainModel` | `Person.DomainModel` |
| EF/Data Layer | `{Domain}.Ef` | `Person.Ef` |
| Server/API | `{Domain}.Server` | `Person.Server` |
| Client App | `{Domain}App` | `PersonApp` |

---

## Naming Conventions

### Observed in Code

#### Classes and Interfaces

```csharp
// Interface with I prefix
public interface IPersonModel { }

// Implementation without I
internal partial class PersonModel : IPersonModel { }

// Factory naming: I{Type}Factory
public interface IShowcaseSaveFactory { }
internal class ShowcaseSaveFactory { }
```

#### Methods

```csharp
// Async suffix NOT required (exception to Microsoft standard)
public async Task<bool> Fetch([Service] IPersonContext personContext)

// Factory operations as verbs
public void Insert() { }
public void Update() { }
public void Delete() { }
public void Create() { }
public void Fetch() { }
```

#### Properties

```csharp
// PascalCase with explicit this. qualification
public string? FirstName { get; set { field = value; this.OnPropertyChanged(); } }
public bool IsDeleted { get; set; }
public bool IsNew { get; set; } = true;
```

#### Parameters

```csharp
// camelCase
public void Insert([Service] IService service) { }
public Task Fetch([Service] IPersonContext personContext)
```

### Attribute Conventions

```csharp
// Factory attributes on classes
[Factory]
[AuthorizeFactory<IPersonModelAuth>]
internal partial class PersonModel { }

// Operation attributes on methods
[Remote]
[Fetch]
public async Task<bool> Fetch() { }

// Service injection attribute
public void Insert([Service] IService service) { }
```

---

## Code Patterns and Best Practices

### 1. File-Scoped Namespaces

```csharp
namespace Neatoo.RemoteFactory;

public class Authorized { }
```

### 2. This Qualification

```csharp
public class Authorized
{
    public bool HasAccess { get; init; }

    public Authorized(bool hasAccess)
    {
        this.HasAccess = hasAccess;  // Always use this.
    }
}
```

### 3. Nullable Reference Types

```csharp
public string? Message { get; init; }  // Nullable
public bool HasAccess { get; init; }   // Non-nullable

// Null checking
ArgumentNullException.ThrowIfNull(result, nameof(result));
```

### 4. Expression-Bodied Members

```csharp
// Properties
public bool IsSave => this.CallMethod.IsSave;

// Methods (when simple)
public static implicit operator bool(Authorized result) =>
    result?.HasAccess ?? false;
```

### 5. Async/Await Patterns

```csharp
// Async suffix is NOT required (exception to Microsoft standard)
public async Task<bool> Fetch([Service] IPersonContext personContext)
{
    var personEntity = await personContext.Persons.FirstOrDefaultAsync(x => x.Id == 1);
    if (personEntity == null)
    {
        return false;
    }
    this.MapFrom(personEntity);
    return true;
}
```

**Note:** The "Async" suffix convention is intentionally NOT enforced in this solution.

### 6. Collection Expressions (.NET 8+)

```csharp
// Modern collection initialization
List<FactoryOperation> defaultFactoryOperations = [];
this.UsingStatements = new EquatableArray<string>([.. usingStatements.Distinct()]);
```

### 7. Pattern Matching

```csharp
if (methodSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not BaseMethodDeclarationSyntax methodSyntax)
{
    continue;
}
```

### 8. Records for DTOs

```csharp
internal record TypeInfo
{
    public string Name { get; }
    public bool IsPartial { get; }
    // ...
}

internal sealed record MethodParameterInfo
{
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
}
```

### 9. Primary Constructors (Where Applicable)

```csharp
internal abstract class FactoryMethod(string serviceType, string implementationType)
{
    public string ServiceType { get; protected set; } = serviceType;
    public string ImplementationType { get; set; } = implementationType;
}
```

---

## Areas for Improvement

### 1. CA1002 Suppression Acknowledged

The codebase explicitly acknowledges this as technical debt:

```ini
# CA1002: Do not expose generic lists
# TODO: I really should NOT suppress this, but
# it's too much of a PITA at the present moment.
dotnet_diagnostic.CA1002.severity = none
```

**Recommendation:** Gradually replace `List<T>` in public APIs with `IReadOnlyList<T>` or `ICollection<T>`.

### 2. Inconsistent Target Frameworks

- Core libraries target `net9.0`
- Example projects target `net8.0`
- Source generator targets `netstandard2.0`

**Consideration:** While the source generator must remain `netstandard2.0`, example projects could be upgraded to match core libraries.

### 3. Documentation Generation Disabled

```xml
<GenerateDocumentationFile>false</GenerateDocumentationFile>
```

**Recommendation:** Enable XML documentation for public APIs to improve IntelliSense and API documentation.

### 4. Unsafe Code Allowed

```xml
<AllowUnsafeBlocks>true</AllowUnsafeBlocks>
```

**Consideration:** Review if unsafe code is actually used; if not, this could be removed for additional safety.

### 5. ConfigureAwait Not Enforced

```ini
# CA2007: Consider calling ConfigureAwait
dotnet_diagnostic.CA2007.severity = none
```

**Note:** This is intentional for application code but should be reconsidered for library code targeting broader consumption.

### 6. Inconsistent Indentation in Some Files

Some source generator output and code samples show mixed indentation. The source generator's output formatting should be reviewed.

---

## Applying These Standards to New Projects

### Quick Start: Directory.Build.props

```xml
<Project>
  <PropertyGroup>
    <AnalysisMode>all</AnalysisMode>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>preview</LangVersion>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <NoWarn>CA1861, CA1865, CA1510, IDE0021, IDE0022, IDE0023, IDE1006, CA1050, CA1822</NoWarn>
    <NuGetAudit>true</NuGetAudit>
    <NuGetAuditMode>all</NuGetAuditMode>
    <NuGetAuditLevel>critical</NuGetAuditLevel>
    <Nullable>enable</Nullable>
    <TargetFrameworks>net9.0</TargetFrameworks>
    <TreatWarningsAsErrors>True</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

### Quick Start: .editorconfig

Copy the full `.editorconfig` from `src/.editorconfig`, which includes:

- Tab indentation (size 3)
- Expression-bodied members required
- `var` required everywhere
- File-scoped namespaces required
- `this.` qualification required
- All NUnit rules elevated to errors
- Async suffix naming rule explicitly disabled (set to `none`)

### Project Templates

**For Library Projects:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <DebugType>embedded</DebugType>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
  </PropertyGroup>
</Project>
```

**For Test Projects:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <AnalysisMode>default</AnalysisMode>
    <NoWarn>$(NoWarn);IDE0044;CS4014</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" PrivateAssets="all" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
</Project>
```

**For Source Generators:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <TargetFrameworks></TargetFrameworks>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

---

## Summary

The Neatoo.RemoteFactory solution employs a strict, modern C# coding standard characterized by:

1. **Strict Quality Enforcement** - All warnings as errors, comprehensive analysis
2. **Modern C# Features** - Preview language features, .NET 9.0, nullable reference types
3. **Consistent Style** - File-scoped namespaces, expression-bodied members, `var` everywhere
4. **Explicit Self-Reference** - `this.` prefix required for all member access
5. **Async Suffix NOT Enforced** - Exception to Microsoft standard (see IDE1006 in NoWarn)
6. **Central Package Management** - Coordinated dependency versions
7. **Security Focused** - NuGet package auditing enabled
8. **Test Quality** - NUnit rules elevated to errors

The standards balance strictness with pragmatism, acknowledging certain suppressions as intentional trade-offs while maintaining high overall code quality.
