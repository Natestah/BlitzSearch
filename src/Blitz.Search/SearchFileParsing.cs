using System.Text;
using System.Text.RegularExpressions;
using Blitz.Interfacing.QueryProcessing;
namespace Blitz.Search;

/// <summary>
/// Generator for SearchFileInformation
/// </summary>
public partial class SearchFileParsing
{
    private readonly string _fileName;
    private readonly SearchQuery _searchQuery;
    public SearchFileParsing( string fileName, SearchQuery query)
    {
        _fileName = fileName;
        _searchQuery = query;
    }

    private const long BytesInMB = 1048576;

    public SearchFileInformation ParseFile(FilesByExtension extensionCache)
    {
        DateTime startTime = DateTime.Now;
        do
        {
            try
            {
               var info = _parseFileTry(extensionCache);
               return info;
            }
            catch (IOException)
            {
                continue;
            }
        } while (DateTime.Now - startTime < TimeSpan.FromSeconds(1));
        var searchFileInformation = new SearchFileInformation
        {
            FileState = SearchFileInformation.ReadState.Unknown
        };
        return searchFileInformation;
    }
    
    private SearchFileInformation _parseFileTry(FilesByExtension extensionCache)
    {
        HashSet<string> uniqueWordCollection = [];
        var searchFileInformation = new SearchFileInformation();
        var fileInfo = new FileInfo(_fileName);
        searchFileInformation.LastModifiedTime = fileInfo.LastWriteTime.ToUniversalTime();
        searchFileInformation.FileSize = fileInfo.Length;

        var converted = fileInfo.Length / BytesInMB;
        if (converted > _searchQuery.RobotFilterMaxSizeMB)
        {
            searchFileInformation.RobotState = RobotFileState.FileToLarge;
            searchFileInformation.FileState = SearchFileInformation.ReadState.Skipped;
        }
        
        
        using var file = new FileStream(this._fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var streamReader = new StreamReader(_fileName);
        var wordBuilder = new StringBuilder();
        int charsOnLine = 0;
        int controlCharCount = 0;
        bool onFirstLine = true;
        bool wordIsAllHex = true;
        while (streamReader.Peek() != -1)
        {
            char charCurrent = (char)streamReader.Read();
            if (char.IsLetterOrDigit(charCurrent))
            {
                if (!Uri.IsHexDigit(charCurrent))
                {
                    wordIsAllHex = false;
                }
                charsOnLine++;
                wordBuilder.Append(charCurrent);
            }
            else
            {
                if (wordBuilder.Length > 0 && !wordIsAllHex)
                {
                    uniqueWordCollection.Add(wordBuilder.ToString().ToLowerInvariant());
                }
                wordBuilder.Clear();
                wordIsAllHex = true;

                if (charCurrent is '\n' or '\r')
                {
                    onFirstLine = false;
                    charsOnLine = 0;
                }
            }

            if (onFirstLine && charsOnLine > _searchQuery.RobotFilterMaxLineChars)
            {
                searchFileInformation.RobotState = RobotFileState.LongReadLines;
                if (!IsRobotSearched())
                {
                    break;
                }
            }

            if (charCurrent == '\0' || !char.IsWhiteSpace(charCurrent) && char.IsControl(charCurrent) &&
                charCurrent != '\r' && charCurrent != '\n')
            {
                controlCharCount++;
                if (controlCharCount > 10)
                {
                    searchFileInformation.RobotState = RobotFileState.LooksLikeBinary;
                    if (!IsRobotSearched())
                    {
                        break;
                    }
                }
            }
        }

        if (searchFileInformation.RobotState == RobotFileState.Unknown)
        {
            searchFileInformation.RobotState = RobotFileState.LooksHuman;
        }
        else if( !IsRobotSearched())
        {
            searchFileInformation.UniqueWords = [];
            searchFileInformation.FileState = SearchFileInformation.ReadState.Skipped;
            return searchFileInformation;
        }


        searchFileInformation.UniqueWords = extensionCache.GetWordPositions(uniqueWordCollection);
        searchFileInformation.FileState = SearchFileInformation.ReadState.Read;
        return searchFileInformation;
    }

    private bool IsRobotSearched() => _searchQuery == null || _searchQuery.EnableRobotFileFilterDefer;

}