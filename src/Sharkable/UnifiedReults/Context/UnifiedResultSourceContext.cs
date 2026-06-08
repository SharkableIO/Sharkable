using System.Text.Json.Serialization;

namespace Sharkable;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(UnifiedResult<string>))]
[JsonSerializable(typeof(UnifiedResult<int>))]
[JsonSerializable(typeof(UnifiedResult<object?>))]
public partial class UnifiedResultSourceContext : JsonSerializerContext
{
}