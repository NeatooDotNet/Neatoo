# Design.App and Design.Server in CI

**Plan #:** 002
**Date:** 2026-10-09
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-4
**Status:** Draft
**Last Updated:** 2026-10-09
**Plan-review opt-in:** —
**Code-review opt-in:** —
**Branch:** skr-002-design-app-server — cut from the arc at Step 2
**PR:** —

---

## Scope

Create `src/Design/Design.App` (Blazor WebAssembly, MudBlazor and MudNeatoo by project reference) and `src/Design/Design.Server` (ASP.NET Core host that serves the client and maps the RemoteFactory endpoint) over Design.Domain, modelled on `Examples/Person`, and add both to `Design.sln` so the existing CI steps build and test them. They hold the regions the skill needs for tier composition: the client and server `Program.cs`, what each tier registers, the keyed `HttpClient`, the `IsServerRuntime` trim switch, and the endpoint (using `Neatoo.RemoteFactory.AspNetCore` if it supplies one). Design.App gets one page per Blazor pattern plan 006 needs, but plan 006 writes the mudneatoo text. Open at drafting: the Design projects are net9.0 while Person is net10.0 and CI installs only the 9.0 SDK; and the test tier for Blazor pages, since the repo has no bUnit.

---

## Intent

## Framework & Architectural Alignment

## Constraints & Invariants

## Steps

## Acceptance

## Current State (Pre-Flight)

## Punchlist

## Test Evidence

## Gate Record

## Plan Amendments

## Notes
