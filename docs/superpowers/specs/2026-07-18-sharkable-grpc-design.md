# Sharkable.Grpc — Design Spec

> Date: 2026-07-18 | Status: Draft | Author: CharleyPeng

## Overview

`Sharkable.Grpc` is a companion NuGet package that auto-generates HTTP endpoints for gRPC services. Users configure gRPC server addresses and credentials; the package discovers service methods (via proto-compiled types or gRPC Server Reflection) and maps each gRPC method to an HTTP route. Integrates via the `ISharkPlugin` mechanism — zero changes to the core `Sharkable` package.

## Goals

- One-line config: `builder.Services.AddSharkableGrpc(opt => { ... })`
- Two discovery modes: compile-time `.proto` scanning (A) + runtime ServerReflection (B)
- All four gRPC method types: Unary, ServerStreaming, ClientStreaming, BidiStreaming
- Full replacement points: discoverer, mapper, auth interceptor, serializer — each with `Func<IServiceProvider, T>` factory
- OpenAPI integration: proto message schemas auto-registered
- Admin panel at `/_sharkable/grpc`
- Opt-in features: retry, circuit breaker, metrics, request logging

## Non-Goals

- gRPC server-side hosting (this package proxies HTTP ↔ gRPC, not hosting gRPC services)
- Streaming in v1 beyond the four core types (no gRPC-Web or transcoding beyond NDJSON/WebSocket)

---

## Architecture

### Package

| Item | Value |
|---|---|
| Package | `Sharkable.Grpc` |
| TFM | `net10.0` |
| Dependencies | `Sharkable` (≥ 0.7.0), `Grpc.Net.Client`, `Google.Protobuf` |
| Optional | `Grpc.Tools` (PrivateAssets=all, only for users who compile `.proto`) |

### Integration Point

`GrpcPlugin : ISharkPlugin` — auto-discovered by `PluginLoader` from assemblies. Exposes three lifecycle hooks:

```
ISharkPlugin.Name = "Sharkable.Grpc"
ConfigureServices(services, option)  → channel creation, method discovery
ConfigurePipeline(app, option)       → HTTP endpoint mapping
ConfigureOpenApi(openApiOpts, option) → proto schema registration
```

### Lifecycle Timing

**During `AddShark()`:**
```
1. WireSharkEndpoint()            ← discover ISharkEndpoint types
2. DiscoverAndConfigurePlugins()
   └─ GrpcPlugin.ConfigureServices()
      ├─ Read GrpcOptions from DI
      ├─ Create GrpcChannel per service (register as singleton)
      ├─ IGrpcServiceDiscoverer.DiscoverAsync() → List<GrpcMethodDef>
      ├─ Pre-compile handler delegates per method
      └─ Store GrpcMethodDef list on plugin instance
3. ConfigurationValidator.Validate()
```

**During `UseShark()`:**
```
1. Rate limiter / CORS / ...
2. GrpcPlugin.ConfigurePipeline(app, option)  ← maps all gRPC HTTP routes
3. Auth middleware
4. Audit / Idempotency / ETag / ...
5. app.MapEndpoints()  ← framework's own endpoint mapping
```

`ConfigurePipeline` receives `WebApplication app` (which is `IEndpointRouteBuilder`), so direct `app.MapPost()`, `app.MapGet()`, `app.Map()` are available.

---

## Configuration Model

### Entry Point

```csharp
builder.Services.AddSharkableGrpc(opt =>
{
    opt.RequestTimeout = TimeSpan.FromSeconds(30);
    opt.MaxResponseBodySize = 4 * 1024 * 1024;
    opt.EnableOpenApi = true;
    opt.EnableMetrics = false;
    opt.EnableRequestLogging = false;
    opt.GrpcRoutePrefix = null;  // null → "/api/grpc"

    // Replace any built-in component
    opt.ServiceDiscovererFactory = sp => new MyDiscoverer();
    opt.HttpMapperFactory = sp => new MyMapper();
    opt.AuthInterceptorFactory = sp => new MyAuthInterceptor();
    opt.MessageSerializerFactory = sp => new MySerializer();

    // Register a service (Mode B: reflection)
    opt.AddService("user-service", svc =>
    {
        svc.Address = "https://user-api:5001";
        svc.Credentials = GrpcServiceCredentials.Ssl("path/to/cert.pem");
        svc.UseReflection = true;
        svc.HttpPrefix = "user";
        svc.EnableRetry = true;
        svc.EnableCircuitBreaker = true;
        svc.ChannelFactory = sp => GrpcChannel.ForAddress("...", new() { ... });
        svc.MethodFilter = def => def.MethodName != "InternalMethod";
        svc.EndpointConvention = builder => builder.RequireAuthorization();
    });
});
```

