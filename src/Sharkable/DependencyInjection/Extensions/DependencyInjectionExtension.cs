
namespace Sharkable;

internal static class DependencyInjectionExtension
{
    internal static void AddDIFactory(this IServiceCollection services)
    {
        services.AddSingleton<IDependencyReflectorFactory, DependencyReflectorFactory>();
        services.AddSingleton<Injector>();
    }
}
