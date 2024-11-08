
namespace Sharkable;

internal static class DependencyInjectionExtension
{
    internal static void AddDiFactory(this IServiceCollection services)
    {
        services.AddSingleton<IDependencyReflectorFactory, DependencyReflectorFactory>();
    }
}