### GrpcOptions Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `RequestTimeout` | `TimeSpan?` | 30s | Per-call gRPC timeout |
| `MaxResponseBodySize` | `long?` | 4MB | Cap response body allocation |
| `EnableOpenApi` | `bool?` | true | Register proto schemas in OpenAPI |
| `EnableMetrics` | `bool?` | false | gRPC call metrics → OpenTelemetry |
| `EnableRequestLogging` | `bool?` | false | Log method/duration/status per call |
| `EnableAdminPanel` | `bool?` | false | Map `/_sharkable/grpc` admin endpoint |
| `GrpcRoutePrefix` | `string?` | null (="/api/grpc") | Override HTTP route prefix |
| `ServiceDiscovererFactory` | `Func<IServiceProvider, IGrpcServiceDiscoverer>?` | null | Replace method discovery |
| `HttpMapperFactory` | `Func<IServiceProvider, IGrpcHttpMapper>?` | null | Replace HTTP route mapping |
| `AuthInterceptorFactory` | `Func<IServiceProvider, IGrpcAuthInterceptor>?` | null | Replace auth forwarding |
| `MessageSerializerFactory` | `Func<IServiceProvider, IGrpcMessageSerializer>?` | null | Replace proto ↔ JSON serialization |
| `Services` | `List<GrpcServiceConfig>` | — | Registered gRPC services |

### GrpcServiceConfig Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Name` | `string` | — | Logical name for diagnostics |
| `Address` | `string?` | null | gRPC server URL |
| `Credentials` | `GrpcServiceCredentials?` | Insecure | Channel credentials |
| `UseReflection` | `bool?` | false | Mode B: runtime reflection discovery |
| `HttpPrefix` | `string?` | null | Override HTTP route segment for this service |
| `HealthCheckPath` | `string?` | null | gRPC health check path (default: none) |
| `EnableRetry` | `bool?` | false | Exponential backoff retry (3 attempts, 5s max) |
| `EnableCircuitBreaker` | `bool?` | false | Open circuit after 5 consecutive failures, half-open after 30s |
| `ChannelFactory` | `Func<IServiceProvider, GrpcChannel>?` | null | Replace channel creation |
| `MethodFilter` | `Func<GrpcMethodDef, bool>?` | null | Exclude specific methods |
| `EndpointConvention` | `Action<RouteHandlerBuilder>?` | null | Per-endpoint auth/cache/etc. |

---

## Core Abstractions

### IGrpcServiceDiscoverer

```
Task<List<GrpcMethodDef>> DiscoverAsync(
    GrpcServiceConfig config,
    GrpcChannel channel,
    IServiceProvider services,
    CancellationToken ct)
```

Two built-in implementations:

**AssemblyScanDiscoverer** (Mode A):
- Scans `Shark.Assemblies` for classes inheriting `ClientBase<TClient>` (Grpc.Tools generated)
- Extracts methods, request/response CLR types, method type via reflection
- AOT-safe (all types are compile-time known)

**ReflectionDiscoverer** (Mode B):
- Connects to gRPC server, calls ServerReflection `ListServices()` → `FileContainingSymbol()`
- Parses `ServiceDescriptorProto` → method list
- Uses `DynamicMessage` for runtime serialization (no compile-time types)
- Blocked when `InternalShark.AotMode == true` (throws at startup)

### IGrpcHttpMapper

```
void MapEndpoints(
    IEndpointRouteBuilder app,
    GrpcServiceConfig config,
    List<GrpcMethodDef> methods,
    SharkOption option)
```

