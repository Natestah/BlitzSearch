namespace Blitz.Interfacing;
using MessagePack;

[MessagePackObject]
public class RobotFileDetails
{
    [Key(nameof(FileName))]
    public string? FileName { get; set; }
    
    [Key(nameof(FileSize))]
    public long FileSize { get; set; }

    [Key(nameof(RobotState))]
    public RobotFileState RobotState { get; set; }
    
}