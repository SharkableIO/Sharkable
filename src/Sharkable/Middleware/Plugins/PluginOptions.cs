namespace Sharkable;

/// <summary>
/// Options for the plugin hot-plug folder scanning system.
/// Passed via <c>opt.ConfigurePlugins(p => ...)</c>.
/// </summary>
public sealed class PluginOptions
{
    /// <summary>
    /// Root directory containing plugin subfolders. Each subfolder is one plugin.
    /// Relative paths are resolved against the app's content root.
    /// Default is <c>"./plugins"</c>.
    /// </summary>
    public string Directory { get; set; } = "./plugins";

    /// <summary>
    /// When <c>true</c>, each subfolder under <see cref="Directory"/> is scanned
    /// at startup for a <c>.dll</c> containing an <see cref="ISharkPlugin"/>
    /// implementation. Requires JIT (not AOT). Default is <c>false</c> (opt-in).
    /// </summary>
    public bool ScanOnStartup { get; set; }
}
