namespace Blitz.Interfacing;

using MessagePack;
[MessagePackObject]

public class WorkspaceExport
{
    [Key(nameof(Name))]
    public string Name { get; set; } = string.Empty;
    
    [Key(nameof(Folders))]
    public List<string> Folders { get; set; } = []; 
}