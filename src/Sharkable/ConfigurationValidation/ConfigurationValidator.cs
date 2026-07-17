using System.Text.RegularExpressions;
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
    /// SHARK-SEC-M002: header names must match this character class. Anything
    /// else (CR/LF, whitespace, control chars) is a header-injection vector
    /// because the value flows into <c>HttpResponse.Headers[...].Set(...)</c>.
    /// </summary>
    private static readonly Regex HeaderNamePattern =
        new(@"^[A-Za-z0-9\-_]+$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

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
        ValidateRateLimiting(opt, errors, warnings);
        ValidateEtag(opt, errors);
        ValidateAuditTrail(opt, errors);
        ValidateHeaderNames(opt, errors);
        ValidateRegexPatterns(opt, errors);

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

        if (opts.MaxResponseSize <= 0)
            errors.Add(
                "Idempotency is enabled but MaxResponseSize must be > 0. " +
                "Set it via: opt.ConfigureIdempotency(o => o.MaxResponseSize = 1048576).");

        if (opts.MaxKeyLength <= 0 || opts.MaxKeyLength > 1024)
            errors.Add(
                $"Idempotency is enabled but MaxKeyLength ({opts.MaxKeyLength}) must be in [1, 1024]. " +
                "Set it via: opt.ConfigureIdempotency(o => o.MaxKeyLength = 255).");

        if (opts.Ttl <= TimeSpan.Zero)
            errors.Add(
                "Idempotency is enabled but Ttl must be > 0. " +
                "Set it via: opt.ConfigureIdempotency(o => o.Ttl = TimeSpan.FromHours(24)).");

        if (opts.InFlightTtl <= TimeSpan.Zero)
            errors.Add(
                "Idempotency is enabled but InFlightTtl must be > 0. " +
                "Set it via: opt.ConfigureIdempotency(o => o.InFlightTtl = TimeSpan.FromSeconds(30)).");

        if (opts.MaxEntries <= 0)
            errors.Add(
                "Idempotency is enabled but MaxEntries must be > 0. " +
                "Set it via: opt.ConfigureIdempotency(o => o.MaxEntries = 10000).");

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
    /// L-17: validate the distributed rate limiter's most critical knobs.
    /// </summary>
    private static void ValidateRateLimiting(SharkOption opt, List<string> errors, List<string> warnings)
    {
        if (opt.RateLimitingOptions == null)
            return;

        var r = opt.RateLimitingOptions;

        if (r.DefaultLimit <= 0)
            errors.Add(
                "Rate limiting DefaultLimit must be > 0. " +
                "Set it via: opt.ConfigureRateLimiting(o => o.DefaultLimit = 100).");

        if (r.DefaultWindow <= TimeSpan.Zero)
            errors.Add(
                "Rate limiting DefaultWindow must be > 0. " +
                "Set it via: opt.ConfigureRateLimiting(o => o.DefaultWindow = TimeSpan.FromMinutes(1)).");

        if (r.MaxEntries <= 0 && r.MaxEntries != -1)
            errors.Add(
                "Rate limiting MaxEntries must be > 0 or -1 for uncapped. " +
                "Set it via: opt.ConfigureRateLimiting(o => o.MaxEntries = 100000).");

        if (r.EnableAdaptive)
        {
            if (r.AdaptiveCpuHighThreshold <= r.AdaptiveCpuLowThreshold)
                errors.Add(
                    $"Rate limiting AdaptiveCpuHighThreshold ({r.AdaptiveCpuHighThreshold}) must be > " +
                    $"AdaptiveCpuLowThreshold ({r.AdaptiveCpuLowThreshold}).");
            if (r.AdaptiveGcHighThreshold <= r.AdaptiveGcLowThreshold)
                errors.Add(
                    $"Rate limiting AdaptiveGcHighThreshold ({r.AdaptiveGcHighThreshold}) must be > " +
                    $"AdaptiveGcLowThreshold ({r.AdaptiveGcLowThreshold}).");
            if (r.MaxPermitLimit < r.MinPermitLimit)
                errors.Add(
                    $"Rate limiting MaxPermitLimit ({r.MaxPermitLimit}) must be >= " +
                    $"MinPermitLimit ({r.MinPermitLimit}).");
        }

        // Proxy-trust guidance: when rate limiting is enabled and the default
        // key generator uses RemoteIpAddress, the absence of ForwardedHeaders
        // configuration collapses all clients into a single rate-limit bucket.
        if (r.KeyGenerator == null)
            warnings.Add(
                "Rate limiting is enabled with the default IP-based key generator. " +
                "Behind a reverse proxy/CDN, configure ForwardedHeadersMiddleware with KnownProxies " +
                "so per-client IPs are preserved. Otherwise all traffic shares one rate-limit bucket.");
    }

    /// <summary>
    /// L-17: validate ETag options.
    /// </summary>
    private static void ValidateEtag(SharkOption opt, List<string> errors)
    {
        if (!opt.EnableETag)
            return;

        var e = opt.ETagOptions ?? new ETagOptions();
        if (e.MaxResponseSize <= 0)
            errors.Add(
                "ETag MaxResponseSize must be > 0. " +
                "Set it via: opt.ETagOptions ??= new ETagOptions { MaxResponseSize = 10 * 1024 * 1024 } " +
                "(or pass it via opt.ETagOptions if you already configured one).");
    }

    /// <summary>
    /// L-17: validate audit-trail options.
    /// </summary>
    private static void ValidateAuditTrail(SharkOption opt, List<string> errors)
    {
        if (opt.AuditTrailOptions == null)
            return;

        var a = opt.AuditTrailOptions;
        if (a.AsyncWrite)
        {
            if (a.BatchSize <= 0)
                errors.Add(
                    "Audit trail AsyncWrite is enabled but BatchSize must be > 0. " +
                    "Set it via: opt.ConfigureAuditTrail(o => o.BatchSize = 1).");
            if (a.FlushInterval <= TimeSpan.Zero)
                errors.Add(
                    "Audit trail AsyncWrite is enabled but FlushInterval must be > 0. " +
                    "Set it via: opt.ConfigureAuditTrail(o => o.FlushInterval = TimeSpan.FromSeconds(5)).");
        }
    }

    /// <summary>
    /// SHARK-SEC-M002: validate that developer-supplied header names only
    /// contain characters that are safe to pass to
    /// <c>HttpResponse.Headers[...].Set(...)</c>. CRLF in a header name is a
    /// header-injection vector. Each call site is independently checked
    /// so the error message points the operator at the exact option to fix.
    /// </summary>
    private static void ValidateHeaderNames(SharkOption opt, List<string> errors)
    {
        if (opt.AuditTrailOptions != null)
        {
            ValidateHeaderName(opt.AuditTrailOptions.CorrelationIdHeader,
                nameof(AuditTrailOptions.CorrelationIdHeader), errors);
        }

        if (opt.IdempotencyOptions != null && opt.EnableIdempotency)
        {
            ValidateHeaderName(opt.IdempotencyOptions.HeaderName,
                nameof(SharkIdempotencyOptions.HeaderName), errors);
            ValidateHeaderName(opt.IdempotencyOptions.ReplayedHeaderName,
                nameof(SharkIdempotencyOptions.ReplayedHeaderName), errors);
        }

        if (opt.RateLimitingOptions != null)
        {
            ValidateHeaderName(opt.RateLimitingOptions.HeaderPrefix,
                nameof(SharkRateLimiterOptions.HeaderPrefix), errors);
        }

        ValidateHeaderName(opt.ApiKeyHeaderName,
            nameof(SharkOption.ApiKeyHeaderName), errors);
        // HealthCheckPath is a URL path, not a header name — paths may
        // legitimately contain '/'. Only require non-empty + no CR/LF.
        ValidatePath(opt.HealthCheckPath,
            nameof(SharkOption.HealthCheckPath), errors);
    }

    private static void ValidatePath(string? value, string optionName, List<string> errors)
    {
        if (string.IsNullOrEmpty(value)) return;
        if (value.Contains('\r') || value.Contains('\n') || value.Contains('\0'))
        {
            errors.Add(
                $"{optionName} = '{value}' contains CR/LF/NUL characters that are unsafe in HTTP path values.");
        }
    }

    private static void ValidateHeaderName(string? value, string optionName, List<string> errors)
    {
        if (string.IsNullOrEmpty(value)) return; // null/empty is acceptable for optional settings
        if (!HeaderNamePattern.IsMatch(value))
        {
            errors.Add(
                $"{optionName} = '{value}' contains characters that are unsafe in HTTP header names. " +
                "Only ASCII letters, digits, '-' and '_' are allowed. CRLF in a header name is a " +
                "header-injection vector (SHARK-SEC-M002).");
        }
    }

    /// <summary>
    /// SHARK-SEC-M001 / SHARK-SEC-L012: try-compile each developer-supplied
    /// regex pattern. A malformed or ReDoS-prone pattern would otherwise
    /// throw on the first request and bring down endpoint registration.
    /// </summary>
    private static void ValidateRegexPatterns(SharkOption opt, List<string> errors)
    {
        ValidateRegex(opt.GroupNameSuffixPattern,
            nameof(SharkOption.GroupNameSuffixPattern), errors);
        ValidateRegex(opt.VersionFormatPattern,
            nameof(SharkOption.VersionFormatPattern), errors);
    }

    private static void ValidateRegex(string pattern, string optionName, List<string> errors)
    {
        if (string.IsNullOrEmpty(pattern)) return;
        try
        {
            // The probe match is bounded by the same 100ms timeout the
            // production code uses, so a malicious pattern that compiles
            // but hangs is caught here as well.
            _ = Regex.IsMatch("", pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));
        }
        catch (ArgumentException ex)
        {
            errors.Add(
                $"{optionName} = '{pattern}' is not a valid regular expression: {ex.Message}. " +
                "Malformed patterns throw on the first request and crash endpoint registration (SHARK-SEC-M001).");
        }
        catch (RegexMatchTimeoutException)
        {
            errors.Add(
                $"{optionName} = '{pattern}' exceeded the 100ms validation timeout — likely a ReDoS pattern. " +
                "Use a linear / anchored pattern (SHARK-SEC-M001).");
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