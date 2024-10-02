namespace Sharkable;

internal static class JsonContextExtension
{
    internal static IServiceCollection AddJsonContext(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, UnifiedResultSourceContext.Default);
        });
        return services;
    }
}