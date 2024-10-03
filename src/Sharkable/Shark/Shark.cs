using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Sharkable;

/// <summary>
/// basic sharkable implements and properties
/// </summary>
public partial class Shark
{
    //private fields

    //private|inernal statics

    internal static readonly object condition = new();
    //internal IServiceProvider services { get => GetApp(_factory); }
    //public statics
    /// <summary>
    /// Sharkable asseblies list for the application entry
    /// </summary>
    public static Assembly[]? Assemblies => AssemblyContext.Assemblies;
    public static IHostEnvironment HostEnvironment => InternalShark.HostEnvironment;
    public static IServiceProvider Services => InternalShark.ServiceProvider;
    public static IWebHostEnvironment WebHostEnvironment => InternalShark.WebHostEnvironment;
    public static IConfiguration Configuration => InternalShark.Configuration;
    //public static HttpContext HttpContext { get; }
    /// <summary>
    /// Assembly context of application entry
    /// </summary>
    public static AssemblyContext? Context => AssemblyContext.Instance;
    public static IServiceScopeFactory ServiceScopeFactory => InternalShark.ServiceScopeFactory;
    /// <summary>
    /// Sharkable options
    /// </summary>
    public static SharkOption SharkOption { get; internal set; } = new SharkOption();
    internal static SwaggerGenOptions? SwaggerGenOptions { get; private set; }
    internal static SwaggerOptions? SwaggerOptions { get; private set; }
    public static UseSharkOptions? UseSharkOptions { get; internal set; }
    //public properties

    public static IServiceProvider GetServiceProvider(Type? serviceType = null)
    {
        // console program
        // if(HostEnvironment == default) 
        //     return Services;

        /*if (Services != null &&
            InternalShark.InternalServices
                .Where(x => x.ServiceType == (serviceType.IsGenericType ? serviceType.GetGenericTypeDefinition() : serviceType))
                .Any(x => x.Lifetime == ServiceLifetime.Singleton))*/
        return Services;
    }

    public static void SetAssebly(Assembly[]? assemblies)
    {
        AssemblyContext.GetAssemblyContext(assemblies);
    }
    /// <summary>
    /// get services
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? GetService<T>(IServiceProvider? serviceProvider = null) where T : class
    {
        var service = GetService(typeof(T), serviceProvider) as T;
        return service;
    }
    /// <summary>
    /// get services
    /// </summary>
    /// <param name="type">service type</param>
    /// <param name="serviceProvider">service provider</param>
    /// <returns>object of instance</returns>
    public static object? GetService(Type type, IServiceProvider? serviceProvider = null)
    {
        return (serviceProvider ?? GetServiceProvider(type)).GetService(type);
    }
    /// <summary>
    /// get keyed services
    /// </summary>
    /// <param name="serviceProvider">service provider</param>
    /// <param name="key">service key</param>
    /// <typeparam name="T">service type</typeparam>
    /// <returns>instance of service</returns>
    public static T? GetKeyedService<T>(string? key = null, IServiceProvider? serviceProvider = null) where T : class
    {
        return string.IsNullOrWhiteSpace(key) ? 
            null : (serviceProvider ?? GetServiceProvider(typeof(T))).GetKeyedService<T>(key);
    }
    /// <summary>
    /// get options
    /// </summary>
    /// <param name="serviceProvider">service provider</param>
    /// <typeparam name="TOptions">typed configuration options</typeparam>
    /// <returns></returns>
    public static TOptions? GetOptions<TOptions>(IServiceProvider? serviceProvider = null) where TOptions : class,new()
    {
        return GetService<IOptions<TOptions>>(serviceProvider)?.Value;
    }
}