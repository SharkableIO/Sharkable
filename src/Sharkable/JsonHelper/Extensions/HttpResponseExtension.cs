using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Sharkable;

internal static class HttpResponseExtension
{
    internal static async Task WriteJsonAsync(this HttpResponse response,
        object? data,
        Type type,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        response.ContentType = "application/json";
        // SHARK-SEC-L001: default to compact JSON (WriteIndented = false) so
        // production responses do not pay the ~2x byte-size tax. The previous
        // default of WriteIndented=true was a leftover from local dev. Callers
        // can still opt back in by passing their own JsonSerializerOptions.
        var json = JsonSerializer.Serialize(data, type, options ?? new JsonSerializerOptions()
        {
            WriteIndented = false
        });

        await response.WriteAsync(json, cancellationToken);
    }
    internal static async Task WriteJsonAsync<T>(this HttpResponse response,
        object? data,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        response.ContentType = "application/json";
        // SHARK-SEC-L001: see above.
        var json = JsonSerializer.Serialize(data, typeof(T), options ?? new JsonSerializerOptions()
        {
            WriteIndented = false
        });

        await response.WriteAsync(json, cancellationToken);
    }
}