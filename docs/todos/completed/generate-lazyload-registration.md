# Generate LazyLoad Property Registration

**Status:** Complete
**Priority:** High
**Created:** 2026-03-14
**Last Updated:** 2026-03-14

---

## Problem

Consumers currently must call `RegisterLazyLoadProperties()` in custom property setters to make LazyLoad properties work with PropertyManager. This leaks framework plumbing into domain code:

```csharp
public LazyLoad<IPersonPhoneList> PersonPhoneList
{
    get => _personPhoneList;
    internal set
    {
        _personPhoneList = value;
        RegisterLazyLoadProperties();  // Consumer should NOT need this
    }
}
```

The consumer cannot be responsible for making the Neatoo API work. Registration should be automatic.

## Solution

Explore how the BaseGenerator and the existing property registration generation (`InitializePropertyBackingFields`) could handle LazyLoad property registration automatically — eliminating the need for consumers to call anything. The generator already detects partial properties and generates registration code; could it also detect LazyLoad properties and generate their registration?

---

## Clarifications

**Q: Which architecture to match?**
A: Match the non-LazyLoad architecture — partial properties with generated backing fields, setters, and registration. LazyLoad being a special case with manual fields and custom setters is the deviation. Option 2 (make LazyLoad properties partial) is closest to how every other property works.

**Q: Generated setter pattern (Choice A)?**
A: A1 -- Use LoadValue. Correct semantics, no rule triggering on container assignment.

**Q: Registration pattern (Choice B)?**
A: B1 -- New `CreateLazyLoad<TInner>` method on `IPropertyFactory<T>`. Explicit and clean. Breaking change is acceptable.

**Q: Deprecation strategy (Choice C)?**
A: Remove immediately. No deprecation. Delete `RegisterLazyLoadProperties`, `FinalizeRegistration`, reflection cache, all of it.

---

## Plans

- [Generate LazyLoad Property Registration via BaseGenerator](../plans/generate-lazyload-registration.md)

---

## Tasks

- [x] Architect questions (Step 2)
- [x] Architect plan (Step 3)
- [x] Developer review (Step 4)
- [x] Implementation (Step 5) -- 2117 tests pass
- [x] Architect verification (Step 6) -- VERIFIED

---

## Progress Log

### 2026-03-14
- Created from user feedback: "The consumer cannot be responsible for making the neatoo api work"
- Architect explored generator code, identified Option 2 (partial LazyLoad properties) as best fit
- User confirmed: "Match the non-LazyLoad architecture as best we can"
- Plan created at `docs/plans/generate-lazyload-registration.md` with before/after, design decisions, and open choices
- User locked design choices: A1 (LoadValue setter), B1 (explicit CreateLazyLoad on IPropertyFactory), C (remove immediately, no deprecation)
- Plan finalized and set to "Under Review (Developer)"
- Developer review: Concerns Raised. Key issues: (1) OnDeserialized reconnection unspecified, (2) serializer no-change confirmation needed, (3) setter access modifier gap, (4) unregistered LazyLoad property conversion decision needed, (5) EntityBase.cs impact.
- All 5 concerns resolved: (1) ReconnectAfterDeserialization on ILazyLoadProperty, (2) serializer confirmed zero changes via code read, (3) plain set accepted -- concrete classes are internal, (4) ALL LazyLoad properties become partial, (5) EntityBase.cs has no FinalizeRegistration calls -- verified no changes needed.
- Plan updated and back to "Under Review (Developer)" for re-review.
- Developer re-reviewed: All concerns resolved. Plan APPROVED. Implementation Contract created. Plan status: Ready for Implementation.
- Implementation complete. All 3 phases done. 2117 tests pass (0 failed). Plan status: Awaiting Verification.
- Architect verification: VERIFIED. Independent build (0 warnings, 0 errors) and test run (2117 pass, 0 fail) confirmed. All 8 verification items checked: generator output matches plan, old infrastructure gone, new APIs present, Person.cs clean, all LazyLoad properties partial.

---

## Results / Conclusions

LazyLoad<T> properties are now partial, matching the architecture of every other Neatoo property. The generator detects LazyLoad<T> types, generates backing fields with look-through property subclasses, setters using LoadValue (no rule triggering), and registration via new CreateLazyLoad<TInner> on IPropertyFactory. All runtime reflection-based registration infrastructure removed (RegisterLazyLoadProperties, FinalizeRegistration, _lazyLoadPropertyCache). Consumers write `public partial LazyLoad<T> Prop { get; set; }` and nothing else. 2117 tests pass.
