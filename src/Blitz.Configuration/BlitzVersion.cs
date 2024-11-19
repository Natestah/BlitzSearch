using System.Text.Json;

namespace Blitz;

public class BlitzVersion
{
    public string Revision { get; set; } = string.Empty;
    
    public string Changes { get; set; } = string.Empty;

    public static BlitzVersionList? DeserializeFrom(string text)
    {
        return JsonSerializer.Deserialize<BlitzVersionList>(text, JsonContext.Default.BlitzVersionList);
    }
}

public class BlitzVersionList : List<BlitzVersion>;