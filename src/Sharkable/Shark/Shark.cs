using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Sharkable;

public partial class Shark
{
    //private fields
    private readonly IServiceScopeFactory _factory;
    //private|inernal statics
    private static Shark? instance = null;
    internal static readonly object condition = new();
    //internal IServiceProvider services { get => GetApp(_factory); }
    //public statics
    public static Assembly[]? Assemblies => AssemblyContext.Assemblies;
    public static IHostEnvironment HostEnvironment => InternalShark.HostEnvironment;
    public static IServiceProvider Services => InternalShark.ServiceProvider;
    public static IWebHostEnvironment WebHostEnvironment => InternalShark.WebHostEnvironment;
    public static IConfiguration Configuration => InternalShark.Configuration;
    public static HttpContext HttpContext { get; }
    public static AssemblyContext? Context => AssemblyContext.Instance;
    public static IServiceScopeFactory ServiceScopeFactory => InternalShark.ServiceScopeFactory;
    public static SharkOption SharkOption { get; internal set; } = new SharkOption();
    //public properties


#region  internal
    internal Shark()
    {

    }
    internal static Shark GetShark(IServiceCollection services)
    {
        lock(condition)
        {
            instance ??= new Shark();
            return instance;
        }
    }

#endregion
    public static IServiceProvider GetServiceProvider(Type serviceType)
    {
        // console program
        if(HostEnvironment == default) 
            return Services;

        if (Services != null &&
            InternalShark.InternalServices
                .Where(x => x.ServiceType == (serviceType.IsGenericType ? serviceType.GetGenericTypeDefinition() : serviceType))
                .Any(x => x.Lifetime == ServiceLifetime.Singleton))
            return Services;

        return default!;
    }

    public static void SetAssebly(Assembly[]? assemblies)
    {
        AssemblyContext.GetAssemblyContext(assemblies);
    }

    public static T GetService<T>(IServiceProvider serviceProvider = default!) where T : class
    {
        var service = GetService(typeof(T), serviceProvider) as T;

        ArgumentNullException.ThrowIfNull(service);

        return service;
    }

    public static object? GetService(Type type, IServiceProvider serviceProvider = default!)
    {
        return (serviceProvider ?? GetServiceProvider(type)).GetService(type);
    }
    public static string? ApiPrefix { get; set; } = "/api";
}