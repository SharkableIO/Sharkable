
using FluentValidation;

namespace Sharkable;

internal static class ValidationExtension
{
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
