using System.Reflection;
using Microsoft.AspNetCore.OpenApi;
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

                // Plugin OpenAPI transforms
                if (Shark.SharkOption.DiscoveredPlugins is { Count: > 0 })
                {
                    foreach (var plugin in Shark.SharkOption.DiscoveredPlugins)
                    {
                        try
                        {
                            plugin.ConfigureOpenApi(options, Shark.SharkOption);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"[Sharkable] Plugin '{plugin.Name}' OpenAPI registration threw: {ex.Message}");
                        }
                    }
                }

                // SHARK-SEC-L009: install the schema transformer that strips
                // properties marked [SharkOpenApiIgnore] from every schema
                // in the generated document. Without this transformer a
                // response DTO containing a Password/RefreshToken/ApiSecret
                // property would expose those fields in the public
                // /openapi/v1.json — which any anonymous browser can read.
                options.AddSchemaTransformer(RemoveSensitiveProperties);

                // User-registered operation transformers
                foreach (var transformer in Shark.SharkOption.OpenApiOperationTransformers)
                    options.AddOperationTransformer(transformer);

                // User-registered schema transformers
                foreach (var transformer in Shark.SharkOption.OpenApiSchemaTransformers)
                    options.AddSchemaTransformer(transformer);

                // Convert ObsoleteAttribute from endpoint metadata to deprecated: true
                options.AddOperationTransformer((operation, context, cancellationToken) =>
                {
                    var metadata = context.Description?.ActionDescriptor?.EndpointMetadata;
                    if (metadata != null && metadata.Any(m => m is ObsoleteAttribute))
                        operation.Deprecated = true;
                    return Task.CompletedTask;
                });

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

    /// <summary>
    /// Schema transformer that removes any property marked
    /// <see cref="SharkOpenApiIgnoreAttribute"/> from the generated
    /// OpenAPI schema. Honors both <c>JsonIgnoreCondition.Always</c> /
    /// <c>[JsonIgnore]</c> via System.Text.Json (already wired by
    /// Microsoft.OpenApi's built-in pipeline) plus the framework-specific
    /// <see cref="SharkOpenApiIgnoreAttribute"/> for callers who do not
    /// want a System.Text.Json dependency (SHARK-SEC-L009).
    /// </summary>
    private static Task<OpenApiSchema> RemoveSensitiveProperties(
        OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (schema.Properties == null || schema.Properties.Count == 0)
            return Task.FromResult(schema);

        var type = context.JsonTypeInfo.Type;
        if (type == null) return Task.FromResult(schema);

        foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance))
        {
            var ignoreAttr = member.GetCustomAttribute<SharkOpenApiIgnoreAttribute>();
            if (ignoreAttr == null) continue;

            var jsonName = member.Name;
            if (member.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>() is { } jpna)
                jsonName = jpna.Name;

            if (schema.Properties.ContainsKey(jsonName))
                schema.Properties.Remove(jsonName);

            if (!string.IsNullOrEmpty(schema.Required?.Count > 0 ? null : null)
                && schema.Required?.Remove(jsonName) == true)
            {
                // best-effort: schema may or may not track Required separately.
            }
        }

        return Task.FromResult(schema);
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
            if (!InternalShark.HostEnvironment.IsDevelopment())
            {
                var logger = app.Services.GetService<ILoggerFactory>()
                    ?.CreateLogger("Sharkable.OpenApi");
                logger?.LogWarning(
                    "OpenAPI and Scalar UI are enabled in a non-Development environment. " +
                    "Full API surface is exposed at /openapi/v1.json and /scalar/v1. " +
                    "Disable with opt.UseOpenApi = false in production.");
            }

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
