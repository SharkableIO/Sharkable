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
        if (opt.JwtAuthority is not null)
        {
            scalar.AddHttpAuthentication("bearer", bearer =>
            {
                bearer.Token = "your-jwt-token";
            });
        }
        if (opt.ApiKeys is { Length: > 0 })
        {
            scalar.AddApiKeyAuthentication("apiKey", apiKey =>
            {
                apiKey.Value = "your-api-key";
            });
        }
    }
}
