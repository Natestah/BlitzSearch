namespace Blitz.Interfacing;

using MessagePack;
[MessagePackObject]
public class SolutionExport
{
    [Key(nameof(Name))] 
    public string Name { get; set; } = string.Empty;
    [Key(nameof(Projects))] 
    public List<Project> Projects { get; set; } = [];
}