Default implementation maps each method type:

| gRPC Type | HTTP Method | URL Pattern | Body Format |
|---|---|---|---|
| Unary | `POST` | `/{prefix}/{service}/{method}` | JSON ↔ JSON |
| ServerStreaming | `POST` | `/{prefix}/{service}/{method}` | JSON → NDJSON stream |
| ClientStreaming | `POST` | `/{prefix}/{service}/{method}` | NDJSON stream → JSON |
| BidiStreaming | `GET` (WebSocket upgrade) | `/ws/{prefix}/{service}/{method}` | JSON frames ↔ JSON frames |

NDJSON format: `application/x-ndjson`, one JSON-encoded proto message per line.

### IGrpcAuthInterceptor

```
void Intercept(ServerCallContext context, Metadata headers)
```

Calls per outgoing gRPC request. Default implementations:
- `NoneGrpcAuth`: pass nothing
- `ForwardJwtAuth`: copy `Authorization: Bearer xxx` from HttpContext
- `ForwardApiKeyAuth`: copy `X-Api-Key` from HttpContext

### IGrpcMessageSerializer

```
object? Deserialize(byte[] data, Type? type, MessageDescriptor? descriptor)
byte[] Serialize(object message, Type? type, MessageDescriptor? descriptor)
```

Default uses `JsonFormatter`/`JsonParser` (with well-known type support) for Mode B. Mode A routes through generated types' native JSON serialization when available, falling back to `JsonFormatter` otherwise.

---

## GrpcMethodDef

```csharp
internal sealed class GrpcMethodDef
{
    required string ServiceName { get; init; }
    required string MethodName { get; init; }
    required GrpcMethodType Type { get; init; }
    Type? RequestClrType { get; init; }       // null for Mode B
    Type? ResponseClrType { get; init; }      // null for Mode B
    MessageDescriptor? RequestDescriptor { get; init; }  // null for Mode A
    MessageDescriptor? ResponseDescriptor { get; init; } // null for Mode A
    required string Route { get; init; }
    required GrpcChannel Channel { get; init; }
    required Func<HttpContext, Task> Handler { get; init; }
}
```

`Route` format: `{GrpcRoutePrefix}/{HttpPrefix ?? ServiceName}/{MethodName}`
Default prefix: `/api/grpc`
Example: `POST /api/grpc/user/UserService/GetUser`

---

## Error Handling

### gRPC Status → HTTP Status

| gRPC StatusCode | HTTP Status |
|---|---|
| OK | 200 |
| InvalidArgument | 400 |
| Unauthenticated | 401 |
| PermissionDenied | 403 |
| NotFound | 404 |
| AlreadyExists | 409 |
| ResourceExhausted | 429 |
| FailedPrecondition | 400 |
| Aborted | 409 |
| OutOfRange | 400 |
| Unimplemented | 501 |
| Internal | 500 |
| Unavailable | 503 |
| DataLoss | 500 |
| DeadlineExceeded | 504 |

Error response body follows the active Sharkable error format (`UnifiedResult` or `ProblemDetails`, per global config). gRPC `google.rpc.Status.details` are included in the `errors` field.

### Handler-Level Guards

- Every handler wraps its call in try/catch; unhandled exceptions → 500
- `CancellationToken` linked from `HttpContext.RequestAborted` passed to every gRPC call
- Response body stream write errors (client disconnect) are logged at Debug level, not thrown

---

## Security

| Concern | Mitigation |
|---|---|
| SSRF | Validate `Address` at startup — reject loopback unless explicitly allowed |
| Timeout | `RequestTimeout` default 30s; per-call `CancellationToken` bound |
| Response size | `MaxResponseBodySize` caps allocation (4MB default) |
| Header injection | Validate gRPC metadata keys/values for `\r\n` before forwarding |
| Auth forwarding | Only forward `Authorization` header; never forward cookies |
| Logging | Redact `authorization`, `api-key`, `x-api-key` metadata keys |
| Proto recursion | Max nesting depth = 64; reject deeper messages as 400 |

---

## Streaming Details

### ServerStreaming Handler

