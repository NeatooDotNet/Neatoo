# Test Review Record — ISNEW-007 — 2026-08-21

**Reviewer:** test-reviewer agent. **Gate: PASSES after the fix loop.**

## Findings and dispositions

**must-cover (plan-related)**

1. **Parent-id propagation into child inserts was unobservable.** `MockEmployeeRepository`
   discarded the `employeeId` argument, so swapping the two statements in `Employee.Insert`
   (write own Id, then delegate) would write every child with `employeeId = 0` and leave all
   three tests green — a silent-orphan failure mode. **Fixed:** the mock now records
   `InsertAddressParentIds`, and both save tests assert children were inserted against the
   employee's generated id.

**should-cover (plan-related)**

2. **Unmodified-existing-child skip unpinned** — `GetAddresses` returned exactly two rows and
   the test consumed both (one modified, one removed), so no clean survivor existed and
   replacing the guard with a bare `else` would still pass. **Fixed:** a third fetched
   address is left untouched, and the exact-match `UpdatedAddressIds` assertion plus an
   explicit `DoesNotContain` now bite.
3. **Address's standalone-root role had zero execution.** **Resolved by removal, not by
   testing** — the code review (veto 1/2) established the role was both unreachable
   (`IAddress : IEntityBase`) and harmful (any parent-less `[Remote]` op emits a public
   `Save(IAddress)` that bypasses the aggregate). The role, `IAddressOnlyRepository`, and its
   mock were deleted; see `007-code-review.md`.

**nice-to-have (plan-related)**

4. `_nextEmployeeId` seeded at 1 could collide with fetched ids in a future multi-root test.
   **Fixed:** seeded at 100, matching `MockOrderRepository`.
5. No second-save idempotency assertion; defensive `!IsNew` delete branch untested. Accepted —
   state-level assertions already cover the underlying risk, and the branch is unreachable by
   design (same disposition as ISNEW-001).

**Tech debt queued to ISNEW-006:** Employee.Update header-guard positive direction,
Employee.Delete coverage (it also ignores `DeletedList`), Employee/Address validation-rule
coverage, `Address.Create(...)` overload never exercised, `GetAddresses` ignoring its
`employeeId` argument, cross-aggregate boundary exception on AddressList.

## Verification of test-infrastructure questions

- Address id seeding (300 vs fetched 201/202/203) prevents coincidental exact-match passes. ✓
- Recording isolation: `AddScoped` + fresh scope per `[TestInitialize]`. ✓
- MSTest runs this assembly sequentially (no `.runsettings`, no `[Parallelize]`), and
  isolation would hold under parallelism anyway. ✓

## Sacred tests

None gutted. `TestInfrastructure.cs` changes are additive (new mocks/registrations); every
other pre-existing Design.Tests file is untouched.

## Logs

`007-build.log` / `007-test.log` (full sln, 0 errors, 2144 passed, 2 pre-existing skips);
`007-design-build.log` / `007-design-test.log` (Design.Tests, 0 errors, 113/113 — final state
after the fix loop; logs re-run and overwritten per gate protocol).

Note carried forward: `src/Neatoo.sln` does not include Design.Tests, so "full solution green"
must always be paired with the Design log to constitute full coverage.
