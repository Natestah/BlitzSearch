using System.Collections.Concurrent;

namespace Blitz.Search;


/// <summary>
/// Type Detection for Blitz Search, Helps create awarenes of types that would trip up, slow down a search.
///
/// Where Typically you would have .gitIgnore helping you avoid binary files, this helps in the case that you Do not.
///
/// There are several layers here, one is that we have built in Known binary types.  This helps typical speed.
///
/// The second is that we do a quick read of the file, typically this can be determined pretty quick by presence of control characters.
///
/// We can also determine that a file is a non-human serialized single line text like a JSON string that you might not want to ever ready in a human intended search.
/// </summary>
public class TypeDetection
{
    private static HashSet<string> _knownTextTypes =
    [
        ".c", ".h", ".cc", ".cpp", ".hpp", ".cs", ".js", ".htm", ".html", ".asp", ".aspx", ".asax", ".asmx", ".ascx",
        ".master", ".boo", ".atg", ".css", ".java", ".patch", ".diff", ".ps1", ".psm1", ".psd1", ".php", ".py", ".pyw",
        ".tex", ".sql", ".vb", ".xml", ".xsl", ".xslt", ".xsd", ".manifest", ".config", ".addin", ".xshd", ".wxs",
        ".wxi", ".wxl", ".proj", ".csproj", ".vbproj", ".ilproj", ".booproj", ".build", ".xfrm", ".targets", ".xaml",
        ".xpt", ".xft", ".map", ".wsdl", ".disco", ".ps1xml", ".nuspec", ".md", ".json"
    ];

    private static HashSet<string> _knownBinaryTypes =
    [
        ".exe", ".bin", ".dll", "*.obj", "*.pdf", ".4db", ".4dc", ".4dd",
        ".4dindy", ".4dindx", ".4dr", ".4dZ", ".accdb", ".accde", ".adt", ".apr", ".box", ".chml", ".daf", ".dat",
        ".dat", ".db", ".db", ".dbf", ".dta", ".egt", ".ess", ".eap", ".fdb", ".fdb", ".fp", ".fp3", ".fp5", ".fp7", 
        ".frm", ".gdb", ".gtable",".kexi", ".kexic", ".kexis", ".ldb", ".lirs", ".mda", ".mdb", ".adp", ".mde", ".mdf", 
        ".myd", ".myi", ".ncf", ".nsf", ".ntf", ".nv2", ".odb", ".ora", ".pcontact", ".pdb", ".pdi", ".pdx", ".prc", 
        ".sQl", ".rec", ".rel", ".rin", ".sdb", 
        ".waJournal", ".wdb", ".wmdb", ".pack"
    ];
    
    public TypeDetection()
    {
        foreach (var extension in _knownTextTypes)
        {
            RegisterSimpleExtension(extension,true,BlitzFileType.DetectReason.BuiltInDetermination);
        }
        
        foreach (var extension in _knownBinaryTypes)
        {
            RegisterSimpleExtension(extension,false,BlitzFileType.DetectReason.BuiltInDetermination);
        }
    }

    public IEnumerable<string> GetBuiltInTextTypes() => _knownTextTypes;
    public IEnumerable<string> GetBuiltInBinaryTypes() => _knownTextTypes;
    
    public static TypeDetection Instance { get; } = new TypeDetection();
    
    private readonly ConcurrentDictionary<string, BlitzFileType> _detectionDictionary = new ();
    
    public void RegisterSimpleExtension(string extension, bool isText, BlitzFileType.DetectReason reason)
    {
        var type = _detectionDictionary.GetOrAdd(extension, (_) => new BlitzFileType());
        type.Reason = reason;
        type.IsTextType = isText;
    }

    public bool IsBinary(string extension, string fileName)
    {
        var type = _detectionDictionary.GetOrAdd(extension, (_) => new BlitzFileType());
        
        if (type.Reason != BlitzFileType.DetectReason.NotDetected)
        {
            return !type.IsTextType;
        }

        if (string.IsNullOrEmpty(extension))
        {
            type.Reason = BlitzFileType.DetectReason.HasNoExtension;
            type.IsTextType = false;
            return true;
        }
        
        if (type.FilesThatHaveManyControlChars.Count > 0)
        {
            type.Reason = BlitzFileType.DetectReason.ManyFilesWithControlChars;
            type.IsTextType = false;
            return true;
        }

        var fileInfo = new FileInfo(fileName);
        if (fileInfo.Length > 2000000)
        {
            type.FilesThatAreVeryLarge[fileName] = 1;
        }

        if (type.FilesThatAreVeryLarge.Count > 10)
        {
            type.Reason = BlitzFileType.DetectReason.LargeFile;
            type.IsTextType = false;
            return true;
        }
        int controlCharsCount = 0;

        using var file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var streamreader = new StreamReader(file);
        int charsRead = 0;
        while (streamreader.Peek() != -1 && charsRead++ < 2000 )
        {
            try
            {
                var charTest = (char)streamreader.Read();
                if (charTest == '\0' || !char.IsWhiteSpace(charTest) && char.IsControl(charTest) && charTest != '\r' && charTest != '\n')
                {
                    controlCharsCount++;
                    if (controlCharsCount > 10)
                    {
                        type.FilesThatHaveManyControlChars[fileName] = 1;
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                return !type.IsTextType;
            }
            
        }
        type.Reason = BlitzFileType.DetectReason.AutoMaticallyLooksLikeAtextFile;
        type.IsTextType = true;
        return !type.IsTextType;
    }
}

public class BlitzFileType
{
    public enum DetectReason
    {
        NotDetected,
        LargeFile,
        BuiltInDetermination,
        ManyFilesWithControlChars,
        HasSyntaxHighlighting,
        AutoMaticallyLooksLikeAtextFile,
        HasNoExtension,
        KnownBackupFile
    }

    public DetectReason Reason { get; set; }
    
    public bool IsTextType { get; set; }

    public ConcurrentDictionary<string, Byte> FilesThatHaveManyControlChars { get; set; } = [];
    public ConcurrentDictionary<string, Byte> FilesThatAreVeryLarge { get; set; } = [];
}