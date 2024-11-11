using System.Text;
using MessagePack;

namespace Blitz.Interfacing.QueryProcessing;

[MessagePackObject]
public class BlitzAndQuery: IBlitzMatchingQuery
{
    [Key(nameof(SearchWord))]
    public string SearchWord { get; set; } = string.Empty;

    [Key(nameof(SubQueries))] 
    public List<IBlitzMatchingQuery> SubQueries { get; set; } = [];

    public BlitzAndQuery(string searchWord, List<IBlitzMatchingQuery> subQueries)
    {
        SearchWord = searchWord;
        SubQueries = subQueries;
    }

    public BlitzAndQuery()
    {
        
    }
    public bool LineMatches(string lineText, bool caseSensitive, out List<BlitzMatch> matches)
    {
        matches = null!;
        foreach (var subQuery in SubQueries)
        {
            if (!subQuery.LineMatches(lineText, caseSensitive, out var matchCollection))
            {
                return false;
            }

            matches ??= [];
            matches.AddRange(matchCollection);
        }
        return true;
    }

    public bool SearchIndexValidFor(IEnumerable<string> uniqueWords)
    {
        foreach (var subQuery in SubQueries)
        {
            if (!subQuery.SearchIndexValidFor(uniqueWords))
            {
                return false;
            }
        }

        return true;
    }


    private static readonly char[] WhiteSpaceChars = new[] { ' ', '\t', };

    public static bool QueryMatches(string querySection, out BlitzAndQuery andQuery)
    {
        andQuery = new BlitzAndQuery() { SearchWord = querySection };
        if (querySection == null)
        {
            return false;
        }
        var subQueries = querySection.Split(WhiteSpaceChars, StringSplitOptions.RemoveEmptyEntries);
        foreach (var subQuery in subQueries)
        {
            if (BlitzOrQuery.QueryMatches(subQuery, out BlitzOrQuery orQuery))
            {
                andQuery.SubQueries.Add(orQuery);
            }
            else
            {
                if (BlitzWordInQuery.QueryMatches(subQuery, out var wordInQuery))
                {
                    andQuery.SubQueries.Add(wordInQuery);
                }
            }
        }
        return true;
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append("Lines Containing: ");

        for (var index = 0; index < SubQueries.Count; index++)
        {
            var subQuery = SubQueries[index];
            if (index < SubQueries.Count - 1)
            {
                builder.Append(" AND ");
            }
            
            switch (subQuery)
            {
                case BlitzOrQuery:
                {
                    builder.Append('(');
                    for (var subQueryIndex = 0; subQueryIndex < subQuery.SubQueries.Count; subQueryIndex++)
                    {
                        builder.Append(subQuery.SearchWord);
                        if (subQueryIndex < subQuery.SubQueries.Count - 1)
                        {
                            builder.Append(" OR ");
                        }
                    }
                    builder.Append(')');
                    break;
                }
                case BlitzWordInQuery:
                    if (subQuery is BlitzWordInQuery { IsExclude: true })
                    {
                        builder.Append(" NOT ");
                    }
                    if (subQuery is BlitzWordInQuery { WholeWord: true })
                    {
                        builder.Append(" WHOLE WORD ");
                    }
                    builder.Append(subQuery.SearchWord);
                    break;
                default:
                    builder.Append(subQuery.SearchWord);
                    break;
            }
        }
        return builder.ToString();
    }
}