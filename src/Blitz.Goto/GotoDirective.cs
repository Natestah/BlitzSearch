namespace Blitz.Goto;

public class GotoDirective(string fileName, int line = 0, int column= 0)
{
    public string FileName { get; } = fileName;
    public int Line { get; } = line;
    public int Column { get; } = column;
}