# Explore AOT Compliance for Neatoo

**Priority:** Low
**Status:** Not Started
**Created:** 2026-01-04

## Overview

Evaluate what would be required to make Neatoo compatible with .NET Native AOT compilation. AOT compilation eliminates the JIT compiler at runtime, which can improve startup time and reduce memory footprint, but restricts certain runtime features.

## Background

.NET AOT has restrictions that may affect Neatoo:
- No runtime code generation (Reflection.Emit)
- Limited reflection capabilities
- All types must be statically analyzable
- JSON serialization requires source generators

Neatoo already uses source generation extensively (RemoteFactory, Neatoo.Generator), which is AOT-friendly. However, there may be runtime reflection usage that would need evaluation.

## Tasks

- [ ] Audit Neatoo for reflection usage patterns
  - [ ] `Type.GetType()` calls
  - [ ] `Activator.CreateInstance()` calls
  - [ ] `MethodInfo.Invoke()` calls
  - [ ] Generic type construction at runtime
- [ ] Review JSON serialization approach
  - [ ] Current System.Text.Json usage
  - [ ] Evaluate if source-generated serializers are needed
- [ ] Check DI container compatibility
  - [ ] Microsoft.Extensions.DependencyInjection AOT support
  - [ ] Service registration patterns
- [ ] Test with `<PublishAot>true</PublishAot>`
  - [ ] Create test console app using Neatoo
  - [ ] Attempt AOT publish
  - [ ] Document warnings and errors
- [ ] Evaluate RemoteFactory AOT compatibility
  - [ ] Check if generated code is AOT-safe
  - [ ] Review any dynamic delegate creation
- [ ] Document findings and estimate effort
- [ ] Decide: pursue AOT compliance or defer

## Notes

- This is exploratory work to understand feasibility and scope
- AOT compliance may require breaking changes
- Consider if AOT is valuable for typical Neatoo use cases (Blazor WASM, ASP.NET Core)
- Blazor WASM uses its own compilation model, not native AOT

## References

- [.NET Native AOT Deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [AOT Compatibility Analyzers](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/warnings/overview)
