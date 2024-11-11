namespace Blitz.Interfacing;
using MessagePack;

[MessagePackObject]
public class FileSearchStatus
{
    /// <summary>
    /// How many files the enumeration has discovered
    /// </summary>
    [Key(nameof(DiscoveredCount))]
    public int DiscoveredCount { get; set; }
    
    /// <summary>
    /// Working used to show progress of Full Search
    /// </summary>
    [Key(nameof(Working))]
    public bool Working { get; set; }

    
    /// <summary>
    /// Discovering is always going to be IsIndeterminate, will be a separate bar
    /// </summary>
    [Key(nameof(Discovering))]
    public bool Discovering { get; set; }
    
    /// <summary>
    /// When finished, Search Tasks can change IsIndeterminate, show an actual progress bar.
    /// </summary>
    [Key(nameof(FileDiscoveryFinished))]
    public bool FileDiscoveryFinished { get; set; }

    [Key(nameof(FilesProcessed))]
    public int FilesProcessed { get; set; }
    
    [Key(nameof(StatusUpdated))]
    public bool StatusUpdated { get; set; }
    
    [Key(nameof(RunningTime))]
    public TimeSpan RunningTime { get; set; }
    
    [Key(nameof(LastResultTime))]
    public TimeSpan LastResultTime { get; set; }
    
    [Key(nameof(TotalFileSIze))]
    public long TotalFileSIze { get; set; }
}