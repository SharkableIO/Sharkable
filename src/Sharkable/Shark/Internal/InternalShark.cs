using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Sharkable;

/// <summary>
/// internal shark class
/// </summary>
internal sealed class InternalShark
{
    internal static IServiceCollection InternalServices { get; private set;} = null!;
    public static IServiceProvider ServiceProvider { get; internal set; } = null!;
    public static IConfiguration Configuration { get; internal set;} = null!;
    public static IWebHostEnvironment WebHostEnvironment { get; internal set; } = null!;
    public static IHostEnvironment HostEnvironment { get; internal set; } = null!;
    public static IServiceScopeFactory ServiceScopeFactory { get; internal set; } = null!;
    public static bool AotMode { get; internal set; }
    internal static void ConfigureShark(IWebHostBuilder builder, Assembly[]? assemblies, IHostBuilder? hostBuilder = default)
    {
        if(hostBuilder == null || hostBuilder == default)
        {
            builder.ConfigureAppConfiguration((h, c) =>
            {
                HostEnvironment = WebHostEnvironment = h.HostingEnvironment;
            });
            // load json files
            //load configs
        }
        else 
        {
            //todo load config 
        }

        builder.ConfigureServices((h, s) =>
        {
            Configuration = h.Configuration;
            InternalServices = s;
            s.AddShark(assemblies);
        });
    }

    [RequiresDynamicCode("use Assembly[] method instead")]
    internal static void ConfigureShark(IWebHostBuilder builder, IHostBuilder? hostBuilder = default)
    {
        if(hostBuilder == null || hostBuilder == default)
        {
            builder.ConfigureAppConfiguration((h, c) =>
            {
                HostEnvironment = WebHostEnvironment = h.HostingEnvironment;
            });
            // load json files
            //load configs
        }
        else 
        {
            //todo load config 
        }

        builder.ConfigureServices((h, s) =>
        {
            Configuration = h.Configuration;
            InternalServices = s;
            s.AddShark();
        });
    }
}
