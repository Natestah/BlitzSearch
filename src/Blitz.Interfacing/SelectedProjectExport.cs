namespace Blitz.Interfacing;
using MessagePack;

[MessagePackObject]
public class SelectedProjectExport
{
    [Key(nameof(ActiveFileInProject))] 
    public string? ActiveFileInProject { get; set; }
    
    [Key(nameof(Name))] 
    public string? Name { get; set; }
    
    [Key(nameof(BelongsToSolution))] 
    public string? BelongsToSolution { get; set; }
}