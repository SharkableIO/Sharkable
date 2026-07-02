using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Sharkable;

internal static class OpenApiExtension
{
    internal static IServiceCollection AddSharkOpenApi(this IServiceCollection services)
    {
        if (Shark.SharkOption.UseOpenApi)
        {
            services.AddOpenApi(options =>
            {
                Shark.SharkOption.OpenApiConfigure?.Invoke(options);

                if (Shark.SharkOption.EnableAutoWrap)
                {
                    options.AddDocumentTransformer((document, context, cancellationToken) =>
                    {
                        foreach (var pathItem in document.Paths.Values)
                        {
                            if (pathItem?.Operations == null)
                                continue;
                            foreach (var operation in pathItem.Operations.Values)
                            {
                                if (operation?.Responses == null)
                                    continue;
                                foreach (var response in operation.Responses.Values)
                                {
                                    if (response?.Content == null)
                                        continue;
                                    if (response.Content.TryGetValue("application/json", out var mediaType)
                                        && mediaType.Schema is OpenApiSchema original
                                        && original.Properties?.ContainsKey("data") != true)
                                    {
                                        var wrapSchema = Shark.SharkOption.WrapSchemaFactory;
                                        mediaType.Schema = wrapSchema != null
                                            ? wrapSchema(original)
                                            : DefaultUnifiedResultSchema(original);
                                    }
                                }
                            }
                        }
                        return Task.CompletedTask;
                    });
                }
            });
        }
        return services;
    }

    private static OpenApiSchema DefaultUnifiedResultSchema(OpenApiSchema original)
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["statusCode"] = new OpenApiSchema { Type = JsonSchemaType.Integer },
                ["data"] = original,
                ["errorMessage"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["extra"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["timeStamp"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int64" },
            },
        };
    }

    internal static WebApplication UseSharkOpenApi(this WebApplication app)
    {
        if (Shark.SharkOption.UseOpenApi)
        {
            app.MapOpenApi();
            var opt = Shark.SharkOption;
            app.MapScalarApiReference(scalar =>
            {
                ApplyAutoAuth(opt, scalar);
                opt.ScalarConfigure?.Invoke(scalar);
            });
        }
        return app;
    }

    private static void ApplyAutoAuth(SharkOption opt, ScalarOptions scalar)
    {
        // SHARK-SEC-009: never pre-fill tokens in non-Development environments
        // — the Scalar UI bundle is served from /scalar/v1 to any caller, and
        // baking a JWT/API key into it would leak it to anyone who can reach
        // the documentation page.
        var isDevelopment = InternalShark.HostEnvironment?.IsDevelopment() ?? false;

        if (opt.JwtAuthority is not null)
        {
            scalar.AddHttpAuthentication("bearer", bearer =>
            {
                bearer.Token = isDevelopment ? (opt.ScalarJwtToken ?? "your-jwt-token") : "";
            });
            if (!isDevelopment && !string.IsNullOrEmpty(opt.ScalarJwtToken))
                WarnLeakedCredential("ScalarJwtToken");
        }
        if (opt.ApiKeys is { Length: > 0 })
        {
            scalar.AddApiKeyAuthentication("apiKey", apiKey =>
            {
                apiKey.Value = isDevelopment ? (opt.ScalarApiKeyValue ?? "your-api-key") : "";
            });
            if (!isDevelopment && !string.IsNullOrEmpty(opt.ScalarApiKeyValue))
                WarnLeakedCredential("ScalarApiKeyValue");
        }
    }

    private static void WarnLeakedCredential(string propertyName)
    {
        var envName = InternalShark.HostEnvironment?.EnvironmentName ?? "<unknown>";
        var message = $"[Sharkable] {propertyName} is set but the current environment " +
                      $"('{envName}') is not Development. The value will NOT be embedded in the " +
                      $"Scalar UI served at /scalar/v1 to avoid leaking credentials. " +
                      $"Set {propertyName} only in Development, or gate it via " +
                      $"IHostEnvironment.IsDevelopment().";

        var logger = InternalShark.ServiceProvider?.GetService<ILoggerFactory>()?.CreateLogger("Sharkable");
        if (logger != null)
            logger.LogWarning(message);
        else
            Console.WriteLine(message);
    }
}
