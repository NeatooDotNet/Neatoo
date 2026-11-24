# FCI-005 code review

**2026-10-16: APPROVED.** Facts, consequences and intents land where D27–D30 put them; no rule branches on its caller; interface-first holds; the fit converges in every case checked; the changed-meta-property lock (128142f) is correct as scoped; no sacred test touched; region names unique.

Callouts:
1. **Fixed.** `AddInterval` ran two fits, the first at the default effort, while its comment claimed one. `Interval.Create` now takes the effort, so the add is the one change.
2. **Fixed.** A created interval never ran its Name validation, so a blank-named interval entered valid. `Interval.Create` runs its own rules; `NewIntervalWithABlankName_EntersInvalid_AndAdmissionFollows` pins it (`005-flip-create-rules.log`).
3. **Fixed.** Anchor nesting and the scope ending on a throw were unpinned. `AnchorScopes_Nest_AndAnInnerScopeRestoresTheOuterAnchor` and `AnchorScope_EndsWhenTheGestureThrows` pin them (`005-flip-anchor-restore.log`).
4. **Parked for Keith, a decision.** The compare-then-reset of `MetaState`/`EntityMetaState` is not atomic under concurrent writes from several threads: a stale baseline swallows the next real flip, and since FCI-004 that loses a meta-triggered rule run, not only a notification. It predates FCI and has not been observed failing. A lock around the compare and reset risks lock-order deadlock across a graph whose children raise into their parents, so the fix is a framework design choice.

Theoretical, not triaged: two non-nested gestures in flight lose the anchor (unreachable while every rule on the cascade completes synchronously); `SlideMinutes` on a parentless interval; negative locked minutes; `Share` overflow above about 214M minutes.
