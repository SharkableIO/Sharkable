using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using Sharkable;
using Sharkable.AotSample;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateSlimBuilder(args);

// ── AOT JSON serialization ────────────────────────────────────────
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

// ── Sharkable: register services, discover endpoints ──────────────
builder.Services.AddShark([typeof(Program).Assembly], opt =>
{
    // Endpoint naming
    opt.Format = EndpointFormat.CamelCase;
    opt.ApiPrefix = "api";

    // OpenAPI documentation
    opt.ConfigureOpenApi(o =>
    {
        o.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
    });

    // Scalar UI
    opt.ConfigureScalar(o =>
    {
        o.Title = "Sharkable Demo API";
        o.DefaultOpenAllTags = true;
    });

    // CORS — allow all origins for demo
    opt.CorsConfigure = cors => cors.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
    );

    // JWT Bearer authentication
    opt.ConfigureJwt(
        authority: "https://demo.sharkable.io",
        audiences: ["sharkable-demo"],
        configure: o =>
        {
            o.TokenValidationParameters.ValidateIssuer = false;
            o.TokenValidationParameters.ValidateAudience = false;
        }
    );

    // API Key authentication
    opt.ApiKeys = ["demo-key"];

    // Rate limiting
    opt.RateLimiterConfigure = limiter =>
    {
        limiter.AddFixedWindowLimiter("fixed", o =>
        {
            o.PermitLimit = 10;
            o.Window = TimeSpan.FromMinutes(1);
            o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            o.QueueLimit = 2;
        });
    };

    // Output cache
    opt.OutputCacheConfigure = cache =>
    {
        cache.AddPolicy("todos", build =>
        {
            build.Expire(TimeSpan.FromSeconds(30));
        });
        cache.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(15);
    };

    // Idempotency
    opt.EnableIdempotency = true;
    opt.ConfigureIdempotency(o =>
    {
        o.Ttl = TimeSpan.FromMinutes(10);
    });

    // Audit trail
    opt.ConfigureAuditTrail(o =>
    {
        o.IncludeQueryString = true;
        o.ExcludePaths = ["/healthz", "/api/info"];
    });

    // Structured log redaction
    opt.ConfigureRedactingLog(o =>
    {
        o.RedactFields = ["password", "secret", "token", "apiKey", "authorization"];
    });

    // Multi-tenant (header-based resolver for demo)
    opt.ConfigureMultiTenant(o =>
    {
        o.ResolveTenant = ctx => ctx.Request.Headers.TryGetValue("X-Tenant-Id", out var tid)
            ? tid.FirstOrDefault()
            : null;
    });

    // FluentValidation
    opt.EnableValidation = true;

    // Health checks
    opt.EnableHealthChecks = true;

    // Exception handler
    opt.ExceptionHandlerOptions.IsDevelopment =
        builder.Environment.IsDevelopment();
});

// ── Manual validator registration (AOT: auto-scan doesn't work) ──
builder.Services.AddSingleton<IValidator<CreateTodoRequest>, TodoValidator>();
builder.Services.AddAuthorization();

// ── Build ──────────────────────────────────────────────────────────
var app = builder.Build();

app.UseShark();

// ── Seed sample data ──────────────────────────────────────────────
var store = app.Services.GetRequiredService<ITodoService>();
store.Create(new CreateTodoRequest("Learn Sharkable", "Go through the docs and samples", TodoPriority.High, DateOnly.FromDateTime(DateTime.Now.AddDays(3))));
store.Create(new CreateTodoRequest("Build an API", "Create a demo API with all Sharkable features", TodoPriority.Medium));
store.Create(new CreateTodoRequest("Write tests", "Add unit tests for the demo service", TodoPriority.Low, DateOnly.FromDateTime(DateTime.Now.AddDays(7))));
store.Create(new CreateTodoRequest("Deploy to production", null, TodoPriority.High, DateOnly.FromDateTime(DateTime.Now.AddDays(14))));

app.Run();

// ── AOT JSON serialization contexts ───────────────────────────────
[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(Todo[]))]
[JsonSerializable(typeof(CreateTodoRequest))]
[JsonSerializable(typeof(UpdateTodoRequest))]
[JsonSerializable(typeof(VersionInfo))]
[JsonSerializable(typeof(TodoListResponse))]
[JsonSerializable(typeof(FeatureFlagsResponse))]
[JsonSerializable(typeof(UnifiedResult<object>))]
[JsonSerializable(typeof(UnifiedResult<Todo>))]
[JsonSerializable(typeof(UnifiedResult<Todo[]>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
