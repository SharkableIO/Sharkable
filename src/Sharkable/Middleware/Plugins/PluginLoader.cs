using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;

namespace Sharkable;

internal static class PluginLoader
{
    private const string DllSearchPattern = "*.dll";

    /// <summary>
    /// Discovers plugins from all three paths and returns deduplicated instances.
    /// Order: NuGet/assembly scanning → folder scanning → manual registration.
    /// First registration wins per Name; subsequent duplicates are logged and skipped.
    /// </summary>
    internal static List<ISharkPlugin> Discover(SharkOption option, ILogger? logger = null)
    {
        var plugins = new List<ISharkPlugin>();

        DiscoverFromAssemblies(option, plugins, logger);
        DiscoverFromFolder(option, plugins, logger);
        DiscoverManual(option, plugins, logger);

        return plugins;
    }

    private static void DiscoverFromAssemblies(SharkOption option, List<ISharkPlugin> plugins, ILogger? logger)
    {
        if (!option.AutoDiscoverPlugins)
            return;

        var assemblies = Shark.Assemblies;
        if (assemblies == null)
            return;

        foreach (var assembly in assemblies)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                logger?.LogDebug(ex, "PluginLoader: skipping assembly {Assembly} — type load failed",
                    assembly.GetName().Name);
                continue;
            }

            foreach (var type in types)
            {
                if (type is { IsAbstract: true } or { IsInterface: true } ||
                    !typeof(ISharkPlugin).IsAssignableFrom(type) ||
                    type.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                try
                {
                    var plugin = (ISharkPlugin)Activator.CreateInstance(type)!;
                    TryAdd(option, plugins, plugin, $"assembly {assembly.GetName().Name}", logger);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "PluginLoader: failed to instantiate {Type}", type.FullName);
                }
            }
        }
    }

    private static void DiscoverFromFolder(SharkOption option, List<ISharkPlugin> plugins, ILogger? logger)
    {
        var pluginOpts = option.PluginOptions;
        if (pluginOpts == null || !pluginOpts.ScanOnStartup)
            return;

        var root = Path.GetFullPath(pluginOpts.Directory);
        if (!Directory.Exists(root))
        {
            logger?.LogDebug("PluginLoader: plugins directory '{Dir}' does not exist — skipping", root);
            return;
        }

        logger?.LogDebug("PluginLoader: scanning '{Dir}' for plugins", root);

        foreach (var subDir in Directory.EnumerateDirectories(root))
        {
            var plugin = LoadFromFolder(subDir, logger);
            if (plugin != null)
                TryAdd(option, plugins, plugin, $"folder {Path.GetFileName(subDir)}", logger);
        }
    }

    private static ISharkPlugin? LoadFromFolder(string folder, ILogger? logger)
    {
        var dllFiles = Directory.GetFiles(folder, DllSearchPattern);

        foreach (var dllPath in dllFiles)
        {
            try
            {
                var alc = new AssemblyLoadContext(Path.GetFileName(folder), isCollectible: true);
                var resolveDep = new AssemblyDependencyResolver(dllPath);
                alc.Resolving += (ctx, name) =>
                {
                    var resolvedPath = resolveDep.ResolveAssemblyToPath(name);
                    if (resolvedPath != null)
                        return ctx.LoadFromAssemblyPath(resolvedPath);
                    return null;
                };

                var assembly = alc.LoadFromAssemblyPath(dllPath);

                foreach (var type in assembly.GetTypes())
                {
                    if (type is { IsAbstract: true } or { IsInterface: true } ||
                        !typeof(ISharkPlugin).IsAssignableFrom(type) ||
                        type.GetConstructor(Type.EmptyTypes) == null)
                        continue;

                    var plugin = (ISharkPlugin)Activator.CreateInstance(type)!;
                    logger?.LogInformation("PluginLoader: loaded '{PluginName}' from {Folder}",
                        plugin.Name, Path.GetFileName(folder));
                    return plugin;
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "PluginLoader: failed to load plugin from {DllPath}", dllPath);
            }
        }

        logger?.LogDebug("PluginLoader: no ISharkPlugin found in folder {Folder}", Path.GetFileName(folder));
        return null;
    }

    private static void DiscoverManual(SharkOption option, List<ISharkPlugin> plugins, ILogger? logger)
    {
        foreach (var plugin in option.ManualPlugins)
            TryAdd(option, plugins, plugin, "manual registration", logger);
    }

    private static bool TryAdd(SharkOption option, List<ISharkPlugin> plugins, ISharkPlugin plugin,
        string source, ILogger? logger)
    {
        if (option.DisabledPlugins.Contains(plugin.Name))
        {
            logger?.LogInformation("PluginLoader: skipping disabled plugin '{Name}' from {Source}",
                plugin.Name, source);
            return false;
        }

        if (plugins.Any(p => p.Name.Equals(plugin.Name, StringComparison.OrdinalIgnoreCase)))
        {
            logger?.LogWarning(
                "PluginLoader: duplicate plugin '{Name}' from {Source} — already registered, skipping",
                plugin.Name, source);
            return false;
        }

        plugins.Add(plugin);
        if (logger?.IsEnabled(LogLevel.Debug) == true)
            logger.LogDebug("PluginLoader: registered '{Name}' from {Source}", plugin.Name, source);
        return true;
    }
}
