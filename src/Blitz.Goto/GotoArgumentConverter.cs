namespace Blitz.Goto;

/// <summary>
/// Converts templated arguments '(e.g -f %filename -l %line -c -%column)' to expanded variable.
/// Also Handles Expanding Environment variables. 
///  </summary>
public class GotoArgumentConverter(GotoDirective gotoDirective)
{
    private static readonly string FileNameAlias = "%filename%";
    private static readonly string LineAlias = "%line%";
    private static readonly string ColumnAlias = "%column%";
    private static readonly string ColumnPlusOneAliass = "%column_plus_one%";
    private static readonly string PositionAlias = "%position%";

    public static IEnumerable<ArgumentAlias> GetArgumentAliases(GotoDirective? gotoDirective)
    {
        gotoDirective ??= new GotoDirective(string.Empty);
        
        return new List<ArgumentAlias>
        {
            new(FileNameAlias, "Goto File",
                (inputArgument) => inputArgument.Replace(FileNameAlias, gotoDirective.FileName)),
            new(LineAlias, "Line number",
                (inputArgument) => inputArgument.Replace(LineAlias, gotoDirective.Line.ToString())),
            new(ColumnAlias, "Column number",
                (inputArgument) => inputArgument.Replace(ColumnAlias, gotoDirective.Column.ToString())),
            new(ColumnPlusOneAliass, "Column+offset",
                (inputArgument) => inputArgument.Replace(ColumnPlusOneAliass, (gotoDirective.Column + 1).ToString())),
            new(PositionAlias, "Buffer position",
                (inputArgument) => inputArgument.Replace(PositionAlias, GetPositionFromLineColumn(gotoDirective).ToString())),
        };
    }

    public static int GetPositionFromLineColumn(GotoDirective? gotoDirective)
    {
        if (gotoDirective == null || !File.Exists(gotoDirective.FileName))
        {
            return 0;
        }
        int currentReadLine = 1;
        int pos = 0;
        var buffer = new char[1];
        int atLineOffset = 0;
        bool inTargetLine = false;
        using (StreamReader re = new StreamReader(gotoDirective.FileName))
        {
            while (re.Peek() != -1)
            {
                var character = re.Read(buffer,0,1);
                if (buffer[0] == '\r')
                {
                    currentReadLine++;
                    if (currentReadLine == gotoDirective.Line)
                    {
                        inTargetLine = true;
                        atLineOffset = 0;
                        pos++;
                        continue;
                    }
                }

                pos++;
                if (buffer[0] == '\n')
                {
                    continue;
                }

                if (inTargetLine)
                {
                    atLineOffset++;
                    if (atLineOffset == gotoDirective.Column)
                    {
                        return pos;
                    }
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// Returns the converted argument parameter. 
    /// 'e.g:  -f %filename => -f c:\example.txt
    /// </summary>
    /// <param name="arguments"></param>
    /// <returns></returns>
    public string ConvertArguments(string arguments)
    {
        foreach (var argumentAlias in GetArgumentAliases(gotoDirective))
        {
            arguments = argumentAlias.ConvertArguments(arguments);
        }

        return Environment.ExpandEnvironmentVariables(arguments);
    }
}