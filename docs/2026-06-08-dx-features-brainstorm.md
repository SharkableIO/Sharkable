# DX Feature Brainstorm — Sharkable

Date: 2026-06-08
Context: Sharkable is a .NET 8 minimal API framework collection (NuGet). Current DX features include
attribute-based DI, auto endpoint discovery/mapping, Swagger integration, and UnifiedResult response pattern.

## Direction: Developer Experience (DX)

### Observed DX gaps

- No global exception → unified response pipeline (bare 500 on unhandled exceptions)
- No request validation abstraction (users hand-write if-checks in every handler)
- Endpoint return values must be manually wrapped via `.AsOkResult()` — no auto-wrap
- Endpoint registration is scan-only, no explicit grouping/tagging API
- No endpoint filter / middleware hook at the framework level

### Proposed features

#### A. Exception handler + auto unified response (recommended as first step)

- `UseSharkExceptionHandler()` middleware that catches unhandled exceptions → `UnifiedResult` JSON
- Customizable error mapping via `SharkOption.ConfigureExceptionHandler(...)` (e.g. `ValidationException` → 400, `NotFoundException` → 404)
- Optional `autoWrap` mode: endpoint return values auto-wrapped in `UnifiedResult<T>` when not already `IResult`
- High value, small change surface, backward compatible

#### B. Request validation integration (FluentValidation)

- Scan and register `IValidator<T>` implementations alongside `[ScopedService]` etc.
- `[Validate]` attribute on endpoint parameters
- Auto-insert `IEndpointFilter` to run validation before handler; invalid → `UnifiedResult` error response
- Dependency: FluentValidation (external, optional)

#### C. Endpoint grouping + OpenAPI tag auto-generation

- `[SharkTag("...")]` attribute or naming convention for Swagger tag grouping
- Allow multiple `ISharkEndpoint` to share a common URL prefix/group
- Auto-generate OperationId from class + method name
- Lower value relative to A and B

### Priority suggestion

1. A (exception pipeline) — highest value, lowest risk
2. B (validation) — moderate value, external dependency consideration
3. C (tag/group) — nice-to-have, deferrable
