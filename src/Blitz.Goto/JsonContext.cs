using System.Text.Json.Serialization;
using Blitz.Goto;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(GotoEditor))]
[JsonSerializable(typeof(List<GotoEditor>))]
[JsonSerializable(typeof(GotoDirective))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
internal partial class JsonContext : JsonSerializerContext;
