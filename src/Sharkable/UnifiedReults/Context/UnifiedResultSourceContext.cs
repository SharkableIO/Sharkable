using System.Text.Json.Serialization;

namespace Sharkable;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> for <see cref="UnifiedResult{T}"/> types.
/// Registered in the <c>TypeInfoResolverChain</c> to enable AOT-compatible serialization.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(UnifiedResult<string>))]
[JsonSerializable(typeof(UnifiedResult<int>))]
[JsonSerializable(typeof(UnifiedResult<object?>))]
public partial class UnifiedResultSourceContext : JsonSerializerContext
{
}