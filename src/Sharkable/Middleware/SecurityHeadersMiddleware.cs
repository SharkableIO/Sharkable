namespace Sharkable;

internal sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
        _options = Shark.SharkOption.SecurityHeadersOptions!;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldSkip(context.Request.Path))
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;

                if (_options.ContentTypeOptions)
                    headers["X-Content-Type-Options"] = "nosniff";

                if (_options.FrameOptions != null)
                    headers["X-Frame-Options"] = _options.FrameOptions;

                if (_options.ReferrerPolicy != null)
                    headers["Referrer-Policy"] = _options.ReferrerPolicy;

                if (_options.ContentSecurityPolicy != null)
                    headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;

                if (_options.PermissionsPolicy != null)
                    headers["Permissions-Policy"] = _options.PermissionsPolicy;

                if (_options.StrictTransportSecurity)
                    headers["Strict-Transport-Security"] = $"max-age={_options.HstsMaxAge}";

                if (_options.CrossOriginResourcePolicy != null)
                    headers["Cross-Origin-Resource-Policy"] = _options.CrossOriginResourcePolicy;

                if (_options.CrossOriginOpenerPolicy != null)
                    headers["Cross-Origin-Opener-Policy"] = _options.CrossOriginOpenerPolicy;

                if (_options.CrossOriginEmbedderPolicy != null)
                    headers["Cross-Origin-Embedder-Policy"] = _options.CrossOriginEmbedderPolicy;

                return Task.CompletedTask;
            });
        }

        await _next(context);
    }

    private bool ShouldSkip(string path)
    {
        if (_options.ExcludePaths.Length == 0)
            return false;

        foreach (var exclude in _options.ExcludePaths)
        {
            if (!string.IsNullOrEmpty(exclude) &&
                path.StartsWith(exclude, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
