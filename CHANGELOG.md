# Changelog

All notable changes to Sharkable are documented here.

## [0.3.0] — 2026-06-27

### feat

- Add `RedactingFormatter` — replaces `ILogger<T>` with a structured-log wrapper that redacts configured sensitive fields (password, secret, token, etc.)
- Add multi-tenant support — `ITenant` scoped service, `TenantResolutionMiddleware`, `TenantResolver` helpers (`FromHost` / `FromClaim` / delegate)
- Add `WrapSchemaFactory` to `SharkOption` — lets custom `IUnifiedResultFactory` users match the OpenAPI schema to their actual response shape
- Add `ConfigureScalar()` to `SharkOption` — custom title, theme, layout, and auto-detected Bearer/API Key auth in Scalar UI
- Add `SharkDescriptionAttribute` — class-level OpenAPI summary/description for `ISharkEndpoint` endpoints
- Add `SharkResponseTypeAttribute` — class-level response metadata for `ISharkEndpoint` endpoints
- Add `SharkDeprecatedAttribute` — marks `ISharkEndpoint` endpoints as deprecated via `ObsoleteAttribute` on endpoint metadata
- Add `[RequiresDynamicCode]` to `SharkEndpointAttribute` — AOT compile-time warning when using old-style endpoints

### test

- Add tests for MultiTenant, ValidationFilter, RedactingLogger, and the new metadata attributes

### fix

- Adapt to `Microsoft.OpenApi` v2 breaking changes — flat namespace, `JsonSchemaType` enum, `IOpenApiSchema` dictionary value type
- Fix `SharkOption` static field leak — `OpenApiConfigure` / `SqlSugarOptionsConfigure` changed from `static` to instance properties
- Fix `TenantResolutionMiddleware` silently failing for custom `ITenant` implementations — `ITenant.TenantId` changed to `{ get; set; }`
- Fix `CreateSharkEndpoint` AOT risk — use `new SharkEndpoint()` instead of `Activator.CreateInstance(nonPublic: true)`
- Fix `AsUnauthorized()` pointless null check — simplified to direct return
- Fix typo in `.csproj` description: `🌈owerful` → `🌈Powerful`

### refactor

- Rename `Utils/AssembliyUtil.cs` → `Utils/AssemblyUtil.cs`
- `SqlSugarOptions.InitKeyType` changed from public field to auto-property
- `AssemblyContext()` constructor changed from `public` to `internal`
- Replace `throw new Exception` with `throw new InvalidOperationException` in two places
- Remove dead code `UrlToDictionary()` from `StringExtension.cs`

### feat

- Make `IIdempotencyStore` fully async (`TryReserveAsync`, `GetAsync`, `StoreAsync`, `ReleaseAsync`) — enables distributed store plugins (Redis, PostgreSQL, etc.) without sync-over-async deadlock
- Add SHA-256 fingerprint helper
- Add `IIdempotencyStore` interface and record types
- Add `MemoryIdempotencyStore`
- Add `SharkIdempotencyMiddleware` state machine
- Route error responses through `UnifiedResult` factory
- Add `EnableIdempotency` flag and `ConfigureIdempotency`
- Register store and options in `AddCommon`
- Wire middleware into pipeline

### test

- Add null-path test and `<returns>` doc to fingerprint
- Add integration tests for state machine
- Add oversize response scenario
- Add scenario 10 (`EnableIdempotency=false`)
- Add `MapGet` to test endpoint and strengthen `GetWithHeader` test

### refactor

- Revert `UnifiedResult.Code`; embed code in `errorMessage`

### docs

- Add Scalar configuration docs (EN + ZH)
- Add `WrapSchemaFactory` usage docs to unified-result (EN + ZH)
- Add idempotency middleware design spec
- Add idempotency middleware implementation plan
- Add README section

### chore

- Enable idempotency middleware for end-to-end AOT verification
- Doc fixes and roadmap

## [Unreleased]

## [0.3.2] — 2026-06-27

### feat

- Add NuGet package icon (`sharkable.jpg`)
- Sync `Sharkable.DbType` enum with SqlSugar 5.1.4.215 — add `TDSQLForPGODBC`, `TDSQL`, `HANA`, `DB2`, `GaussDBNative`, `DuckDB`, `MongoDb`
- Update `Sharkable.AutoCrud.SqlSugar` to v0.3.2 with SqlSugarCore 5.1.4.215

