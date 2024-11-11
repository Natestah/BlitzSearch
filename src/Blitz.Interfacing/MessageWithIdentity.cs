using System.Reflection.Metadata.Ecma335;

namespace Blitz.Interfacing;
using MessagePack;

[MessagePack.Union(0, typeof(FileNameResult))]
[MessagePack.Union(1, typeof(SearchQuery))]
[MessagePack.Union(2, typeof(SearchTaskResult))]
[MessagePack.Union(3, typeof(RobotFileDetectionSummary))]
[MessagePackObject]
public class MessageWithIdentity
{
    /// <summary>
    /// In the context of users typing and getting real time results,
    /// each character typed should have a new MessageIdentity. To avoid incorrect results coming into view.
    /// </summary>
    [Key(nameof(MessageIdentity))]
    public int MessageIdentity { get; set; }

    
    /// <summary>
    /// Identify the message by Process ID.
    /// </summary>
    [Key(nameof(ProcessIdentity))]
    public int ProcessIdentity{get; set; }

 
    /// <summary>
    /// When Clients desire multiple requests at the same time. 
    /// </summary>
    [Key(nameof(InstanceIdentity))]
    public int InstanceIdentity{get; set; }
    
    
    public bool QueryMatches(MessageWithIdentity message)
    {
        return MessageIdentity == message.MessageIdentity
               && ProcessIdentity == message.ProcessIdentity
               && InstanceIdentity == message.InstanceIdentity;
    }

    public void AlignIdentity(MessageWithIdentity other)
    {
        MessageIdentity = other.MessageIdentity;
        ProcessIdentity = other.ProcessIdentity;
        InstanceIdentity = other.InstanceIdentity;
    }
}