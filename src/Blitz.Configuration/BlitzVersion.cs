using System.Text.Json;

namespace Blitz;

public class BlitzVersion
{
    public string Revision { get; set; }
    
    public string Changes { get; set; }

    public static BlitzVersionList DeserializeFrom(string text)
    {
        return JsonSerializer.Deserialize<BlitzVersionList>(text, JsonContext.Default.BlitzVersionList);
    }
}

public class BlitzVersionList : List<BlitzVersion>;