namespace Blitz.Goto;

public class GotoDirective(string? solution, string fileName, int line = 0, int column= 0)
{
    public string? Title { get; set; } = null;
    public string? SolutionId { get; set; } = null;
    public string? SolutionName { get; } = solution;
    public string FileName { get; } = fileName;
    public int Line { get; } = line;
    public int Column { get; } = column;
}