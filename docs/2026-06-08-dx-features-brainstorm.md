# DX Feature Brainstorm — Sharkable

Date: 2026-06-08 (updated 2026-06-08)
Context: Sharkable is a .NET 10 minimal API framework collection (NuGet).

## Status

| Feature | Status | Branch |
|---------|--------|--------|
| A. Exception handler + auto unified response | ✅ Done (merged) | — |
| B. FluentValidation integration | ✅ Done (not merged) | `feat/fluent-validation` |
| C. Endpoint grouping + OpenAPI tags | ✅ Done (merged) | — |
| D. API versioning | ✅ Done (not merged) | `feat/tests-and-native-test-demo` |
| E. Built-in middleware (RateLimiter/OutputCache/HealthChecks) | ✅ Done (not merged) | `feat/tests-and-native-test-demo` |
| F. Security (CORS/API Key/JWT) | ✅ Done (not merged) | `feat/tests-and-native-test-demo` |
| XML doc comments + bug fixes | ✅ Done (not merged) | `fix/audit-comments-and-bugs` |
| .NET 10 + Scalar upgrade | ✅ Done (merged) | — |

## Proposed feature directions

### C. 端点分组 + OpenAPI 标签 (pending from v1 brainstorm)

- `[SharkTag("...")]` attribute or naming convention for OpenAPI tag grouping
- Allow multiple `ISharkEndpoint` to share a common URL prefix/group
- Auto-generate OperationId from class + method name
- Group name already derived from class name (strips `Endpoint` suffix), but not exposed as OpenAPI tag

### D. API 版本控制

- URL prefix versioning: `api/v1/...` / `api/v2/...`
- Header-based versioning as alternative
- Existing `V{digits}` → `@{digits}` transform helper already exists but is unused in routing
- `SharkEndpointAttribute.Version` field declared but not wired into `MapSharkEndpoints()`
- Groups could map to different route builders per version

### E. 内置中间件（限流/缓存/健康检查）

- **RateLimiter**: wrap `builder.Services.AddRateLimiter()` with Sharkable options, apply per-endpoint or global
- **OutputCache**: `[OutputCache]` policy configuration via SharkOption
- **HealthChecks**: auto-register health endpoint (`/healthz`) with UnifiedResult response
- All are built into ASP.NET Core — no new NuGet deps

### F. 安全相关（CORS / API Key / JWT）

- **CORS**: `opt.ConfigureCors(c => c.AllowOrigin(...))` — simple wrapper
- **API Key auth**: lightweight middleware, validate against configured key(s)
- **JWT hardening**: opinionated defaults + validation error → UnifiedResult

### G. Auto wrap 完善

- Current `UnifiedResultWrapFilter` wraps only on exception path
- Goal: every endpoint return value auto-wrapped in `UnifiedResult<T>` unless already `IResult`
- AOT-compatible approach needed (no runtime reflection on return types)
- Could be an opt-in: `opt.AutoWrapResults = true`

### H. 请求日志 / Audit Trail

- Structured request/response logging middleware
- Configurable: exclude paths, log levels, sensitive data redaction
- Correlation ID generation/forwarding

### I. Rate Limiting 集成

```csharp
// 1. 全局限流
opt.ConfigureRateLimiter(r => r.GlobalPolicy = "...")

// 2. 端点级限流 (new style ISharkEndpoint)
app.MapGet("hello", () => "hi").SharkRateLimit("fixed")
```

### J. 响应缓存 (OutputCache)

- OutputCache 策略自动注册
- 端点级 `[OutputCache]` 等价物

## Decision record

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-06-08 | A first (exception handler) | High value, small change surface |
| 2026-06-08 | B second (validation) | Most requested DX feature |
| 2026-06-08 | .NET 10 + Scalar third | Platform dependency upgrade |
| 2026-06-08 | Vault created | All directions recorded for future decision |
