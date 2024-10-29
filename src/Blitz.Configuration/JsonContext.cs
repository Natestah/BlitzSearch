using System.Text.Json.Serialization;
using Blitz.Goto;
using Blitz.AvaloniaEdit.Models;

namespace Blitz;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Configuration))]
[JsonSerializable(typeof(ScopeConfig))]
[JsonSerializable(typeof(ConfigSearchPath))]
[JsonSerializable(typeof(BlitzEditorConfig))]
[JsonSerializable(typeof(BlitzVersion))]
[JsonSerializable(typeof(BlitzVersionList))]
[JsonSerializable(typeof(GotoEditor))]
[JsonSerializable(typeof(FolderWorkspace))]
[JsonSerializable(typeof(BlitzTheme))]
[JsonSerializable(typeof(BlitzColor))]
[JsonSerializable(typeof(Avalonia.Rect))]
[JsonSerializable(typeof(Avalonia.Size))]
[JsonSerializable(typeof(Avalonia.Point))]
[JsonSerializable(typeof(Avalonia.PixelPoint))]
[JsonSerializable(typeof(RobotDetectionSettings))]
public partial class JsonContext : JsonSerializerContext
{
    
}