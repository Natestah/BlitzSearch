
public class FolderWorkspace
{
    public string Name { get; set; } = string.Empty;
    public string ExeForIcon { get; set; } = string.Empty;
    public List<string> Folders { get; set; } = [];
    public List<string> FolderNames { get; set; } = [];

    public string ProjectName { get; set; } = string.Empty;
    public string WorkspaceFileName { get; set; } = string.Empty;
}