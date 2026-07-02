# Changelog

All notable changes to Sharkable are documented here.

## [0.5.4] — 2026-06-30

### security

- Add 100ms regex timeout to `FormatAsGroupName` / `GetVersionFormat` — prevent ReDoS via malicious `GroupNameSuffixPattern` / `VersionFormatPattern`
- Add `SafeSoftDeleteField` validation in `AutoCrudGenerator` — reject non-alphanumeric field names to prevent SQL injection

## [0.5.3] — 2026-06-30

### feat

- Add `HttpContext.Localize(string key, params object[] args)` overload — format args in localized strings
- Add `HttpContext.GetCulture()` extension — resolve client culture from `Accept-Language` header
- `ApiKeyFilter` automatically skipped when `AuthorizationInterceptorFactory` is set — interceptor owns auth entirely
- Add `SqlSugarOptions.MaxPageSize` / `DefaultPageSize` — configurable AutoCrud pagination limits (defaults: 100 / 20)
- Add `SqlSugarOptions.SoftDeleteFieldName` — configurable soft delete field (default: `"IsDeleted"`)

### security

- Add 100ms regex timeout to `FormatAsGroupName` / `GetVersionFormat` — prevent ReDoS via malicious `GroupNameSuffixPattern` / `VersionFormatPattern`
- Add `SafeSoftDeleteField` validation in `AutoCrudGenerator` — reject non-alphanumeric field names to prevent SQL injection

## [0.5.2] — 2026-06-30

### feat

- Add `SharkOption.ETagOptions.CacheableMethods` / `CacheControlHeader` / `ShouldSkipStatus` — ETag method set, cache header, and cacheable status logic are now configurable
- Add `SharkOption.ApiKeyHeaderName` — configurable API key request header
- Add `GracefulShutdownOptions.ShutdownStatusCode` / `DrainPollingInterval` — configurable 503 status code and drain polling interval
- Add `SharkOption.ProblemDetailsTypeFactory` / `ProblemDetailsTitleFactory` — delegates for customizing RFC 7807 `type` URI and `title`
- Add `SharkOption.DefaultCulture` — configurable default locale for error localizer
- Add `SharkRateLimiterOptions.AdaptiveGcHighThreshold` / `AdaptiveGcLowThreshold` / `AdaptiveReductionDivisor` — GC pressure thresholds and reduction factor for adaptive rate limiting
- Add `ProfilerOptions.MaxEntries` — configurable profiler ring buffer size
- Add `SharkIdempotencyOptions.RetryAfterSeconds` / `ShouldCacheStatus` — configurable Retry-After and cacheable status predicate
- Add `SharkOption.HealthCheckPath` — configurable health check endpoint path
- Add `TracingOptions.ActivitySourceName` — configurable `ActivitySource` name (separate from `ServiceName`)
- Add `SharkOption.GroupNameSuffixPattern` / `VersionFormatPattern` / `VersionFormatReplacement` — customizable endpoint URL naming regexes
- Remove hardcoded `"Sharkable"` ActivitySourceName in `TracingMiddleware` — now uses `TracingOptions.ActivitySourceName`
- Add `HttpContext.Localize(string key)` extension method — easy localization in user endpoints
- Add `LocalizationExtensions` — public scaffolding for endpoint-level error translation

### docs

- Add complete error localization guide with `IErrorLocalizer` implementation example, `HttpContext.Localize()` usage, and middleware integration pattern (EN + ZH)

## [0.5.1] — 2026-06-29

### fix

- Fix `InvalidOperationException` when `UseAuthorization` is called without `AddAuthorization` — register `AddAuthorization` by default (configurable via `EnableAuthorization`), with factory support via `ConfigureAuthorization` delegate
- Fix `UnifiedResultWrapFilter` always wrapping with status 200 — now uses actual `HttpContext.Response.StatusCode`
- Fix `AuditTrailMiddleware` response header hardcoded to `"X-Correlation-Id"` — now respects `AuditTrailOptions.CorrelationIdHeader`

### feat

- Add `SharkOption.EnableAuthorization` (default `true`) — allows users to opt out of authorization service registration
- Add `SharkOption.ConfigureAuthorization` — delegate for customizing `AuthorizationOptions` (policies, fallback, etc.)
- Add `SharkOption.ScalarJwtToken` / `SharkOption.ScalarApiKeyValue` — configure pre-filled credentials in Scalar UI

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

### security

- Add `MaxFingerprintBodySize` (default 64 KiB) to `SharkIdempotencyOptions` — prevent OOM via attacker-controlled `Content-Length` header in idempotency middleware fingerprinting
- Replace `Thread.Sleep` polling with `await Task.Delay` in graceful shutdown drain — prevent ApplicationStopping thread block (SHARK-SEC-003)
- Make `SagaExecutor.LockTtl` configurable; add `LockRenewalInterval` with periodic lock extension — prevent split-brain sagas when step duration exceeds lock TTL (SHARK-SEC-004, cross-repo with `Sharkable.Cache.Redis`)
- Add `[CrudAllow]` attribute for explicit field allowlist on AutoCrud insertable/updateable — reject endpoints with no allow-listed fields to prevent mass-assignment privilege escalation (SHARK-SEC-006)

### feat

- Add `UnifiedResult<T>` AOT preservation — Source Generator auto-emits `typeof(UnifiedResult<T>)` for all endpoint return types

## [0.5.0] — 2026-06-29

