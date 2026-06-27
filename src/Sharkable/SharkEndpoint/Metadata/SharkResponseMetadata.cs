using Microsoft.AspNetCore.Http.Metadata;

namespace Sharkable;

internal sealed class SharkResponseMetadata : IProducesResponseTypeMetadata
{
    public int StatusCode { get; set; }
    public Type? Type { get; set; }
    public IEnumerable<string>? ContentTypes { get; set; }
}
