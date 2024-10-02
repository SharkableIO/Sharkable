using System.Text.Json.Serialization;

namespace Sharkable;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(UnifiedResult<>))]
[JsonSerializable(typeof(UnifiedResult<string>))]
[JsonSerializable(typeof(UnifiedResult<int>))]
public partial class UnifiedResultSourceContext : JsonSerializerContext
{
}