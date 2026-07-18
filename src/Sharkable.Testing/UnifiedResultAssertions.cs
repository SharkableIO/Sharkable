using System.Net;
using System.Text.Json;
using Sharkable;

namespace Sharkable.Testing;

/// <summary>
/// Fluent assertions for <see cref="UnifiedResult{T}"/> responses in tests.
/// </summary>
public static class UnifiedResultAssertions
{
    /// <summary>
    /// Deserializes the response body as <see cref="UnifiedResult{T}"/> and returns it.
    /// </summary>
    public static async Task<UnifiedResult<T>?> ReadAsUnifiedResultAsync<T>(this HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<UnifiedResult<T>>(json);
    }

    /// <summary>
    /// Asserts that the response has the expected HTTP status code.
    /// </summary>
    public static void AssertStatusCode(this HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
            throw new AssertFailedException(
                $"Expected status {(int)expected} {expected}, but got {(int)response.StatusCode} {response.StatusCode}.");
    }

    /// <summary>
    /// Asserts that the unified result has the expected status code, data, and no error.
    /// </summary>
    public static async Task AssertOkAsync<T>(this HttpResponseMessage response, T? expectedData)
    {
        response.AssertStatusCode(HttpStatusCode.OK);
        var result = await response.ReadAsUnifiedResultAsync<T>();
        if (result == null)
            throw new AssertFailedException("Response body was null.");
        if (result.ErrorMessage != null)
            throw new AssertFailedException($"Expected no error, but got: {result.ErrorMessage}");
        if (!EqualityComparer<T>.Default.Equals(expectedData!, result.Data!))
            throw new AssertFailedException($"Expected data '{expectedData}', but got '{result.Data}'.");
    }

    /// <summary>
    /// Asserts that the response is a unified result error with the expected status code and message.
    /// </summary>
    public static async Task AssertErrorAsync(this HttpResponseMessage response, HttpStatusCode expectedStatus, string expectedMessageSubstring)
    {
        response.AssertStatusCode(expectedStatus);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var statusCode = root.TryGetProperty("statusCode", out var sc) ? sc.GetInt32() : -1;
        if (statusCode != (int)expectedStatus)
            throw new AssertFailedException($"Expected status {(int)expectedStatus}, but got {statusCode}.");

        var errorMessage = root.TryGetProperty("errorMessage", out var em) ? em.GetString() : null;
        if (errorMessage == null || !errorMessage.Contains(expectedMessageSubstring))
            throw new AssertFailedException($"Expected error message containing '{expectedMessageSubstring}', but got '{errorMessage}'.");
    }
}

/// <summary>
/// Thrown by <see cref="UnifiedResultAssertions"/> when an assertion fails.
/// </summary>
public sealed class AssertFailedException : Exception
{
    /// <summary>
    /// Initializes a new instance with the failure message.
    /// </summary>
    public AssertFailedException(string message) : base(message) { }
}
