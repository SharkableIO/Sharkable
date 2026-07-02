using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sharkable;

/// <summary>
/// Validates <see cref="SharkOption"/> configuration at startup,
/// catching common misconfigurations before the application runs.
/// </summary>
internal static class ConfigurationValidator
    {
        /// <summary>
        /// Maximum <see cref="SharkIdempotencyOptions.MaxEntries"/> value before
        /// a startup warning is logged. Configurations above this threshold
        /// are almost always misconfigurations (e.g. someone passed
        /// <c>int.MaxValue</c>) and risk memory exhaustion under attack.
        /// </summary>
        private const long IdempotencyMaxEntriesWarningThreshold = 1_000_000;

        /// <summary>
        /// Runs all validation rules against the current <see cref="SharkOption"/>.
        /// Returns an empty list when no issues are found.
        /// </summary>
        internal static List<string> Validate()
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            var opt = Shark.SharkOption;

            ValidateJwt(opt, errors);
            ValidateMultiTenant(opt, errors);
            ValidateIdempotency(opt, errors, warnings);
            ValidateSaga(opt, errors);

            if (warnings.Count > 0)
            {
                foreach (var warning in warnings)
                    Console.Error.WriteLine($"[Sharkable warning] {warning}");
            }

            return errors;
        }

    private static void ValidateJwt(SharkOption opt, List<string> errors)
    {
        if (opt.JwtAuthority == null)
            return;

        if (string.IsNullOrWhiteSpace(opt.JwtAuthority))
            errors.Add("JWT is configured but Authority is empty. Set it via: opt.ConfigureJwt(authority: \"https://your-tenant.authority.com\", ...)");

        if (opt.JwtAudiences == null || opt.JwtAudiences.Length == 0)
            errors.Add("JWT is configured but no Audiences specified. Set them via: opt.ConfigureJwt(authority: \"...\", audiences: [\"api://default\"], ...)");
    }

    private static void ValidateMultiTenant(SharkOption opt, List<string> errors)
    {
        if (opt.TenantOptions == null)
            return;

        if (opt.TenantOptions.ResolveTenant == null)
            errors.Add("Multi-tenant is configured but ResolveTenant delegate is not set. Provide it via: opt.ConfigureMultiTenant(cfg => cfg.ResolveTenant = ctx => ...)");
    }

    private static void ValidateIdempotency(SharkOption opt, List<string> errors, List<string> warnings)
    {
        if (!opt.EnableIdempotency)
            return;

        var opts = opt.IdempotencyOptions;
        if (opts == null)
            return;

        if (opts.MaxFingerprintBodySize <= 0)
            errors.Add(
                "Idempotency is enabled but MaxFingerprintBodySize must be > 0. " +
                "Set it via: opt.ConfigureIdempotency(o => o.MaxFingerprintBodySize = 65536).");

        if (opts.MaxEntries > IdempotencyMaxEntriesWarningThreshold)
            warnings.Add(
                $"IdempotencyOptions.MaxEntries = {opts.MaxEntries} exceeds the 1,000,000 safety threshold. " +
                "Values this large are almost always misconfigurations and risk memory exhaustion under " +
                "Idempotency-Key flooding. Recommended cap is 10,000 (default) to 100,000 for high-traffic APIs. " +
                "Set it via: opt.ConfigureIdempotency(o => o.MaxEntries = 10_000).");
    }

    private static void ValidateSaga(SharkOption opt, List<string> errors)
    {
        // Only validate when the user explicitly opted in to a custom saga store
        // (the framework's MemorySagaStore defaults are already safe). For other
        // configurations, LockTtl / LockRenewalInterval default to 5 min and
        // LockTtl / 3 respectively and pass validation automatically.
        if (opt.SagaStoreFactory == null)
            return;

        try
        {
            var probe = new SagaExecutor(
                opt.SagaStoreFactory(new EmptyServiceProvider()),
                NullLogger<SagaExecutor>.Instance);

            if (probe.LockTtl <= TimeSpan.Zero)
                errors.Add(
                    "Saga LockTtl must be > 0. Configure it on the SagaExecutor instance or use " +
                    "the 3-arg SagaExecutor(store, logger, lockTtl) constructor.");

            if (probe.LockRenewalInterval > TimeSpan.Zero
                && probe.LockRenewalInterval >= probe.LockTtl)
            {
                errors.Add(
                    $"Saga LockRenewalInterval ({probe.LockRenewalInterval}) must be less than " +
                    $"LockTtl ({probe.LockTtl}). The renewal protocol is ineffective otherwise.");
            }
        }
        catch (ArgumentOutOfRangeException ex)
        {
            // Defensive: factory-returned store plus default executor shouldn't throw,
            // but if it does, surface the failure as a configuration error.
            errors.Add($"Saga lock configuration is invalid: {ex.Message}");
        }
    }

    /// <summary>
    /// Minimal <see cref="IServiceProvider"/> used only to satisfy the
    /// <see cref="SharkOption.SagaStoreFactory"/> contract during validation.
    /// Cannot resolve any service; throws if anything is requested.
    /// </summary>
    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            throw new InvalidOperationException(
                $"No services are registered during configuration validation; " +
                $"{serviceType.Name} cannot be resolved.");
        }
    }
}