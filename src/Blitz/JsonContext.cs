using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Blitz.Goto;

namespace Blitz;

[JsonSourceGenerationOptions(WriteIndented = true)]
// [JsonSerializable(typeof(Configuration))]
[JsonSerializable(typeof(Goto.GotoEditor))]
[JsonSerializable(typeof(List<GotoEditor>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
internal partial class JsonContext : JsonSerializerContext
{
}