```
1. Read request body → deserialize proto
2. Call gRPC ServerStreaming
3. Set response Content-Type: application/x-ndjson
4. Set response status 200
5. while (await responseStream.MoveNext(ct))
       serialize response → JSON line + \n → write to response body
6. Response body stream complete
```

### ClientStreaming Handler

```
1. Set up request stream from gRPC call
2. Read request body line-by-line (Content-Type: application/x-ndjson)
3. foreach line: deserialize → requestStream.WriteAsync(proto)
4. requestStream.CompleteAsync()
5. Await response
6. Serialize response → JSON → return 200
```

### BidiStreaming Handler

```
1. Accept WebSocket upgrade
2. Start two concurrent loops:
   a. Read loop: WebSocket.ReceiveAsync → deserialize → requestStream.WriteAsync
   b. Write loop: responseStream.MoveNext → serialize → WebSocket.SendAsync
3. Either side closes → close other side → close WebSocket
```

---

## Admin Panel

Path: `GET /_sharkable/grpc` (requires API key if `ApiKeys` is configured; returns 404 if none set)

Response:
```json
{
  "services": [
    {
      "name": "user-service",
      "address": "https://user-api:5001",
      "state": "Healthy",
      "methods": [
        { "name": "GetUser", "type": "Unary", "route": "POST /api/grpc/user/GetUser" }
      ]
    }
  ]
}
```

---

## File Structure

```
src/Sharkable.Grpc/
├── Sharkable.Grpc.csproj
├── Sharkable.Grpc.nuspec
├── SharkGrpcPlugin.cs
├── GrpcOptions.cs
├── GrpcServiceConfig.cs
├── GrpcServiceCredentials.cs
├── GrpcMethodDef.cs
├── GrpcAuthForwarding.cs                    // enum: None / ForwardJwt / ForwardApiKey
├── GrpcStatusMapper.cs
├── Abstractions/
│   ├── IGrpcServiceDiscoverer.cs
│   ├── IGrpcHttpMapper.cs
│   ├── IGrpcAuthInterceptor.cs
│   └── IGrpcMessageSerializer.cs
├── Discoverers/
│   ├── AssemblyScanDiscoverer.cs
│   └── ReflectionDiscoverer.cs
├── Mapping/
│   ├── DefaultGrpcHttpMapper.cs
│   ├── UnaryHandler.cs
│   ├── ServerStreamHandler.cs
│   ├── ClientStreamHandler.cs
│   └── BidiStreamHandler.cs
├── Serialization/
│   └── DefaultGrpcMessageSerializer.cs
├── Auth/
│   ├── DefaultGrpcAuthInterceptor.cs
│   ├── ForwardJwtAuth.cs
│   └── ForwardApiKeyAuth.cs
├── Admin/
│   └── GrpcAdminEndpoint.cs
└── Extensions/
    └── SharkableGrpcExtensions.cs
```

---

## Usage Example

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSharkableGrpc(opt =>
{
    opt.AddService("user-service", svc =>
    {
        svc.Address = "https://user-api:5001";
        svc.Credentials = GrpcServiceCredentials.Insecure;
        svc.UseReflection = true;
        svc.EnableRetry = true;
        svc.EndpointConvention = builder => builder.RequireAuthorization();
    });

    opt.AddService("payment-service", svc =>
    {
        svc.Address = "https://payment-api:5002";
        svc.UseReflection = false;  // Mode A: scan Grpc.Tools generated types
        svc.HttpPrefix = "payments";
        svc.MethodFilter = def => !def.MethodName.StartsWith("Internal");
    });

    opt.EnableMetrics = true;
    opt.AuthInterceptorFactory = sp => new ForwardJwtAuth();
});

builder.Services.AddShark();

var app = builder.Build();
app.UseShark();
app.Run();
```

Generated routes:
```
POST /api/grpc/user/UserService/GetUser         → user-api:5001
POST /api/grpc/user/UserService/ListUsers       → user-api:5001 (stream)
POST /api/grpc/payments/PaymentService/Charge   → payment-api:5002
POST /api/grpc/payments/PaymentService/Refund   → payment-api:5002
```
