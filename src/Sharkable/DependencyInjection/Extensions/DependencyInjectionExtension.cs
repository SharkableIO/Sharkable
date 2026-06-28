#pragma warning disable CS0618 // Internal use of legacy attribute-based endpoint system

namespace Sharkable;

internal static class DependencyInjectionExtension
{
    internal static void AddDiFactory(this IServiceCollection services)
    {
        services.AddSingleton<IDependencyReflectorFactory, DependencyReflectorFactory>();
#pragma warning restore CS0618
        services.AddSingleton<IUnifiedResultFactory, DefaultUnifiedResultFactory>();
    }
}
