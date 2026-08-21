# RemoteFactory Boundary Hardening

**Plan #:** 003
**Date:** 2026-08-11
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-11
**Plan-review opt-in:** (declared at draft time — security-sensitive; expect Yes)
**Code-review opt-in:** (declared at draft time)

---

## Scope

Harden the RemoteFactory transport boundary per FableFeedback.md: make the `/api/neatoo` endpoint securable through the public API (stop discarding the `RouteHandlerBuilder`), decide and enforce an auth posture for `RaiseFactoryEventRemote`, put an allow-list in front of client-supplied type resolution (remove the `Type.GetType` fallback and the dead-but-deserialized `Target` path), define a structured error contract that distinguishes domain failures from authorization denials from outages, and stand up a real-HTTP test suite (`WebApplicationFactory` against the existing but unreferenced TestServer projects) covering the endpoint, the transport serializer, and `[AspAuthorize]`. Add a diagnostic for Ordinal-serialization silent degradation on non-partial classes. This plan happens in the RemoteFactory repo; wire-format versioning is a candidate follow-up plan, not part of this one.
