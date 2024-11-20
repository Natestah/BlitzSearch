using System.Text.Json.Serialization;

namespace Blitz.Interfacing;
using MessagePack;

[MessagePackObject]
public class SelectedProjectExport
{
    [Key(nameof(ActiveFileInProject))] 
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ActiveFileInProject { get; set; }
    
    [Key(nameof(Name))] 

        
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }
    
    [Key(nameof(BelongsToSolution))] 
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]

    public string? BelongsToSolution { get; set; }
}