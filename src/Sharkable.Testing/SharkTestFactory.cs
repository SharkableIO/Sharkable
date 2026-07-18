using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Sharkable;

namespace Sharkable.Testing;

/// <summary>
/// Base <see cref="WebApplicationFactory{TEntryPoint}"/> for Sharkable integration tests.
/// The entry point's own <c>AddShark()</c> call runs first; the optional
/// <c>configureShark</c> callback is applied afterward to override request-time settings.
/// <para>
/// <b>Important:</b> Options that control DI registration (e.g., <c>EnableIdempotency</c>,
/// <c>EnableETag</c>, <c>EnableHealthChecks</c>) are consumed during the entry point's
/// <c>AddShark()</c> service registration and will NOT reflect test overrides. To enable
/// these for tests, configure them in the entry point or use
/// <c>ConfigureTestServices</c>. Request-time options (rate limits, TTL values, header
/// values, etc.) do take effect.
/// </para>
/// </summary>
/// <typeparam name="TEntryPoint">The entry point class (usually <c>Program</c>).</typeparam>
public abstract class SharkTestFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint>
    where TEntryPoint : class
{
    private readonly Action<SharkOption>? _configureShark;

    /// <summary>
    /// Initializes a new factory with optional Sharkable configuration.
    /// </summary>
    /// <param name="configureShark">Optional callback to override Sharkable options for the test environment.</param>
    protected SharkTestFactory(Action<SharkOption>? configureShark = null)
    {
        _configureShark = configureShark;
    }

    /// <summary>
    /// The Sharkable configuration callback, available for subclasses to apply
    /// during <c>ConfigureWebHost</c>.
    /// </summary>
    protected Action<SharkOption>? ConfigureShark => _configureShark;

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Entry point's AddShark() has already run and set Shark.SharkOption.
            // Apply the test-specific overrides if provided.
            _configureShark?.Invoke(Shark.SharkOption);
        });
    }

    /// <summary>
    /// Creates a builder for use with Sharkable testing.
    /// Also exposed as a static helper for use outside the factory.
    /// </summary>
    public static WebApplicationBuilder CreateSharkBuilder(string[]? args = null)
    {
        return WebApplication.CreateBuilder(args ?? []);
    }

    /// <summary>
    /// Creates and configures a <see cref="WebApplicationBuilder"/> for testing.
    /// Override to set environment, configuration, or services before <c>AddShark()</c>.
    /// </summary>
    protected virtual WebApplicationBuilder CreateTestBuilder()
    {
        return CreateSharkBuilder();
    }

    /// <summary>
    /// Configures a built <see cref="WebApplication"/> for testing with <c>UseShark()</c>.
    /// Override to customize middleware pipeline.
    /// </summary>
    public static void ConfigureSharkApp(WebApplication app)
    {
        app.UseShark();
    }

    /// <summary>
    /// Registers a fake <see cref="IIdempotencyStore"/> in the test service collection.
    /// </summary>
    public static void UseFakeIdempotencyStore(IServiceCollection services)
    {
        services.AddSingleton<IIdempotencyStore, FakeIdempotencyStore>();
    }

    /// <summary>
    /// Registers a fake <see cref="IDistributedRateLimitStore"/> in the test service collection.
    /// </summary>
    public static void UseFakeRateLimitStore(IServiceCollection services)
    {
        services.AddSingleton<IDistributedRateLimitStore, FakeRateLimitStore>();
    }
}