## [0.3.1] — 2026-06-27

### feat

- Add exception handler logging — `SharkExceptionHandlerMiddleware` now logs unhandled exceptions with HTTP method, request path, and full exception details via `ILogger`

## [0.2.0] — 2026-06-08

- feat: add built-in middleware (E) and security (F) features
- feat: add `[SharkVersion]` attribute for API versioning
- feat: add `EnableAutoWrap` to `SharkOption`, double-wrap guard, AOT types, and tests
- feat: add test project + NativeTest demo improvements
- docs: comprehensive README.md update with all features
- docs: add linear history requirement for Rebase and merge
- fix: correct license expression, add README.md and packaging items

## [0.1.1] — 2026-06-08

- feat: add endpoint grouping, OpenAPI tags, and auto OperationId
- fix: use `Add()` convention for AOT-compatible tags + OperationId

## [0.1.0] — 2026-06-08

- feat: upgrade to .NET 10, replace Swagger with Scalar
- feat: add FluentValidation integration with auto-validate filter
- feat: add global exception handler + auto unified result wrap
- refactor: pluggable `UnifiedResult` with `IUnifiedResultFactory`
- fix: audit XML comments, fix bugs, remove dead code
- fix: invoke method as nullable
- docs: add XML doc comments to validation code + coding standard to AGENTS.md
- update AGENTS.md: add branch sync & rebase workflow

## [0.0.25] — 2024-10-03

- feat/fix: add more unified result extensions (`AsBadRequest`)
- fix: where results like BadRequest should only contain error message
- fix: where generic type of return delegate might be null

## [0.0.24] — 2024-10-02

- add `GlobalSuppressions` to suppress warnings
- will not proceed if `SqlSugarOptionsConfigure` is null

## [0.0.23] — 2024-09-27

- fix: where CRUD options not invoked properly
- fix: where auto CRUD options might be null
- fix: a bug where `SharkOption` might be null

## [0.0.22] — 2024-09-27

- add SqlSugar options and enums
- add Swagger UI options and fix options invoke method
- move files to suitable folders
- rename `DependencyInjection.cs` to `SharkableExtension.cs`
- add summary for `Shark.cs`

## [0.0.21] — 2024-09-25

- add SqlSugar options and enums
- make class sealed

## [0.0.20] — 2024-09-25

- update project home URL
- add CRUD example for AOT project

## [0.0.19] — 2024-09-24

- add auto CRUD for API generation using SqlSugar client
- fix: where SharkEndpoint wiring only maps one while using `TryAddSingleton`

## [0.0.17] — 2024-09-23

- add `AotMode` option
- add editor config to suppress warnings
- fix: where AOT mode detection not properly displayed
- move Swagger configuration to OpenAPI
- rename `CommonExtension` to `SharkExtension`

## [0.0.16] — 2024-09-21

- add Swagger options
- add `UseShark` options for Swagger and other upcoming settings
- use `TryAddSingleton` for `WireSharkEndpoint`

## [0.0.15] — 2024-09-19

- fix dependency for packing

## [0.0.14] — 2024-09-19

- add Swagger UI and OpenAPI doc
- add `IEndpointRouteBuilder` extension
- add `GetCaseFormat` static method
- set `RegexInlineRouteConstraint` in service
- code refactor `StringExtension`

## [0.0.13] — 2024-09-19

- fix: `FormatAsGroupName` version format may display disorderly
- fix: SharkEndpoint AOT compatibility

## [0.0.12] — 2024-09-18

- add version format for endpoint class
- setup service provider
- add `SharkOption` statics
- code refactor

## [0.0.10] — 2024-09-18

- add SnakeCase format support
- add `SharkEndpoint` create instance factory
- fix CamelCase format display

## [0.0.9] — 2024-09-17

- add format for CamelCase and ToLower

## [0.0.7] — 2024-09-16

- add assembly context and Shark essentials
- WIP `SharkEndpoint`

## [0.0.5] — 2024-09-12

- add `SharkMethod` string syntax
- WIP `ISharkEndpoint`

## [0.0.4] — 2024-09-11

- WIP endpoint mapper initial support
- Initial commit
