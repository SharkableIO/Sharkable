# Changelog

All notable changes to Sharkable are documented here.

## [Unreleased]

### feat

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

- Add idempotency middleware design spec
- Add idempotency middleware implementation plan
- Add README section

### chore

- Enable idempotency middleware for end-to-end AOT verification
- Doc fixes and roadmap

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
