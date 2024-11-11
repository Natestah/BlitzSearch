namespace Blitz.Goto;

/// <summary>
/// Definition of a GotoEditor
/// </summary>
public class GotoEditor
{
    public string Title { get; set; } = string.Empty;
    public string Executable { get; set; } = string.Empty;

    public string RunningProcessName { get; set; } = string.Empty;
    public string CodeExecute { get; set; } = string.Empty;
    public string ExecutableIconHint { get; set; } = string.Empty;
    public string ExecutableWorkingDirectory { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}