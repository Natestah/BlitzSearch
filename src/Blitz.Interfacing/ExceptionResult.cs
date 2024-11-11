using System.Text;

namespace Blitz.Interfacing;
using MessagePack;

[MessagePackObject]
public class ExceptionResult
{
    
    [Key(nameof(ExceptionMessage))]
    public string? ExceptionMessage { get; set; }


    [Key(nameof(ExceptionStack))] 
    public string? ExceptionStack { get; set; }


    public static ExceptionResult CreateFromException(Exception ex)
    {
        return new ExceptionResult
        {
            ExceptionMessage = ex.Message,
            ExceptionStack = CreateExceptionStackString(ex)
        };
    }
    private static  string CreateExceptionStackString(Exception exception)
    {
        var builder = new StringBuilder();
        if (exception.InnerException?.StackTrace is { } asString)
        {
            builder.AppendLine("INNER EXCEPTION:");
            builder.AppendLine(asString);
        }

        if (exception.StackTrace is not { } topStack)
        {
            return builder.ToString();
        }
        builder.AppendLine("STACK:");
        builder.AppendLine(topStack);

        return builder.ToString();
    }
}