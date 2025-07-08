using System.Text;
using System.Text.RegularExpressions;

namespace Blitz;

public static class ChangeLog
{
    public const string LatestGitHubChangeLogURL = "https://raw.githubusercontent.com/Natestah/BlitzSearch/refs/heads/main/src/Blitz/Documentation/Change_Log.md";
    public static List<BlitzVersion> ParseChangeMarkDown( StreamReader re )
    {
        var versionMatch = new Regex(@"Version (\d*\.\d*\.\d*)", RegexOptions.Compiled);
        var versionBuilder = new Dictionary<string, StringBuilder>();
        string? currentVersion = null;
        while (re.Peek() != -1)
        {
            var line = re.ReadLine()!;
            var match = versionMatch.Match(line);
            if (match.Success)
            {
                currentVersion = match.Groups[1].ToString();
                versionBuilder[currentVersion] = new StringBuilder();
                continue;
            }

            if (currentVersion != null)
            {
                versionBuilder[currentVersion].AppendLine(line);
            }
        }
        var versionList = new List<BlitzVersion>();
        foreach (var kvp in versionBuilder)
        {
            versionList.Add(new BlitzVersion{ Revision = kvp.Key,Changes = kvp.Value.ToString()});
        }

        return versionList;
    }
}