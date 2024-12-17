namespace Blitz;

public class SetSearchBoxContext
{
    public int ProcessId { get; set; }
    public SolutionID? EditorId { get; set; } = null;
    public string SearchBoxString { get; set; } = string.Empty;
}