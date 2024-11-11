using Blitz.Interfacing.QueryProcessing;

namespace Blitz.Interfacing;
using MessagePack;

[MessagePackObject]
public class FileNameResult : MessageWithIdentity
{
    [Key(nameof(FileName))]
    public string? FileName { get; set; }
    
    [Key(nameof(BlitzMatches))]
    public List<BlitzMatch>? BlitzMatches { get; set; }

    [Key(nameof(ContentResults))] 
    public List<FileContentResult> ContentResults { get; set; } = [];
    
    [Key( nameof(DebugInformation)) ]
    public string? DebugInformation { get; set; }
}