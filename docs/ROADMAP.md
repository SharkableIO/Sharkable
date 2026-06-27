# Roadmap

候选新功能列表。所有条目约束：**不引入新的第三方 NuGet 包**，仅使用现有 4 个包（FluentValidation / Microsoft.AspNetCore.OpenApi / Scalar.AspNetCore / Microsoft.AspNetCore.Authentication.JwtBearer）+ `Microsoft.AspNetCore.App` 框架引用内置能力。

## 待评估（零依赖可做）

| # | 功能 | 备注 |
|---|---|---|
| 3 | 幂等键中间件 | 纯代码，Cache 用 `IMemoryCache` / `OutputCache` |
| 4 | 分页/过滤/排序 DTO | 纯 POCO，配 AutoCrud 零成本 |
| 5 | 响应压缩 | `Microsoft.AspNetCore.ResponseCompression` 已在框架引用里 |
| 6 | 结构化日志 + 字段脱敏 | `ILogger` + 自定义 `RedactingFormatter` |
| 8 | 多租户 | `IHttpContextAccessor` + 抽象，纯代码 |
| 9 | 轻量特性开关 | 配置驱动，不引 `Microsoft.FeatureManagement` |
| 10 | Webhook 出站 + HMAC 签名 | `System.Security.Cryptography`，零依赖 |
| 13 | Scalar 增强 | 已有 Scalar，加 example / auth UI 配置 |
| 15 | 集成测试基类 | `WebApplicationFactory<>` 在 `Microsoft.AspNetCore.TestHost` 里 |
| 16 | Correlation ID | 纯中间件 |
| 17 | ETag / 304 条件请求 | `Microsoft.Net.Http.Headers.ETag` 内置 |
| 18 | ProblemDetails 规范化 | ASP.NET Core 9+ 内置 `IProblemDetailsService` |
| 19 | 软删除全局过滤器 | AutoCrud 内部，纯表达式树 |
| 20 | `SharkBackgroundService` 抽象 | 封装 `BackgroundService`，零依赖 |

## 已剔除（需新第三方包）

- OpenTelemetry → `OpenTelemetry.*` 一整套
- Resilience（Polly v8） → `Polly` / `Microsoft.Extensions.Resilience`
- gRPC → `Grpc.AspNetCore`
- `dotnet new` 模板 → `Microsoft.TemplateEngine.*`
- OpenAPI 客户端生成 → `Kiota` / `NSwag` / `Swashbuckle`
- 完整后台任务调度（Hangfire 等） → `Hangfire`

## 灰色（取决于运行时内置情况）

- API 版本控制 → .NET 10 稳定版暂无内置 API，需 `Microsoft.AspNetCore.Mvc.Versioning` 第三方包
