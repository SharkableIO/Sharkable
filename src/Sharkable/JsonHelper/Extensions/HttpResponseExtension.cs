using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Sharkable;

internal static class HttpResponseExtension
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = false
    };

    internal static async Task WriteJsonAsync(this HttpResponse response,
        object? data,
        Type type,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        response.ContentType = "application/json";
        var json = JsonSerializer.Serialize(data, type, options ?? DefaultOptions);
        await response.WriteAsync(json, cancellationToken);
    }
    internal static async Task WriteJsonAsync<T>(this HttpResponse response,
        object? data,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        response.ContentType = "application/json";
        var json = JsonSerializer.Serialize(data, typeof(T), options ?? DefaultOptions);
        await response.WriteAsync(json, cancellationToken);
    }
}