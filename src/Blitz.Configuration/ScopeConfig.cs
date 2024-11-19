using System.Text.Json.Serialization;

public class ScopeConfig
{
    public string RawExtensionList { get; set; }= string.Empty;
    
    public string ScopeImage { get; set; }= string.Empty;
    public string ScopeTitle { get; set; } = string.Empty;
    public bool UseGitIgnore { get; set; } = true;
    public List<ConfigSearchPath> SearchPaths { get; set; } = [];
}

public class ConfigSearchPath
{
    public string Folder { get; set; } = string.Empty;
    public bool TopLevelOnly { get; set; }
}