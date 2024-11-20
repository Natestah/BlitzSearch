using System.ComponentModel;
using System.Text.Json.Serialization;

public class ScopeConfig
{
    [DefaultValue("")]
    public string RawExtensionList { get; set; }= string.Empty;
    
    [DefaultValue("")]
    public string ScopeImage { get; set; }= string.Empty;
    [DefaultValue("")]
    public string ScopeTitle { get; set; } = string.Empty;
    [DefaultValue(true)]
    public bool UseGitIgnore { get; set; } = true;
    public List<ConfigSearchPath> SearchPaths { get; set; } =[];
}

public class ConfigSearchPath
{
    [DefaultValue("")]
    public string Folder { get; set; } = string.Empty;
    public bool TopLevelOnly { get; set; }
}