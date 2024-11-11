using System.Text;

namespace Blitz.Goto;

/// <summary>
/// Helper class to write a simple testable file, file has many lines in order to make sure the editor
/// is scrolling to a line, and  then a clear indication of desired goto position
/// </summary>
public class GotoTestFile
{
    /// <summary>
    /// Creates and writes a goto test file
    /// </summary>
    public string CreateAndTestFile(out string testFileName, out int lineTest, out int testColumn, bool rebuilt = false)
    {
        var testPath = Path.Combine(Path.GetTempPath(), "blitz_testGoto");
        testFileName = Path.Combine(testPath, "GotoTest.txt");
        Directory.CreateDirectory(testPath);
        lineTest = 2000;
        var builder = new StringBuilder();
        for (int i = 0; i < lineTest - 1; i++)
        {
            builder.AppendLine((i+1).ToString());
        }

        string testLine = "\t\t  ><-caret should fall between the '><'.. ";
        testColumn = testLine.IndexOf(">", StringComparison.Ordinal) + 1;
        builder.AppendLine(testLine);

        if (rebuilt || !File.Exists(testFileName))
        {
            File.WriteAllText(testFileName, builder.ToString());
        }
        return testFileName;
    }
}