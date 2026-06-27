# Roadmap

候选新功能列表。所有条目约束：**不引入新的第三方 NuGet 包**，仅使用现有 4 个包（FluentValidation / Microsoft.AspNetCore.OpenApi / Scalar.AspNetCore / Microsoft.AspNetCore.Authentication.JwtBearer）+ `Microsoft.AspNetCore.App` 框架引用内置能力。

## 已实现

| # | 功能 | 备注 |
|---|---|---|
| 3 | 幂等键中间件 | 纯代码，Cache 用 `IMemoryCache` / `OutputCache` |
| 6 | 结构化日志 + 字段脱敏 | `ILogger` + 自定义 `RedactingFormatter` |
| 8 | 多租户 | `IHttpContextAccessor` + 抽象，纯代码 |

## 待评估

| # | 功能 | 备注 |
|---|---|---|
| 13 | Scalar 增强 | 已有 Scalar，加 example / auth UI 配置 |
| 18 | ProblemDetails 规范化 | ASP.NET Core 9+ 内置 `IProblemDetailsService` |

## AutoCrud 内部

- 软删除全局过滤器 — AutoCrud 内部，纯表达式树

## 已剔除

- 响应压缩 → ASP.NET Core 已内置一行配置，框架封装无价值
- 分页/过滤/排序 DTO → 用户自定几行代码的事，框架提供增加认知负担
- 轻量特性开关 → `IConfiguration` 本身就是特性开关
- Webhook 出站 + HMAC 签名 → 场景特定且复杂度高，超出框架范围
- 集成测试基类 → 需测试框架，且认证/数据库/mock 高度定制化
- Correlation ID → `AuditTrailMiddleware` 已覆盖
- ETag / 304 条件请求 → 强业务侵入性，框架无法透明实现
- `SharkBackgroundService` 抽象 → `BackgroundService` 已满足需求

## 已剔除（需新第三方包）

- OpenTelemetry → `OpenTelemetry.*` 一整套
- Resilience（Polly v8） → `Polly` / `Microsoft.Extensions.Resilience`
- gRPC → `Grpc.AspNetCore`
- `dotnet new` 模板 → `Microsoft.TemplateEngine.*`
- OpenAPI 客户端生成 → `Kiota` / `NSwag` / `Swashbuckle`
- 完整后台任务调度（Hangfire 等） → `Hangfire`

## 灰色（取决于运行时内置情况）

- API 版本控制 → .NET 10 稳定版暂无内置 API，需 `Microsoft.AspNetCore.Mvc.Versioning` 第三方包
