using System.Text;

namespace Blitz.Interfacing;

public static class FileNameResultsExtensions
{
    
    public static string GetReplaceResults(this FileNameResult result, string contents)
    {
        bool replacing = result.ContentResults.Any(c => c.Replacing);
        if (!replacing)
        {
            return contents;
        }

        int currentLine = 0;
        StringBuilder output = new StringBuilder();
        var contentEnumerator = result.ContentResults.GetEnumerator();
        contentEnumerator.MoveNext();
        using var re = new StringReader(contents);
        while (re.Peek() != -1)
        {
            string lineText = re.ReadLine()!;
            currentLine++;
            if (contentEnumerator.Current != null && contentEnumerator.Current.LineNumber == currentLine)
            {
                output.AppendLine(contentEnumerator.Current.ReplacedContents);
                contentEnumerator.MoveNext();
            }
            else
            {
                output.AppendLine(lineText);
            }
        }

        return output.ToString();
    }

    
}