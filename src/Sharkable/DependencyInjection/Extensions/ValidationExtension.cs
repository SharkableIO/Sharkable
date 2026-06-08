
using FluentValidation;

namespace Sharkable;

/// <summary>
/// Scans assemblies for <see cref="IValidator{T}"/> implementations and registers them in DI.
/// Skipped in AOT mode — validators must be registered manually.
/// </summary>
internal static class ValidationExtension
{
    /// <summary>
    /// Scans all registered assemblies for concrete <see cref="IValidator{T}"/> implementations
    /// and registers each as a singleton keyed by its validator interface.
    /// No-op when <see cref="SharkOption.EnableValidation"/> is false or in AOT mode.
    /// </summary>
    internal static void AddValidators(this IServiceCollection services)
    {
        if (!Shark.SharkOption.EnableValidation)
            return;

        if (Shark.SharkOption.AotMode)
        {
            Utils.WriteDebug("Validation scanning skipped in AOT mode. Register validators manually.");
            return;
        }

        var assemblies = Shark.Assemblies;
        if (assemblies == null || assemblies.Length == 0)
            return;

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                foreach (var iface in type.GetInterfaces())
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IValidator<>))
                    {
                        Utils.WriteDebug($"registering validator: {iface.Name} -> {type.Name}");
                        services.AddSingleton(iface, type);
                        break;
                    }
                }
            }
        }
    }
}
