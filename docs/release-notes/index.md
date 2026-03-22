# Release Notes

## Current Version

**Neatoo 0.23.2** (2026-03-21)

---

## Highlights

New features, breaking changes, and significant bug fixes.

| Version | Date | Type | Summary |
|---------|------|------|---------|
| [0.23.2](v0.23.2.md) | 2026-03-21 | Patch | Migrate converters to NeatooReferenceResolver.Current API |
| [0.23.1](v0.23.1.md) | 2026-03-21 | Patch | RemoteFactory 0.23.1; shared reference handling, factory fetch NRE fix |
| [0.23.0](v0.23.0.md) | 2026-03-15 | **Breaking** | Revert LazyLoad.Value auto-trigger; Value is passive read; GetAwaiter removed |
| [0.22.0](v0.22.0.md) | 2026-03-14 | **Breaking** | LazyLoad unified into PropertyManager; partial properties, look-through subclasses, no reflection |
| [0.21.0](v0.21.0.md) | 2026-03-13 | Feature | LazyLoad.Value auto-triggers load (reverted in 0.23.0); WaitForTasks awaits LazyLoad children |
| [0.20.1](v0.20.1.md) | 2026-03-13 | Bug Fix | WaitForTasks crash with LazyLoad remote fetch |
| [0.20.0](v0.20.0.md) | 2026-03-12 | Feature | Nullable LazyLoad support |
| [0.19.0](v0.19.0.md) | 2026-03-08 | Dependency | RemoteFactory 0.21.0; [Remote] methods must be internal |
| [0.18.0](v0.18.0.md) | 2026-03-07 | Feature | IL trimming support for Blazor WASM consumers |
| [0.17.0](v0.17.0.md) | 2026-03-07 | **Breaking** | IEntityRoot interface; LazyLoad state propagation fix |
| [0.16.0](v0.16.0.md) | 2026-03-06 | Bug Fix | LazyLoad properties survive client-server serialization |
| [0.15.1](v0.15.1.md) | 2026-03-03 | Bug Fix | IsValid/IsSavable stale during RunRules() in factory operations |
| [0.15.0](v0.15.0.md) | 2026-03-02 | Dependency | RemoteFactory updated to 0.16.1 |
| [0.14.2](v0.14.2.md) | 2026-03-02 | Bug Fix | DI version pinning for net10.0 WASM consumers |
| [0.14.1](v0.14.1.md) | 2026-03-02 | Bug Fix | Blazor WASM publish fix for DI version mismatch |
| [0.14.0](v0.14.0.md) | 2026-03-01 | Dependency | KnockOff and RemoteFactory re-versioned to 0.x.x |
| [0.13.0](v0.13.0.md) | 2026-03-01 | Bug Fix | Stale meta property caches after ResumeAllActions() |
| [0.12.0](v0.12.0.md) | 2026-02-26 | Bug Fix | Dictionary properties survive JSON bridge round-trip |
| [0.11.0](v0.11.0.md) | 2026-01-18 | Feature | LazyLoad&lt;T&gt; wrapper type for async lazy loading |
| [0.10.0](v0.10.0.md) | 2026-01-16 | Feature | Source-generated property backing fields, lazy loading |
| [0.7.1](v0.7.1.md) | 2026-01-11 | Patch | List IsValid/IsBusy/IsModified caching optimization |
| [0.7.0](v0.7.0.md) | 2026-01-11 | Feature | Stable rule identification via source generation |
| [0.6.3](v0.6.3.md) | 2026-01-10 | Patch | Generator unit test infrastructure |
| [0.6.2](v0.6.2.md) | 2026-01-10 | Patch | Parameterless constructor for EntityBaseServices<T> (unit testing) |
| [0.6.1](v0.6.1.md) | 2026-01-09 | Patch | KnockOff 10.12.0 upgrade, Moq removed from test projects |
| [0.6.0](v0.6.0.md) | 2026-01-05 | Feature | RemoteFactory upgrade to 10.5.0 with CancellationToken support |
| [0.5.0](v0.5.0.md) | 2026-01-04 | Feature | CancellationToken support for async operations |
| [0.4.0](v0.4.0.md) | 2026-01-04 | Feature | Collapse Base layer - simplified inheritance hierarchy |
| [0.3.0](v0.3.0.md) | 2026-01-03 | Feature | Root property, ContainingList, Delete/Remove consistency |
| [0.2.0](v0.2.0.md) | 2026-01-02 | Feature | Stubbable public interfaces |
| [0.1.1](v0.1.1.md) | 2026-01-01 | Feature | Record support for Value Objects |

---

## All Releases

| Version | Date |
|---------|------|
| [0.23.2](v0.23.2.md) | 2026-03-21 |
| [0.23.1](v0.23.1.md) | 2026-03-21 |
| [0.23.0](v0.23.0.md) | 2026-03-15 |
| [0.22.0](v0.22.0.md) | 2026-03-14 |
| [0.21.0](v0.21.0.md) | 2026-03-13 |
| [0.20.1](v0.20.1.md) | 2026-03-13 |
| [0.20.0](v0.20.0.md) | 2026-03-12 |
| [0.19.0](v0.19.0.md) | 2026-03-08 |
| [0.18.0](v0.18.0.md) | 2026-03-07 |
| [0.17.0](v0.17.0.md) | 2026-03-07 |
| [0.16.0](v0.16.0.md) | 2026-03-06 |
| [0.15.1](v0.15.1.md) | 2026-03-03 |
| [0.15.0](v0.15.0.md) | 2026-03-02 |
| [0.14.2](v0.14.2.md) | 2026-03-02 |
| [0.14.1](v0.14.1.md) | 2026-03-02 |
| [0.14.0](v0.14.0.md) | 2026-03-01 |
| [0.13.0](v0.13.0.md) | 2026-03-01 |
| [0.12.0](v0.12.0.md) | 2026-02-26 |
| [0.11.0](v0.11.0.md) | 2026-01-18 |
| [0.10.0](v0.10.0.md) | 2026-01-16 |
| [0.7.1](v0.7.1.md) | 2026-01-11 |
| [0.7.0](v0.7.0.md) | 2026-01-11 |
| [0.6.3](v0.6.3.md) | 2026-01-10 |
| [0.6.2](v0.6.2.md) | 2026-01-10 |
| [0.6.1](v0.6.1.md) | 2026-01-09 |
| [0.6.0](v0.6.0.md) | 2026-01-05 |
| [0.5.0](v0.5.0.md) | 2026-01-04 |
| [0.4.0](v0.4.0.md) | 2026-01-04 |
| [0.3.0](v0.3.0.md) | 2026-01-03 |
| [0.2.0](v0.2.0.md) | 2026-01-02 |
| [0.1.1](v0.1.1.md) | 2026-01-01 |

---

## Version Naming

| Change Type | Version Bump |
|-------------|--------------|
| Breaking changes | Major (0.x → 1.0) |
| New features | Minor (0.1 → 0.2) |
| Bug fixes | Patch (0.1.0 → 0.1.1) |

---

## Links

- [NuGet Package](https://www.nuget.org/packages/Neatoo)
- [GitHub Repository](https://github.com/NeatooDotNet/Neatoo)
