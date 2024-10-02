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
        var json = JsonSerializer.Serialize(data, type, options ?? new JsonSerializerOptions()
        {
            WriteIndented = true
        });

        await response.WriteAsync(json, cancellationToken);
    }
    internal static async Task WriteJsonAsync<T>(this HttpResponse response, 
        object? data,
        JsonSerializerOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        response.ContentType = "application/json";
        var json = JsonSerializer.Serialize(data, typeof(T), options ?? new JsonSerializerOptions()
        {
            WriteIndented = true
        });

        await response.WriteAsync(json, cancellationToken);
    }
}