# Code Audit — 2026-06-27

## ✅ 已修复（2026-06-27）

- Bug 1: 去掉 `OpenApiConfigure` / `SqlSugarOptionsConfigure` 的 `static`
- Bug 2: `ITenant.TenantId` 改为 `{ get; set; }`，中间件不再依赖内部类型
- AOT 3: 非问题 — `rd.xml` 的 `Dynamic="Required All"` 已保证 AOT 下反射可用，加 `[RequiresDynamicCode]` 会产生假阳性警告
- AOT 4: `CreateSharkEndpoint` 改用 `new SharkEndpoint()` 代替 `Activator.CreateInstance(nonPublic: true)`
- #9: 补填 `GlobalSuppressions` 的 `Justification`
- #10: 删除 `Shark.cs` 注释掉的死代码

---

## 🟡 代码质量问题（已修复）

| # | 问题 | 状态 |
|---|---|---|
| 5 | 文件名 typo：`AssembliyUtil.cs` → `AssemblyUtil.cs` | ✅ 已重命名 |
| 6 | `SqlSugarOptions` 使用 public 字段而非属性 | ✅ 已改属性 |
| 7 | `AssemblyContext` 公有参数构造器绕过单例 | ✅ 已改 internal |
| 8 | `throw new Exception(...)` 应抛出更具体的异常类型 | ✅ 已改 `InvalidOperationException` |
| 9 | `GlobalSuppressions.cs` Justification 为 `"<Pending>"` | ✅ 已补填 |
| 10 | `Shark.cs:51-57` 注释掉的死代码 | ✅ 已删除 |
| 11 | `UrlToDictionary()` 定义但从未被调用（死代码） | ✅ 已删除 |
| 12 | `_global.cs:13` — `global using Sharkable;` 冗余 | ❌ 不冗余，entry point 扩展类在别的 namespace |
| 13 | `UnifiedResultExtension.AsUnauthorized()` 空检查无效 | ✅ 已简化 |
| 14 | `SharkEndpoint.cs:7` — ReSharper 特定的 `SuppressMessage` | ❌ 保留，无功能等价替代 | |

---

## 🟠 XML 文档缺失

| 违反 AGENTS.md（公有 API 必须 XML 注释） | 位置 |
|---|---|
| `SqlSugarOptions` 整个类 + 所有属性 | `AutoCrud/SqlSugar/Options/` |
| `DbType` / `LanguageType` / `InitKeyType` enum + 所有成员 | `AutoCrud/SqlSugar/Enums/` |
| `Shark.GetService<T>()` 空 `<returns>` | `Shark/Shark.cs:72` |

---

## 🔵 测试缺口

| 模块 | 覆盖率 |
|---|---|
| MultiTenant | 0% — 没有测试 |
| RedactingLogger | 0% — 没有测试 |
| ValidationFilter | 0% — 没有测试 |
| StringExtensions | 0% — 没有测试 |
| AutoCrud/SqlSugar | 0% — 没有测试 |
| Reflector | 0% — 没有测试 |
| UnifiedResultExtension | 0% — 没有测试 |
| SharkEndpointDsl | 0% — 没有测试 |

---

## ⚪ 功能缺口评估

### 已有功能（审核误判修正）

| 功能 | 状态 | 说明 |
|---|---|---|
| API 版本控制 | ✅ 已实现 | `[SharkVersion("v2")]` 生成 `/api/v2/{group}/{route}` URL 路径 |
| 限流 | ✅ 已实现 | `SharkRequireRateLimiting` + `opt.RateLimiterConfigure` |
| 输出缓存 | ✅ 已实现 | `SharkCacheOutput` + `opt.OutputCacheConfigure` |
| CORS | ✅ 已实现 | `opt.CorsConfigure` |
| 健康检查 | ✅ 已实现 | `opt.EnableHealthChecks` |
| JWT 认证 | ✅ 已实现 | `opt.ConfigureJwt()` |
| API Key 认证 | ✅ 已实现 | `opt.ApiKeys` |
| 审计日志 | ✅ 已实现 | `opt.ConfigureAuditTrail()` |
| 幂等中间件 | ✅ 已实现 | `opt.EnableIdempotency` |

### 实际缺口

**中等优先级：**

1. **ISharkEndpoint 级别 OpenAPI 元数据** — 目前没有在 endpoint 类上声明 summary / description / request example 的约定方式。用户在 `AddRoutes()` 里必须手动调用 `.WithDescription()` / `.WithSummary()`。可以加 `[SharkDescription]` 属性或让 `ISharkEndpoint` 返回元数据对象。

2. **异常处理器可观察性** — `ExceptionHandlerMiddleware` 在 catch 之后没有记录日志。用户看到 `UnifiedResult` 错误响应，但服务端没有日志追踪异常根源。

3. **AutoCrud 没有单元测试** — 8 个 enum 文件 + 1 个 options 文件 + 1 个 extension 文件完全无测试，也不参与构建验证。

**低优先级：**

- 调试 / 开发模式自动丰富错误信息（当前异常处理器不暴露堆栈或内部细节）
- Content negotiation / 响应格式协商

---

## 处理建议

### 立即做（30 分钟）

1. Bug 1: 去掉 `OpenApiConfigure` / `SqlSugarOptionsConfigure` 的 `static`
2. Bug 2: 修 `TenantResolutionMiddleware`
3. #10: 删注释掉的死代码
4. #9: 补填 `GlobalSuppressions` 的 `Justification`

### 应该做（2 小时）

5. AOT 3: 添加 `[RequiresDynamicCode]`
6. AOT 4: 修复 `CreateSharkEndpoint`
7. 补 `SqlSugarOptions` 和 `DbType` 等 enum 的 XML 文档
8. #5: 重命名 `AssembliyUtil.cs`

### 可以做

9. 补充 MultiTenant 和 Validation 测试
10. 异常处理器加日志
11. `ISharkEndpoint` 元数据属性
