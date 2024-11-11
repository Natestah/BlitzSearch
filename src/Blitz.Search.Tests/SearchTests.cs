using System.Collections.Concurrent;
using System.Text;
using Blitz.Interfacing;
using Blitz.Interfacing.QueryProcessing;
using MessagePack;

namespace Blitz.Search.Tests;

public class SearchTests : IDisposable
{
    private const string OneFile = "file.cs";
    private const string TwoFile = "file2.cs";
    private readonly string _tempPath;
    private readonly Dictionary<string, string> _filesAndContents;

    public SearchTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), "blitz_search_tests");
        Directory.CreateDirectory(_tempPath);
        _filesAndContents = new Dictionary<string, string>();
        PrepareTestFiles();
    }

    [Fact]
    public void Test_ParseDictionary_Serialized()
    {
        var freshCache = new SearchExtensionCache();
        foreach (var keyFileName in _filesAndContents.Keys)
        {
            string fileName = Path.Combine(_tempPath, keyFileName);
            var fileSearcher = new SearchFileParsing(fileName, new SearchQuery(string.Empty,[],[]));
            string extension = Path.GetExtension(fileName);
            var fileCache = freshCache.GetOrAdd(extension,
                (_) => new FilesByExtension());
            var searchFileInformation = fileSearcher.ParseFile(fileCache);
            fileCache.TryAdd(keyFileName, searchFileInformation);
        }

        foreach (var cache in freshCache.Values)
        {
            byte[] bytes = MessagePackSerializer.Serialize(cache);
            var deserialized = MessagePackSerializer.Deserialize<ConcurrentDictionary<string, SearchFileInformation>>(bytes);
            
            Assert.All(deserialized, deserializedItem =>
            {
                Assert.True(cache.TryGetValue(deserializedItem.Key, out var cacheItemValue));
                Assert.Equal(cacheItemValue!.UniqueWords, deserializedItem.Value.UniqueWords);
                Assert.Equal(cacheItemValue.FileSize, deserializedItem.Value.FileSize);
                Assert.Equal(cacheItemValue.FileState, deserializedItem.Value.FileState);
                Assert.Equal(cacheItemValue.LastModifiedTime, deserializedItem.Value.LastModifiedTime);
            });
        }
    }

    [Theory()]
    [InlineData(OneFile,new[] {"one","two","three"} )]
    [InlineData(TwoFile,new[] {"apple","orange","three", "the", "end"} )]
    public void Test_SearchParsing_SingleFile(string file, string[] uniqueWords)
    {
        string fileName = Path.Combine(_tempPath, file);
        var fileSearcher = new SearchFileParsing(fileName, new SearchQuery(string.Empty,[],[]));
        var filesByExtension = new FilesByExtension();
        var searchFileInformation = fileSearcher.ParseFile(filesByExtension);
        var exceptSet = new HashSet<string>(uniqueWords);
        exceptSet.ExceptWith(filesByExtension.GetWordStrings(searchFileInformation.UniqueWords));
        Assert.Empty(exceptSet);
    }

    [Theory()]
    [InlineData(OneFile, "one",new[] {"one two three","one"})]
    [InlineData(OneFile, "three", new[] {"one two three","three three three"})]
    [InlineData(OneFile, "OnE", new string[] {})]
    [InlineData(TwoFile, "apple", new[] {"apple orange"})]
    [InlineData(TwoFile, "orange", new[] {"apple orange"})]
    [InlineData(TwoFile, "end", new[] {"the end"})]
    [InlineData(TwoFile, "END", new string[] {})]
    [InlineData(TwoFile, "one", new string[] {})]
    
    public void Tests_SearchIng_SingleFile(string file, string searchTerm, string[] resultLines)
    {
        string fileName = Path.Combine(_tempPath, file);
        BlitzAndQuery.QueryMatches(searchTerm, out BlitzAndQuery andQuery);
        var fileSearcher = new SearchFileContents(fileName, new SearchTaskParameters(andQuery), null);
        fileSearcher.DoSearchFind(out var results);

        Assert.Equal(resultLines.Length, results.Count);
        for (int i = 0; i < resultLines.Length; i++)
        {
            Assert.Equal(resultLines[i],results[i].CapturedContents);
        }
    }
    private void PrepareTestFiles()
    {
        var oneFileContents = new StringBuilder();
        oneFileContents.AppendLine("one two three");
        oneFileContents.AppendLine("three three three");
        oneFileContents.AppendLine("one");
        _filesAndContents[OneFile] = oneFileContents.ToString();

        var twoFIleContents = new StringBuilder();
        twoFIleContents.AppendLine("apple orange");
        twoFIleContents.AppendLine("Three Three Three");
        twoFIleContents.AppendLine("the end");
        _filesAndContents[TwoFile] = twoFIleContents.ToString();

        foreach (var kvp in _filesAndContents)
        {
            string fileName = Path.Combine(_tempPath, kvp.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(fileName)!);
            File.WriteAllText(fileName, kvp.Value);
        }
    }
    private void CleanupTestFiles()
    {
        var dirs = new HashSet<string>();
        foreach (var shortFile in _filesAndContents.Keys)
        {
            var fileName = Path.Combine(_tempPath, shortFile);
            var dir = Path.GetDirectoryName(fileName)!;
            dirs.Add(dir);
            File.Delete(fileName);
        }

        foreach (var dir in dirs)
        {
            Directory.Delete(dir);
        }
    }


    public void Dispose()
    {
        CleanupTestFiles();
    }
}