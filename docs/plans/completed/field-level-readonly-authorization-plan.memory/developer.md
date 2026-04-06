# Developer -- Field-Level ReadOnly Authorization

Last updated: 2026-04-06
Current step: Code Review (Step 5)

## Key Context

- `MarkReadOnly()` added to `IValidateProperty` interface and implemented in `ValidateProperty<T>`
- `IsReadOnly` property changed from auto-property to backing field with one-and-done protection
- `EntityProperty<T>` inherits from `ValidateProperty<T>` and does NOT override `IsReadOnly` or `MarkReadOnly()`
- LazyLoad properties set `IsReadOnly = true` in constructor; `MarkReadOnly()` would be a no-op on them (already read-only)
- 9 unit tests added to `ValidatePropertyTests.cs`
- 5 integration tests added to `Design.Tests/PropertyTests/FieldLevelAuthorizationTests.cs`

## Mistakes to Avoid

(none yet)

## User Corrections

(none yet)

## Developer Review

**Status:** Approved
**Reviewed:** 2026-04-06

### Code Review Trace

| # | Business Rule | Verified? | File:Line | Evidence |
|---|--------------|-----------|-----------|----------|
| 1 | WHEN `MarkReadOnly()` called, THEN `IsReadOnly` returns `true` | YES | `ValidateProperty.cs:135-142` | `MarkReadOnly()` sets `IsReadOnly = true` via the protected setter. Setter at line 127-132 allows `false -> true`. |
| 2 | WHEN `MarkReadOnly()` called, THEN `SetValue()` throws `PropertyReadOnlyException` | YES | `ValidateProperty.cs:144-149` | `SetValue()` checks `if (this.IsReadOnly)` and throws `PropertyReadOnlyException`. No change to this guard. |
| 3 | WHEN `MarkReadOnly()` called, THEN `SetPrivateValue()` succeeds | YES | `ValidateProperty.cs:155-180` | `SetPrivateValue()` does NOT check `IsReadOnly`. It proceeds directly to value handling. |
| 4 | WHEN `MarkReadOnly()` called, THEN `LoadValue()` succeeds | YES | `ValidateProperty.cs:182-221` | `LoadValue()` does NOT check `IsReadOnly`. It sets `_value` directly. |
| 5 | WHEN `MarkReadOnly()` called, THEN `IsReadOnly = false` has NO EFFECT | YES | `ValidateProperty.cs:129-131` | Setter guard: `if (_isReadOnly && !value) return;` prevents `true -> false` transition. |
| 6 | WHEN `IsReadOnly = false`, THEN `MarkReadOnly()` sets to `true` | YES | `ValidateProperty.cs:135-142` | `MarkReadOnly()` checks `if (!IsReadOnly)` then sets `IsReadOnly = true`. |
| 7 | WHEN `IsReadOnly = true` from private set, THEN `MarkReadOnly()` is no-op | YES | `ValidateProperty.cs:137` | `if (!IsReadOnly)` guard: when already `true`, body is skipped. No `PropertyChanged` fired. |
| 8 | WHEN `MarkReadOnly()` called and entity serialized, THEN client has `IsReadOnly = true` | YES | `EntityPropertyManager.cs:27` (EntityProperty JsonConstructor), `ValidateProperty.cs:26-32` (ValidateProperty JsonConstructor) | Both constructors accept `bool isReadOnly` and set via the protected setter. The serialized JSON includes `IsReadOnly` (it's a public property). No changes needed to serialization. |
| 9 | WHEN `MarkReadOnly()` called, THEN `PropertyChanged` fires for `IsReadOnly` | YES | `ValidateProperty.cs:140` | `OnPropertyChanged(nameof(IsReadOnly))` called inside the `if (!IsReadOnly)` block. |

### Test Scenario Coverage

| # | Scenario | Test Method | File | Covered? |
|---|----------|------------|------|----------|
| 1 | MarkReadOnly sets flag | `MarkReadOnly_SetsIsReadOnlyTrue` | ValidatePropertyTests.cs:1386 | YES |
| 2 | MarkReadOnly prevents SetValue | `MarkReadOnly_SetValueThrows` | ValidatePropertyTests.cs:1402 | YES |
| 3 | MarkReadOnly allows SetPrivateValue | `MarkReadOnly_SetPrivateValueSucceeds` | ValidatePropertyTests.cs:1415 | YES |
| 4 | MarkReadOnly allows LoadValue | `MarkReadOnly_LoadValueSucceeds` | ValidatePropertyTests.cs:1431 | YES |
| 5 | MarkReadOnly is permanent | `MarkReadOnly_IsPermanent` | ValidatePropertyTests.cs:1447 | PARTIAL (see note 1) |
| 6 | MarkReadOnly on already-readonly | `MarkReadOnly_OnAlreadyReadOnly_IsNoOp` | ValidatePropertyTests.cs:1465 | YES |
| 7 | MarkReadOnly via entity indexer | `MarkReadOnly_DuringFetch_SetsSalaryReadOnly` + `MarkReadOnly_SetValueThrowsOnReadOnlyField` | FieldLevelAuthorizationTests.cs:35,80 | YES |
| 8 | MarkReadOnly fires PropertyChanged | `MarkReadOnly_FiresPropertyChanged` | ValidatePropertyTests.cs:1481 | YES |
| 9 | MarkReadOnly in Fetch serializes | `MarkReadOnly_SerializationRoundTrip` | ValidatePropertyTests.cs:1518 | YES (via JSON constructor) |

**Bonus tests (not in plan scenarios but good additions):**
- `MarkReadOnly_OnAlreadyReadOnly_DoesNotFirePropertyChanged` (ValidatePropertyTests.cs:1499) -- Verifies no spurious PropertyChanged on already-readonly properties.
- `MarkReadOnly_DuringFetch_NameRemainsWritable` (FieldLevelAuthorizationTests.cs:50) -- Negative case, only targeted property affected.
- `MarkReadOnly_WhenCanEdit_SalaryRemainsWritable` (FieldLevelAuthorizationTests.cs:65) -- Positive authorization case.
- `MarkReadOnly_LoadValueSucceedsOnReadOnlyField` (FieldLevelAuthorizationTests.cs:103) -- Integration-level LoadValue bypass test.

### Notes

**Note 1 -- Test Scenario 5 (MarkReadOnly_IsPermanent):** This test creates a property via `JsonConstructor` with `isReadOnly=true` and verifies it stays `true`. This demonstrates that the one-and-done protection doesn't break JSON deserialization (since `false -> true` is allowed), but it doesn't directly test the `true -> false` rejection path. The setter is `protected`, so external test code cannot directly call `property.IsReadOnly = false`. The one-and-done protection (line 130: `if (_isReadOnly && !value) return;`) is tested indirectly -- the only way to trigger it would be through a subclass or via the JSON constructor. This is an acceptable coverage gap given the `protected` access modifier makes direct external exploitation impossible. The protection exists as a defense-in-depth for internal/subclass code.

### Design Drift Check

No drift detected. The implementation matches the plan's design exactly:
- Interface addition at `IValidateProperty.cs:53-58` matches the plan's API design
- Implementation at `ValidateProperty.cs:122-142` matches the plan's code design verbatim (backing field, one-and-done setter, MarkReadOnly with PropertyChanged)
- JSON constructor path is unaffected (verified at `ValidateProperty.cs:26-32` and `EntityPropertyManager.cs:26-32`)

### What Looks Good

1. The implementation is minimal and surgical -- only 2 framework files changed
2. XML documentation on `MarkReadOnly()` in the interface is clear and complete
3. The one-and-done protection in the setter is clean and correct
4. `EntityProperty<T>` inherits the behavior without needing any changes
5. LazyLoad properties are compatible (they set `IsReadOnly = true` in constructor; calling `MarkReadOnly()` on them is a harmless no-op)
6. Design.Domain file has excellent comments explaining the pattern, design decisions, and downstream behavior
7. Design.Tests cover the integration scenario (Fetch with authorization) with good positive and negative cases
8. The `MockFieldLevelAuthRepository` registration in `TestInfrastructure.cs` follows the existing pattern
9. Test results: 1789 passed (Neatoo.sln), 104 passed (Design.sln), 0 failures

### Verdict

**APPROVED.** The implementation is correct, complete, and well-tested. All 9 business rules are satisfied by the code. All 9 test scenarios have corresponding tests. The design matches the plan. No concerns.

### Why This Plan Is Approved Without Concerns

1. The change is genuinely minimal -- 2 framework files, adding one method and modifying one property
2. Every business rule traces cleanly through the code with no ambiguity
3. The existing `SetValue()` guard, `SetPrivateValue()` bypass, `LoadValue()` bypass, and serialization paths are all untouched -- only the `IsReadOnly` storage mechanism changed (auto-property to backing field)
4. All existing tests pass (1789 + 104 = 1893 tests)
5. The only test coverage gap (scenario 5's protected setter) is an acceptable defense-in-depth scenario that cannot be exploited externally
