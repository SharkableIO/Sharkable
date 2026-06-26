using System.Net;
using System.Text;

namespace Sharkable.Tests.Idempotency;

/// <summary>
/// Integration tests for <see cref="SharkIdempotencyMiddleware"/> exercised
/// through the full ASP.NET Core pipeline via <see cref="IdempotencyTestFixture"/>.
/// </summary>
public class IdempotencyIntegrationTests : IClassFixture<IdempotencyTestFixture>
{
    private readonly HttpClient _client;

    public IdempotencyIntegrationTests(IdempotencyTestFixture fixture)
    {
        _client = fixture.Client;
        IdempotencyTestEndpoint.Reset();
    }

    private static HttpRequestMessage NewIdempotentRequest(string key, string body = "{\"x\":1}")
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/idempotency/test");
        req.Headers.Add("Idempotency-Key", key);
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");
        return req;
    }

    [Fact]
    public async Task NoHeader_PassesThrough()
    {
        var res = await _client.PostAsync("/api/idempotency/test", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Single(IdempotencyTestEndpoint.Invocations);
    }

    [Fact]
    public async Task SameKeySameBody_Replayed()
    {
        var key = Guid.NewGuid().ToString();
        var r1 = await _client.SendAsync(NewIdempotentRequest(key));
        var r2 = await _client.SendAsync(NewIdempotentRequest(key));

        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
        Assert.Single(IdempotencyTestEndpoint.Invocations);
        Assert.Equal("true", r2.Headers.GetValues("X-Idempotent-Replayed").First());
    }

    [Fact]
    public async Task SameKeyDifferentBody_422()
    {
        var key = Guid.NewGuid().ToString();
        var r1 = await _client.SendAsync(NewIdempotentRequest(key, "{\"a\":1}"));
        var r2 = await _client.SendAsync(NewIdempotentRequest(key, "{\"a\":2}"));

        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r2.StatusCode);
    }

    [Fact]
    public async Task ConcurrentSameKey_OneSucceedsOthersConflict()
    {
        var key = Guid.NewGuid().ToString();

        var tasks = Enumerable.Range(0, 5)
            .Select(_ =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/idempotency/test?delay=2000&status=200");
                req.Headers.Add("Idempotency-Key", key);
                req.Content = new StringContent("{\"x\":1}", Encoding.UTF8, "application/json");
                return _client.SendAsync(req);
            })
            .ToArray();
        var results = await Task.WhenAll(tasks);

        var ok = results.Count(r => r.StatusCode == HttpStatusCode.OK);
        var conflict = results.Count(r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(1, ok);
        Assert.Equal(4, conflict);
    }

    [Fact]
    public async Task First500_NotCached_SecondExecutes()
    {
        var key = Guid.NewGuid().ToString();

        var r1Req = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/idempotency/test?status=500");
        r1Req.Headers.Add("Idempotency-Key", key);
        r1Req.Content = new StringContent("{\"x\":1}", Encoding.UTF8, "application/json");

        var r2Req = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/idempotency/test?status=500");
        r2Req.Headers.Add("Idempotency-Key", key);
        r2Req.Content = new StringContent("{\"x\":1}", Encoding.UTF8, "application/json");

        var r1 = await _client.SendAsync(r1Req);
        var r2 = await _client.SendAsync(r2Req);

        Assert.Equal(HttpStatusCode.InternalServerError, r1.StatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, r2.StatusCode);
        Assert.Equal(2, IdempotencyTestEndpoint.Invocations.Count);
    }

    [Fact]
    public async Task First400_Cached_Replayed()
    {
        var key = Guid.NewGuid().ToString();

        var r1Req = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/idempotency/test?status=400");
        r1Req.Headers.Add("Idempotency-Key", key);
        r1Req.Content = new StringContent("{\"x\":1}", Encoding.UTF8, "application/json");

        var r2Req = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/idempotency/test?status=400");
        r2Req.Headers.Add("Idempotency-Key", key);
        r2Req.Content = new StringContent("{\"x\":1}", Encoding.UTF8, "application/json");

        var r1 = await _client.SendAsync(r1Req);
        var r2 = await _client.SendAsync(r2Req);

        Assert.Equal(HttpStatusCode.BadRequest, r1.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
        Assert.Single(IdempotencyTestEndpoint.Invocations);
    }

    [Fact]
    public async Task GetWithHeader_Ignored()
    {
        var key = Guid.NewGuid().ToString();
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/idempotency/test");
        req.Headers.Add("Idempotency-Key", key);

        var res = await _client.SendAsync(req);
        Assert.NotEqual(HttpStatusCode.Conflict, res.StatusCode);
        Assert.NotEqual(HttpStatusCode.UnprocessableEntity, res.StatusCode);
    }

    [Fact]
    public async Task KeyTooLong_400()
    {
        var key = new string('a', 256);
        var res = await _client.SendAsync(NewIdempotentRequest(key));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}