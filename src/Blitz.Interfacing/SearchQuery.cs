using System.ComponentModel;
using System.Text;
using Blitz.Interfacing.QueryProcessing;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace Blitz.Interfacing;

[MessagePackObject]
public class SearchQuery : MessageWithIdentity
{
    [Key(nameof(TextBoxQuery))] 
    public string TextBoxQuery { get; set; }

    [Key(nameof(FileNameQuery))] 
    public string? FileNameQuery { get; set; }

    [Key(nameof(LiteralSearchQuery))] 
    public string? LiteralSearchQuery { get; set; }    
    
    [Key(nameof(RegexSearchQuery))] 
    public string? RegexSearchQuery { get; set; }

    [Key(nameof(ReplaceLiteralTextQuery))] 
    public string? ReplaceLiteralTextQuery { get; set; }
    
    [Key(nameof(ReplaceRegexTextQuery))] 
    public string? ReplaceRegexTextQuery { get; set; }
    
    [Key(nameof(ReplaceTextQuery))] 
    public string? ReplaceTextQuery { get; set; }
    
    [Key(nameof(ReplaceTextWithQuery))] 
    public string? ReplaceTextWithQuery { get; set; }
    
    [Key(nameof(DebugFileNameQuery))] 
    public string? DebugFileNameQuery { get; set; }
    
    [Key(nameof(FileNameQueryEnabled))]
    public bool FileNameQueryEnabled { get; set; }

    [Key(nameof(LiteralSearchEnabled))]
    public bool LiteralSearchEnabled { get; set; }
    
    [Key(nameof(RegexSearchEnabled))]
    public bool RegexSearchEnabled { get; set; }

    [Key(nameof(ReplaceInFileEnabled))]
    public bool ReplaceInFileEnabled { get; set; }


    //Todo: FilePaths / Extensions go to their own exportable Discipline export

    [Key(nameof(FilePaths))] 
    public List<SearchPath> FilePaths { get; set; } = [];

    [Key(nameof(PriorityExtensions))]
    public List<SearchExtension> PriorityExtensions { get; set; }

    [Key(nameof(UseGitIgnore))] 
    [DefaultValue(true)]
    public bool UseGitIgnore;

    [Key(nameof(VerboseFileSystemException))]
    [DefaultValue(true)]
    public bool VerboseFileSystemException;

    [Key(nameof(EnableSearchIndex))] 
    [DefaultValue(true)]
    public bool EnableSearchIndex;

    [Key(nameof(SearchThreads))] 
    [DefaultValue(32)]
    public int SearchThreads;

    [Key(nameof(EnableResultsRecycling))] 
    [DefaultValue(true)]
    public bool EnableResultsRecycling;

    [Key(nameof(EnableRobotFileFilterIgnore))]
    [DefaultValue(false)]
    public bool EnableRobotFileFilterIgnore;
    
    [Key(nameof(EnableRobotFileFilterDefer))]
    [DefaultValue(false)]
    public bool EnableRobotFileFilterDefer;
    
    [Key(nameof(EnableRobotFileFilterSkipAndReport))]
    [DefaultValue(true)]
    public bool EnableRobotFileFilterSkipAndReport;
    
    [Key(nameof(RobotFilterMaxSizeMB))]
    [DefaultValue(1.6)]
    public double RobotFilterMaxSizeMB;
    
    [Key(nameof(RobotFilterMaxLineChars))]
    [DefaultValue(8000)]
    public int RobotFilterMaxLineChars;

    [Key(nameof(CaseSensitive))]
    [DefaultValue(false)]
    public bool CaseSensitive;
    
    [Key(nameof(RegexCaseSensitive))]
    [DefaultValue(false)]
    public bool RegexCaseSensitive;
    
    [Key(nameof(LiteralCaseSensitive))]
    [DefaultValue(false)]
    public bool LiteralCaseSensitive;
    
    [Key(nameof(ReplaceCaseSensitive))]
    [DefaultValue(false)]
    public bool ReplaceCaseSensitive;

    [Key(nameof(SmartCase))]
    [DefaultValue(false)]
    public bool SmartCase;
    
    [Key(nameof(FlatSearchFilesList))]
    public List<string>? FlatSearchFilesList;

    [Key( nameof(SolutionExports))]
    public List<SolutionExport>? SolutionExports;
    
    [Key(nameof(SelectedProjectName))]
    public string? SelectedProjectName { get; set; }
    
    public SearchQuery(string textBoxQuery, List<SearchPath> filePaths, List<SearchExtension> priorityExtensions,
        bool useGitIgnore = true, bool enableSearchIndex = true, bool enableResultsRecycling = true, int searchThreads = 32)
    {
        TextBoxQuery = textBoxQuery;
        FilePaths = filePaths;
        PriorityExtensions = priorityExtensions ?? [];
        UseGitIgnore = useGitIgnore;
        EnableSearchIndex = enableSearchIndex;
        SearchThreads = searchThreads;
        EnableResultsRecycling = enableResultsRecycling;
    }

    //Todo: Put stuff below in it's own "helper" class

    [IgnoreMember] private string? _rawExtension;

    private static readonly char[] Spacechars = new[] { ' ', '\t' };

    [IgnoreMember]
    public string RawExtensionList
    {
        get
        {
            if (_rawExtension != null)
            {
                return _rawExtension;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < PriorityExtensions.Count; i++)
            {
                var extension = PriorityExtensions[i];
                if (extension.Extension != null)
                {
                    builder.Append(extension.Extension.TrimStart('.'));
                }
                if (i < PriorityExtensions.Count - 1)
                {
                    builder.Append(' ');
                }
            }

            return _rawExtension = builder.ToString();
        }
        set
        {
            _rawExtension = value;
            var newExtensions = new List<SearchExtension>();

            if (_rawExtension != null)
            {
                string[] split = _rawExtension.Split(Spacechars, StringSplitOptions.RemoveEmptyEntries);
                foreach (var extension in split)
                {
                    var add = $".{extension.ToLower().TrimStart('.')}";
                    newExtensions.Add(new SearchExtension { Extension = add });
                }
            }

            PriorityExtensions = newExtensions;
            _rawExtension = value;
        }
    }
    

    public static void SaveFile(string storeFile, SearchQuery query)
    {
        byte[] bytes = MessagePackSerializer.Serialize<SearchQuery>(query);
        File.WriteAllBytes(storeFile, bytes);
    }

    public static SearchQuery LoadFile(string storeFile)
    {
        var bytes = File.ReadAllBytes(storeFile);
        try
        {
            return MessagePackSerializer.Deserialize<SearchQuery>(bytes);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null!;
    }

    public SearchQuery Clone()
    {
        return MessagePackSerializer.Deserialize<SearchQuery>(MessagePackSerializer.Serialize<SearchQuery>(this));
    }

}