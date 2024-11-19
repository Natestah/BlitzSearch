using Blitz.Interfacing.QueryProcessing;

namespace Blitz.Interfacing;
using MessagePack;

[MessagePackObject]
public class FileContentResult
{

    [Key(nameof(CapturedContents))] 
    public string CapturedContents { get; set; } =string.Empty;


    [Key(nameof(ReplacedContents))] public string? ReplacedContents { get; set; } = null;

    [Key(nameof(Replacing))]
    public bool Replacing { get; set; }

    [Key(nameof(BlitzMatches))] 
    public List<BlitzMatch> BlitzMatches { get; set; } = [];

    [Key(nameof(LineNumber))] 
    public int LineNumber { get; set; }
}