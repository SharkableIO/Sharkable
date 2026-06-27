# Roadmap

候选新功能列表。所有条目约束：**不引入新的第三方 NuGet 包**，仅使用现有 4 个包（FluentValidation / Microsoft.AspNetCore.OpenApi / Scalar.AspNetCore / Microsoft.AspNetCore.Authentication.JwtBearer）+ `Microsoft.AspNetCore.App` 框架引用内置能力。

## 已实现

| # | 功能 | 备注 |
|---|---|---|
| 2 | API 版本控制 | `[SharkVersion("v2")]` → `/api/v2/{group}/{route}` |
| 3 | 幂等键中间件 | 纯代码，Cache 用 `IMemoryCache` / `OutputCache` |
| 6 | 结构化日志 + 字段脱敏 | `ILogger` + 自定义 `RedactingFormatter` |
| 8 | 多租户 | `IHttpContextAccessor` + 抽象，纯代码 |
| 13 | Scalar 增强 | `ConfigureScalar()` + 自动认证 UI 配置 |

## AutoCrud 内部

- 软删除全局过滤器 — AutoCrud 内部，纯表达式树

## 已剔除

- 响应压缩 → ASP.NET Core 已内置一行配置，框架封装无价值
- 分页/过滤/排序 DTO → 用户自定几行代码的事，框架提供增加认知负担
- 轻量特性开关 → `IConfiguration` 本身就是特性开关
- Webhook 出站 + HMAC 签名 → 场景特定且复杂度高，超出框架范围
- 集成测试基类 → 需测试框架，且认证/数据库/mock 高度定制化
- Correlation ID → `AuditTrailMiddleware` 已覆盖
- ProblemDetails (RFC 9457) → 与 `UnifiedResult<T>` 统一格式定位冲突，两种错误格式反而让用户困惑
- ETag / 304 条件请求 → 强业务侵入性，框架无法透明实现
- `SharkBackgroundService` 抽象 → `BackgroundService` 已满足需求

## 已剔除（需新第三方包）

- OpenTelemetry → `OpenTelemetry.*` 一整套
- Resilience（Polly v8） → `Polly` / `Microsoft.Extensions.Resilience`
- gRPC → `Grpc.AspNetCore`
- `dotnet new` 模板 → `Microsoft.TemplateEngine.*`
- OpenAPI 客户端生成 → `Kiota` / `NSwag` / `Swashbuckle`
- 完整后台任务调度（Hangfire 等） → `Hangfire`

## 技术负债（来自 2026-06-27 审计）

- 修 `SharkOption` 静态字段泄漏（`OpenApiConfigure` / `SqlSugarOptionsConfigure` 不应是 `static`）
- 修 `TenantResolutionMiddleware` 对自定义 `ITenant` 静默失败
- 补充 MultiTenant / Validation / RedactingLogger 测试
- `SqlSugarOptions` 和 enum 补充 XML 文档注释
- `ISharkEndpoint` 层 OpenAPI 元数据（概要 / 描述）属性
