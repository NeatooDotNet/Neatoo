# Strict Mode Reference

[Home](../SKILL.md) > [References](./) > Strict Mode

Strict mode controls how stubs handle unconfigured members. When enabled, unconfigured method calls throw `StubException` instead of returning default values.

---

## Per-Stub Strict Mode

Enable strict mode for individual stubs:

**Via attribute property** (default for all instances):

<!-- snippet: strict-attribute-property -->
```cs
[KnockOff(Strict = true)]
public partial class StrictUserRepoStub : IStrictUserRepo { }
```
<!-- endSnippet -->

**Via generic attribute**:

<!-- snippet: strict-inline-attribute -->
```cs
[KnockOff<IStrictUserRepo>(Strict = true)]
public partial class StrictMyTests { }
```
<!-- endSnippet -->

**Via constructor** (inline stubs only):

<!-- snippet: strict-constructor -->
```cs
// Via constructor (inline stubs only)
var stub = new Stubs.IStrictUserRepo(strict: true);
```
<!-- endSnippet -->

**Via fluent extension method**:

<!-- snippet: strict-fluent-extension -->
```cs
// Via fluent extension method
var stub = new StrictUserRepoStub().Strict();
```
<!-- endSnippet -->

**Via property at runtime**:

<!-- snippet: strict-property-runtime -->
```cs
// Via property at runtime
stub.Strict = true;
```
<!-- endSnippet -->

---

## Assembly-Wide Strict Mode

Apply `[assembly: KnockOffStrict]` to make all stubs in an assembly default to strict mode:

<!-- snippet: strict-assembly-declaration -->
```cs
// In AssemblyInfo.cs or any file in your test project
// [assembly: KnockOffStrict]
```
<!-- endSnippet -->

All stubs now default to strict mode:

<!-- snippet: strict-assembly-usage -->
```cs
[KnockOff<IStrictUserService>]
public partial class StrictUserTests { }
```
<!-- endSnippet -->

Unconfigured calls throw StubException:

<!-- snippet: strict-assembly-throws -->
```cs
// Unconfigured calls throw StubException
// service.GetUser(42);  // Throws StubException!
```
<!-- endSnippet -->

---

## Opting Out

Individual stubs can opt out when assembly-wide strict mode is enabled:

<!-- snippet: strict-assembly-declaration -->
```cs
// In AssemblyInfo.cs or any file in your test project
// [assembly: KnockOffStrict]
```
<!-- endSnippet -->

**Opt out via attribute property**:

<!-- snippet: strict-opt-out-attribute -->
```cs
// Opt out via attribute property
[KnockOff<IStrictLegacyService>(Strict = false)]
public partial class StrictLegacyTests { }
```
<!-- endSnippet -->

**Opt out via constructor** (inline stubs only):

<!-- snippet: strict-opt-out-constructor -->
```cs
// Opt out via constructor (inline stubs only)
var stub = new Stubs.IStrictLegacyService(strict: false);
```
<!-- endSnippet -->

**Opt out at runtime**:

<!-- snippet: strict-opt-out-runtime -->
```cs
// Opt out at runtime
stub.Strict = false;
```
<!-- endSnippet -->

---

## Precedence

Settings are resolved in this order (highest to lowest):

1. **Runtime:** `stub.Strict = false` or `stub.Strict()`
2. **Constructor:** `new Stubs.IService(strict: false)` (inline stubs only)
3. **Attribute:** `[KnockOff(Strict = false)]` or `[KnockOff<T>(Strict = false)]`
4. **Assembly:** `[assembly: KnockOffStrict]`
5. **Default:** `false` (non-strict)

---

## When to Use

**Use assembly-wide strict mode when:**
- You want strict behavior as the default for your entire test project
- You want to enforce explicit stub configuration as a coding standard
- You prefer opting out of strict mode rather than opting in

**Use per-stub strict mode when:**
- Only certain tests require strict verification
- You're migrating an existing test project incrementally
- Different tests have different strictness requirements

---

**UPDATED:** 2026-02-05
