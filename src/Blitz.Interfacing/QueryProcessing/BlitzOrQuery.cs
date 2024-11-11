using System.Net;
using System.Net.Sockets;
using MessagePack;

namespace Blitz.Interfacing.QueryProcessing;

[MessagePackObject]
public class BlitzOrQuery: IBlitzMatchingQuery
{
    [Key(nameof(SearchWord))]
    public string SearchWord { get; set; }= string.Empty;
    
    [Key(nameof(SubQueries))]
    public List<IBlitzMatchingQuery> SubQueries { get; set; } = [];
    
    public bool LineMatches(string lineText, bool caseSensitive, out List<BlitzMatch> matches)
    {
        bool anyMatched = false;
        matches = null!;
        foreach (var subQuery in SubQueries)
        {
            if (!subQuery.LineMatches(lineText, caseSensitive, out var subQueryMatch))
            {
                continue;
            }

            matches ??= [];
            matches.AddRange(subQueryMatch);
            
            anyMatched = true;
        }
        return anyMatched;
    }

    public bool SearchIndexValidFor(IEnumerable<string> uniqueWords)
    {
        foreach (var subQuery in SubQueries)
        {
            if (!subQuery.SearchIndexValidFor(uniqueWords)) return false;
        }
        return true;
    }

    private static char BlitzOrCharacter => '|';
    public static bool QueryMatches(string querySection, out BlitzOrQuery orQuery)
    {
        orQuery = null!;
        if (!querySection.Contains(BlitzOrCharacter))
        {
            return false;
        }

        orQuery = new BlitzOrQuery() { SearchWord = querySection };
        var splitOr = querySection.Split(BlitzOrCharacter, StringSplitOptions.RemoveEmptyEntries);

        foreach (var orWord in splitOr)
        {
            BlitzWordInQuery.QueryMatches(orWord, out var result);
            orQuery.SubQueries.Add(result);
        }
        return true;
    }
}