# Switch KnockOff and RemoteFactory to v0.x.x Versions

**Status:** Complete
**Priority:** High
**Created:** 2026-03-01
**Last Updated:** 2026-03-01

---

## Problem

KnockOff and RemoteFactory were published under v10.x.x version numbers, which is inappropriate for libraries that haven't reached v1.0 maturity. All v10.x.x NuGet packages have been unlisted and new v0.x.x versions published. Neatoo still references the old v10.x.x versions.

## Solution

Update `Directory.Packages.props` to reference the latest v0.x.x versions:

| Package | Current (unlisted) | Target |
|---------|-------------------|--------|
| Neatoo.RemoteFactory | 10.14.0 | 0.16.0 |
| Neatoo.RemoteFactory.AspNetCore | 10.14.0 | 0.16.0 |
| KnockOff | 10.26.0 | 0.45.0 |

---

## Plans

None - straightforward version update.

---

## Tasks

- [x] Update `Directory.Packages.props` with new versions
- [x] Build solution
- [x] Fix breaking changes from KnockOff API updates
- [x] Run all tests
- [x] Verify no breaking changes from version switch

---

## Progress Log

### 2026-03-01
- Created todo
- Identified current versions and latest v0.x.x targets
- Updated Directory.Packages.props with new versions
- Built solution and identified 42 build errors in Person.DomainModel.Tests
- All errors were KnockOff API changes (no RemoteFactory breaking changes):
  - Property interceptor `OnGet` renamed to `Get()` method
  - Method interceptor `OnCall` renamed to `Return()` for value-returning methods
  - Method interceptor `OnCall` renamed to `Call()` for void methods
  - Delegate interceptor `Interceptor.OnCall =` changed to `Interceptor.Return()` method
  - Verification `Times` renamed to `Called`
- Fixed all 4 affected test files:
  - `UniqueNameRuleTests.cs`
  - `UniquePhoneNumberRuleTests.cs`
  - `UniquePhoneTypeRuleTests.cs`
  - `PersonTests.cs`
- Both `src/Neatoo.sln` and `src/Design/Design.sln` build successfully
- All 2058 tests pass (0 failures)

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass
- [x] Design project builds successfully

**Verification results:**
- Build: PASS (0 errors, 17 warnings)
- Design Build: PASS (0 errors, 0 warnings)
- Tests: PASS (2058 passed, 1 skipped, 0 failed)

---

## Results / Conclusions

Successfully updated all three NuGet packages to v0.x.x versions. The only breaking changes were in KnockOff's API (no RemoteFactory breaking changes). The KnockOff API changes were:

1. **Property getters**: `stub.Prop.OnGet = () => value` changed to `stub.Prop.Get(() => value)`
2. **Value-returning methods**: `stub.Method.OnCall(callback)` changed to `stub.Method.Return(callback)`
3. **Void methods**: `stub.Method.OnCall(callback)` changed to `stub.Method.Call(callback)`
4. **Delegate interceptors**: `stub.Interceptor.OnCall = callback` changed to `stub.Interceptor.Return(callback)`
5. **Verification**: `Times.Once`/`Times.Never` changed to `Called.Once`/`Called.Never`
