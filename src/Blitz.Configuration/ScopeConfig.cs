using System.Text.Json.Serialization;

public class ScopeConfig
{
    public string? RawExtensionList { get; set; }
    
    public string? ScopeImage { get; set; }
    public string? ScopeTitle { get; set; }
    public bool UseGitIgnore { get; set; } = true; 
    public List<ConfigSearchPath>? SearchPaths { get; set; } 
}

public class ConfigSearchPath
{
    public string? Folder { get; set; }
    public bool TopLevelOnly { get; set; }
}