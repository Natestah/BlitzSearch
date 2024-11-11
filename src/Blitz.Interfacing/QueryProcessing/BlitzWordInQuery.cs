using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using MessagePack;

namespace Blitz.Interfacing.QueryProcessing;

[MessagePackObject]
public partial class BlitzWordInQuery: IBlitzMatchingQuery
{

    [Key(nameof(SearchWord))]
    public string SearchWord { get; set; }= string.Empty;
    
    [Key(nameof(SubQueries))] 
    public List<IBlitzMatchingQuery> SubQueries { get; set; } = [];

    [Key(nameof(IndexWords))] 
    public List<string> IndexWords { get; set; } = [];

    [Key(nameof(WholeWord))] 
    public bool WholeWord { get; set; }
    
    [Key(nameof(CaseSensitive))] 
    public bool CaseSensitive { get; set; }

    [Key(nameof(IsExclude))] 
    public bool IsExclude { get; set; }
    
    public BlitzWordInQuery()
    {
        
    }
    
    public BlitzWordInQuery(string searchWord, List<IBlitzMatchingQuery> subQueries)
    {
        SearchWord = searchWord;
        SubQueries = subQueries;
    }

    
    //Todo: I did this as an optimization to search terms like "a" yielding inhuman and clunky results..
    //Should look to add it as optional parameter.
    private const int MaxMatches = 10;   
    
    
    public bool LineMatches(string lineText, bool _, out List<BlitzMatch> matches)
    {
        if (WholeWord)
        {
            return LineMatchesWholeWord(lineText, CaseSensitive, out matches);
        }
            
        int startingIndex = 0;
        bool hitAny = false;
        int indexOf = -1;
        matches = [];
        StringComparison comparison =
            CaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
        
        while(true)
        {
            indexOf = lineText.IndexOf(SearchWord, startingIndex, comparison);
            if (indexOf == -1)
            {
                break;
            }

            hitAny = true;
            matches.Add(new BlitzMatch{MatchIndex = indexOf, MatchLength = SearchWord.Length});
            startingIndex = indexOf + SearchWord.Length;

            if (matches.Count > MaxMatches)
            {
                break;
            }
        }

        return IsExclude ? !hitAny : hitAny;
    }

    bool CharIsWordBoundary(char c)
    {
        if (char.IsLetterOrDigit(c))
        {
            return false;
        }

        return c != '_';
    }

    public bool LineMatchesWholeWord(string lineText, bool _, out List<BlitzMatch> matches)
    {
        int matchIndex = 0;
        bool matchedAny = false;
        
        StringComparison comparison = CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        matches = [];
        if (SearchWord.Length == 0)
        {
            return false;
        }
        while (true)
        {
            matchIndex = lineText.IndexOf(SearchWord,matchIndex, comparison);
            if (matchIndex == -1)
            {
                break;
            }

            bool matchedStart = matchIndex == 0 || CharIsWordBoundary(lineText[matchIndex - 1]) || !char.IsLetterOrDigit(lineText[matchIndex]);
            int endWordIndex = matchIndex + SearchWord.Length;
            bool matchedEnd = endWordIndex == lineText.Length || CharIsWordBoundary(lineText[endWordIndex]);

            if (matchedStart && matchedEnd)
            {
                matchedAny = true;
                var expressedMatch = new BlitzMatch
                {
                    MatchIndex = matchIndex,
                    MatchLength = SearchWord.Length
                };
                matches.Add(expressedMatch);
            }
            matchIndex++;
        }
        return IsExclude ? !matchedAny : matchedAny;
    }

    public bool SearchIndexValidFor(IEnumerable<string> uniqueWords)
    {
        foreach (var word in IndexWords)
        {
            bool anyContains = false;

            foreach (var uniqueWord in uniqueWords)
            {  
                if (uniqueWord.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    anyContains = true;
                }
            }

            if (!anyContains)
            {
                return false;
            }
        }
        return true;
    }
    
    public static readonly Regex WordsExpression = MyRegex();

    public static List<string> GetOnlyIndexingWords(string querySection)
    {
        var wordBuilder = new StringBuilder();
        var newWords = new HashSet<string>();
        bool isAllHex = true;

        void ClearWord()
        {
            if (wordBuilder.Length > 0 && !isAllHex)
            {
                newWords.Add(wordBuilder.ToString().ToLowerInvariant());
            }
            wordBuilder.Clear();
            isAllHex = true;
        }
        foreach (char charCurrent in querySection)
        {
            if (char.IsLetterOrDigit(charCurrent))
            {
                if (!Uri.IsHexDigit(charCurrent))
                {
                    isAllHex = false;
                }
                wordBuilder.Append(charCurrent);
            }
            else
            {
                ClearWord();
            }

        }
        ClearWord();
        return newWords.ToList();
    }
    
    
    public static bool QueryMatches(string querySection, out BlitzWordInQuery wordInQuery)
    {
        bool wholeWord = false;
        bool forceSensitive = false;
        bool smartSensitive = false;
        bool isExclude = false;
        int wordStartIndex = 0;
        int wordStartEndIndex = querySection.Length;
        for (int i = 0; i < querySection.Length; i++)
        {
            wordStartIndex = i;
            if (querySection[i] == WholeWordCharacter)
            {
                wholeWord = true;
                
            }
            else if (querySection[i] == CaseSensitiveCharacter)
            {
                forceSensitive = true;
            }
            else if (querySection[i] == ExcludeCharacter)
            {
                isExclude = true;
            }
            else
            {
                break;
            }
        }

        for (int i = querySection.Length - 1; i >= 0; i--)
        {
            if (querySection[i] == WholeWordCharacter)
            {
                wholeWord = true;
                
            }
            else if (querySection[i] == CaseSensitiveCharacter)
            {
                forceSensitive = true;
            }
            else if (querySection[i] == ExcludeCharacter)
            {
                isExclude = true;
            }
            else
            {
                break;
            }
            wordStartEndIndex = i;
        }

        for (int i = wordStartIndex; i < wordStartEndIndex; i++)
        {
            if (char.IsUpper(querySection[i]))
            {
                smartSensitive = true;
                break;
            }
        }
        
        int length = wordStartEndIndex - wordStartIndex;
        
        string updatedWord =length<0? "": querySection.Substring(wordStartIndex, length);;

        wordInQuery = new BlitzWordInQuery
        {
            SearchWord = updatedWord,
            IndexWords = GetOnlyIndexingWords(updatedWord),
            WholeWord = wholeWord,
            CaseSensitive = forceSensitive || smartSensitive,
            IsExclude = isExclude
        };
        return true;
    }
    
    private static char ExcludeCharacter => '!';
    private static char WholeWordCharacter => '@';
    private static char CaseSensitiveCharacter => '^';
   

    [GeneratedRegex(@"\w+", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}