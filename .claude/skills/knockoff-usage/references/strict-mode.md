# Strict Mode Reference

[Home](../SKILL.md) > [References](./) > Strict Mode

Strict mode controls how stubs handle unconfigured members. When enabled, unconfigured method calls throw `StubException` instead of returning default values.

---

## Per-Stub Strict Mode

Enable strict mode for individual stubs:

```cs
// Via attribute property (default for all instances)
[KnockOff(Strict = true)]
public partial class UserRepoStub : IUserRepo { }

// Via generic attribute
[KnockOff<IUserRepo>(Strict = true)]
public partial class MyTests { }

// Via constructor (inline stubs only)
var stub = new Stubs.IUserRepo(strict: true);

// Via fluent extension method
var stub = new UserRepoStub().Strict();

// Via property at runtime
stub.Strict = true;
```

---

## Assembly-Wide Strict Mode

Apply `[assembly: KnockOffStrict]` to make all stubs in an assembly default to strict mode:

```cs
// In AssemblyInfo.cs or any file in your test project
[assembly: KnockOffStrict]

// All stubs now default to strict mode
[KnockOff<IUserService>]
public partial class UserTests { }

// Unconfigured calls throw StubException
var stub = new UserTests.Stubs.IUserService();
IUserService service = stub;
service.GetUser(42);  // Throws StubException!
```

---

## Opting Out

Individual stubs can opt out when assembly-wide strict mode is enabled:

```cs
[assembly: KnockOffStrict]

// Opt out via attribute property
[KnockOff<ILegacyService>(Strict = false)]
public partial class LegacyTests { }

// Opt out via constructor (inline stubs only)
var stub = new UserTests.Stubs.IUserService(strict: false);

// Opt out at runtime
stub.Strict = false;
```

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

**UPDATED:** 2026-01-27
