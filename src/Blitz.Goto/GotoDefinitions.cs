using System.Reflection;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace Blitz.Goto;

public class GotoDefinitions
{
    public List<GotoEditor> GetBuiltInEditors()
    {
        var gotoAssembly = Assembly.GetAssembly(typeof(GotoEditor));
        Debug.Assert(gotoAssembly != null, nameof(gotoAssembly) + " != null");
        using var stream = gotoAssembly.GetManifestResourceStream("Blitz.Goto.Resources.GotoDefs.json");
        Debug.Assert(stream != null, nameof(stream) + " != null");
        using var reader = new StreamReader(stream);
        var result = reader.ReadToEnd();
       
        return JsonSerializer.Deserialize<List<GotoEditor>>(result, JsonContext.Default.ListGotoEditor) ?? throw new InvalidOperationException();
    }
}