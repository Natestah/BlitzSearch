namespace Blitz.Interfacing;
using MessagePack;

[MessagePackObject]
public class SelectedProjectExport
{
    [Key(nameof(Name))] 
    public string? Name { get; set; }
    
    [Key(nameof(BelongsToSolution))] 
    public string? BelongsToSolution { get; set; }
}