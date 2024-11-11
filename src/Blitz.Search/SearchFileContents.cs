using System.Runtime.CompilerServices;
using Blitz.Interfacing.QueryProcessing;
using System.Text;
using System.Text.RegularExpressions;
namespace Blitz.Search;

public class SearchFileContents
{
    private readonly string _fileName;
    private readonly SearchTaskParameters _taskParameters;
    private readonly CancellationTokenSource _cancellationTokenSource;
    
    public SearchFileContents(string file, SearchTaskParameters parameters, CancellationTokenSource? cancellationTokenSource)
    {
        _fileName = file;
        _cancellationTokenSource = cancellationTokenSource ?? new CancellationTokenSource();
        _taskParameters = parameters;
        _fileName = file;
    }
    

    public static bool ReplaceMatches(string lineContents,
        SearchTaskParameters taskParameters,
        out string? replaceContents,
        out List<BlitzMatch>? replaceMatches)
    {

        if (!string.IsNullOrEmpty(taskParameters.ReplaceLiteralSearch))
        {
            if (ReplaceLiteralMatches(lineContents,taskParameters, out replaceContents, out replaceMatches))
            {
                return true;
            }
            replaceMatches = null;
            replaceContents = null;
            return false;
        }
        
        if (taskParameters.ReplaceRegex != null)
        {
            if (ReplaceRegexMatches(lineContents,taskParameters, out replaceContents, out replaceMatches))
            {
                return true;
            }
            replaceMatches = null;
            replaceContents = null;
            return false;
        }
        if (taskParameters.ReplaceQuery == null)
        {
            replaceMatches = null;
            replaceContents = null;
            return false;
        }

        bool replaceMatch  =  taskParameters.ReplaceQuery.LineMatches(lineContents, taskParameters.ReplaceCaseSensitive, out replaceMatches);

        if (!replaceMatch)
        {
            replaceMatches = null;
            replaceContents = null;
            return false;
        }
        
        ApplyBlitzMatchReplacements(lineContents,taskParameters, out replaceContents, replaceMatches);
        return true;
    }
    public static bool ReplaceLiteralMatches(string lineContents,
        SearchTaskParameters taskParameters,
        out string? replaceContents,
        out List<BlitzMatch>? replaceMatches)
    {

        if (string.IsNullOrEmpty(taskParameters.ReplaceLiteralSearch))
        {
            replaceMatches = null;
            replaceContents = null;
            return false;
        }

        var literalMatches = new List<BlitzMatch>();
        if (!GetLiteralMatches(lineContents,taskParameters,taskParameters.ReplaceLiteralSearch, ref literalMatches, taskParameters.ReplaceCaseSensitive))
        {
            replaceMatches = null;
            replaceContents = null;
            return false;
        }
        replaceMatches = literalMatches;
        ApplyBlitzMatchReplacements(lineContents,taskParameters, out replaceContents, replaceMatches);
        return true;
    }

    private static void ApplyBlitzMatchReplacements(string lineContents, SearchTaskParameters taskParameters, out string replaceContents, List<BlitzMatch> replaceMatches)
    {
        if (taskParameters.ReplaceWith == null)
        {
            replaceContents = lineContents;
            return;
        }
        var replaceLine = new StringBuilder(lineContents);
        int addOffset = 0;
        foreach (var match in replaceMatches!)
        {
            match.Replacement =  lineContents.Substring(match.MatchIndex, match.MatchLength);
        }
                
        foreach (var match in replaceMatches)
        {
            var thisOffset = match.MatchIndex + addOffset; 
            replaceLine.Remove(thisOffset, match.MatchLength);
            replaceLine.Insert(thisOffset, taskParameters.ReplaceWith);
            addOffset += taskParameters.ReplaceWith.Length - match.MatchLength;
            match.MatchLength = taskParameters.ReplaceWith.Length;
            match.MatchIndex = thisOffset;
        }
        replaceContents = replaceLine.ToString();
    }

    private static bool ReplaceRegexMatches(string lineContents,
        SearchTaskParameters taskParameters,
        out string? replaceContents,
        out List<BlitzMatch>? replaceMatches)
    {
        if (taskParameters.ReplaceRegex == null)
        {
            replaceMatches = null;
            replaceContents = null;
            return false;
        }

        var regexMatches = new List<BlitzMatch>();
        if (!GetRegexMatches(lineContents,taskParameters.ReplaceRegex, ref regexMatches))
        {
            replaceMatches = null;
            replaceContents = null;
            return false;
        }
        replaceMatches = regexMatches;
        ApplyBlitzMatchReplacements(lineContents,taskParameters, out replaceContents, replaceMatches);
        return true;
    }
    
