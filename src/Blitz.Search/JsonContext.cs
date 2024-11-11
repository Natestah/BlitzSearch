using System.Text.Json.Serialization;
namespace Blitz.Search;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SearchFileContents))]
internal partial class JsonContext : JsonSerializerContext;
