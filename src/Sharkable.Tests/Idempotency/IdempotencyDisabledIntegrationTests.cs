using System.Net;
using System.Text;

namespace Sharkable.Tests.Idempotency;

/// <summary>
/// Integration tests verifying that when <c>EnableIdempotency</c> is false
/// (the default), the <c>Idempotency-Key</c> header is ignored and every
/// request executes end-to-end.
/// </summary>
public class IdempotencyDisabledIntegrationTests : IClassFixture<IdempotencyDisabledTestFixture>
{
    private readonly HttpClient _client;

    public IdempotencyDisabledIntegrationTests(IdempotencyDisabledTestFixture fixture)
    {
        _client = fixture.Client;
        IdempotencyTestEndpoint.Reset();
    }

    [Fact]
    public async Task EnableIdempotencyFalse_HeaderIgnored_EveryRequestExecutes()
    {
        var key = Guid.NewGuid().ToString();
        var make = (string body) =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/idempotency/test");
            req.Headers.Add("Idempotency-Key", key);
            req.Content = new StringContent(body, Encoding.UTF8, "application/json");
            return req;
        };

        var r1 = await _client.SendAsync(make("{\"a\":1}"));
        var r2 = await _client.SendAsync(make("{\"a\":2}"));

        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
        Assert.Equal(2, IdempotencyTestEndpoint.Invocations.Count);
        // No replay header on either response.
        Assert.False(r1.Headers.Contains("X-Idempotent-Replayed"));
        Assert.False(r2.Headers.Contains("X-Idempotent-Replayed"));
    }
}