    public bool DoSearchFind(out List<FileContentResult> contentResults)
    {
        contentResults = [];
        using var file = new FileStream(this._fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var streamReader = new StreamReader(file);
        var lineNumber = 0;
        
        while (streamReader.Peek() != -1)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                return false;
            }
            lineNumber++;
            var lineContents = streamReader.ReadLine()!;

            List<BlitzMatch>? blitzMatches = null;
            List<BlitzMatch>? replaceBlitzMatches = null;

            bool findMatch = false;
            bool replaceMatch = false;

            string? replacedContents = null;

            bool isReplaceQuery = _taskParameters.ReplaceQuery != null; 
            if (isReplaceQuery)
            {
                replaceMatch = ReplaceMatches(lineContents, _taskParameters,
                    out replacedContents,
                    out replaceBlitzMatches);
            }


            bool performedAndQuery = _taskParameters.TextBoxQuery is { SubQueries.Count: > 0 }; 
            if (performedAndQuery )
            {
                if (_taskParameters.TextBoxQuery != null)
                {
                    findMatch = _taskParameters.TextBoxQuery.LineMatches(lineContents, false,
                        out blitzMatches);
                }
                else
                {
                    performedAndQuery = false;
                }
            }

            if (performedAndQuery && !findMatch)
            {
                continue;
            }

            if (!string.IsNullOrEmpty( _taskParameters.LiteralSearch ))
            {
                blitzMatches ??= [];
                if (!GetLiteralMatches(lineContents,_taskParameters, _taskParameters.LiteralSearch, ref blitzMatches, _taskParameters.LiteralCaseSensitive))
                {
                    continue;
                }
                else
                {
                    findMatch = true;
                }
            }
            
            if (_taskParameters.RegexSearch != null)
            {
                if (performedAndQuery && !findMatch)
                {
                    continue;
                }
                var rxMatch = _taskParameters.RegexSearch.Match(lineContents);
                if (!rxMatch.Success)
                {
                    continue;
                }

                blitzMatches = [];
                findMatch = true;
                foreach (Match matchInstance in _taskParameters.RegexSearch.Matches(lineContents))
                {
                    for (var index = 0; index < matchInstance.Groups.Count; index++)
                    {
                        var subGroup = matchInstance.Groups[index];
                        var match = new BlitzMatch
                            { MatchIndex = subGroup.Index, MatchLength = subGroup.Length, IsRegexSubgroup = index > 0};
                        blitzMatches.Add(match);
                    }
                }
            }

            if (!findMatch && !replaceMatch)
            {
                continue;
            }

            if (isReplaceQuery  )
            {
                if (!replaceMatch)
                {
                    continue;
                }

                if (performedAndQuery && !findMatch)
                {
                    continue;
                }
            }

            blitzMatches ??= [];
            if (replaceBlitzMatches != null)
            {
                blitzMatches.AddRange(replaceBlitzMatches);
            }

            var fileContentResult = new FileContentResult 
            { 
                CapturedContents = lineContents, 
                BlitzMatches = blitzMatches,
                ReplacedContents = replacedContents,
                Replacing = replaceMatch,
                LineNumber = lineNumber
            };
            contentResults.Add(fileContentResult);
        }
        return contentResults.Count > 0;
    }

    private static bool GetLiteralMatches(string lineContents,SearchTaskParameters taskParameters ,string searchFor, ref List<BlitzMatch> blitzMatches, bool caseSensitive)
    {
        bool foundAny = false;
        int index = 0;
        do
        {
            index = lineContents.IndexOf(searchFor, index,  caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            if (index == -1)
            {
                break;
            }
            var match = new BlitzMatch { MatchIndex = index, MatchLength = searchFor.Length};
            blitzMatches ??= [];
            blitzMatches.Add(match);
            
            index+= searchFor.Length;
            foundAny = true;
        } while (index > -1 && index < lineContents.Length);
        
        return foundAny;
    }
    private static bool GetRegexMatches(string lineContents, Regex expression, ref List<BlitzMatch> blitzMatches)
    {
        
        var rxMatch = expression.Match(lineContents);
        if (!rxMatch.Success)
        {
            return false;
        }

        blitzMatches ??= [];
        foreach (Match matchInstance in expression.Matches(lineContents))
        {
            var match = new BlitzMatch { MatchIndex = matchInstance.Index, MatchLength = matchInstance.Length };
            blitzMatches.Add(match);
        }

        return true;
    }
}