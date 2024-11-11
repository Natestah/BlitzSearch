namespace Blitz.Interfacing;
using MessagePack;

[MessagePackObject]
public class Project
{
    [Key(nameof(Name))]
    public string? Name { get; set; }
    
    [Key(nameof(Files))]
    public List<string>? Files { get; set; }
}