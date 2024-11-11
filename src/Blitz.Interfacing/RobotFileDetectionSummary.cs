namespace Blitz.Interfacing;
using MessagePack;

[MessagePackObject]
public class RobotFileDetectionSummary  : MessageWithIdentity
{
    [Key(nameof(RobotFileDetectionSummary))]
    public List<RobotFileDetails> RobotFileDetailsList { get; set; } = [];

    [Key(nameof(ActionMessage))]
    public string ActionMessage { get; set; } = "";
}