### feat

- Add `IAuthorizationInterceptor` — pluggable authorization hook (claim-based RBAC, tenant-scoped access, custom API-key logic) via endpoint filter
- Fix JWT events chaining — user `OnTokenValidated` / custom handlers now chain with Sharkable's default `OnChallenge` / `OnForbidden` instead of being overwritten
- Add pagination to AutoCrud `List` — `GET /{group}?page=1&pageSize=20` returns `{ items, total, page, pageSize, totalPages }`
- Add `CrudOperations.ListAll` — optionally expose full-table dump via dedicated `GET /all` route, intentionally excluded from `All` for safety
- Add AutoCrud search/filtering — `filter[field][op]=value` convention with 10 operators (eq/ne/gt/gte/lt/lte/like/in/nin/null), sort support, field-level validation
- Add AutoCrud AOT zero rd.xml — Source Generator preserves entity types at compile time, no manual configuration
- Add ProblemDetails (RFC 7807) support — `SharkOption.UseProblemDetails` flag, all error responses output standard format with `type`/`title`/`status`/`detail`/`instance`/`traceId`
- Add response compression — `SharkOption.EnableResponseCompression`, uses ASP.NET Core built-in middleware
- Add soft delete — `ISoftDeletable` marker, AutoCrud auto-filters `IsDeleted = 0` on reads + soft-deletes on `CrudOperations.Delete`
- Add `SharkBackgroundService` — enhanced `BackgroundService` with health reporting, retry policy, execution tracing
- Add distributed transactions (SAGA) — `ISagaStep` + `ISagaStore` + `SagaExecutor` + `MemorySagaStore`, crash-recovery, distributed lock, `RedisSagaStore` in Cache.Redis
- Add distributed cron job scheduler — 6-field cron expression parser, `ICronJobStore` with retry/timeout/concurrency control, admin endpoint `/_sharkable/jobs`, `RedisCronJobStore` in Cache.Redis

## [0.4.1] — 2026-06-28

### feat

- Add route conflict Roslyn Analyzer (`SHARK001`) — compile-time detection of duplicate `ISharkEndpoint` route registrations, ships with NuGet package automatically
- Add distributed tracing (`TracingMiddleware`) — W3C `traceparent` via `ActivitySource`, `ITracingExporter` interface for OpenTelemetry plugins, zero external dependencies
- Add built-in profiler (`ProfilerMiddleware` + `/_sharkable/profiler`) — request counts, average latency, top-N slowest recent requests, memory delta tracking
- Add extensible health checks — structured JSON `/healthz` via ASP.NET Core `HealthCheckService`, `HealthChecksConfigure` callback for custom checks, auto JWT authority reachability check, uptime + version in response
- Add multi-tenant data source isolation — `ITenantDataSource` scoped service, `TenantOptions.ConfigureDataSource()` for per-tenant connection string routing
- Add adaptive rate limiting — `SharkRateLimiterOptions.EnableAdaptive`, dynamic CPU/GC-based permit adjustment via `AdaptiveLimitMonitor`
- Add automatic ETag generation (304 Not Modified) — `SharkOption.EnableETag`, SHA256 content hashing, `ETagMiddleware`
- Add error message localization — `IErrorLocalizer` interface + `ErrorLocalizerFactory`, `Accept-Language`-aware translation for middleware errors
- Add AutoCrud abstractions — `IAutoCrudEntity<T>` + `CrudOperations` + `IAutoCrudGenerator` for pluggable CRUD route generation

## [0.4.0] — 2026-06-28

### feat

- Add startup configuration self-check — `ConfigurationValidator` validates JWT, multi-tenant, and API key settings at startup, throwing `SharkConfigurationException` with all errors
- Add graceful shutdown — `GracefulShutdownMiddleware` + `GracefulShutdownOptions`, health check returns 503 during drain, configurable `DrainTimeout`
- Add audit trail async/batch write — `AuditLogBuffer` (Channel-based, configurable `BatchSize`/`FlushInterval`/`AsyncWrite`), `AuditTrailMiddleware` supports async fire-and-forget logging
- Add `IDistributedRateLimitStore` — pluggable distributed counter store interface for rate limiting (Redis, PostgreSQL, etc.), with `MemoryRateLimitStore` as default
- Add `SharkRateLimiterMiddleware` — fixed-window rate limiting middleware backed by `IDistributedRateLimitStore`, configured via `SharkOption.ConfigureRateLimiting()`
- Add `SharkRateLimiterOptions` — per-endpoint or global rate limit configuration
- Add `IdempotencyStoreFactory` and `RateLimitStoreFactory` to `SharkOption` — factory delegates for plugging in custom stores inside the `AddShark()` callback

### refactor

- `IIdempotencyStore` registration inside `AddCommon()` now uses `TryAddSingleton` — NuGet plugin packages can register a custom store before `AddShark()` and it will take precedence
- Deprecate `[SharkEndpoint]` / `[SharkMethod]` / `SharkHttpMethod` and related reflection infrastructure (`IDependencyReflectorFactory`, `DependencyReflectorFactory`, `Reflector`, `ReflectorExtension`) — all marked `[Obsolete]` with guidance to migrate to `ISharkEndpoint`

### fix

- Fix 5 pre-existing nullability warnings (`CS8766`, `CS8601`, `CS8602`) in `SharkResponseMetadata.cs`, `AssemblyUtil.cs`, `SwaggerExtension.cs`

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
